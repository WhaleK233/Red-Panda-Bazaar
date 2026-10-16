using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewUI;
using StardewUI.Graphics;
using StardewUI.Layout;
using StardewUI.Widgets;
using StardewValley;

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
        // 棋盘按可用高度缩放；右侧信息卡保持固定上限，避免小分辨率下内容互相挤压。
        var cell = Math.Clamp((Game1.uiViewport.Height - 280) / TetrisBoard.VisibleHeight, 14, 32);

        var board = new Frame
        {
            Background = UiSprites.MenuSlotInset,
            Padding = new(12),
            Layout = LayoutParameters.FitContent(),
            Content = new TetrisBoardView(_board, cell),
        };

        var side = MenuUi.Column(maxWidth: 240, maxHeight: 780);
        side.Children.Add(MenuUi.Card(BuildNextCard(), topMargin: 0));
        side.Children.Add(MenuUi.Card(BuildStatsCard(), topMargin: MenuUi.SectionGap));
        side.Children.Add(MenuUi.Card(BuildControlsCard(), topMargin: MenuUi.SectionGap));

        var row = MenuUi.Row(align: Alignment.Middle);
        row.Children.Add(board);
        side.Margin = new(Left: MenuUi.SectionGap);
        row.Children.Add(side);
        _root!.Children.Add(row);
    }

    /// <summary>下一块预览单独成卡，避免棋盘与说明信息混在同一列。</summary>
    private Lane BuildNextCard()
    {
        var card = MenuUi.Column();
        card.Children.Add(
            MenuUi.TextRow(
                Tools.GetI18n(I18nKeys.Tetris_Next).ToString(),
                color: MenuUi.Accent,
                align: Alignment.Middle,
                bold: true
            )
        );
        var previewRow = MenuUi.Row(Alignment.Middle, topMargin: MenuUi.RowGap);
        previewRow.Children.Add(new TetrisNextView(_board));
        card.Children.Add(previewRow);
        return card;
    }

    /// <summary>实时统计卡：每个数值均有独立引用，更新时不重建棋盘和焦点树。</summary>
    private Lane BuildStatsCard()
    {
        var card = MenuUi.Column();
        _scoreText = MenuUi.Text("0", color: Color.Black, scale: 1.15f, bold: true);
        card.Children.Add(
            MenuUi.RowBetween(
                MenuUi.Text(Tools.GetI18n(I18nKeys.Tetris_Score).ToString(), color: MenuUi.Muted),
                _scoreText,
                topMargin: 0
            )
        );

        _linesText = MenuUi.Text("0", color: Color.Black, scale: 1.15f, bold: true);
        card.Children.Add(
            MenuUi.RowBetween(
                MenuUi.Text(Tools.GetI18n(I18nKeys.Tetris_Lines).ToString(), color: MenuUi.Muted),
                _linesText,
                topMargin: MenuUi.RowGap
            )
        );

        _levelText = MenuUi.Text("0", color: Color.Black, scale: 1.15f, bold: true);
        card.Children.Add(
            MenuUi.RowBetween(
                MenuUi.Text(Tools.GetI18n(I18nKeys.Tetris_Level).ToString(), color: MenuUi.Muted),
                _levelText,
                topMargin: MenuUi.RowGap
            )
        );
        return card;
    }

    /// <summary>操作卡：主按钮突出暂停，退出作为次要操作；帮助文字降低对视觉的干扰。</summary>
    private Lane BuildControlsCard()
    {
        var card = MenuUi.Column();
        card.Children.Add(
            MenuUi.PrimaryButton(
                Tools.GetI18n(I18nKeys.Tetris_Pause).ToString(),
                Pause,
                new(Bottom: MenuUi.RowGap)
            )
        );
        card.Children.Add(MenuUi.SecondaryButton(Tools.GetI18n(I18nKeys.Tetris_Exit).ToString(), Close));
        card.Children.Add(MenuUi.Divider(MenuUi.SectionGap));
        card.Children.Add(MenuUi.Text(Tools.GetI18n(I18nKeys.Tetris_Help1).ToString(), Game1.tinyFont, MenuUi.Muted));
        card.Children.Add(
            MenuUi.Text(
                Tools.GetI18n(I18nKeys.Tetris_Help2).ToString(),
                Game1.tinyFont,
                MenuUi.Muted,
                topMargin: MenuUi.RowGap
            )
        );
        return card;
    }

    private void BuildPauseUi()
    {
        _root!.Children.Add(MenuUi.Header(Tools.GetI18n(I18nKeys.Tetris_Paused).ToString()));
        AddCenteredButton(Tools.GetI18n(I18nKeys.Tetris_Resume).ToString(), Resume, primary: true);
        AddCenteredButton(Tools.GetI18n(I18nKeys.Tetris_Restart).ToString(), Restart);
        AddCenteredButton(Tools.GetI18n(I18nKeys.Tetris_Exit).ToString(), Close);
    }

    private void BuildGameOverUi()
    {
        _root!.Children.Add(MenuUi.Header(Tools.GetI18n(I18nKeys.Tetris_GameOver).ToString()));
        var summary = MenuUi.Column();
        summary.Children.Add(
            MenuUi.TextRow(
                $"{Tools.GetI18n(I18nKeys.Tetris_Score)}: {_board.Score}",
                Game1.dialogueFont,
                align: Alignment.Middle
            )
        );
        summary.Children.Add(
            MenuUi.TextRow(
                $"{Tools.GetI18n(I18nKeys.Tetris_Lines)}: {_board.Lines}　"
                    + $"{Tools.GetI18n(I18nKeys.Tetris_Level)}: {_board.Level}",
                color: MenuUi.Muted,
                align: Alignment.Middle,
                topMargin: MenuUi.RowGap
            )
        );
        _root.Children.Add(MenuUi.Card(summary, tint: new Color(236, 245, 236), topMargin: MenuUi.SectionGap));
        AddCenteredButton(Tools.GetI18n(I18nKeys.Tetris_Restart).ToString(), Restart, primary: true);
        AddCenteredButton(Tools.GetI18n(I18nKeys.Tetris_Exit).ToString(), Close);
    }

    /// <summary>单按钮占一行并在行内居中；主操作使用亮色按钮，次操作使用默认暗色按钮。</summary>
    private void AddCenteredButton(string text, Action onClick, bool primary = false)
    {
        var row = MenuUi.Row(Alignment.Middle, topMargin: MenuUi.RowGap);
        row.Children.Add(primary ? MenuUi.PrimaryButton(text, onClick) : MenuUi.SecondaryButton(text, onClick));
        _root!.Children.Add(row);
    }

    private void SyncSideLabels()
    {
        if (_scoreText == null || _linesText == null || _levelText == null)
            return;
        _scoreText.Text = _board.Score.ToString();
        _linesText.Text = _board.Lines.ToString();
        _levelText.Text = _board.Level.ToString();
    }
}
