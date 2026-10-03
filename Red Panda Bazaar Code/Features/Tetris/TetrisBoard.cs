using WhaleK.Common;

namespace Red_Panda_Bazaar_Code.Features.Tetris;

/// <summary>
/// 俄罗斯方块规则引擎:棋盘、7-bag 随机、碰撞、旋转踢墙、锁定延迟、消行与计分。
/// 纯逻辑类,不含渲染与输入;时间单位毫秒,由菜单每帧调用 <see cref="Update"/> 驱动。
/// </summary>
public class TetrisBoard
{
    public const int Width = 10;
    public const int VisibleHeight = 20;

    /// <summary>顶部隐藏缓冲行数,方块在此生成,不计入可见棋盘。</summary>
    public const int BufferRows = 2;
    public const int Height = VisibleHeight + BufferRows;

    private const int LockDelayMs = 350;
    private const int ClearingMs = 350;
    private const int MaxFallIntervalMs = 850;
    private const int MinFallIntervalMs = 100;
    private static readonly int[] LineScores = { 0, 100, 300, 500, 800 };

    /// <summary>棋盘事件,用于音效与结算展示。</summary>
    public enum EventType
    {
        /// <summary>开始消行,参数为行数(1-4)。</summary>
        LinesCleared,
        /// <summary>等级提升,参数为新等级。</summary>
        LevelUp,
        /// <summary>游戏结束。</summary>
        GameOver,
    }

    public event Action<EventType, int>? OnEvent;

    private readonly int[] _grid = new int[Width * Height];
    private readonly List<int> _bag = new();
    private readonly List<int> _clearingRows = new();
    private bool[,] _shape = new bool[0, 0];
    private int _nextKind;
    private int _gravityTimer;
    private int _groundedTimer;
    private int _clearingTimer;

    public TetrisBoard()
    {
        Reset();
    }

    /// <summary>重置到全新对局。</summary>
    public void Reset()
    {
        Array.Clear(_grid);
        _bag.Clear();
        _clearingRows.Clear();
        _shape = new bool[0, 0];
        Score = 0;
        Lines = 0;
        Level = 1;
        GameOver = false;
        Clearing = false;
        _gravityTimer = 0;
        _groundedTimer = 0;
        _clearingTimer = 0;
        _nextKind = TakeFromBag();
        Spawn();
    }

    public int Score { get; private set; }
    public int Lines { get; private set; }
    public int Level { get; private set; } = 1;
    public bool GameOver { get; private set; }

    /// <summary>是否处于消行白闪阶段(期间无活动方块、输入无效)。</summary>
    public bool Clearing { get; private set; }

    /// <summary>消行白闪剩余毫秒,供渲染计算闪烁相位。</summary>
    public int ClearingTimer => _clearingTimer;

    /// <summary>当前方块种类(0-6),无活动方块时为 -1。</summary>
    public int Kind { get; private set; } = -1;
    public int X { get; private set; }
    public int Y { get; private set; }

    /// <summary>当前方块形状矩阵,无活动方块时为空阵。</summary>
    public bool[,] Shape => _shape;

    /// <summary>下一个方块种类(0-6)。</summary>
    public int NextKind => _nextKind;

    /// <summary>读取棋盘格:0 为空,否则为方块种类 + 1。y 为含缓冲区的数组行。</summary>
    public int GetCell(int x, int y) =>
        x < 0 || x >= Width || y < 0 || y >= Height ? 0 : _grid[y * Width + x];

    /// <summary>该可见行是否正处于消行白闪状态。</summary>
    public bool IsClearingRow(int y) => Clearing && _clearingRows.Contains(y);

    /// <summary>每帧推进:消行计时、重力下落与锁定延迟。</summary>
    public void Update(int dtMs)
    {
        if (GameOver) return;

        if (Clearing)
        {
            _clearingTimer -= dtMs;
            if (_clearingTimer <= 0)
                FinishClearing();
            return;
        }

        if (_shape.GetLength(0) == 0) return;

        if (CanPlace(_shape, X, Y + 1))
        {
            _groundedTimer = 0;
            _gravityTimer += dtMs;
            if (_gravityTimer >= FallInterval)
            {
                _gravityTimer = 0;
                Y++;
            }
        }
        else
        {
            _gravityTimer = 0;
            _groundedTimer += dtMs;
            if (_groundedTimer >= LockDelayMs)
                LockPiece();
        }
    }

    /// <summary>尝试平移方块,成功返回 true。</summary>
    public bool TryMove(int dx, int dy)
    {
        if (!HasActivePiece()) return false;
        if (!CanPlace(_shape, X + dx, Y + dy)) return false;
        X += dx;
        Y += dy;
        // 着地状态下的成功操作重置锁定延迟
        if (!CanPlace(_shape, X, Y + 1))
            _groundedTimer = 0;
        return true;
    }

    /// <summary>软降:下移一格并计 1 分。</summary>
    public bool SoftDrop()
    {
        if (!TryMove(0, 1)) return false;
        Score++;
        return true;
    }

