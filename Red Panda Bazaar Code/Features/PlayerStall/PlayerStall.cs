using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.PlayerStall;

/// <summary>玩家摊位系统：支持多个 Action 摊位，各自独立概率和物品。</summary>
public static class PlayerStall {
    private const string SaveKey = "WhaleK233.RedPandaBazaar.PlayerStall";

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
    public static IReadOnlyDictionary<string, StallConfig> StallConfigs => RegisteredStalls;

    /// <summary>摊位图块坐标缓存（进入地图时扫描 Action 属性获得）。</summary>
    private static readonly Dictionary<string, Vector2> StallTilePositions = new();

    private const float StallItemScale = 0.75f;

    internal static IReadOnlyDictionary<string, Vector2> StallTiles => StallTilePositions;

    /// <summary>摊位显示的物品图标缓存，库存变化时失效。</summary>
    private static readonly Dictionary<string, Item> DisplayItemCache = new();


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
        RegisterAll();
        Tools.Helper.Events.Input.ButtonPressed += OnButtonPressed;
        Tools.Helper.Events.GameLoop.DayEnding += OnDayEnding;
        Tools.Helper.Events.GameLoop.Saving += OnSaving;
        Tools.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        Tools.Helper.Events.GameLoop.DayStarted += OnDayStarted;
        Tools.Helper.Events.Player.Warped += OnPlayerWarped;
        Tools.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
        Tools.Helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
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
        Tools.Helper.Input.Suppress(e.Button);
    }

    private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) {
        Data = Tools.Helper.Data.ReadSaveData<StallSaveData>(SaveKey) ?? new StallSaveData();
    }

    /// <summary>新的一天开始，客机向主机请求最新内存数据。</summary>
    private static void OnDayStarted(object? sender, DayStartedEventArgs e) {
        if (!Context.IsMainPlayer) {
            Tools.Helper.Multiplayer.SendMessage(
                true, MPMessageType.PlayerStall_SyncRequest,
                modIDs: new[] { Tools.ModManifest.UniqueID },
                playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
        }
    }

    private static void OnSaving(object? sender, SavingEventArgs e) {
        if (Context.IsMainPlayer) WriteSaveData();
    }

    /// <summary>立即写入存档。</summary>
    public static void WriteSaveData() {
        Tools.Helper.Data.WriteSaveData(SaveKey, Data);
    }

    /// <summary>获取指定摊位的在售物品副本。</summary>
    public static List<StallItem> GetItems(string actionId) {
        return Data.Stock.TryGetValue(actionId, out var list) ? list.ToList() : new();
    }

    /// <summary>上架物品到指定摊位（乐观本地更新 + 主机权威同步）。</summary>
    public static bool AddItem(string actionId, string itemId, int amount, int price) {
        if (amount <= 0 || price < 1 || !RegisteredStalls.ContainsKey(actionId)) return false;
        if (Data.Stock.TryGetValue(actionId, out var existing) && existing.Count > 0 && existing[0].ItemId != itemId)
            return false;
        var item = new StallItem {
            Id = Guid.NewGuid().ToString(),
            ActionId = actionId,
            ItemId = itemId,
            Amount = amount,
            Price = price
        };
        if (!Data.Stock.TryGetValue(actionId, out var list))
            Data.Stock[actionId] = list = new();
        list.Add(item);
        DisplayItemCache.Remove(actionId);
        Tools.SendToHostOrBroadcast(item, MPMessageType.PlayerStall_AddItem);
        return true;
    }

    /// <summary>从摊位下架物品（乐观本地更新 + 主机权威同步）。</summary>
    public static bool RemoveItem(string actionId, int index) {
        if (!Data.Stock.TryGetValue(actionId, out var list)) return false;
        if (index < 0 || index >= list.Count) return false;
        var itemId = list[index].Id;
        list.RemoveAt(index);
        if (list.Count == 0) Data.Stock.Remove(actionId);
        DisplayItemCache.Remove(actionId);
        Tools.SendToHostOrBroadcast(itemId, MPMessageType.PlayerStall_RemoveItem);
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
        DisplayItemCache.Remove(item.ActionId);
    }

    /// <summary>更新物品数量/价格（乐观本地更新 + 主机权威同步）。</summary>
    public static void UpdateItem(string stallItemId, int amount, int price) {
        var item = Data.Stock.SelectMany(kv => kv.Value).FirstOrDefault(i => i.Id == stallItemId);
        if (item == null) return;
        item.Amount = amount;
        item.Price = price;
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

    /// <summary>累计历史税收总额。</summary>
    public static int TotalTax => Data?.TotalTax ?? 0;

    /// <summary>追加税收到累计总额。</summary>
    public static void AddTax(int amount) {
        if (amount > 0) Data.TotalTax += amount;
    }

    /// <summary>今日账单是否已收取。</summary>
    public static bool IsCollectedToday => Data?.CollectDay == Game1.stats.DaysPlayed;

    /// <summary>尝试收取今日账单。主机结算并广播，客机仅发送请求。</summary>
    public static int TryCollectToday() {
        if (!Context.IsMainPlayer) {
            Tools.Helper.Multiplayer.SendMessage(
                true, MPMessageType.PlayerStall_CollectRequest,
                modIDs: new[] { Tools.ModManifest.UniqueID },
                playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
            return CollectPending;
        }

        return TryCollectTodayHost();
    }

    private static int TryCollectTodayHost() {
        if (Data.CollectDay == Game1.stats.DaysPlayed) return -1;

        var gross = Data.SoldRecords.Sum(r => r.Price * r.Amount);
        if (gross <= 0) return -1;

        Data.CollectDay = (int)Game1.stats.DaysPlayed;
        Data.LastSoldRecords = Data.SoldRecords.ToList();
        Data.SoldRecords.Clear();

        // 通过 SMAPI 多人消息通知其他玩家
        Tools.SendToHostOrBroadcast(Data.CollectDay, MPMessageType.PlayerStall_Collect);

        var tax = (int)Math.Round(gross * Tools.ModConfig.TaxRate);
        Data.TotalTax += tax;
        return gross - tax;
    }

    /// <summary>接收其他玩家的同步消息。主机负责接收客机请求并转发给所有客机；客机直接应用主机广播。</summary>
    private static void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e) {
        if (e.FromModID != Tools.ModManifest.UniqueID) return;

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
                Tools.Helper.Multiplayer.SendMessage(
                    result, MPMessageType.PlayerStall_CollectResult,
                    modIDs: new[] { Tools.ModManifest.UniqueID },
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
                Tools.Helper.Multiplayer.SendMessage(
                    Data, MPMessageType.PlayerStall_SyncData,
                    modIDs: new[] { Tools.ModManifest.UniqueID },
                    playerIDs: new[] { e.FromPlayerID });
                break;
            }

            case MPMessageType.PlayerStall_SyncData: {
                if (Context.IsMainPlayer) break;
                if (e.FromPlayerID != Game1.MasterPlayer.UniqueMultiplayerID) break;
                var syncData = e.ReadAs<StallSaveData>();
                if (syncData != null) {
                    Data = syncData;
                    DisplayItemCache.Clear();
                }
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
                    Tools.Helper.Multiplayer.SendMessage(newItem, MPMessageType.PlayerStall_AddItem,
                        modIDs: new[] { Tools.ModManifest.UniqueID });
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
                    Tools.Helper.Multiplayer.SendMessage(removeId, MPMessageType.PlayerStall_RemoveItem,
                        modIDs: new[] { Tools.ModManifest.UniqueID });
                }

                break;
            }

            case MPMessageType.PlayerStall_UpdateItem: {
                var update = e.ReadAs<ItemUpdateData>();
                if (update == null) break;
                var target = Data.Stock.SelectMany(kv => kv.Value).FirstOrDefault(i => i.Id == update.Id);
                if (target == null) break;
                target.Amount = update.Amount;
                target.Price = update.Price;
                if (Context.IsMainPlayer) {
                    Tools.Helper.Multiplayer.SendMessage(update, MPMessageType.PlayerStall_UpdateItem,
                        modIDs: new[] { Tools.ModManifest.UniqueID });
                }

                break;
            }
        }
    }

    /// <summary>清空所有已售记录并返回总收益（仅修改内存，由 OnSaving 写入存档）。</summary>
    public static int ClearSoldItems() {
        var total = Data.SoldRecords.Sum(r => r.Price * r.Amount);
        Data.SoldRecords.Clear();
        Data.TotalEarnings = 0;
        return total;
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

            var stallItem = GetItems(actionId).FirstOrDefault();
            if (stallItem == null) {
                DisplayItemCache.Remove(actionId);
                continue;
            }

            if (!DisplayItemCache.TryGetValue(actionId, out var obj)) {
                obj = ItemRegistry.Create(stallItem.ItemId, 1);
                if (obj == null) continue;
                DisplayItemCache[actionId] = obj;
            }

            var screenPos = Game1.GlobalToLocal(Game1.viewport,
                new Vector2(tilePos.X * 64f - 8f, tilePos.Y * 64f - 16f));

            // tile 中心绘制物品（64x64 居中）
            var offset = (64 - 64 * StallItemScale) / 2;
            obj.drawInMenu(b, screenPos + new Vector2(offset, offset), StallItemScale);
        }
    }

    /// <summary>收取指定摊位的已售金币（仅修改内存）。</summary>
    public static int CollectEarnings(string actionId) {
        var total = 0;
        Data.SoldRecords.RemoveAll(r => {
            if (r.ActionId != actionId) return false;
            total += r.Price * r.Amount;
            return true;
        });
        return total;
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
        var rnd = new Random((int)(Game1.uniqueIDForThisGame ^ Game1.stats.DaysPlayed));
        var luckMod = 1.0 + Game1.player.DailyLuck * 2;
        var weatherMod = Tools.IsGoodWeather() ? 1.0 : 0.85;
        var factor = luckMod * weatherMod;
        var totalSold = 0;

        foreach (var (actionId, config) in RegisteredStalls) {
            // 概率 = 基础概率 × 运气系数 × 天气系数
            var chance = config.BaseSellChance * luckMod * weatherMod;
            chance = Math.Clamp(chance, 0, 1);

            // 随机值 >= 概率则该摊位今晚不开张
            if (rnd.NextDouble() >= chance) continue;

            if (!Data.Stock.TryGetValue(actionId, out var stock) || stock.Count == 0) continue;

            // Fisher-Yates 洗牌副本，确保每次售出物品来源均匀随机
            var unsold = stock.ToList();
            for (var i = unsold.Count - 1; i > 0; i--) {
                var j = rnd.Next(i + 1);
                (unsold[i], unsold[j]) = (unsold[j], unsold[i]);
            }

            var totalStock = unsold.Sum(i => i.Amount);
            var baseAmount = Math.Sqrt(totalStock) * 1.5;
            var targetSell = (int)(rnd.Next((int)(baseAmount * 0.5), (int)(baseAmount * 1.5)) * factor);
            var remaining = targetSell;
            var soldCount = 0;

            foreach (var item in unsold) {
                if (remaining <= 0) break;

                // 从当前条目中扣减，不够则取全部剩余
                var sellAmount = Math.Min(remaining, item.Amount);
                item.Amount -= sellAmount;
                Data.TotalEarnings += item.Price * sellAmount;
                remaining -= sellAmount;
                soldCount += sellAmount;

                // 写入售出记录，供结算菜单展示
                Data.SoldRecords.Add(new SoldRecord {
                    ActionId = item.ActionId,
                    ItemId = item.ItemId,
                    Amount = sellAmount,
                    Price = item.Price,
                    SoldDate = $"{Game1.seasonIndex}_{Game1.Date.DayOfMonth}_{Game1.Date.Year}"
                });

                // 原条目卖空后移除，避免 Amount=0 的空记录留在列表里
                if (item.Amount <= 0)
                    stock.Remove(item);
            }

            // 摊位卖空后清理空列表
            if (stock.Count == 0) Data.Stock.Remove(actionId);

            DisplayItemCache.Remove(actionId);

            totalSold += soldCount;
            Tools.Log($"[{actionId}] 触发销售，售出 {soldCount} 件");
        }

        if (totalSold > 0)
            Tools.Log($"[PlayerStall] 当日共售出 {totalSold} 件物品");
    }
}