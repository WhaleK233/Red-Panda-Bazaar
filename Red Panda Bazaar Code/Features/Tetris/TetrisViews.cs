using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using WhaleK.Common.UI;
using WhaleK.Common.UI.Controls;

namespace Red_Panda_Bazaar_Code.Features.Tetris;

/// <summary>棋盘渲染:底板、已固定格、落点投影、当前方块与消行白闪。</summary>
public class TetrisBoardView : UiElement
{
    private const int Border = 3;
    private static readonly Color BackdropColor = new(24, 26, 38);

    private readonly TetrisBoard _board;

    private int CellSize { get; }

    public TetrisBoardView(TetrisBoard board, int cellSize)
    {
        _board = board;
        CellSize = cellSize;
    }

    protected internal override void MeasureCore(UiSize available)
    {
        SetDesired(CellSize * TetrisBoard.Width + Border * 2,
                   CellSize * TetrisBoard.VisibleHeight + Border * 2);
    }

    protected internal override void ArrangeCore(Rectangle contentRect) { }

    protected internal override void DrawCore(UiRenderContext ctx)
    {
        if (!Visible) return;
        var b = ctx.Batch;

        b.Draw(Game1.staminaRect, Bounds, BackdropColor);

        var x0 = Bounds.X + Border;
        var y0 = Bounds.Y + Border;

        // 已固定格(消行中的行画白闪)
        for (var y = TetrisBoard.BufferRows; y < TetrisBoard.Height; y++)
        for (var x = 0; x < TetrisBoard.Width; x++)
        {
            var cell = _board.GetCell(x, y);
            if (cell == 0) continue;
            var color = Tetromino.Colors[cell - 1];
            if (_board.IsClearingRow(y))
                color = Color.White * (0.5f + 0.5f * (_board.ClearingTimer / 80 % 2));
            DrawCell(b, x0 + x * CellSize, y0 + (y - TetrisBoard.BufferRows) * CellSize, color);
        }

        // 落点投影(与当前方块不重叠时才画)
        var ghostY = _board.ComputeGhostY();
        if (ghostY != _board.Y)
            DrawPiece(b, x0, y0, ghostY, color => color * 0.25f);

        // 当前方块
        DrawPiece(b, x0, y0, _board.Y, color => color);
    }

    /// <summary>按指定数组行 y 画整个活动方块(行号含缓冲区,缓冲区内不画)。</summary>
    private void DrawPiece(SpriteBatch b, int x0, int y0, int pieceY, Func<Color, Color> tint)
    {
        var shape = _board.Shape;
        var n = shape.GetLength(0);
        if (n == 0 || _board.Kind < 0) return;

        var color = Tetromino.Colors[_board.Kind];
        for (var r = 0; r < n; r++)
        for (var c = 0; c < n; c++)
        {
            if (!shape[r, c]) continue;
            var by = pieceY + r;
            if (by < TetrisBoard.BufferRows) continue;
            DrawCell(b, x0 + (_board.X + c) * CellSize, y0 + (by - TetrisBoard.BufferRows) * CellSize,
                tint(color));
        }
    }

    /// <summary>画一个格子:主体色 + 上/左亮边 + 下/右暗边(伪 3D 像素风)。</summary>
    private void DrawCell(SpriteBatch b, int px, int py, Color color)
    {
        b.Draw(Game1.staminaRect, new Rectangle(px, py, CellSize, CellSize), color);
        var bevel = Math.Max(2, CellSize / 8);
        b.Draw(Game1.staminaRect, new Rectangle(px, py, CellSize, bevel), Color.White * 0.35f);
        b.Draw(Game1.staminaRect, new Rectangle(px, py, bevel, CellSize), Color.White * 0.35f);
        b.Draw(Game1.staminaRect, new Rectangle(px, py + CellSize - bevel, CellSize, bevel), Color.Black * 0.35f);
        b.Draw(Game1.staminaRect, new Rectangle(px + CellSize - bevel, py, bevel, CellSize), Color.Black * 0.35f);
    }
}

/// <summary>下一块预览:在 4×4 小框内居中绘制 board.NextKind 的初始形状。</summary>
public class TetrisNextView : UiElement
{
    private const int CellSize = 16;
    private const int BoxCells = 4;

    private readonly TetrisBoard _board;

    public TetrisNextView(TetrisBoard board)
    {
        _board = board;
    }

    protected internal override void MeasureCore(UiSize available) => SetDesired(CellSize * BoxCells, CellSize * BoxCells);

    protected internal override void ArrangeCore(Rectangle contentRect) { }

    protected internal override void DrawCore(UiRenderContext ctx)
    {
        if (!Visible) return;
        var b = ctx.Batch;

        var shape = Tetromino.PeekShape(_board.NextKind);
        var n = shape.GetLength(0);
        var color = Tetromino.Colors[_board.NextKind];

        // 方块实际占用的行列范围,在框内居中
        var minRow = n; var maxRow = -1; var minCol = n; var maxCol = -1;
        for (var r = 0; r < n; r++)
        for (var c = 0; c < n; c++)
        {
            if (!shape[r, c]) continue;
            minRow = Math.Min(minRow, r);
            maxRow = Math.Max(maxRow, r);
            minCol = Math.Min(minCol, c);
            maxCol = Math.Max(maxCol, c);
        }

        var offsetX = (Bounds.Width - (maxCol - minCol + 1) * CellSize) / 2 - minCol * CellSize;
        var offsetY = (Bounds.Height - (maxRow - minRow + 1) * CellSize) / 2 - minRow * CellSize;

        for (var r = 0; r < n; r++)
        for (var c = 0; c < n; c++)
        {
            if (!shape[r, c]) continue;
            b.Draw(Game1.staminaRect,
                new Rectangle(Bounds.X + offsetX + c * CellSize, Bounds.Y + offsetY + r * CellSize, CellSize, CellSize),
                color);
        }
    }
}
