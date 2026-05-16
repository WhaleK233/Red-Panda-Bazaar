using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Features.Bank;

public class BankMenu : IClickableMenu
{
    private int _currentTab;
    private readonly string[] _tabLabels;
    private readonly List<ClickableComponent> _tabButtons = new();

    private readonly List<ClickableComponent> _actionButtons = new();
    private readonly List<ClickableComponent> _fixedActionButtons = new();

    private const int TabCount = 3;
    private const int TabHeight = 48;
    private const int TabTopOffset = 24; // 页签距菜单顶部间距
    private const int TabSideMargin = 24; // 页签左右距菜单边框间距
    private const int ContentPadding = 24;
    private const int TabGap = 4;

    /// <summary>根据各页签内容计算所需宽度。</summary>
    private static int CalcWidth()
    {
        var font = Game1.smallFont;
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();
        var maxAmount = 9999999;

        // 取各页签最大内容宽度作为菜单宽度基准
        var checkingW = font.MeasureString(
            Tools.GetI18n(I18nKeys.Bank_CheckingBalance).Tokens(new { amount = maxAmount, gold }).ToString()
        ).X;

        var fixedW = font.MeasureString(
            $"[1] {Tools.GetI18n(I18nKeys.Bank_FixedAmount).Tokens(new { amount = maxAmount, gold })} | 112 {Tools.GetI18n(I18nKeys.Bank_Days).ToString()} | {Tools.GetI18n(I18nKeys.Bank_FixedStatusActive).ToString()}"
        ).X + 200;

        var taxW = font.MeasureString(
            Tools.GetI18n(I18nKeys.Bank_TotalTax).Tokens(new { amount = maxAmount, gold }).ToString()
        ).X;

        var w = (int)Math.Max(checkingW, Math.Max(fixedW, taxW)) + ContentPadding * 3 + 40;
        return Math.Clamp(w, 600, Game1.uiViewport.Width - 40);
    }

    /// <summary>根据各页签内容计算所需高度。</summary>
    private static int CalcHeight()
    {
        var baseH = TabTopOffset + TabHeight + ContentPadding; // 标题上方 + 页签 + 内容上间距
        var bottomPad = ContentPadding + 40; // 底部间距 + 关闭按钮

        // 活期页签：余额 + 利息 + 利率 + 按钮区
        var checkingH = 180;

        // 定期页签：按钮 + 列表行（最多假设 5 笔）
        var fixedH = 56 + 40 * Math.Min(Bank.GetFixedDeposits().Count + 1, 5) + 60;

        // 税收页签：一行文本
        var taxH = 60;

        var h = baseH + Math.Max(checkingH, Math.Max(fixedH, taxH)) + bottomPad;
        return Math.Clamp(h, 200, Game1.uiViewport.Height - 40);
    }

    public BankMenu()
        : base(
            (Game1.uiViewport.Width - CalcWidth()) / 2,
            (Game1.uiViewport.Height - CalcHeight()) / 2,
            CalcWidth(),
            CalcHeight(),
            showUpperRightCloseButton: true)
    {
        _tabLabels = new[]
        {
            Tools.GetI18n(I18nKeys.Bank_CheckingTab).ToString(),
            Tools.GetI18n(I18nKeys.Bank_FixedTab).ToString(),
            Tools.GetI18n(I18nKeys.Bank_TaxTab).ToString()
        };

        var availableWidth = width - TabSideMargin * 2;
        var tabWidth = (availableWidth - TabGap * (TabCount - 1)) / TabCount;
        for (var i = 0; i < TabCount; i++)
        {
            _tabButtons.Add(new ClickableComponent(
                new Rectangle(xPositionOnScreen + TabSideMargin + (tabWidth + TabGap) * i, yPositionOnScreen + TabTopOffset, tabWidth, TabHeight),
                $"tab_{i}"));
        }

        RefreshActionButtons();
    }

