using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace Red_Panda_Bazaar_Code.Features.PrizeMachines;

/// <summary>
/// Joja 抽奖机：消耗 Joja 抽奖券。
/// 比经典机多一个空闲态效果——没在抽奖时，每隔 150ms 换一个奖品在轨道上闪烁预览。
/// </summary>
public class JojaPrizeMachineMenu : PrizeMachineMenuBase
{
    public Item flashingItem = null!; // 首次绘制前必然在 draw 中赋值，但声明处不预置会引发可空警告
    public float flashTimer;

    protected override string TexturePath => "assets/Textures/RedPandaBazaar_PrizeTicketMenu_2.png";

    protected override Color TicketCountColor => new(11, 241, 239);

    /// <summary>按 Joja 奖池随机生成一个奖品。</summary>
    protected override Item NewPrizeItem()
    {
        var commonList = PrizeMachines.JojaCommonPrizeList;
        var couponList = PrizeMachines.JojaCouponPrizeList;
        var chance = RandomUtils.NextDouble();
        var random = RandomUtils.Next();
        int i, amount;
        string itemId;
        Item prize;
        switch (chance)
        {
            case < 0.01: // 1% 自动抚摸机
                prize = ItemRegistry.Create(ItemsKeys.Vanilla.AutoPetter);
                break;
            case < 0.03: // 2% 五彩碎片
                prize = ItemRegistry.Create(ItemsKeys.Vanilla.PrismaticShard);
                break;
            case < 0.13: // 10% JOJA兑换券
                prize = ItemRegistry.Create(ItemsKeys.Tickets.JojaRedemptionCoupon);
                break;
            case < 0.33: // 20% 18种兑换券 2张
                i = random % couponList.Count;
                itemId = couponList[i].Item1;
                amount = couponList[i].Item2;
                prize = ItemRegistry.Create(itemId, amount);
                break;
            default: // 67% 其他物品
                i = random % commonList.Count;
                itemId = commonList[i].Item1;
                amount = commonList[i].Item2;
                prize = ItemRegistry.Create(itemId, amount);
                break;
        }

        return prize;
    }

    protected override int CountTickets() => Game1.player.Items.CountId(ItemsKeys.Tickets.JojaTicket);

    protected override void ConsumeOneTicket() => Game1.player.Items.ReduceId(ItemsKeys.Tickets.JojaTicket, 1);

    public override void update(GameTime time)
    {
        // 与原实现保持一致：flashTimer 在 base.update 之前递减
        if (flashTimer > 0.0)
            flashTimer -= (float)(int)time.ElapsedGameTime.TotalMilliseconds;

        base.update(time);
    }

    /// <summary>抽奖中绘制飞出的奖品；空闲时绘制闪烁预览。</summary>
    protected override void DrawPrize(SpriteBatch b)
    {
        if (this.gettingReward)
        {
            base.DrawPrize(b);
            return;
        }

        // 没在抽奖时，闪烁预览奖品
        if (flashTimer <= 0.0f)
        {
            flashingItem = NewPrizeItem();
            flashTimer = 150f;
        }

        Vector2 v2 = new Vector2(52f, 21f) * 4f;
        flashingItem.drawInMenu(b, this.Position + v2, 1f, 1f, 0.9f, StackDrawType.Draw, Color.White, false);
    }
}
