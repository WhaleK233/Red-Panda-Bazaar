using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.PlayerStall;

/// <summary>玩家摊位系统：支持多个 Action 摊位，各自独立概率和物品。</summary>
public static class PlayerStall {
    private const string SaveKey = StatsKeys.PlayerStallSave;

    public const int CollectPending = -2;

    private class ItemUpdateData {
        public string Id { get; set; } = "";
        public int Amount { get; set; }
        public int Price { get; set; }
    }

    private class CollectResultData {
        public long PlayerId { get; set; }
        public int NetEarnings { get; set; }
        public int CollectDay { get; set; }
    }

    private static readonly Dictionary<string, StallConfig> RegisteredStalls = new();
    private static StallSaveData Data { get; set; } = new();

    /// <summary>摊位图块坐标缓存（进入地图时扫描 Action 属性获得）。</summary>
    private static readonly Dictionary<string, Vector2> StallTilePositions = new();

    private const float StallItemScale = 0.75f;


    /// <summary>注册结算账单的 Tile Action。</summary>
    public static void RegisterSettlement() {
        GameLocation.RegisterTileAction("RedPandaBazaar_PlayerStallSettlement", (_, _, _, _) => {
            Game1.activeClickableMenu = new SettlementMenu();
            return false;
        });
    }

    /// <summary>注册所有摊位和结算。</summary>
    public static void RegisterAll() {
        for (var i = 1; i <= 20; i++)
            RegisterStallAction($"RedPandaBazaar_PlayerStall_{i}", 0.30);
        RegisterSettlement();
    }


    public static void Init() {
        Tools.Log("PlayerStall Initializing.");
        ItemSerializer.WarmUp();
        RegisterAll();
        ModInfo.Helper.Events.Input.ButtonPressed += OnButtonPressed;
        ModInfo.Helper.Events.GameLoop.DayEnding += OnDayEnding;
        ModInfo.Helper.Events.GameLoop.Saving += OnSaving;
        ModInfo.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        ModInfo.Helper.Events.GameLoop.DayStarted += OnDayStarted;
        ModInfo.Helper.Events.Player.Warped += OnPlayerWarped;
        ModInfo.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
        ModInfo.Helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        Tools.Log("PlayerStall Initialized.");
    }

    /// <summary>注册一个摊位 Tile Action。</summary>
    public static void RegisterStallAction(string actionId, double baseChance) {
        RegisteredStalls[actionId] = new StallConfig {
            ActionId = actionId,
            BaseSellChance = Math.Clamp(baseChance, 0.01, 1.0)
        };

        GameLocation.RegisterTileAction(actionId, (_, _, _, _) => { return false; });
    }

    /// <summary>基于鼠标位置打开摊位菜单，避免走 TileAction 的朝向判断。</summary>
    private static void OnButtonPressed(object? sender, ButtonPressedEventArgs e) {
        if (!Tools.IsValidButtonAction(e)) return;
        if (Game1.currentLocation is not { Name: "Custom_FarmerShop1" }) return;

        var tile = e.Cursor.GrabTile;
        var action = Game1.currentLocation.doesTileHaveProperty(
            (int)tile.X, (int)tile.Y, "Action", "Buildings");
        if (action == null || !action.StartsWith("RedPandaBazaar_PlayerStall_")) return;

        Game1.activeClickableMenu = new PlayerStallMenu(action);
        ModInfo.Helper.Input.Suppress(e.Button);
    }