    /// <summary>根据当前页签重新计算所有按钮的坐标和可见性。</summary>
    private void RefreshActionButtons()
    {
        _actionButtons.Clear();
        _fixedActionButtons.Clear();

        var contentX = xPositionOnScreen + ContentPadding;
        var contentY = yPositionOnScreen + TabTopOffset + TabHeight + ContentPadding;
        var contentW = width - ContentPadding * 2;

        switch (_currentTab)
        {
            case 0:
            {
                var claimBtn = new ClickableComponent(
                    new Rectangle(contentX + 300, contentY + 48, 100, 40), "claim");
                _actionButtons.Add(claimBtn);

                var depositBtn = new ClickableComponent(
                    new Rectangle(contentX, contentY + 120, 120, 40), "deposit");
                _actionButtons.Add(depositBtn);

                var withdrawBtn = new ClickableComponent(
                    new Rectangle(contentX + 140, contentY + 120, 120, 40), "withdraw");
                _actionButtons.Add(withdrawBtn);
                break;
            }
            case 1:
            {
                var newFixedBtn = new ClickableComponent(
                    new Rectangle(contentX, contentY, 160, 40), "newFixed");
                _actionButtons.Add(newFixedBtn);

                var deposits = Bank.GetFixedDeposits();
                var listY = contentY + 56;
                for (var i = 0; i < deposits.Count; i++)
                {
                    if (deposits[i].Withdrawn) continue;
                    var redeemBtn = new ClickableComponent(
                        new Rectangle(contentX + contentW - 200, listY, 90, 36), $"redeem_{i}");
                    _fixedActionButtons.Add(redeemBtn);

                    var earlyBtn = new ClickableComponent(
                        new Rectangle(contentX + contentW - 100, listY, 90, 36), $"early_{i}");
                    _fixedActionButtons.Add(earlyBtn);

                    listY += 40;
                }
                break;
            }
        }
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (Game1.activeClickableMenu != this) return;

        for (var i = 0; i < _tabButtons.Count; i++)
        {
            if (_tabButtons[i].bounds.Contains(x, y))
            {
                _currentTab = i;
                Game1.playSound("smallSelect");
                RefreshActionButtons();
                return;
            }
        }

        foreach (var btn in _actionButtons)
        {
            if (!btn.bounds.Contains(x, y)) continue;
            HandleActionButton(btn.name);
            return;
        }

        foreach (var btn in _fixedActionButtons)
        {
            if (!btn.bounds.Contains(x, y)) continue;
            HandleFixedAction(btn.name);
            return;
        }
    }

    /// <summary>处理页签主按钮事件：存/取/领/开。</summary>
    private void HandleActionButton(string name)
    {
        switch (name)
        {
            case "claim":
                Bank.ClaimInterest();
                Game1.playSound("coin");
                exitThisMenu();
                break;

            case "deposit":
                Game1.activeClickableMenu = new NumberSelectionMenu(
                    Tools.GetI18n(I18nKeys.Bank_DepositTitle).ToString(),
                    (number, price, who) =>
                    {
                        if (number > 0)
                        {
                            Bank.Deposit(number);
                            Game1.playSound("coin");
                            if (Context.IsMainPlayer)
                                Game1.activeClickableMenu = new BankMenu();
                            else
                                Game1.exitActiveMenu();
                        }
                        else
                        {
                            Game1.activeClickableMenu = new BankMenu();
                        }
                    },
                    price: -1,
                    minValue: 1,
                    maxValue: Math.Max(1, Game1.player.Money),
                    defaultNumber: Math.Min(100, Game1.player.Money));
                break;

            case "withdraw":
                Game1.activeClickableMenu = new NumberSelectionMenu(
                    Tools.GetI18n(I18nKeys.Bank_WithdrawTitle).ToString(),
                    (number, price, who) =>
                    {
                        if (number > 0)
                        {
                            Bank.Withdraw(number);
                            Game1.playSound("coin");
                            if (Context.IsMainPlayer)
                                Game1.activeClickableMenu = new BankMenu();
                            else
                                Game1.exitActiveMenu();
                        }
                        else
                        {
                            Game1.activeClickableMenu = new BankMenu();
                        }
                    },
                    price: -1,
                    minValue: 1,
                    maxValue: Math.Max(1, Bank.GetCheckingBalance()),
                    defaultNumber: Math.Min(100, Bank.GetCheckingBalance()));
                break;

            case "newFixed":
                ShowFixedTermSelection();
                break;
        }
    }

    /// <summary>用游戏内置对话选择期限：7/28/112 天。</summary>
    private void ShowFixedTermSelection()
    {
        var termOptions = BankCalculator.FixedTermOptions;
        var labels = termOptions.Select(t =>
            $"{t} {Tools.GetI18n(I18nKeys.Bank_Days).ToString()}").ToArray();

        Game1.currentLocation.createQuestionDialogue(
            Tools.GetI18n(I18nKeys.Bank_NewFixedTitle).ToString(),
            labels.Select((l, i) => new Response($"{termOptions[i]}", l)).ToArray(),
            (who, answerKey) =>
            {
                if (int.TryParse(answerKey, out var termDays) && termOptions.Contains(termDays))
                {
                    ShowFixedAmountInput(termDays);
                }
                else
                {
                    Game1.activeClickableMenu = new BankMenu();
                }
            });
    }

