using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewUI;
using StardewUI.Graphics;
using StardewUI.Layout;
using StardewUI.Widgets;
using StardewValley;
using WhaleK.Common;

namespace Red_Panda_Bazaar_Code.Features.Tetris;

/// <summary>
/// 俄罗斯方块主菜单：进行/暂停/结束三种状态与输入分发，规则引擎在 <see cref="TetrisBoard"/>。
/// 游戏状态全部保存在菜单字段（视图树会按状态重建，只做纯视图）。
/// </summary>
public class TetrisMenu : ViewMenu
{
    private enum State
    {
        Playing,
        Paused,
        GameOver,
    }

    private State _state = State.Playing;
    private readonly TetrisBoard _board = new();

    /// <summary>上一帧按住的键：菜单不收按键释放事件，按下沿检测靠自行对比。</summary>
    private readonly HashSet<Keys> _prevDown = new();

    private Lane? _root;
    private Label? _scoreText;
    private Label? _linesText;
    private Label? _levelText;

    protected override IView CreateView()
    {
        CloseButtonSprite = UiSprites.CloseButton;

        _board.OnEvent += OnBoardEvent;
        _root = MenuUi.Column(maxHeight: 780);
        BuildUi();
        return MenuUi.Window(_root);
    }

    /// <summary>按当前状态重建视图树（等价于原框架的 <c>Rebuild</c>）。</summary>
    private void BuildUi()
    {
        if (_root is null)
        {
            return;
        }
        _root.Children.Clear();
        _scoreText = null;
        _linesText = null;
        _levelText = null;

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

    public override void update(GameTime time)
    {
        base.update(time);

        if (_state != State.Playing) return;

        _board.Update(time.ElapsedGameTime.Milliseconds);
        HandlePressEdges();
        SyncSideLabels();
    }

    /// <summary>上键（含手柄十字键映射）旋转、空格硬降——触发类操作必须单次触发，不能吃连发。</summary>
    private void HandlePressEdges()
    {
        // Game1 只公开上帧键盘状态（oldKBState），当前帧用 MonoGame 直接读取
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

    public override void receiveKeyPress(Keys key)
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
                    Close();
                    break;
            }
            return;
        }

        if (_state != State.Playing)
        {
            base.receiveKeyPress(key);
            return;
        }

