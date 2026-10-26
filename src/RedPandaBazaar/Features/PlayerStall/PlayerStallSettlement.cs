using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.PlayerStall;

/// <summary>
/// 摊位结算：夜间按概率触发销售、账单收取。
/// 入口与地图交互见 PlayerStall.cs，数据读写见 PlayerStallData.cs，多人同步见 PlayerStallSync.cs。
/// </summary>
public static partial class PlayerStall
{
    /// <summary>获取售出记录。未收取时返回当前待领取的记录；已收取后当天仍返回最近一次记录。</summary>
    public static List<SoldRecord> GetSoldItems()
    {
        if (Data.SoldRecords.Count > 0)
            return Data.SoldRecords.ToList();
        if (Data.CollectDay == Game1.stats.DaysPlayed && Data.LastSoldRecords != null)
            return Data.LastSoldRecords.ToList();
        return new();
    }

    /// <summary>今日账单是否已收取。</summary>
    public static bool IsCollectedToday => Data?.CollectDay == Game1.stats.DaysPlayed;

    /// <summary>尝试收取今日账单。主机结算并广播，客机仅发送请求。</summary>
    public static int TryCollectToday()
    {
        // 分屏客机与主机同进程，直接执行主机结算；返回值由调用方入账（Game1.player 即分屏玩家自己）
        if (Context.IsOnHostComputer)
            return TryCollectTodayHost();

        ModInfo.Helper.Multiplayer.SendMessage(
            true, MPMessageType.PlayerStall_CollectRequest,
            modIDs: new[] { ModInfo.ModManifest.UniqueID },
            playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
        return CollectPending;
    }

    private static int TryCollectTodayHost()
    {
        if (Data.CollectDay == Game1.stats.DaysPlayed)
            return -1;

        var gross = Data.SoldRecords.Sum(r => r.Price * (r.GetItem()?.Stack ?? 0));
        if (gross <= 0)
            return -1;

        Data.CollectDay = (int)Game1.stats.DaysPlayed;
        Data.LastSoldRecords = Data.SoldRecords.ToList();
        Data.SoldRecords.Clear();

        // 分屏客机触发时经主机屏代发，保证远端客机收到的是主机玩家身份的广播
        RelayOnHostScreen(() =>
            Tools.SendToHostOrBroadcast(Data.CollectDay, MPMessageType.PlayerStall_Collect));

        return gross;
    }

    /// <summary>
    /// 夜间结算：各摊位按概率触发销售。
    /// 售出量 = sqrt(该摊位总库存) × 1.5，再随机 ±50%，受运气/天气影响。
    /// 库存越多卖得越多，但增速递减。
    /// </summary>
    private static void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        // 仅主机执行结算，客机通过同步消息获取结果
        if (!Context.IsMainPlayer)
            return;

        // 用存档 ID + 天数做种子，同一天内所有玩家随机序列一致
        var rnd = new RandomUtils.RandomSeed("PlayerStall");
        var luckMod = 1.0 + Game1.player.DailyLuck * 2;
        var weatherMod = Tools.IsGoodWeather() ? 1.0 : 0.85;
        var factor = luckMod * weatherMod;
        var totalSold = 0;

        foreach (var (actionId, config) in RegisteredStalls)
        {
            if (!Data.Stock.TryGetValue(actionId, out var stock) || stock.Count == 0)
                continue;

            // 定价因子 = 原版售价/现价，开张率与夜销销量同乘（均负相关）：
            // 定价是周转率旋钮——高价单件利厚但出货慢（吞吐 ∝ 1/定价²），低价快速清仓单件薄利；
            // 开张率约在 0.3× 定价以下封顶 100%，销量始终受当日库存封顶
            var first = stock.FirstOrDefault(i => i.GetItem() != null);
            if (first == null)
                continue;
            var priceFactor = (double)Math.Max(1, first.GetItem()!.sellToStorePrice()) / first.Price;
            var chance = config.BaseSellChance * luckMod * weatherMod * priceFactor;
            chance = Math.Clamp(chance, 0, 1);

            // 随机值 >= 概率则该摊位今晚不开张
            if (rnd.NextDouble() >= chance)
                continue;

            // Fisher-Yates 洗牌副本，确保每次售出物品来源均匀随机
            var unsold = stock.Where(i => i.GetItem() != null).ToList();
            for (var i = unsold.Count - 1; i > 0; i--)
            {
                var j = rnd.Next(i + 1);
                (unsold[i], unsold[j]) = (unsold[j], unsold[i]);
            }

            var totalStock = unsold.Sum(i => i.GetItem()!.Stack);
            var baseAmount = Math.Sqrt(totalStock) * 1.5;
            // 夜销销量同样乘定价因子（负相关），实际售出受当日库存封顶（清仓式销售）
            var targetSell = (int)(rnd.Next((int)(baseAmount * 0.5), (int)(baseAmount * 1.5)) * factor * priceFactor);
            var remaining = targetSell;
            var soldCount = 0;

            foreach (var item in unsold)
            {
                if (remaining <= 0)
                    break;

                var source = item.GetItem()!;

                // 从当前条目中扣减，不够则取全部剩余
                var sellAmount = Math.Min(remaining, source.Stack);
                source.Stack -= sellAmount;
                remaining -= sellAmount;
                soldCount += sellAmount;

                // 写入售出记录快照，供结算菜单展示；快照的 Stack 即本次售出数量
                var snapshot = source.getOne();
                snapshot.Stack = sellAmount;
                Data.SoldRecords.Add(new SoldRecord
                {
                    ActionId = item.ActionId,
                    ItemXml = ItemSerializer.Serialize(snapshot) ?? "",
                    Price = item.Price,
                    SoldDate = $"{Game1.seasonIndex}_{Game1.Date.DayOfMonth}_{Game1.Date.Year}"
                });

                // 原条目卖空后移除，否则把扣减后的数量写回快照
                if (source.Stack <= 0)
                    stock.Remove(item);
                else
                    item.RefreshSnapshot();
            }

            // 摊位卖空后清理空列表
            if (stock.Count == 0)
                Data.Stock.Remove(actionId);

            totalSold += soldCount;
            Tools.Log($"[PlayerStall] 摊位 {actionId} 触发销售，售出 {soldCount} 件");
        }

        if (totalSold > 0)
            Tools.Log($"[PlayerStall] 当日共售出 {totalSold} 件物品");
    }
}