    private void ShowFixedAmountInput(int termDays)
    {
        Game1.activeClickableMenu = new NumberSelectionMenu(
            Tools.GetI18n(I18nKeys.Bank_NewFixedAmountTitle).ToString(),
            (number, price, who) =>
            {
                if (number > 0)
                {
                    Bank.CreateFixedDeposit(number, termDays);
                    Game1.playSound("coin");
                    if (Context.IsMainPlayer)
                        Game1.activeClickableMenu = new BankMenu();
                    else
                        Game1.exitActiveMenu();
                }
                else
                {
                    Game1.activeClickableMenu = new BankMenu();
                }
            },
            price: -1,
            minValue: 1,
            maxValue: Math.Max(1, Bank.GetCheckingBalance()),
            defaultNumber: Math.Min(100, Bank.GetCheckingBalance()));
    }

    private void HandleFixedAction(string name)
    {
        var parts = name.Split('_');
        if (parts.Length < 2 || !int.TryParse(parts[1], out var index)) return;

        switch (parts[0])
        {
            case "redeem":
                Bank.RedeemFixedDeposit(index);
                Game1.playSound("coin");
                exitThisMenu();
                break;
            case "early":
                Bank.EarlyWithdrawFixedDeposit(index);
                Game1.playSound("coin");
                exitThisMenu();
                break;
        }
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        width = CalcWidth();
        height = CalcHeight();
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;

        initializeUpperRightCloseButton();

        _tabButtons.Clear();
        var availableWidth = width - TabSideMargin * 2;
        var tabWidth = (availableWidth - TabGap * (TabCount - 1)) / TabCount;
        for (var i = 0; i < TabCount; i++)
        {
            _tabButtons.Add(new ClickableComponent(
                new Rectangle(xPositionOnScreen + TabSideMargin + (tabWidth + TabGap) * i, yPositionOnScreen + TabTopOffset, tabWidth, TabHeight),
                $"tab_{i}"));
        }

        RefreshActionButtons();
    }