        // 方向类操作吃原版方向键轮询的连发节奏（250ms 首延迟 + 70ms 连发），且尊重玩家键位绑定
        if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
            _board.TryMove(-1, 0);
        else if (Game1.options.doesInputListContain(Game1.options.moveRightButton, key))
            _board.TryMove(1, 0);
        else if (Game1.options.doesInputListContain(Game1.options.moveDownButton, key))
            _board.SoftDrop();
        // 对局中吞掉其余按键，避免透传到基类做焦点移动
    }

    public override void receiveGamePadButton(Buttons b)
    {
        if (_state != State.Playing)
        {
            base.receiveGamePadButton(b);
            return;
        }
        if (b == Buttons.A)
            _board.Rotate();
        // 对局中拦截全部手柄按钮：防止 B 误关菜单；方向输入经原版映射转成按键事件
    }

    public override void applyMovementKey(int directionValue)
    {
        if (_state != State.Playing)
        {
            base.applyMovementKey(directionValue);
            return;
        }
        if (directionValue == 0)
            _board.Rotate();
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
                BuildUi();
                break;
        }
    }

    private void Pause()
    {
        _state = State.Paused;
        BuildUi();
    }

    private void Resume()
    {
        _state = State.Playing;
        BuildUi();
    }

    private void Restart()
    {
        _board.Reset();
        _state = State.Playing;
        BuildUi();
    }

    // ---- 界面 ----

    private void BuildPlayUi()
    {
        // 格子按窗口高度自适应，避免小窗口下棋盘被菜单边框裁剪
        var cell = Math.Clamp((Game1.uiViewport.Height - 280) / TetrisBoard.VisibleHeight, 14, 32);

        var side = new Lane { Orientation = Orientation.Vertical, Layout = LayoutParameters.FitContent() };
        AddSide(side, MenuUi.Text(Tools.GetI18n(I18nKeys.Tetris_Next).ToString()));
        AddSide(side, new TetrisNextView(_board));
        _scoreText = AddValueRow(side, Tools.GetI18n(I18nKeys.Tetris_Score).ToString());
        _linesText = AddValueRow(side, Tools.GetI18n(I18nKeys.Tetris_Lines).ToString());
        _levelText = AddValueRow(side, Tools.GetI18n(I18nKeys.Tetris_Level).ToString());
        side.Children.Add(MenuUi.Button(Tools.GetI18n(I18nKeys.Tetris_Pause).ToString(), Pause, new(Top: 4)));
        side.Children.Add(MenuUi.Button(Tools.GetI18n(I18nKeys.Tetris_Exit).ToString(), Close, new(Top: 4)));
        AddSide(side, MenuUi.Text(Tools.GetI18n(I18nKeys.Tetris_Help1).ToString(), Game1.tinyFont));
        AddSide(side, MenuUi.Text(Tools.GetI18n(I18nKeys.Tetris_Help2).ToString(), Game1.tinyFont));

        var row = MenuUi.Row();
        row.Children.Add(new TetrisBoardView(_board, cell));
        side.Margin = new(Left: 16);
        row.Children.Add(side);
        _root!.Children.Add(row);
    }

    private void BuildPauseUi()
    {
        _root!.Children.Add(
            MenuUi.TextRow(
                Tools.GetI18n(I18nKeys.Tetris_Paused).ToString(),
                Game1.dialogueFont,
                align: Alignment.Middle
            )
        );
        AddCenteredButton(Tools.GetI18n(I18nKeys.Tetris_Resume).ToString(), Resume);
        AddCenteredButton(Tools.GetI18n(I18nKeys.Tetris_Restart).ToString(), Restart);
        AddCenteredButton(Tools.GetI18n(I18nKeys.Tetris_Exit).ToString(), Close);
    }

    private void BuildGameOverUi()
    {
        _root!.Children.Add(
            MenuUi.TextRow(
                Tools.GetI18n(I18nKeys.Tetris_GameOver).ToString(),
                Game1.dialogueFont,
                align: Alignment.Middle
            )
        );
        _root.Children.Add(
            MenuUi.TextRow(
                $"{Tools.GetI18n(I18nKeys.Tetris_Score)}: {_board.Score}    "
                    + $"{Tools.GetI18n(I18nKeys.Tetris_Lines)}: {_board.Lines}    "
                    + $"{Tools.GetI18n(I18nKeys.Tetris_Level)}: {_board.Level}",
                align: Alignment.Middle,
                topMargin: MenuUi.RowGap
            )
        );
        AddCenteredButton(Tools.GetI18n(I18nKeys.Tetris_Restart).ToString(), Restart);
        AddCenteredButton(Tools.GetI18n(I18nKeys.Tetris_Exit).ToString(), Close);
    }

    /// <summary>单按钮占一行并在行内居中（等价原框架 <c>Root.ContentHAlign = Center</c> 的效果）。</summary>
    private void AddCenteredButton(string text, Action onClick)
    {
        var row = MenuUi.Row(Alignment.Middle, topMargin: MenuUi.RowGap);
        row.Children.Add(MenuUi.Button(text, onClick));
        _root!.Children.Add(row);
    }

    /// <summary>侧栏加一项（首项不加间距，其余留 4px 上间距）。</summary>
    private static void AddSide(Lane side, View item)
    {
        if (side.Children.Count > 0)
            item.Margin = new(Top: 4);
        side.Children.Add(item);
    }

    /// <summary>侧栏加一组"标签 + 数值"行，数值文本由每帧 <see cref="SyncSideLabels"/> 刷新。</summary>
    private static Label AddValueRow(Lane side, string label)
    {
        AddSide(side, MenuUi.Text(label));
        var value = MenuUi.Text("0");
        value.Layout = new() { Width = Length.Px(96), Height = Length.Content() };
        AddSide(side, value);
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
