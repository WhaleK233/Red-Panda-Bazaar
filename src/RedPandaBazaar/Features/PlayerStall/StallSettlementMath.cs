namespace Red_Panda_Bazaar_Code.Features.PlayerStall;

/// <summary>
/// 摊位夜间结算的纯计算部分：定价因子、开张概率、销量。
/// 不含任何游戏状态读写，便于单独测试（见 tests 中的 StallSettlementMathTests）。
/// </summary>
public static class StallSettlementMath
{
    /// <summary>天气不佳时的销量与开张率折扣。</summary>
    public const double BadWeatherModifier = 0.85;

    /// <summary>运气对结算的影响倍率（DailyLuck 可正可负）。</summary>
    public const double LuckFactor = 2.0;

    /// <summary>销量基础系数：售出量 = sqrt(库存) × 该值，再随机 ±50%。</summary>
    public const double SellAmountFactor = 1.5;

    /// <summary>运气与天气合成的结算倍率。</summary>
    public static double Modifier(double dailyLuck, bool goodWeather)
    {
        var luckMod = 1.0 + dailyLuck * LuckFactor;
        var weatherMod = goodWeather ? 1.0 : BadWeatherModifier;
        return luckMod * weatherMod;
    }

    /// <summary>
    /// 定价因子 = 原版售价 / 现价。开张率与销量都乘以它（均与定价负相关）：
    /// 高价单件利厚但出货慢，低价快速清仓单件薄利。
    /// 现价非法（≤0）时返回 0，避免除零；调用方据此跳过该摊位。
    /// </summary>
    public static double PriceFactor(int storePrice, int price)
    {
        if (price <= 0)
            return 0;
        return (double)Math.Max(1, storePrice) / price;
    }

    /// <summary>开张概率：基础概率 × 结算倍率 × 定价因子，钳制在 [0, 1]。</summary>
    public static double OpenChance(double baseSellChance, double modifier, double priceFactor)
    {
        return Math.Clamp(baseSellChance * modifier * priceFactor, 0, 1);
    }

    /// <summary>
    /// 今晚的目标销量：sqrt(库存) × 系数，随机 ±50%，再乘结算倍率与定价因子。
    /// 返回值不保证 ≤ 库存，调用方按库存封顶（清仓式销售）。
    /// roll 为 [0,1) 的随机数，用于在 ±50% 区间内取值。
    /// </summary>
    public static int TargetSellAmount(int totalStock, double modifier, double priceFactor, double roll)
    {
        if (totalStock <= 0)
            return 0;

        var baseAmount = Math.Sqrt(totalStock) * SellAmountFactor;
        var low = (int)(baseAmount * 0.5);
        var high = (int)(baseAmount * 1.5);
        // 与原实现 Random.Next(low, high) 保持一致：上界不包含，取值区间是 [low, high-1]。
        // 库存极少时 low 可能等于 high，此时 Next(min, max) 会抛异常，这里退化为取 low。
        var span = high - low;
        if (span <= 0)
            return (int)(low * modifier * priceFactor);
        var index = low + (int)(span * Math.Clamp(roll, 0, 0.999999));
        return (int)(index * modifier * priceFactor);
    }
}
