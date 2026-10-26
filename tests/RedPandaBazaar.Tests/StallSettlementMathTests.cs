using Red_Panda_Bazaar_Code.Features.PlayerStall;
using Xunit;

namespace Red_Panda_Bazaar_Code.Tests;

/// <summary>
/// 摊位夜间结算的纯计算测试。
/// 源文件以链接方式编译（见 csproj），不依赖游戏程序集。
/// </summary>
public class StallSettlementMathTests
{
    // ── Modifier：运气与天气合成 ──────────────────────────────

    [Fact]
    public void Modifier_NoLuckAndGoodWeather_IsOne()
    {
        Assert.Equal(1.0, StallSettlementMath.Modifier(0, goodWeather: true), 10);
    }

    [Fact]
    public void Modifier_BadWeatherAppliesDiscount()
    {
        Assert.Equal(StallSettlementMath.BadWeatherModifier,
            StallSettlementMath.Modifier(0, goodWeather: false), 10);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(-0.1)]
    public void Modifier_LuckScalesLinearly(double luck)
    {
        var expected = 1.0 + luck * StallSettlementMath.LuckFactor;
        Assert.Equal(expected, StallSettlementMath.Modifier(luck, goodWeather: true), 10);
    }

    [Fact]
    public void Modifier_BadLuckCanZeroOutOrGoNegative()
    {
        // DailyLuck 下限约 -0.1，此时倍率为 0.8，仍为正数
        Assert.True(StallSettlementMath.Modifier(-0.1, goodWeather: true) > 0);
        // 但极端负运气叠加坏天气会显著压低，这是设计允许的
        Assert.True(StallSettlementMath.Modifier(-0.1, goodWeather: false) < 1);
    }

    // ── PriceFactor：定价因子 ────────────────────────────────

    [Theory]
    [InlineData(100, 100, 1.0)]    // 按原价卖：因子 1
    [InlineData(100, 200, 0.5)]    // 高价：因子 < 1，出货慢
    [InlineData(100, 50, 2.0)]     // 低价：因子 > 1，清仓快
    public void PriceFactor_IsStorePriceOverPrice(int storePrice, int price, double expected)
    {
        Assert.Equal(expected, StallSettlementMath.PriceFactor(storePrice, price), 10);
    }