    private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) {
        Data = new StallSaveData();

        // 存档只在主机电脑上可读写，远程客机靠 DayStarted 的同步请求获取数据
        if (!Context.IsOnHostComputer) return;

        Data = ModInfo.Helper.Data.ReadSaveData<StallSaveData>(SaveKey) ?? new StallSaveData();
        PurgeInvalidSnapshots();
    }

    /// <summary>丢弃快照无法还原的条目（旧存档只存了 ItemId，没有 ItemXml）。</summary>
    private static void PurgeInvalidSnapshots() {
        foreach (var actionId in Data.Stock.Keys.ToList()) {
            Data.Stock[actionId].RemoveAll(i => i.GetItem() == null);
            if (Data.Stock[actionId].Count == 0) Data.Stock.Remove(actionId);
        }

        Data.SoldRecords.RemoveAll(r => r.GetItem() == null);
        Data.LastSoldRecords?.RemoveAll(r => r.GetItem() == null);
    }

    /// <summary>新的一天开始，客机向主机请求最新内存数据。</summary>
    private static void OnDayStarted(object? sender, DayStartedEventArgs e) {
        if (!Context.IsMainPlayer) {
            ModInfo.Helper.Multiplayer.SendMessage(
                true, MPMessageType.PlayerStall_SyncRequest,
                modIDs: new[] { ModInfo.ModManifest.UniqueID },
                playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
        }
    }

    private static void OnSaving(object? sender, SavingEventArgs e) {
        if (Context.IsMainPlayer) WriteSaveData();
    }

    /// <summary>立即写入存档。</summary>
    public static void WriteSaveData() {
        // 存档只写 ItemXml 字符串：落盘前统一把内存里的物品重拍快照，
        // 否则任何漏拍路径（如部分取回被原版原地改小的条目）都会把旧数量写进存档
        foreach (var entry in Data.Stock.Values.SelectMany(list => list))
            entry.RefreshSnapshot();
        ModInfo.Helper.Data.WriteSaveData(SaveKey, Data);
    }

    /// <summary>获取指定摊位的在售物品副本（跳过快照失效的条目）。</summary>
    public static List<StallItem> GetItems(string actionId) {
        if (!Data.Stock.TryGetValue(actionId, out var list)) return new();
        return list.Where(i => i.GetItem() != null).ToList();
    }

    /// <summary>上架物品到指定摊位（乐观本地更新 + 主机权威同步）。数量取自物品自身的 Stack。</summary>
    public static bool AddItem(string actionId, Item item, int price) {
        if (item == null || item.Stack <= 0 || price < 1 || !RegisteredStalls.ContainsKey(actionId)) return false;

        // 摊位已有物品时必须可叠加，canStackWith 内含品质、染色、orderData、名称比对
        var existing = GetItems(actionId).FirstOrDefault();
        if (existing != null) {
            var stocked = existing.GetItem();
            if (stocked == null || !stocked.canStackWith(item)) return false;
        }

        var xml = ItemSerializer.Serialize(item);
        if (xml == null) return false;

        var stallItem = new StallItem {
            Id = Guid.NewGuid().ToString(),
            ActionId = actionId,
            ItemXml = xml,
            Price = price
        };
        if (!Data.Stock.TryGetValue(actionId, out var list))
            Data.Stock[actionId] = list = new();
        list.Add(stallItem);
        Tools.SendToHostOrBroadcast(stallItem, MPMessageType.PlayerStall_AddItem);
        return true;
    }

    /// <summary>直接删除物品实例，避免按 ID 查询匹配错位。</summary>
    public static void RemoveItem(StallItem item) {
        if (item == null) return;
        if (Data.Stock.TryGetValue(item.ActionId, out var list)) {
            list.Remove(item);
            if (list.Count == 0) Data.Stock.Remove(item.ActionId);
        }

        Tools.SendToHostOrBroadcast(item.Id, MPMessageType.PlayerStall_RemoveItem);
    }

    /// <summary>更新物品数量/价格（乐观本地更新 + 主机权威同步）。</summary>
    public static void UpdateItem(string stallItemId, int amount, int price) {
        var entry = Data.Stock.SelectMany(kv => kv.Value).FirstOrDefault(i => i.Id == stallItemId);
        if (entry?.GetItem() is not { } stocked) return;

        stocked.Stack = amount;
        entry!.RefreshSnapshot();
        entry.Price = price;
        Tools.SendToHostOrBroadcast(
            new ItemUpdateData { Id = stallItemId, Amount = amount, Price = price },
            MPMessageType.PlayerStall_UpdateItem);
    }

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
    public static int TryCollectToday() {
        if (!Context.IsMainPlayer) {
            ModInfo.Helper.Multiplayer.SendMessage(
                true, MPMessageType.PlayerStall_CollectRequest,
                modIDs: new[] { ModInfo.ModManifest.UniqueID },
                playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
            return CollectPending;
        }

        return TryCollectTodayHost();
    }

    private static int TryCollectTodayHost() {
        if (Data.CollectDay == Game1.stats.DaysPlayed) return -1;

        var gross = Data.SoldRecords.Sum(r => r.Price * (r.GetItem()?.Stack ?? 0));
        if (gross <= 0) return -1;

        Data.CollectDay = (int)Game1.stats.DaysPlayed;
        Data.LastSoldRecords = Data.SoldRecords.ToList();
        Data.SoldRecords.Clear();

        // 通过 SMAPI 多人消息通知其他玩家
        Tools.SendToHostOrBroadcast(Data.CollectDay, MPMessageType.PlayerStall_Collect);

        return gross;
    }

    /// <summary>接收其他玩家的同步消息。主机负责接收客机请求并转发给所有客机；客机直接应用主机广播。</summary>
    private static void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e) {
        if (e.FromModID != ModInfo.ModManifest.UniqueID) return;

        switch (e.Type) {
            case MPMessageType.PlayerStall_Collect:
                if (!Context.IsMainPlayer && e.FromPlayerID != Game1.MasterPlayer.UniqueMultiplayerID)
                    break;
                Data.CollectDay = e.ReadAs<int>();
                Data.LastSoldRecords = Data.SoldRecords.ToList();
                Data.SoldRecords.Clear();
                break;

            case MPMessageType.PlayerStall_CollectRequest: {
                if (!Context.IsMainPlayer) break;
                var net = TryCollectTodayHost();
                var result = new CollectResultData {
                    PlayerId = e.FromPlayerID,
                    NetEarnings = net,
                    CollectDay = Data.CollectDay
                };
                ModInfo.Helper.Multiplayer.SendMessage(
                    result, MPMessageType.PlayerStall_CollectResult,
                    modIDs: new[] { ModInfo.ModManifest.UniqueID },
                    playerIDs: new[] { e.FromPlayerID });
                break;
            }

            case MPMessageType.PlayerStall_CollectResult: {
                var result = e.ReadAs<CollectResultData>();
                if (result == null) break;
                if (result.PlayerId != Game1.player.UniqueMultiplayerID) break;
                if (result.NetEarnings >= 0) {
                    Game1.player.Money += result.NetEarnings;
                    Game1.playSound("coin");
                }

                break;
            }

            case MPMessageType.PlayerStall_SyncRequest: {
                if (!Context.IsMainPlayer) break;
                ModInfo.Helper.Multiplayer.SendMessage(
                    Data, MPMessageType.PlayerStall_SyncData,
                    modIDs: new[] { ModInfo.ModManifest.UniqueID },
                    playerIDs: new[] { e.FromPlayerID });
                break;
            }

            case MPMessageType.PlayerStall_SyncData: {
                if (Context.IsMainPlayer) break;
                if (e.FromPlayerID != Game1.MasterPlayer.UniqueMultiplayerID) break;
                var syncData = e.ReadAs<StallSaveData>();
                if (syncData != null) Data = syncData;
                break;
            }

            case MPMessageType.PlayerStall_AddItem: {
                var newItem = e.ReadAs<StallItem>();
                if (newItem == null) break;
                if (!Data.Stock.TryGetValue(newItem.ActionId, out var list))
                    Data.Stock[newItem.ActionId] = list = new();
                if (!list.Any(i => i.Id == newItem.Id))
                    list.Add(newItem);

                if (Context.IsMainPlayer) {
                    ModInfo.Helper.Multiplayer.SendMessage(newItem, MPMessageType.PlayerStall_AddItem,
                        modIDs: new[] { ModInfo.ModManifest.UniqueID });
                }

                break;
            }

            case MPMessageType.PlayerStall_RemoveItem: {
                var removeId = e.ReadAs<string>();
                foreach (var list in Data.Stock.Values)
                    list.RemoveAll(i => i.Id == removeId);
                // 清理空摊位
                var emptyKeys = Data.Stock.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key).ToList();
                foreach (var key in emptyKeys) Data.Stock.Remove(key);

                if (Context.IsMainPlayer) {
                    ModInfo.Helper.Multiplayer.SendMessage(removeId, MPMessageType.PlayerStall_RemoveItem,
                        modIDs: new[] { ModInfo.ModManifest.UniqueID });
                }

                break;
            }

            case MPMessageType.PlayerStall_UpdateItem: {
                var update = e.ReadAs<ItemUpdateData>();
                if (update == null) break;
                var target = Data.Stock.SelectMany(kv => kv.Value).FirstOrDefault(i => i.Id == update.Id);
                if (target?.GetItem() is not { } stocked) break;
                stocked.Stack = update.Amount;
                target!.RefreshSnapshot();
                target.Price = update.Price;
                if (Context.IsMainPlayer) {
                    ModInfo.Helper.Multiplayer.SendMessage(update, MPMessageType.PlayerStall_UpdateItem,
                        modIDs: new[] { ModInfo.ModManifest.UniqueID });
                }

                break;
            }
        }
    }

    /// <summary>进入摊位地图时扫描所有 tile，缓存 Action 对应的图块坐标。</summary>
    private static void OnPlayerWarped(object? sender, WarpedEventArgs e) {
        if (e.NewLocation.Name != "Custom_FarmerShop1") {
            // 离开地图时清理缓存
            if (StallTilePositions.Count > 0 && e.OldLocation?.Name == "Custom_FarmerShop1")
                StallTilePositions.Clear();
            return;
        }

        StallTilePositions.Clear();
        var map = e.NewLocation.Map;
        if (map?.Layers == null || map.Layers.Count == 0) return;

        var width = map.Layers[0].LayerWidth;
        var height = map.Layers[0].LayerHeight;

        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++) {
            var action = e.NewLocation.doesTileHaveProperty(x, y, "Action", "Buildings");
            if (action == null || !action.StartsWith("RedPandaBazaar_PlayerStall_")) continue;
            StallTilePositions[action] = new Vector2(x, y);
        }
    }

    /// <summary>在地图摊位 tile 中心绘制该摊位的物品图标和价格。</summary>
    private static void OnRenderedWorld(object? sender, RenderedWorldEventArgs e) {
        if (!Context.IsWorldReady) return;
        if (Game1.currentLocation?.Name != "Custom_FarmerShop1") return;
        if (StallTilePositions.Count == 0) return;

        var b = e.SpriteBatch;

        foreach (var kv in StallTilePositions) {
            var actionId = kv.Key;
            var tilePos = kv.Value;

            if (!Data.Stock.TryGetValue(actionId, out var stock)) continue;
            var obj = stock.Select(i => i.GetItem()).FirstOrDefault(i => i != null);
            if (obj == null) continue;

            var screenPos = Game1.GlobalToLocal(Game1.viewport,
                new Vector2(tilePos.X * 64f - 8f, tilePos.Y * 64f - 16f));

            // tile 中心绘制物品（64x64 居中）
            var offset = (64 - 64 * StallItemScale) / 2;
            obj.drawInMenu(b, screenPos + new Vector2(offset, offset), StallItemScale);
        }
    }

    /// <summary>
    /// 夜间结算：各摊位按概率触发销售。
    /// 售出量 = sqrt(该摊位总库存) × 1.5，再随机 ±50%，受运气/天气影响。
    /// 库存越多卖得越多，但增速递减。
    /// </summary>
    private static void OnDayEnding(object? sender, DayEndingEventArgs e) {
        // 仅主机执行结算，客机通过同步消息获取结果
        if (!Context.IsMainPlayer) return;

        // 用存档 ID + 天数做种子，同一天内所有玩家随机序列一致
        var rnd = new RandomUtils.RandomSeed("PlayerStall");
        var luckMod = 1.0 + Game1.player.DailyLuck * 2;
        var weatherMod = Tools.IsGoodWeather() ? 1.0 : 0.85;
        var factor = luckMod * weatherMod;
        var totalSold = 0;

        foreach (var (actionId, config) in RegisteredStalls) {
            if (!Data.Stock.TryGetValue(actionId, out var stock) || stock.Count == 0) continue;

            // 定价因子 = 原版售价/现价，开张率与夜销销量同乘（均负相关）：
            // 定价是周转率旋钮——高价单件利厚但出货慢（吞吐 ∝ 1/定价²），低价快速清仓单件薄利；
            // 开张率约在 0.3× 定价以下封顶 100%，销量始终受当日库存封顶
            var first = stock.FirstOrDefault(i => i.GetItem() != null);
            if (first == null) continue;
            var priceFactor = (double)Math.Max(1, first.GetItem()!.sellToStorePrice()) / first.Price;
            var chance = config.BaseSellChance * luckMod * weatherMod * priceFactor;
            chance = Math.Clamp(chance, 0, 1);

            // 随机值 >= 概率则该摊位今晚不开张
            if (rnd.NextDouble() >= chance) continue;

            // Fisher-Yates 洗牌副本，确保每次售出物品来源均匀随机
            var unsold = stock.Where(i => i.GetItem() != null).ToList();
            for (var i = unsold.Count - 1; i > 0; i--) {
                var j = rnd.Next(i + 1);
                (unsold[i], unsold[j]) = (unsold[j], unsold[i]);
            }

            var totalStock = unsold.Sum(i => i.GetItem()!.Stack);
            var baseAmount = Math.Sqrt(totalStock) * 1.5;
            // 夜销销量同样乘定价因子（负相关），实际售出受当日库存封顶（清仓式销售）
            var targetSell = (int)(rnd.Next((int)(baseAmount * 0.5), (int)(baseAmount * 1.5)) * factor * priceFactor);
            var remaining = targetSell;
            var soldCount = 0;

            foreach (var item in unsold) {
                if (remaining <= 0) break;

                var source = item.GetItem()!;

                // 从当前条目中扣减，不够则取全部剩余
                var sellAmount = Math.Min(remaining, source.Stack);
                source.Stack -= sellAmount;
                remaining -= sellAmount;
                soldCount += sellAmount;

                // 写入售出记录快照，供结算菜单展示；快照的 Stack 即本次售出数量
                var snapshot = source.getOne();
                snapshot.Stack = sellAmount;
                Data.SoldRecords.Add(new SoldRecord {
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
            if (stock.Count == 0) Data.Stock.Remove(actionId);

            totalSold += soldCount;
            Tools.Log($"[{actionId}] 触发销售，售出 {soldCount} 件");
        }

        if (totalSold > 0)
            Tools.Log($"[PlayerStall] 当日共售出 {totalSold} 件物品");
    }
}