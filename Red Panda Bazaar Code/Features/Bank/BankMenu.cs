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

    // 三种按钮列表分开管理，避免同名冲突
    private readonly List<ClickableComponent> _actionButtons = new();
    private readonly List<ClickableComponent> _loanRepayButtons = new();
    private readonly List<ClickableComponent> _fixedActionButtons = new();

    private const int TabCount = 4;
    private const int TabHeight = 48;
    private const int ContentPadding = 24;
    private const int TabGap = 4;
    private const int PlanPanelHeight = 70;

    /// <summary>根据各页签内容计算所需宽度。</summary>
    private static int CalcWidth()
    {
        var font = Game1.smallFont;
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();
        var maxAmount = 9999999;

        // 三个信用额度项并列展示，取总宽度
        var creditW = new[]
        {
            Tools.GetI18n(I18nKeys.Bank_CreditTotal).Tokens(new { amount = maxAmount, gold }).ToString(),
            Tools.GetI18n(I18nKeys.Bank_CreditUsed).Tokens(new { amount = maxAmount, gold }).ToString(),
            Tools.GetI18n(I18nKeys.Bank_CreditRemain).Tokens(new { amount = maxAmount, gold }).ToString()
        }.Sum(t => font.MeasureString(t).X + 20);

        // 方案描述（最长的是方案C）
        var maxRate = 0.12;
        var descW = font.MeasureString(
            Tools.GetI18n(I18nKeys.Bank_PlanDescC)
                .Tokens(new { amount = maxAmount, gold, rate = maxRate.ToString("F2") }).ToString()
        ).X + 140; // + apply button width

        // 贷款列表行
        var loanW = font.MeasureString(
            $"[A] {Tools.GetI18n(I18nKeys.Bank_LoanPrincipal).Tokens(new { amount = maxAmount, gold })} | " +
            $"{Tools.GetI18n(I18nKeys.Bank_LoanInterest).Tokens(new { amount = maxAmount, gold })} | " +
            $"{Tools.GetI18n(I18nKeys.Bank_LoanLockDays).Tokens(new { days = 7 })})"
        ).X + 100;

        var w = (int)Math.Max(creditW, Math.Max(descW, loanW)) + ContentPadding * 3 + 40;
        return Math.Clamp(w, 600, Game1.uiViewport.Width - 40);
    }

    /// <summary>根据各页签内容计算所需高度。</summary>
    private static int CalcHeight()
    {
        var baseH = 16 + TabHeight + ContentPadding; // 标题上方 + 页签 + 内容上间距
        var bottomPad = ContentPadding + 40; // 底部间距 + 关闭按钮

        // 活期页签：余额 + 利息 + 利率 + 按钮区
        var checkingH = 180;

        // 定期页签：按钮 + 列表行（最多假设 5 笔）
        var fixedH = 56 + 40 * Math.Min(Bank.GetFixedDeposits().Count + 1, 5) + 60;

        // 贷款页签（一般最高）：信用额度 + 3 方案面板 + 分隔 + 贷款列表
        var planArea = 44 + 3 * (PlanPanelHeight + 20);
        var loanCount = Bank.GetLoans().Count(l => !l.Repaid);
        var loanListTop = planArea + 20;
        var loanArea = loanListTop + 8 + Math.Max(loanCount, 1) * 40;
        var loanH = loanArea + ContentPadding;

        // 税收页签：一行文本
        var taxH = 60;

        var h = baseH + Math.Max(checkingH, Math.Max(fixedH, Math.Max(loanH, taxH))) + bottomPad;
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
            Tools.GetI18n(I18nKeys.Bank_LoanTab).ToString(),
            Tools.GetI18n(I18nKeys.Bank_TaxTab).ToString()
        };

        var tabWidth = (width - TabGap * (TabCount - 1)) / TabCount;
        for (var i = 0; i < TabCount; i++)
        {
            _tabButtons.Add(new ClickableComponent(
                new Rectangle(xPositionOnScreen + (tabWidth + TabGap) * i, yPositionOnScreen + 16, tabWidth, TabHeight),
                $"tab_{i}"));
        }

        RefreshActionButtons();
    }

    /// <summary>根据当前页签重新计算所有按钮的坐标和可见性。</summary>
    private void RefreshActionButtons()
    {
        _actionButtons.Clear();
        _loanRepayButtons.Clear();
        _fixedActionButtons.Clear();

        var contentX = xPositionOnScreen + ContentPadding;
        var contentY = yPositionOnScreen + 16 + TabHeight + ContentPadding;
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
            case 2:
            {
                for (var p = 0; p < 3; p++)
                {
                    var applyBtn = new ClickableComponent(
                        new Rectangle(contentX + contentW - 120, contentY + 20 + p * 80, 100, 36), $"apply_{p}");
                    _actionButtons.Add(applyBtn);
                }

                var loans = Bank.GetLoans();
                var loanListY = contentY + 280;
                for (var i = 0; i < loans.Count; i++)
                {
                    if (loans[i].Repaid) continue;
                    var repayBtn = new ClickableComponent(
                        new Rectangle(contentX + contentW - 100, loanListY, 80, 36), $"repay_{i}");
                    _loanRepayButtons.Add(repayBtn);
                    loanListY += 40;
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

        foreach (var btn in _loanRepayButtons)
        {
            if (!btn.bounds.Contains(x, y)) continue;
            HandleLoanRepay(btn.name);
            return;
        }

        foreach (var btn in _fixedActionButtons)
        {
            if (!btn.bounds.Contains(x, y)) continue;
            HandleFixedAction(btn.name);
            return;
        }
    }

    /// <summary>处理页签主按钮事件：存/取/领/开/贷。</summary>
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
                            // 主机同步完成可直接刷新，客机需等主机广播
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

            case "apply_0":
                Bank.ApplyLoan(0);
                Game1.playSound("coin");
                exitThisMenu();
                break;
            case "apply_1":
                Bank.ApplyLoan(1);
                Game1.playSound("coin");
                exitThisMenu();
                break;
            case "apply_2":
                Bank.ApplyLoan(2);
                Game1.playSound("coin");
                exitThisMenu();
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

    /// <summary>方案A弹部分还款输入框，方案B/C直接全额还，方案C需检查锁定期。</summary>
    private void HandleLoanRepay(string name)
    {
        var parts = name.Split('_');
        if (parts.Length < 2 || !int.TryParse(parts[1], out var index)) return;

        var loans = Bank.GetLoans();
        if (index < 0 || index >= loans.Count) return;
        var loan = loans[index];
        if (loan.Repaid) return;

        if (loan.PlanType == 2)
        {
            var elapsed = (int)Game1.stats.DaysPlayed - loan.StartDay;
            if (elapsed < 7)
            {
                Game1.drawObjectDialogue(Tools.GetI18n(I18nKeys.Bank_LockedCantRepay).ToString());
                return;
            }
        }

        if (loan.PlanType == 0)
        {
            var totalDebt = loan.Principal + loan.InterestAccrued;
            Game1.activeClickableMenu = new NumberSelectionMenu(
                Tools.GetI18n(I18nKeys.Bank_RepayPartTitle).ToString(),
                (number, price, who) =>
                {
                    if (number > 0)
                    {
                        Bank.RepayLoanPartial(index, number);
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
                maxValue: Math.Max(1, Math.Min(totalDebt, Game1.player.Money)),
                defaultNumber: Math.Min(100, totalDebt));
        }
        else
        {
            var total = loan.Principal + loan.InterestAccrued;
            if (Game1.player.Money < total)
            {
                Game1.drawObjectDialogue(Tools.GetI18n(I18nKeys.Bank_NoMoney).ToString());
                return;
            }
            Bank.RepayLoan(index);
            Game1.playSound("coin");
            exitThisMenu();
        }
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
            b.Draw(Game1.staminaRect, bounds, bgColor);
            if (!isActive)
            {
                b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y + bounds.Height - 2, bounds.Width, 2), Color.Gray);
            }
            Utility.drawTextWithShadow(b, _tabLabels[i], Game1.smallFont,
                new Vector2(bounds.X + (bounds.Width - Game1.smallFont.MeasureString(_tabLabels[i]).X) / 2,
                    bounds.Y + (bounds.Height - Game1.smallFont.MeasureString(_tabLabels[i]).Y) / 2),
                isActive ? Game1.textColor : Color.DarkGray);
        }

        var contentX = xPositionOnScreen + ContentPadding;
        var contentY = yPositionOnScreen + 16 + TabHeight + ContentPadding;
        var contentW = width - ContentPadding * 2;

        switch (_currentTab)
        {
            case 0: DrawCheckingTab(b, contentX, contentY, contentW); break;
            case 1: DrawFixedTab(b, contentX, contentY, contentW); break;
            case 2: DrawLoanTab(b, contentX, contentY, contentW); break;
            case 3: DrawTaxTab(b, contentX, contentY, contentW); break;
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

    private void DrawLoanTab(SpriteBatch b, int x, int y, int w)
    {
        var playerMoney = Game1.player.Money;
        var totalCredit = BankCalculator.GetTotalCreditLimit(playerMoney);
        var loans = Bank.GetLoans().Where(l => !l.Repaid).ToList();
        var usedCredit = loans.Sum(l => l.Principal + l.InterestAccrued);
        var remainingCredit = totalCredit - usedCredit;
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();

        Utility.drawTextWithShadow(b,
            Tools.GetI18n(I18nKeys.Bank_CreditTotal).Tokens(new { amount = totalCredit, gold }).ToString(),
            Game1.smallFont, new Vector2(x, y), Color.Black);
        Utility.drawTextWithShadow(b,
            Tools.GetI18n(I18nKeys.Bank_CreditUsed).Tokens(new { amount = usedCredit, gold }).ToString(),
            Game1.smallFont, new Vector2(x + 220, y), Color.DarkRed);
        Utility.drawTextWithShadow(b,
            Tools.GetI18n(I18nKeys.Bank_CreditRemain).Tokens(new { amount = Math.Max(0, remainingCredit), gold }).ToString(),
            Game1.smallFont, new Vector2(x + 400, y), Color.DarkGreen);

        var planY = y + 44;
        for (var p = 0; p < 3; p++)
        {
            var planNames = new[]
            {
                Tools.GetI18n(I18nKeys.Bank_PlanA),
                Tools.GetI18n(I18nKeys.Bank_PlanB),
                Tools.GetI18n(I18nKeys.Bank_PlanC)
            };
            var planDescs = new[]
            {
                I18nKeys.Bank_PlanDescA,
                I18nKeys.Bank_PlanDescB,
                I18nKeys.Bank_PlanDescC
            };
            var availAmount = BankCalculator.GetAvailableLoanAmount(p, playerMoney, remainingCredit);
            var rate = BankCalculator.LoanDailyRate[p];

            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
                x, planY, w, 70, Color.White * 0.9f, 4f);

            Utility.drawTextWithShadow(b, planNames[p].ToString(), Game1.smallFont,
                new Vector2(x + 12, planY + 8), Color.Black);
            Utility.drawTextWithShadow(b,
                Tools.GetI18n(I18nKeys.Bank_DailyRateLabel).Tokens(new { rate = (rate * 100).ToString("F2") }).ToString(),
                Game1.smallFont, new Vector2(x + 160, planY + 8), Color.Gray);
            Utility.drawTextWithShadow(b,
                Tools.GetI18n(planDescs[p]).Tokens(new { amount = availAmount, gold, rate = (rate * 100).ToString("F2") }).ToString(),
                Game1.smallFont, new Vector2(x + 12, planY + 30), Color.DarkSlateGray);

            var applyBtn = _actionButtons.FirstOrDefault(a => a.name == $"apply_{p}");
            if (applyBtn != null && availAmount > 0)
            {
                DrawButton(b, applyBtn, Tools.GetI18n(I18nKeys.Bank_Apply).ToString());
            }

            planY += 90;
        }

        var loanListY = planY + 20;
        Utility.drawTextWithShadow(b, "---", Game1.smallFont,
            new Vector2(x, loanListY), Color.Gray);
        loanListY += 8;

        var activeLoans = Bank.GetLoans().Where(l => !l.Repaid).ToList();
        if (activeLoans.Count == 0)
        {
            Utility.drawTextWithShadow(b, Tools.GetI18n(I18nKeys.Bank_NoLoans).ToString(),
                Game1.smallFont, new Vector2(x, loanListY), Color.Gray);
            return;
        }

        var repayBtnIndex = 0;
        for (var i = 0; i < activeLoans.Count; i++)
        {
            var l = activeLoans[i];
            var planLabels = new[] { "A", "B", "C" };
            var line = $"[{planLabels[l.PlanType]}] ";
            line += Tools.GetI18n(I18nKeys.Bank_LoanPrincipal).Tokens(new { amount = l.Principal, gold }).ToString();
            line += " | ";
            line += Tools.GetI18n(I18nKeys.Bank_LoanInterest).Tokens(new { amount = l.InterestAccrued, gold }).ToString();

            if (l.PlanType == 2)
            {
                var elapsed = (int)Game1.stats.DaysPlayed - l.StartDay;
                var lockRemaining = 7 - elapsed;
                if (lockRemaining > 0)
                {
                    line += " | ";
                    line += Tools.GetI18n(I18nKeys.Bank_LoanLockDays).Tokens(new { days = lockRemaining }).ToString();
                }
            }

            Utility.drawTextWithShadow(b, line, Game1.smallFont,
                new Vector2(x, loanListY), Color.Black);

            if (!(l.PlanType == 2 && (int)Game1.stats.DaysPlayed - l.StartDay < 7))
            {
                if (repayBtnIndex < _loanRepayButtons.Count)
                {
                    var repayBtn = _loanRepayButtons[repayBtnIndex++];
                    DrawButton(b, repayBtn, Tools.GetI18n(I18nKeys.Bank_Repay).ToString());
                }
            }

            loanListY += 40;
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
