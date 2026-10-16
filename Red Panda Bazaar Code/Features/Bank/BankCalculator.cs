using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.Bank;

public static class BankCalculator {
    /// <summary>三种贷款方案日利率：灵活贷 / 标准贷 / 定期贷</summary>
    public static readonly double[] LoanDailyRate = { 0.02, 0.018, 0.014 };

    /// <summary>三种贷款方案占信用额度的比例</summary>
    private static readonly double[] LoanPlanFactor = { 0.1, 0.3, 0.6 };

    /// <summary>三种贷款方案的最低可贷额度</summary>
    private static readonly long[] LoanMinAmount = { 500, 8000, 20000 };

    /// <summary>有效收入 = 总收入 − 历史上所有贷款本金。</summary>
    private static long GetEffectiveEarnings(List<LoanAccount> allLoans) {
        var totalBorrowed = allLoans.Sum(l => l.Principal);
        var effective = Game1.player.totalMoneyEarned - totalBorrowed;
        return effective > 0 ? effective : 0;
    }

    /// <summary>总信用额度 = max(有效收入 × 0.5, 10000)。</summary>
    public static long GetTotalCreditLimit(List<LoanAccount> allLoans) {
        var byEarnings = (long)(GetEffectiveEarnings(allLoans) * 0.5);
        return Math.Max(byEarnings, 10000);
    }

    /// <summary>剩余可用额度 = 总额度 − 所有未还贷款的（本金+利息）。</summary>
    public static long GetRemainingCredit(List<LoanAccount> allLoans) {
        var total = GetTotalCreditLimit(allLoans);
        var used = allLoans.Where(l => !l.Repaid).Sum(l => l.Principal + l.InterestAccrued);
        return total - used;
    }

    /// <summary>某方案当前可贷 = max(信用额度 × 方案比例, 方案最低) 且不超过剩余额度。</summary>
    public static long GetAvailableLoanAmount(int planType, long remainingCredit, List<LoanAccount> allLoans) {
        if (planType < 0 || planType >= LoanPlanFactor.Length) return 0;
        var totalCredit = GetTotalCreditLimit(allLoans);
        var planMax = (long)(totalCredit * LoanPlanFactor[planType]);
        planMax = Math.Max(planMax, LoanMinAmount[planType]);
        return Math.Min(planMax, Math.Max(0L, remainingCredit));
    }
}