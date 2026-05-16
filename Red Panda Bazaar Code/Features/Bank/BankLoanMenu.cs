using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Features.Bank;

public class BankLoanMenu : IClickableMenu
{
    private const int ContentPadding = 24;
    private const int TopPadding = 40;
    private readonly List<ClickableComponent> _actionButtons = new();
    private readonly List<ClickableComponent> _loanActionButtons = new();

    private static int CalcWidth()
    {
        return Math.Clamp(640, 600, Game1.uiViewport.Width - 40);
    }

    private static int CalcHeight()
    {
        var loans = Bank.GetLoans().Count(l => !l.Repaid);
        var listH = 56 + 40 * Math.Min(loans + 1, 5) + 60;
        var h = TopPadding + ContentPadding + 160 + listH + ContentPadding + 40;
        return Math.Clamp(h, 200, Game1.uiViewport.Height - 40);
    }

    public BankLoanMenu()
        : base(
            (Game1.uiViewport.Width - CalcWidth()) / 2,
            (Game1.uiViewport.Height - CalcHeight()) / 2,
            CalcWidth(), CalcHeight(),
            showUpperRightCloseButton: true)
    {
        RefreshActionButtons();
    }

    private void RefreshActionButtons()
    {
        _actionButtons.Clear();
        _loanActionButtons.Clear();

        var cx = xPositionOnScreen + ContentPadding;
        var cy = yPositionOnScreen + TopPadding;

        var allLoans = Bank.GetLoans();
        for (var t = 0; t < 3; t++)
        {
            var remaining = BankCalculator.GetRemainingCredit(allLoans);
            var available = BankCalculator.GetAvailableLoanAmount(t, remaining, allLoans);

            // 该方案已有未还贷款则隐藏申请按钮
            var hasActiveLoan = allLoans.Any(l => !l.Repaid && l.PlanType == t);
            if (available <= 0 || hasActiveLoan) continue;

            _actionButtons.Add(new ClickableComponent(
                new Rectangle(cx + 480, cy + 60 + t * 28, 60, 24), $"apply_{t}"));
        }

        var loans = Bank.GetLoans();
        var listY = cy + 170;
        var contentW = width - ContentPadding * 2;
        for (var i = 0; i < loans.Count; i++)
        {
            if (loans[i].Repaid) continue;

            _loanActionButtons.Add(new ClickableComponent(
                new Rectangle(cx + contentW - 90, listY, 70, 36), $"repay_{i}"));

            listY += 40;
        }
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (Game1.activeClickableMenu != this) return;

        foreach (var btn in _actionButtons)
        {
            if (!btn.bounds.Contains(x, y)) continue;
            HandleApplyButton(btn.name);
            return;
        }

        foreach (var btn in _loanActionButtons)
        {
            if (!btn.bounds.Contains(x, y)) continue;
            var parts = btn.name.Split('_');
            if (parts.Length >= 2 && int.TryParse(parts[1], out var idx))
                Bank.RepayLoan(idx);
            Game1.playSound("coin");
            if (Context.IsMainPlayer)
                Game1.activeClickableMenu = new BankLoanMenu();
            else
                Game1.exitActiveMenu();
            return;
        }
    }

    private void HandleApplyButton(string name)
    {
        if (!name.StartsWith("apply_") || !int.TryParse(name.Replace("apply_", ""), out var planType)) return;

        var allLoans = Bank.GetLoans();
        var remaining = BankCalculator.GetRemainingCredit(allLoans);
        var available = BankCalculator.GetAvailableLoanAmount(planType, remaining, allLoans);
        if (available <= 0)
        {
            Game1.drawObjectDialogue(Tools.GetI18n(I18nKeys.Bank_CreditLimitReached).ToString());
            return;
        }

        Bank.ApplyLoan(planType);
        Game1.playSound("coin");
        if (Context.IsMainPlayer)
            Game1.activeClickableMenu = new BankLoanMenu();
        else
            Game1.exitActiveMenu();
    }