    public override void receiveRightClick(int x, int y, bool playSound = true) { }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);

        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
            xPositionOnScreen, yPositionOnScreen, width, height, Color.White, 4f);

        var title = Tools.GetI18n(I18nKeys.Bank_Title).ToString();
        var titleSize = Game1.dialogueFont.MeasureString(title);
        Utility.drawTextWithShadow(b, title, Game1.dialogueFont,
            new Vector2(xPositionOnScreen + (width - titleSize.X) / 2, yPositionOnScreen - 32),
            Color.Black);

        for (var i = 0; i < TabCount; i++)
        {
            var bounds = _tabButtons[i].bounds;
            var isActive = i == _currentTab;
            var bgColor = isActive ? Color.White : new Color(180, 180, 180);
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
                bounds.X, bounds.Y, bounds.Width, bounds.Height, bgColor, 4f);
            if (isActive)
            {
                // 覆盖选中页签底部边框，使之与内容区视觉相连
                b.Draw(Game1.staminaRect,
                    new Rectangle(bounds.X + 4, bounds.Y + bounds.Height - 4, bounds.Width - 8, 4), Color.White);
            }
            Utility.drawTextWithShadow(b, _tabLabels[i], Game1.smallFont,
                new Vector2(bounds.X + (bounds.Width - Game1.smallFont.MeasureString(_tabLabels[i]).X) / 2,
                    bounds.Y + (bounds.Height - Game1.smallFont.MeasureString(_tabLabels[i]).Y) / 2),
                isActive ? Game1.textColor : Color.DarkGray);
        }

        var contentX = xPositionOnScreen + ContentPadding;
        var contentY = yPositionOnScreen + TabTopOffset + TabHeight + ContentPadding;
        var contentW = width - ContentPadding * 2;

        switch (_currentTab)
        {
            case 0: DrawCheckingTab(b, contentX, contentY, contentW); break;
            case 1: DrawFixedTab(b, contentX, contentY, contentW); break;
            case 2: DrawTaxTab(b, contentX, contentY, contentW); break;
        }

        base.draw(b);
        drawMouse(b);
    }

    private void DrawCheckingTab(SpriteBatch b, int x, int y, int w)
    {
        var balance = Bank.GetCheckingBalance();
        var interest = Bank.GetInterestEarned();
        var rate = BankCalculator.GetDailyCheckingRate();
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();

        Utility.drawTextWithShadow(b,
            Tools.GetI18n(I18nKeys.Bank_CheckingBalance).Tokens(new { amount = balance, gold }).ToString(),
            Game1.dialogueFont, new Vector2(x, y), Color.Black);

        var interestText = Tools.GetI18n(I18nKeys.Bank_InterestEarned).Tokens(new { amount = interest, gold }).ToString();
        Utility.drawTextWithShadow(b, interestText, Game1.smallFont,
            new Vector2(x, y + 52), Color.Black);

        var claimBtn = _actionButtons.FirstOrDefault(a => a.name == "claim");
        if (claimBtn != null && interest > 0)
        {
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9),
                claimBtn.bounds.X, claimBtn.bounds.Y, claimBtn.bounds.Width, claimBtn.bounds.Height,
                Color.White, 4f);
            var claimLabel = Tools.GetI18n(I18nKeys.Bank_ClaimInterest).ToString();
            Utility.drawTextWithShadow(b, claimLabel, Game1.smallFont,
                new Vector2(claimBtn.bounds.X + (claimBtn.bounds.Width - Game1.smallFont.MeasureString(claimLabel).X) / 2,
                    claimBtn.bounds.Y + 8), Game1.textColor);
        }

        Utility.drawTextWithShadow(b,
            Tools.GetI18n(I18nKeys.Bank_TodayRate).Tokens(new { rate = (rate * 100).ToString("F4") }).ToString(),
            Game1.smallFont, new Vector2(x, y + 90), Color.Gray);

        var depositBtn = _actionButtons.FirstOrDefault(a => a.name == "deposit");
        var withdrawBtn = _actionButtons.FirstOrDefault(a => a.name == "withdraw");

        DrawButton(b, depositBtn, Tools.GetI18n(I18nKeys.Bank_Deposit).ToString());
        DrawButton(b, withdrawBtn, Tools.GetI18n(I18nKeys.Bank_Withdraw).ToString());
    }

    private void DrawFixedTab(SpriteBatch b, int x, int y, int w)
    {
        var newFixedBtn = _actionButtons.FirstOrDefault(a => a.name == "newFixed");
        DrawButton(b, newFixedBtn, Tools.GetI18n(I18nKeys.Bank_NewFixed).ToString());

        var deposits = Bank.GetFixedDeposits();
        var listY = y + 56;
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();

        if (deposits.Count == 0)
        {
            Utility.drawTextWithShadow(b, Tools.GetI18n(I18nKeys.Bank_FixedEmpty).ToString(),
                Game1.smallFont, new Vector2(x, listY), Color.Gray);
            return;
        }

        var btnIndex = 0;
        for (var i = 0; i < deposits.Count; i++)
        {
            var d = deposits[i];
            var elapsed = (int)Game1.stats.DaysPlayed - d.StartDay;
            var matured = elapsed >= d.TermDays;

            var line = $"[{i + 1}] ";
            line += Tools.GetI18n(I18nKeys.Bank_FixedAmount).Tokens(new { amount = d.Amount, gold }).ToString();
            line += $" | {d.TermDays} {Tools.GetI18n(I18nKeys.Bank_Days).ToString()}";
            line += " | ";

            if (d.Withdrawn)
            {
                line += Tools.GetI18n(I18nKeys.Bank_FixedStatusWithdrawn).ToString();
            }
            else if (matured)
            {
                line += Tools.GetI18n(I18nKeys.Bank_FixedStatusMature).ToString();
            }
            else
            {
                var remaining = d.TermDays - elapsed;
                line += Tools.GetI18n(I18nKeys.Bank_FixedStatusActive).Tokens(new { remaining }).ToString();
            }

            Utility.drawTextWithShadow(b, line, Game1.smallFont, new Vector2(x, listY), Color.Black);

            if (!d.Withdrawn)
            {
                if (btnIndex < _fixedActionButtons.Count)
                {
                    var redeemBtn = _fixedActionButtons[btnIndex++];
                    DrawButton(b, redeemBtn, Tools.GetI18n(I18nKeys.Bank_Redeem).ToString());
                }
                if (btnIndex < _fixedActionButtons.Count)
                {
                    var earlyBtn = _fixedActionButtons[btnIndex++];
                    DrawButton(b, earlyBtn, Tools.GetI18n(I18nKeys.Bank_EarlyWithdraw).ToString());
                }
            }

            listY += 40;
        }
    }

    /// <summary>税收页签：读取 PlayerStall 累计税金展示。</summary>
    private void DrawTaxTab(SpriteBatch b, int x, int y, int w)
    {
        var totalTax = PlayerStall.PlayerStall.TotalTax;
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();
        Utility.drawTextWithShadow(b,
            Tools.GetI18n(I18nKeys.Bank_TotalTax).Tokens(new { amount = totalTax, gold }).ToString(),
            Game1.dialogueFont, new Vector2(x, y), Color.Black);
    }

    private static void DrawButton(SpriteBatch b, ClickableComponent? btn, string label)
    {
        if (btn == null) return;
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9),
            btn.bounds.X, btn.bounds.Y, btn.bounds.Width, btn.bounds.Height, Color.White, 4f);
        Utility.drawTextWithShadow(b, label, Game1.smallFont,
            new Vector2(btn.bounds.X + (btn.bounds.Width - Game1.smallFont.MeasureString(label).X) / 2, btn.bounds.Y + 8),
            Game1.textColor);
    }
}
