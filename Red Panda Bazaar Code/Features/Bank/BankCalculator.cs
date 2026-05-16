using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.Bank;

public static class BankCalculator
{
    private const double BaseCheckingRate = 0.0005;

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
}
