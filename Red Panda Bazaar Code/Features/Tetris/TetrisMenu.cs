using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using WhaleK.Common;
using WhaleK.Common.UI;
using WhaleK.Common.UI.Controls;
using WhaleK.Common.UI.Layout;
using Red_Panda_Bazaar_Code.Constant;

namespace Red_Panda_Bazaar_Code.Features.Tetris;

/// <summary>
/// 俄罗斯方块主菜单:进行/暂停/结束三种状态与输入分发,规则引擎在 <see cref="TetrisBoard"/>。
/// 游戏状态全部保存在菜单字段(组件树会被 Rebuild 重建,只做纯视图)。
/// </summary>
public class TetrisMenu : UiBaseMenu
{
    private enum State
    {
        Playing,
        Paused,
        GameOver,
    }

    private State _state = State.Playing;
    private readonly TetrisBoard _board = new();

    /// <summary>上一帧按住的键:菜单不收按键释放事件,按下沿检测靠自行对比。</summary>
    private readonly HashSet<Keys> _prevDown = new();

    private UiText? _scoreText;
    private UiText? _linesText;
    private UiText? _levelText;

    public TetrisMenu()
    {
        // 基类构造已按初始状态完成一次 BuildUi,这里只需挂事件
        _board.OnEvent += OnBoardEvent;
    }

    protected override void BuildUi()
    {
        switch (_state)
        {
            case State.Playing:
                BuildPlayUi();
                break;
            case State.Paused:
                BuildPauseUi();
                break;
            case State.GameOver:
                BuildGameOverUi();
                break;
        }
    }

    // ---- 每帧与输入 ----

    protected override void OnTick(GameTime time)
    {
        if (_state != State.Playing) return;

        _board.Update(time.ElapsedGameTime.Milliseconds);
        HandlePressEdges();
        SyncSideLabels();
    }

    /// <summary>上键(含手柄十字键映射)旋转、空格硬降——触发类操作必须单次触发,不能吃连发。</summary>
    private void HandlePressEdges()
    {
        // Game1 只公开上帧键盘状态(oldKBState),当前帧用 MonoGame 直接读取
        var down = new HashSet<Keys>(Keyboard.GetState().GetPressedKeys());

        var upKey = Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.moveUpButton);
        if (upKey != Keys.None && down.Contains(upKey) && !_prevDown.Contains(upKey))
            _board.Rotate();
        if (down.Contains(Keys.Space) && !_prevDown.Contains(Keys.Space))
            _board.HardDrop();

