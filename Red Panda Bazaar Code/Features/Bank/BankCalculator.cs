using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.Bank;

public static class BankCalculator
{
    private const double BaseCheckingRate = 0.0005;

    /// <summary>三种贷款方案日利率：灵活贷 / 标准贷 / 定期贷</summary>
    public static readonly double[] LoanDailyRate = { 0.0012, 0.0010, 0.0007 };

    /// <summary>三种贷款方案占信用额度的比例</summary>
    private static readonly double[] LoanPlanFactor = { 0.1, 0.4, 0.8 };

    /// <summary>三种贷款方案的最低可贷额度</summary>
    private static readonly long[] LoanMinAmount = { 500, 8000, 20000 };

    /// <summary>定期存款可选期限</summary>
    public static readonly int[] FixedTermOptions = { 7, 28, 112 };

    /// <summary>定期利率相对活期的倍率：7天/28天/112天</summary>
    private static readonly double[] FixedRateMultiplier = { 1.5, 2.0, 2.5 };

    /// <summary>每日活期利率会随当日运气和日期浮动。</summary>
    public static double GetDailyCheckingRate()
    {
        var luckMod = 1.0 + Game1.player.DailyLuck * 2;
        var dayOfMonth = Game1.Date.DayOfMonth;
        var dayMod = 1.0 + Math.Sin(Math.PI * dayOfMonth / 28.0) * 0.3;
        return BaseCheckingRate * luckMod * dayMod;
    }

    /// <summary>定期利率 = 逐日活期利率求和 × 期限倍率。</summary>
    public static double GetFixedTermRate(int termDays)
    {
        var dailyRate = GetDailyCheckingRate();
        var index = Array.IndexOf(FixedTermOptions, termDays);
        var multiplier = index >= 0 ? FixedRateMultiplier[index] : 1.0;
        return dailyRate * termDays * multiplier;
    }

    /// <summary>玩家靠自己赚的钱 = 总收入 − 历史上所有贷款本金。</summary>
    private static long GetEffectiveEarnings(List<LoanAccount> allLoans)
    {
        var totalBorrowed = allLoans.Sum(l => l.Principal);
        var effective = Game1.player.totalMoneyEarned - totalBorrowed;
        return effective > 0 ? effective : 0;
    }

    /// <summary>总信用额度 = max(有效收入 × 0.5, 游戏天数 × 10000) + 总税收 × 0.5。</summary>
    public static long GetTotalCreditLimit(List<LoanAccount> allLoans)
    {
        var byEarnings = (long)(GetEffectiveEarnings(allLoans) * 0.5);
        var byDays = Game1.stats.DaysPlayed * 10000L;
        var taxBonus = (long)(PlayerStall.PlayerStall.TotalTax * 0.5);
        return Math.Max(byEarnings, byDays) + taxBonus;
    }

    /// <summary>剩余可用额度 = 总额度 − 所有未还贷款的（本金+利息）。</summary>
    public static long GetRemainingCredit(List<LoanAccount> allLoans)
    {
        var total = GetTotalCreditLimit(allLoans);
        var used = allLoans.Where(l => !l.Repaid).Sum(l => l.Principal + l.InterestAccrued);
        return total - used;
    }

    /// <summary>某方案当前可贷 = max(信用额度 × 方案比例, 方案最低) 且不超过剩余额度。</summary>
    public static long GetAvailableLoanAmount(int planType, long remainingCredit, List<LoanAccount> allLoans)
    {
        if (planType < 0 || planType >= LoanPlanFactor.Length) return 0;
        var totalCredit = GetTotalCreditLimit(allLoans);
        var planMax = (long)(totalCredit * LoanPlanFactor[planType]);
        planMax = Math.Max(planMax, LoanMinAmount[planType]);
        return Math.Min(planMax, Math.Max(0L, remainingCredit));
    }
}