    /// <summary>顺时针旋转,带简单踢墙(横向 ±1/±2 与上移),成功返回 true。</summary>
    public bool Rotate()
    {
        if (!HasActivePiece()) return false;

        var rotated = Tetromino.RotateCW(_shape);
        foreach (var (ox, oy) in new[] { (0, 0), (-1, 0), (1, 0), (0, -1), (-2, 0), (2, 0) })
        {
            if (!CanPlace(rotated, X + ox, Y + oy)) continue;
            _shape = rotated;
            X += ox;
            Y += oy;
            if (!CanPlace(_shape, X, Y + 1))
                _groundedTimer = 0;
            return true;
        }
        return false;
    }

    /// <summary>硬降:直接落到最低可达位置,每格计 2 分并立即锁定。</summary>
    public void HardDrop()
    {
        if (!HasActivePiece()) return;

        var distance = 0;
        while (CanPlace(_shape, X, Y + 1))
        {
            Y++;
            distance++;
        }
        Score += distance * 2;
        LockPiece();
    }

    /// <summary>当前方块的落点投影 y(ghost piece)。</summary>
    public int ComputeGhostY()
    {
        var y = Y;
        if (!HasActivePiece()) return y;
        while (CanPlace(_shape, X, y + 1))
            y++;
        return y;
    }

    private bool HasActivePiece() => !GameOver && !Clearing && _shape.GetLength(0) > 0;

    private int FallInterval => Math.Max(MinFallIntervalMs, MaxFallIntervalMs - (Level - 1) * 65);

    /// <summary>形状能否放在棋盘坐标 (px, py):横向越界/触底/与已有格冲突均不可;越顶允许(旋转踢墙可能用)。</summary>
    private bool CanPlace(bool[,] shape, int px, int py)
    {
        var n = shape.GetLength(0);
        for (var r = 0; r < n; r++)
        for (var c = 0; c < n; c++)
        {
            if (!shape[r, c]) continue;
            var bx = px + c;
            var by = py + r;
            if (bx < 0 || bx >= Width || by >= Height) return false;
            if (by >= 0 && _grid[by * Width + bx] != 0) return false;
        }
        return true;
    }

    private void Spawn()
    {
        Kind = _nextKind;
        _nextKind = TakeFromBag();
        _shape = Tetromino.GetShape(Kind);
        X = (Width - _shape.GetLength(0)) / 2;
        Y = 0;
        _gravityTimer = 0;
        _groundedTimer = 0;
        if (!CanPlace(_shape, X, Y))
        {
            GameOver = true;
            OnEvent?.Invoke(EventType.GameOver, 0);
        }
    }

    /// <summary>把当前方块写入棋盘,随后判定失败 / 进入消行 / 直接生成下一块。</summary>
    private void LockPiece()
    {
        var n = _shape.GetLength(0);
        var topOut = false;
        for (var r = 0; r < n; r++)
        for (var c = 0; c < n; c++)
        {
            if (!_shape[r, c]) continue;
            var by = Y + r;
            _grid[by * Width + X + c] = Kind + 1;
            if (by < BufferRows)
                topOut = true;
        }
        _shape = new bool[0, 0];
        Kind = -1;

        if (topOut)
        {
            GameOver = true;
            OnEvent?.Invoke(EventType.GameOver, 0);
            return;
        }

        _clearingRows.Clear();
        for (var y = 0; y < Height; y++)
        {
            var full = true;
            for (var x = 0; x < Width; x++)
            {
                if (_grid[y * Width + x] != 0) continue;
                full = false;
                break;
            }
            if (full)
                _clearingRows.Add(y);
        }

        if (_clearingRows.Count > 0)
        {
            Clearing = true;
            _clearingTimer = ClearingMs;
            OnEvent?.Invoke(EventType.LinesCleared, _clearingRows.Count);
        }
        else
        {
            Spawn();
        }
    }

    /// <summary>消行白闪结束:移除满行、结算得分与等级,并生成下一块。</summary>
    private void FinishClearing()
    {
        Clearing = false;
        var cleared = _clearingRows.Count;

        var write = Height - 1;
        for (var read = Height - 1; read >= 0; read--)
        {
            if (_clearingRows.Contains(read)) continue;
            if (write != read)
                Array.Copy(_grid, read * Width, _grid, write * Width, Width);
            write--;
        }
        for (; write >= 0; write--)
            Array.Clear(_grid, write * Width, Width);
        _clearingRows.Clear();

        Lines += cleared;
        Score += LineScores[Math.Min(cleared, LineScores.Length - 1)] * Level;
        var newLevel = Lines / 10 + 1;
        if (newLevel > Level)
        {
            Level = newLevel;
            OnEvent?.Invoke(EventType.LevelUp, Level);
        }

        Spawn();
    }

    /// <summary>7-bag 取下一块:装满 7 种洗牌后逐个取出,保证分布均匀。</summary>
    private int TakeFromBag()
    {
        if (_bag.Count == 0)
        {
            for (var i = 0; i < Tetromino.KindCount; i++)
                _bag.Add(i);
            for (var i = _bag.Count - 1; i > 0; i--)
            {
                var j = RandomUtils.Next(i + 1);
                (_bag[i], _bag[j]) = (_bag[j], _bag[i]);
            }
        }
        var kind = _bag[^1];
        _bag.RemoveAt(_bag.Count - 1);
        return kind;
    }
}
