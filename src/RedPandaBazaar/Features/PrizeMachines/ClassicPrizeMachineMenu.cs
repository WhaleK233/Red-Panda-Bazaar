using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.PrizeMachines;

/// <summary>经典抽奖机：消耗经典抽奖券（无则消耗通用抽奖券）。</summary>
public class ClassicPrizeMachineMenu : PrizeMachineMenuBase
{
    protected override string TexturePath => "assets/Textures/RedPandaBazaar_PrizeTicketMenu_1.png";

    protected override Color TicketCountColor => new(159, 144, 118);

    /// <summary>按经典奖池随机生成一个奖品。</summary>
    protected override Item NewPrizeItem()
    {
        var commonList = PrizeMachines.CommonPrizeList;
        var couponList = PrizeMachines.CouponPrizeList;
        var chance = RandomUtils.NextDouble();
        var random = RandomUtils.Next();
        int i, amount;
        string itemId;
        Item prize;
        switch (chance)
        {
            case < 0.01: // 1% 古代种子
                prize = ItemRegistry.Create(ItemsKeys.Vanilla.AncientSeed);
                break;
            case < 0.11: // 10% 星之果茶
                prize = ItemRegistry.Create(ItemsKeys.Vanilla.StardropTea);
                break;
            case < 0.31: // 20% 18种兑换券
                i = random % couponList.Count;
                itemId = couponList[i].Item1;
                amount = couponList[i].Item2;
                prize = ItemRegistry.Create(itemId, amount);
                break;
            default: // 69% 其他物品
                i = random % commonList.Count;
                itemId = commonList[i].Item1;
                amount = commonList[i].Item2;
                prize = ItemRegistry.Create(itemId, amount);
                break;
        }

        return prize;
    }

    protected override int CountTickets() =>
        Game1.player.Items.CountId(ItemsKeys.Tickets.ClassicTicket)
        + Game1.player.Items.CountId(ItemsKeys.Tickets.GenericTicket);

    /// <summary>优先消耗经典抽奖券，没有时消耗通用抽奖券。</summary>
    protected override void ConsumeOneTicket()
    {
        if (Game1.player.Items.CountId(ItemsKeys.Tickets.ClassicTicket) > 0)
        {
            Game1.player.Items.ReduceId(ItemsKeys.Tickets.ClassicTicket, 1);
        }
        else
        {
            Game1.player.Items.ReduceId(ItemsKeys.Tickets.GenericTicket, 1);
        }
    }
}
