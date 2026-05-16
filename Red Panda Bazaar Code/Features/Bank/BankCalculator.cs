using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.Bank;

public static class BankCalculator
{
    private const double BaseCheckingRate = 0.0005;

    /// <summary>三种贷款方案日利率：灵活贷 / 标准贷 / 定期贷</summary>
    public static readonly double[] LoanDailyRate = { 0.0012, 0.0010, 0.0007 };

    /// <summary>三种贷款方案的可贷倍数（相对于玩家持有金币）</summary>
    public static readonly double[] LoanMultiplier = { 0.5, 1.0, 1.5 };

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

    /// <summary>总信用额度 = 玩家持有金币 × 1.5。</summary>
    public static int GetTotalCreditLimit(int playerMoney)
    {
        return (int)(playerMoney * 1.5);
    }

    /// <summary>剩余可用额度 = 总额度 − 所有未还贷款的（本金+利息）。</summary>
    public static int GetRemainingCredit(int playerMoney, List<LoanAccount> loans)
    {
        var total = GetTotalCreditLimit(playerMoney);
        var used = loans.Where(l => !l.Repaid).Sum(l => l.Principal + l.InterestAccrued);
        return total - used;
    }

    /// <summary>某方案当前可贷 = min(方案上限, 剩余额度)。</summary>
    public static int GetAvailableLoanAmount(int planType, int playerMoney, int remainingCredit)
    {
        if (planType < 0 || planType >= LoanMultiplier.Length) return 0;
        var planMax = (int)(playerMoney * LoanMultiplier[planType]);
        return Math.Min(planMax, Math.Max(0, remainingCredit));
    }
}