    [Fact]
    public void PriceFactor_ZeroStorePriceClampsToOne()
    {
        // 售价为 0 的物品按 1 计，避免因子归零导致永不开张
        Assert.Equal(0.1, StallSettlementMath.PriceFactor(0, 10), 10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void PriceFactor_InvalidPriceReturnsZero(int price)
    {
        // 现价非法时返回 0 而不是抛除零异常，调用方据此跳过该摊位
        Assert.Equal(0, StallSettlementMath.PriceFactor(100, price), 10);
    }

    // ── OpenChance：开张概率 ─────────────────────────────────

    [Fact]
    public void OpenChance_MultipliesAllInputs()
    {
        Assert.Equal(0.15, StallSettlementMath.OpenChance(0.3, 1.0, 0.5), 10);
    }

    [Fact]
    public void OpenChance_ClampsToOne()
    {
        // 低价（因子 4）会把基础概率放大到超过 100%
        Assert.Equal(1.0, StallSettlementMath.OpenChance(0.3, 1.0, 4.0), 10);
    }

    [Fact]
    public void OpenChance_ClampsToZero()
    {
        Assert.Equal(0.0, StallSettlementMath.OpenChance(0.3, 1.0, 0.0), 10);
    }

    [Fact]
    public void OpenChance_NeverNegative()
    {
        // 极端负运气会让乘积为负，必须钳到 0
        Assert.Equal(0.0, StallSettlementMath.OpenChance(0.3, -5.0, 1.0), 10);
    }

    // ── TargetSellAmount：销量 ───────────────────────────────

    [Fact]
    public void TargetSellAmount_ZeroStockIsZero()
    {
        Assert.Equal(0, StallSettlementMath.TargetSellAmount(0, 1.0, 1.0, 0.5));
    }

    [Fact]
    public void TargetSellAmount_NegativeStockIsZero()
    {
        Assert.Equal(0, StallSettlementMath.TargetSellAmount(-10, 1.0, 1.0, 0.5));
    }

    [Fact]
    public void TargetSellAmount_RollZeroUsesLowerBound()
    {
        // 库存 100 -> base = 10 * 1.5 = 15 -> low = 7（C# 截断，非四舍五入）
        Assert.Equal(7, StallSettlementMath.TargetSellAmount(100, 1.0, 1.0, 0.0));
    }

    [Fact]
    public void TargetSellAmount_RollNearOneStaysBelowUpperBound()
    {
        // high = 22，与原实现 Random.Next(7, 22) 一致，上界不含，最大取 21
        Assert.Equal(21, StallSettlementMath.TargetSellAmount(100, 1.0, 1.0, 0.999999));
    }

    [Fact]
    public void TargetSellAmount_TinyStockDoesNotThrow()
    {
        // 库存 1 -> base = 1.5 -> low = 0, high = 2。原实现用 Random.Next(low, high)
        // 在 low == high 时会抛异常，这里必须安全返回
        var amount = StallSettlementMath.TargetSellAmount(1, 1.0, 1.0, 0.5);
        Assert.InRange(amount, 0, 2);
    }

    [Fact]
    public void TargetSellAmount_ClampsRollOutsideRange()
    {
        var atLow = StallSettlementMath.TargetSellAmount(100, 1.0, 1.0, -1.0);
        var atHigh = StallSettlementMath.TargetSellAmount(100, 1.0, 1.0, 2.0);
        Assert.Equal(7, atLow);
        // roll 超范围时钳到上界内侧，不会取到 high=22
        Assert.Equal(21, atHigh);
    }

    [Fact]
    public void TargetSellAmount_ScalesWithPriceFactor()
    {
        // 低价（因子 2）销量翻倍
        var normal = StallSettlementMath.TargetSellAmount(100, 1.0, 1.0, 0.5);
        var cheap = StallSettlementMath.TargetSellAmount(100, 1.0, 2.0, 0.5);
        Assert.Equal(normal * 2, cheap);
    }

    [Fact]
    public void TargetSellAmount_ScalesWithModifier()
    {
        var normal = StallSettlementMath.TargetSellAmount(100, 1.0, 1.0, 0.5);
        var lucky = StallSettlementMath.TargetSellAmount(100, 1.2, 1.0, 0.5);
        Assert.True(lucky > normal);
    }

    [Fact]
    public void TargetSellAmount_IsMonotonicInStock()
    {
        // 库存越多卖得越多（增速递减但单调不减）
        var previous = 0;
        foreach (var stock in new[] { 1, 4, 16, 64, 256, 1024 })
        {
            var amount = StallSettlementMath.TargetSellAmount(stock, 1.0, 1.0, 0.5);
            Assert.True(amount >= previous, $"库存 {stock} 的销量 {amount} 低于前一档 {previous}");
            previous = amount;
        }
    }

    // ── 组合行为 ─────────────────────────────────────────────

    [Fact]
    public void FullPipeline_TypicalStallProducesReasonableSale()
    {
        // 基础概率 0.3、运气 0、好天气、按原价卖、库存 100
        const double modifier = 1.0;
        var priceFactor = StallSettlementMath.PriceFactor(storePrice: 50, price: 50);
        var chance = StallSettlementMath.OpenChance(0.3, modifier, priceFactor);
        var amount = StallSettlementMath.TargetSellAmount(100, modifier, priceFactor, 0.5);

        Assert.Equal(1.0, priceFactor, 10);
        Assert.Equal(0.3, chance, 10);
        Assert.InRange(amount, 7, 21);
    }
}