        _prevDown.Clear();
        foreach (var k in down)
            _prevDown.Add(k);
    }

    protected override bool OnKeyPressed(Keys key)
    {
        if (Game1.options.doesInputListContain(Game1.options.menuButton, key))
        {
            switch (_state)
            {
                case State.Playing:
                    Pause();
                    break;
                case State.Paused:
                    Resume();
                    break;
                case State.GameOver:
                    exitThisMenu();
                    break;
            }
            return true;
        }

        if (_state != State.Playing) return false;

        // 方向类操作吃原版方向键轮询的连发节奏(250ms 首延迟 + 70ms 连发),且尊重玩家键位绑定
        if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
            _board.TryMove(-1, 0);
        else if (Game1.options.doesInputListContain(Game1.options.moveRightButton, key))
            _board.TryMove(1, 0);
        else if (Game1.options.doesInputListContain(Game1.options.moveDownButton, key))
            _board.SoftDrop();
        return true; // 对局中吞掉其余按键,避免透传到基类做焦点移动
    }

    protected override bool OnGamePadButton(Buttons b)
    {
        if (_state != State.Playing) return false;
        if (b == Buttons.A)
            _board.Rotate();
        return true; // 对局中拦截全部手柄按钮:防止 B 误关菜单;方向输入经原版映射转成按键事件
    }

    protected override bool OnMovementKey(int direction)
    {
        if (_state != State.Playing) return false;
        if (direction == 0)
            _board.Rotate();
        return true;
    }

    // ---- 状态切换与棋盘事件 ----

    private void OnBoardEvent(TetrisBoard.EventType type, int value)
    {
        switch (type)
        {
            case TetrisBoard.EventType.LinesCleared:
                Game1.playSound(value >= 4 ? "newArtifact" : value == 3 ? "shiny4" : "smallSelect");
                break;
            case TetrisBoard.EventType.LevelUp:
                Game1.playSound("reward");
                break;
            case TetrisBoard.EventType.GameOver:
                Game1.playSound("fishEscape");
                _state = State.GameOver;
                Rebuild();
                break;
        }
    }

    private void Pause()
    {
        _state = State.Paused;
        Rebuild();
    }

    private void Resume()
    {
        _state = State.Playing;
        Rebuild();
    }

    private void Restart()
    {
        _board.Reset();
        _state = State.Playing;
        Rebuild();
    }

    // ---- 界面 ----

    private void BuildPlayUi()
    {
        // 格子按窗口高度自适应,避免小窗口下棋盘被菜单边框裁剪
        var cell = Math.Clamp((Game1.uiViewport.Height - 280) / TetrisBoard.VisibleHeight, 14, 32);

        var side = new UiColumn { Spacing = 4 };
        side.Add(new UiText(Tools.GetI18n(I18nKeys.Tetris_Next).ToString()));
        side.Add(new TetrisNextView(_board));
        _scoreText = AddValueRow(side, Tools.GetI18n(I18nKeys.Tetris_Score).ToString());
        _linesText = AddValueRow(side, Tools.GetI18n(I18nKeys.Tetris_Lines).ToString());
        _levelText = AddValueRow(side, Tools.GetI18n(I18nKeys.Tetris_Level).ToString());
        side.Add(new UiButton(Tools.GetI18n(I18nKeys.Tetris_Pause).ToString(), Pause));
        side.Add(new UiButton(Tools.GetI18n(I18nKeys.Tetris_Exit).ToString(), () => exitThisMenu()));
        side.Add(new UiText(Tools.GetI18n(I18nKeys.Tetris_Help1).ToString(), Game1.tinyFont));
        side.Add(new UiText(Tools.GetI18n(I18nKeys.Tetris_Help2).ToString(), Game1.tinyFont));

        Root.Add(new UiRow { Spacing = 16 }
            .Add(new TetrisBoardView(_board, cell))
            .Add(side));
    }

    private void BuildPauseUi()
    {
        Root.ContentHAlign = UiAlign.Center;
        Root.Add(new UiText(Tools.GetI18n(I18nKeys.Tetris_Paused).ToString(), Game1.dialogueFont));
        Root.Add(new UiButton(Tools.GetI18n(I18nKeys.Tetris_Resume).ToString(), Resume));
        Root.Add(new UiButton(Tools.GetI18n(I18nKeys.Tetris_Restart).ToString(), Restart));
        Root.Add(new UiButton(Tools.GetI18n(I18nKeys.Tetris_Exit).ToString(), () => exitThisMenu()));
    }

    private void BuildGameOverUi()
    {
        Root.ContentHAlign = UiAlign.Center;
        Root.Add(new UiText(Tools.GetI18n(I18nKeys.Tetris_GameOver).ToString(), Game1.dialogueFont));
        Root.Add(new UiText(
            $"{Tools.GetI18n(I18nKeys.Tetris_Score)}: {_board.Score}    " +
            $"{Tools.GetI18n(I18nKeys.Tetris_Lines)}: {_board.Lines}    " +
            $"{Tools.GetI18n(I18nKeys.Tetris_Level)}: {_board.Level}"));
        Root.Add(new UiButton(Tools.GetI18n(I18nKeys.Tetris_Restart).ToString(), Restart));
        Root.Add(new UiButton(Tools.GetI18n(I18nKeys.Tetris_Exit).ToString(), () => exitThisMenu()));
    }

    /// <summary>侧栏加一组"标签 + 数值"行,数值文本由每帧 <see cref="SyncSideLabels"/> 刷新。</summary>
    private static UiText AddValueRow(UiColumn side, string label)
    {
        side.Add(new UiText(label));
        var value = new UiText("0") { MinWidth = 96 };
        side.Add(value);
        return value;
    }

    private void SyncSideLabels()
    {
        if (_scoreText == null || _linesText == null || _levelText == null) return;
        _scoreText.Text = _board.Score.ToString();
        _linesText.Text = _board.Lines.ToString();
        _levelText.Text = _board.Level.ToString();
    }
}
