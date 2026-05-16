using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Framework.UI;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.Bank;

public class BankLoanMenu : UiBaseMenu
{
    protected override Point CalcContentSize() => new(600, 300);

    protected override void BuildUi()
    {
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();
        var allLoans = Bank.GetLoans();
        var remaining = BankCalculator.GetRemainingCredit(allLoans);
        var totalCredit = BankCalculator.GetTotalCreditLimit(allLoans);
        var usedCredit = totalCredit - remaining;

        // 信用额度信息
        Root.Add(
            new UiText(Tools.GetI18n(I18nKeys.Bank_CreditTotal)
                .Tokens(new { amount = totalCredit, gold }).ToString()),
            new UiText(Tools.GetI18n(I18nKeys.Bank_CreditUsed)
                .Tokens(new { amount = Math.Max(0, usedCredit), gold }).ToString(), color: Color.DarkRed),
            new UiText(Tools.GetI18n(I18nKeys.Bank_CreditRemain)
                .Tokens(new { amount = Math.Max(0, remaining), gold }).ToString(), color: Color.DarkGreen)
        );

        // 方案分隔
        Root.Add(new UiText("── " + Tools.GetI18n(I18nKeys.Bank_LoanTab) + " ──", color: Color.Gray));

        // 三个贷款方案
        var descKeys = new[] { I18nKeys.Bank_PlanDescA, I18nKeys.Bank_PlanDescB, I18nKeys.Bank_PlanDescC };
        for (var t = 0; t < 3; t++)
        {
            var planType = t; // capture
            var ratePercent = (BankCalculator.LoanDailyRate[t] * 100).ToString("F2");
            var avail = BankCalculator.GetAvailableLoanAmount(t,
                BankCalculator.GetRemainingCredit(allLoans), allLoans);
            var hasActive = allLoans.Any(l => !l.Repaid && l.PlanType == t);

            var desc = Tools.GetI18n(descKeys[t])
                .Tokens(new { amount = Math.Max(0, avail), rate = ratePercent, gold }).ToString();

            if (hasActive || avail <= 0)
            {
                Root.Add(new UiText(desc, color: Color.Gray));
            }
            else
            {
                var btn = new UiButton(Tools.GetI18n(I18nKeys.Bank_Apply).ToString(),
                    () => HandleApply(planType));
                Root.Add(new UiRow { Spacing = 20 }.Add(new UiText(desc)).Add(btn));
            }
        }

        // 现有贷款分隔
        Root.Add(new UiText("── " + Tools.GetI18n(I18nKeys.Bank_LoanRepayTitle) + " ──", color: Color.Gray));

        // 现有贷款列表
        var planNames = new[]
        {
            Tools.GetI18n(I18nKeys.Bank_PlanA).ToString(),
            Tools.GetI18n(I18nKeys.Bank_PlanB).ToString(),
            Tools.GetI18n(I18nKeys.Bank_PlanC).ToString()
        };

        var activeLoans = allLoans.Where(l => !l.Repaid).ToList();
        if (activeLoans.Count == 0)
        {
            Root.Add(new UiText(Tools.GetI18n(I18nKeys.Bank_NoLoans).ToString(), color: Color.Gray));
        }
        else
        {
            for (var i = 0; i < allLoans.Count; i++)
            {
                var loan = allLoans[i];
                if (loan.Repaid) continue;
                var idx = i; // capture

                var line = $"[{planNames[loan.PlanType]}] ";
                line += Tools.GetI18n(I18nKeys.Bank_LoanPrincipal)
                    .Tokens(new { amount = loan.Principal, gold }).ToString();
                line += " | ";
                line += Tools.GetI18n(I18nKeys.Bank_LoanInterest)
                    .Tokens(new { amount = loan.InterestAccrued, gold }).ToString();

                var repayBtn = new UiButton(Tools.GetI18n(I18nKeys.Bank_Repay).ToString(),
                    () => HandleRepay(idx));
                Root.Add(new UiRow { Spacing = 20 }.Add(new UiText(line)).Add(repayBtn));
            }
        }
    }

    private void HandleApply(int planType)
    {
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
        if (Context.IsMainPlayer) Rebuild();
        else Game1.exitActiveMenu();
    }

    private void HandleRepay(int loanIndex)
    {
        Bank.RepayLoan(loanIndex);
        Game1.playSound("coin");
        if (Context.IsMainPlayer) Rebuild();
        else Game1.exitActiveMenu();
    }
}