    public override void receiveRightClick(int x, int y, bool playSound = true) { }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        width = CalcWidth();
        height = CalcHeight();
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
        initializeUpperRightCloseButton();
        RefreshActionButtons();
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
            xPositionOnScreen, yPositionOnScreen, width, height, Color.White, 4f);

        var title = Tools.GetI18n(I18nKeys.Bank_LoanTab).ToString();
        var titleSize = Game1.dialogueFont.MeasureString(title);
        Utility.drawTextWithShadow(b, title, Game1.dialogueFont,
            new Vector2(xPositionOnScreen + (width - titleSize.X) / 2, yPositionOnScreen - 32), Color.Black);

        var cx = xPositionOnScreen + ContentPadding;
        var cy = yPositionOnScreen + TopPadding;
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();

        var allLoans = Bank.GetLoans();
        var remaining = BankCalculator.GetRemainingCredit(allLoans);
        var totalCredit = BankCalculator.GetTotalCreditLimit(allLoans);
        var usedCredit = totalCredit - remaining;

        Utility.drawTextWithShadow(b,
            Tools.GetI18n(I18nKeys.Bank_CreditTotal).Tokens(new { amount = totalCredit, gold }).ToString(),
            Game1.smallFont, new Vector2(cx, cy), Color.Black);
        Utility.drawTextWithShadow(b,
            Tools.GetI18n(I18nKeys.Bank_CreditUsed).Tokens(new { amount = Math.Max(0, usedCredit), gold }).ToString(),
            Game1.smallFont, new Vector2(cx + 260, cy), Color.DarkRed);
        Utility.drawTextWithShadow(b,
            Tools.GetI18n(I18nKeys.Bank_CreditRemain).Tokens(new { amount = Math.Max(0, remaining), gold }).ToString(),
            Game1.smallFont, new Vector2(cx + 420, cy), Color.DarkGreen);

        Utility.drawTextWithShadow(b, "── " + Tools.GetI18n(I18nKeys.Bank_LoanTab) + " ──",
            Game1.smallFont, new Vector2(cx, cy + 32), Color.Gray);

        var planLabels = new[]
        {
            Tools.GetI18n(I18nKeys.Bank_PlanA).ToString(),
            Tools.GetI18n(I18nKeys.Bank_PlanB).ToString(),
            Tools.GetI18n(I18nKeys.Bank_PlanC).ToString()
        };
        var planDescKeys = new[]
        {
            I18nKeys.Bank_PlanDescA,
            I18nKeys.Bank_PlanDescB,
            I18nKeys.Bank_PlanDescC
        };

        for (var t = 0; t < 3; t++)
        {
            var ratePercent = (BankCalculator.LoanDailyRate[t] * 100).ToString("F2");
            var avail = BankCalculator.GetAvailableLoanAmount(t,
                BankCalculator.GetRemainingCredit(allLoans), allLoans);
            var yPos = cy + 60 + t * 28;

            Utility.drawTextWithShadow(b,
                Tools.GetI18n(planDescKeys[t])
                    .Tokens(new { amount = Math.Max(0, avail), rate = ratePercent, gold }).ToString(),
                Game1.smallFont, new Vector2(cx, yPos), Color.Black);

            if (avail > 0)
            {
                var applyBtn = _actionButtons.FirstOrDefault(a => a.name == $"apply_{t}");
                if (applyBtn != null)
                {
                    DrawButton(b, applyBtn.bounds.X, applyBtn.bounds.Y,
                        applyBtn.bounds.Width, applyBtn.bounds.Height,
                        Tools.GetI18n(I18nKeys.Bank_Apply).ToString());
                }
            }
        }

        var listY = cy + 170;

        Utility.drawTextWithShadow(b, "── " + Tools.GetI18n(I18nKeys.Bank_LoanRepayTitle) + " ──",
            Game1.smallFont, new Vector2(cx, listY - 24), Color.Gray);

        var loans = Bank.GetLoans();
        var activeLoans = loans.Where(l => !l.Repaid).ToList();

        if (activeLoans.Count == 0)
        {
            Utility.drawTextWithShadow(b, Tools.GetI18n(I18nKeys.Bank_NoLoans).ToString(),
                Game1.smallFont, new Vector2(cx, listY), Color.Gray);
        }
        else
        {
            var btnIndex = 0;
            for (var i = 0; i < loans.Count; i++)
            {
                var loan = loans[i];
                if (loan.Repaid) continue;

                var planName = planLabels[loan.PlanType];
                var line = $"[{planName}] ";
                line += Tools.GetI18n(I18nKeys.Bank_LoanPrincipal).Tokens(new { amount = loan.Principal, gold }).ToString();
                line += " | ";
                line += Tools.GetI18n(I18nKeys.Bank_LoanInterest).Tokens(new { amount = loan.InterestAccrued, gold }).ToString();

                Utility.drawTextWithShadow(b, line, Game1.smallFont, new Vector2(cx, listY), Color.Black);

                if (btnIndex < _loanActionButtons.Count)
                {
                    var repayBtn = _loanActionButtons[btnIndex++];
                    DrawButton(b, repayBtn.bounds.X, repayBtn.bounds.Y,
                        repayBtn.bounds.Width, repayBtn.bounds.Height,
                        Tools.GetI18n(I18nKeys.Bank_Repay).ToString());
                }

                listY += 40;
            }
        }

        base.draw(b);
        drawMouse(b);
    }

    private static void DrawButton(SpriteBatch b, int x, int y, int w, int h, string label)
    {
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9),
            x, y, w, h, Color.White, 4f);
        Utility.drawTextWithShadow(b, label, Game1.smallFont,
            new Vector2(x + (w - Game1.smallFont.MeasureString(label).X) / 2, y + 8), Game1.textColor);
    }
}
