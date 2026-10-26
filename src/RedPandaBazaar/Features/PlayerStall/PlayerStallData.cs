using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.PlayerStall;

/// <summary>
/// 摊位数据的读写与物品操作：注册摊位、读写存档、增删改在售物品。
/// 入口与地图交互见 PlayerStall.cs，多人同步见 PlayerStallSync.cs，夜间结算见 PlayerStallSettlement.cs。
/// </summary>
public static partial class PlayerStall
{
    private const string SaveKey = StatsKeys.PlayerStallSave;

    /// <summary>注册结算账单的 Tile Action。</summary>
    public static void RegisterSettlement()
    {
        GameLocation.RegisterTileAction("RedPandaBazaar_PlayerStallSettlement", (_, _, _, _) =>
        {
            Game1.activeClickableMenu = new SettlementMenu();
            return false;
        });
    }

    /// <summary>注册所有摊位和结算。</summary>
    public static void RegisterAll()
    {
        for (var i = 1; i <= 20; i++)
            RegisterStallAction($"RedPandaBazaar_PlayerStall_{i}", 0.30);
        RegisterSettlement();
    }

    /// <summary>注册一个摊位 Tile Action。</summary>
    public static void RegisterStallAction(string actionId, double baseChance)
    {
        RegisteredStalls[actionId] = new StallConfig
        {
            ActionId = actionId,
            BaseSellChance = Math.Clamp(baseChance, 0.01, 1.0)
        };

        GameLocation.RegisterTileAction(actionId, (_, _, _, _) => { return false; });
    }

    private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        // 分屏玩家中途加入会在本机再次触发本事件：Data 是进程内共享的当日最新状态，
        // 既不能重置也不能按存档重读（那是上次入睡时的版本），否则当日改动整体回滚
        if (Context.IsSplitScreen && !Context.IsMainPlayer)
            return;

        Data = new StallSaveData();
        PendingRollbacks.Clear();
        HostScreenRelay.Clear();

        // 存档只在主机电脑上可读写，远程客机靠 DayStarted 的同步请求获取数据
        if (!Context.IsMainPlayer)
            return;

        Data = ModInfo.Helper.Data.ReadSaveData<StallSaveData>(SaveKey) ?? new StallSaveData();
        PurgeInvalidSnapshots();
    }

    /// <summary>回到标题界面：丢弃乐观操作的回滚登记，闭包里捕获的物品引用一并释放。</summary>
    private static void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        PendingRollbacks.Clear();
        HostScreenRelay.Clear();
    }

    /// <summary>丢弃快照无法还原的条目（旧存档只存了 ItemId，没有 ItemXml）。</summary>
    private static void PurgeInvalidSnapshots()
    {
        foreach (var actionId in Data.Stock.Keys.ToList())
        {
            Data.Stock[actionId].RemoveAll(i => i.GetItem() == null);
            if (Data.Stock[actionId].Count == 0)
                Data.Stock.Remove(actionId);
        }

        Data.SoldRecords.RemoveAll(r => r.GetItem() == null);
        Data.LastSoldRecords?.RemoveAll(r => r.GetItem() == null);
    }

    private static void OnSaving(object? sender, SavingEventArgs e)
    {
        if (Context.IsMainPlayer)
            WriteSaveData();
    }

    /// <summary>立即写入存档。</summary>
    public static void WriteSaveData()
    {
        // 存档只写 ItemXml 字符串：落盘前统一把内存里的物品重拍快照，
        // 否则任何漏拍路径（如部分取回被原版原地改小的条目）都会把旧数量写进存档
        foreach (var entry in Data.Stock.Values.SelectMany(list => list))
            entry.RefreshSnapshot();
        ModInfo.Helper.Data.WriteSaveData(SaveKey, Data);
    }

    /// <summary>获取指定摊位的在售物品副本（跳过快照失效的条目）。</summary>
    public static List<StallItem> GetItems(string actionId)
    {
        if (!Data.Stock.TryGetValue(actionId, out var list))
            return new();
        return list.Where(i => i.GetItem() != null).ToList();
    }

    /// <summary>上架物品到指定摊位（乐观本地更新 + 主机权威同步）。数量取自物品自身的 Stack。</summary>
    public static bool AddItem(string actionId, Item item, int price)
    {
        if (item == null || item.Stack <= 0 || price < 1 || !RegisteredStalls.ContainsKey(actionId))
            return false;

        var stallItem = new StallItem
        {
            Id = Guid.NewGuid().ToString(),
            ActionId = actionId,
            ItemXml = ItemSerializer.Serialize(item) ?? "",
            Price = price
        };
        if (stallItem.ItemXml.Length == 0)
            return false;

        // 分屏客机与主机同进程共享 Data：同步走主机校验即可，无需乐观更新与回滚登记
        if (Context.IsSplitScreen && !Context.IsMainPlayer)
        {
            if (!ValidateAddItem(stallItem))
                return false;
            ApplyAddItem(stallItem);
            RelayOnHostScreen(() => BroadcastAddItem(stallItem));
            return true;
        }

        var list = Data.Stock.TryGetValue(actionId, out var stock) ? stock : Data.Stock[actionId] = new();
        list.Add(stallItem);
        // 客机登记回滚：主机拒绝（并发抢空位等）时把物品原样退回背包，菜单的乐观删除才算安全
        string? requestId = null;
        if (!Context.IsMainPlayer)
        {
            requestId = Guid.NewGuid().ToString();
            PendingRollbacks[requestId] = () => RefundToInventory(item);
        }
        SendAddItemRequest(stallItem, requestId);
        return true;
    }

    /// <summary>直接删除物品实例，避免按 ID 查询匹配错位。</summary>
    public static void RemoveItem(StallItem item)
    {
        if (item == null)
            return;

        // 分屏客机与主机同进程：条目仍在共享 Data 中才移除（防御并发抢空位，菜单读取与点击同帧内不会发生）
        if (Context.IsSplitScreen && !Context.IsMainPlayer)
        {
            if (FindItem(item.Id) == null)
            {
                Tools.Log($"[PlayerStall] 分屏移除摊位条目 {item.Id} 时条目已不存在，跳过。");
                return;
            }
            ApplyRemoveItem(item.Id);
            RelayOnHostScreen(() => BroadcastRemoveItem(item.Id));
            return;
        }

        if (Data.Stock.TryGetValue(item.ActionId, out var list))
        {
            list.Remove(item);
            if (list.Count == 0)
                Data.Stock.Remove(item.ActionId);
        }

        string? requestId = null;
        if (!Context.IsMainPlayer && item.GetItem() is { } taken)
        {
            // 客机登记回滚：主机拒绝（他人先一步取走）时按快照从背包扣回，避免凭空复制。
            // 数量取快照而非实例：全额并入已有堆时原版会把实例 Stack 归零、整格放入空位时
            // 实例本身就成了背包物品，两种情况下实例现值都不可靠
            var proto = taken.getOne();
            var count = item.SnapshotStack();
            if (count > 0)
            {
                requestId = Guid.NewGuid().ToString();
                PendingRollbacks[requestId] = () => RemoveFromInventoryMatching(proto, count);
            }
        }
        SendRemoveItemRequest(item.Id, requestId);
    }

    /// <summary>更新物品数量/价格（乐观本地更新 + 主机权威同步）。
    /// 数量走增量：并发叠加在主机端可交换，绝对值会互相覆盖造成「先到者扣了背包却丢库存」。</summary>
    public static void UpdateItem(string stallItemId, int stackDelta, int price)
    {
        var entry = Data.Stock.SelectMany(kv => kv.Value).FirstOrDefault(i => i.Id == stallItemId);
        if (entry?.GetItem() is not { } stocked)
            return;

        // 分屏客机与主机同进程：同步校验并直接改共享 Data（数量与乐观路径同口径以快照为基准：
        // 部分取回会原地改小共享实例，从实例现值出发会双扣）
        if (Context.IsSplitScreen && !Context.IsMainPlayer)
        {
            var newStack = entry.SnapshotStack() + stackDelta;
            if (newStack < 1 || newStack > stocked.maximumStackSize())
                return;
            ApplyUpdateItemSnapshot(entry, newStack, price);
            RelayOnHostScreen(() => BroadcastUpdateItem(new ItemUpdateData { Id = stallItemId, StackDelta = stackDelta, Price = price }));
            return;
        }

        string? requestId = null;
        if (!Context.IsMainPlayer && stackDelta != 0)
        {
            // 客机登记回滚：增量 > 0 时背包已扣，拒绝则退回；增量 < 0 时物品已进背包，拒绝则扣回
            var proto = stocked.getOne();
            var refund = stackDelta > 0;
            var count = Math.Abs(stackDelta);
            requestId = Guid.NewGuid().ToString();
            PendingRollbacks[requestId] = () =>
            {
                if (refund)
                {
                    var copy = proto.getOne();
                    copy.Stack = count;
                    RefundToInventory(copy);
                }
                else
                {
                    RemoveFromInventoryMatching(proto, count);
                }
            };
        }

        // 数量以快照为基准应用增量：部分取回路径中原版已把缓存实例原地改小，
        // 从实例现值出发会双扣；快照在 RefreshSnapshot 之前始终是变更前的权威数量
        ApplyUpdateItemSnapshot(entry, entry.SnapshotStack() + stackDelta, price);
        SendUpdateItemRequest(new ItemUpdateData { RequestId = requestId, Id = stallItemId, StackDelta = stackDelta, Price = price });
    }

    /// <summary>按变更后的权威数量写回条目并刷新快照。</summary>
    private static void ApplyUpdateItemSnapshot(StallItem entry, int newStack, int price)
    {
        if (entry.GetItem() is not { } stocked)
            return;
        stocked.Stack = Math.Clamp(newStack, 1, stocked.maximumStackSize());
        entry.RefreshSnapshot();
        entry.Price = price;
    }

    /// <summary>把物品放回背包，放不下则掉落在脚下。</summary>
    private static void RefundToInventory(Item item)
    {
        var leftover = Game1.player.addItemToInventory(item);
        if (leftover != null)
            Game1.createItemDebris(leftover, Game1.player.getStandingPosition(), Game1.player.FacingDirection);
    }

    /// <summary>按快照（含品质等）从背包扣回 count 个同类物品；背包已被玩家挪动时不强拆，记日志放弃。</summary>
    private static void RemoveFromInventoryMatching(Item proto, int count)
    {
        for (var i = 0; i < Game1.player.Items.Count && count > 0; i++)
        {
            var inv = Game1.player.Items[i];
            if (inv == null || !inv.canStackWith(proto))
                continue;
            var take = Math.Min(count, inv.Stack);
            inv.Stack -= take;
            count -= take;
            if (inv.Stack <= 0)
                Game1.player.Items[i] = null;
        }
        if (count > 0)
            Tools.Log($"[PlayerStall] 回滚时背包中找不到 {count} 个 {proto.QualifiedItemId}，跳过扣回。");
    }

    /// <summary>主机按客户端请求的原始数据校验，不接受未经检查的快照。</summary>
    private static bool ValidateAddItem(StallItem item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.Id) ||
            !RegisteredStalls.ContainsKey(item.ActionId) ||
            item.Price < 1)
            return false;

        var restored = item.GetItem();
        if (restored == null || restored.Stack <= 0 || restored.Stack > restored.maximumStackSize())
            return false;

        // 摊位非空一律拒绝：并发窗口内两人同抢空位，后到者靠拒绝回执退回物品；
        // 叠加必须走 UpdateItem 的增量路径，在此合并会产生双条目与超额覆盖
        return GetItems(item.ActionId).FirstOrDefault() == null;
    }

    private static bool ValidateUpdateItem(ItemUpdateData update)
    {
        if (update == null || string.IsNullOrWhiteSpace(update.Id) || update.Price < 1)
            return false;

        var target = FindItem(update.Id);
        var stocked = target?.GetItem();
        if (stocked == null)
            return false;
        var newStack = stocked.Stack + update.StackDelta;
        return newStack >= 1 && newStack <= stocked.maximumStackSize();
    }

    private static StallItem? FindItem(string id) =>
        Data.Stock.SelectMany(kv => kv.Value).FirstOrDefault(i => i.Id == id);

    private static void ApplyAddItem(StallItem item)
    {
        if (!Data.Stock.TryGetValue(item.ActionId, out var list))
            Data.Stock[item.ActionId] = list = new();
        if (!list.Any(i => i.Id == item.Id))
            list.Add(item);
    }

    private static void ApplyRemoveItem(string id)
    {
        foreach (var list in Data.Stock.Values)
            list.RemoveAll(i => i.Id == id);
        var emptyKeys = Data.Stock.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key).ToList();
        foreach (var key in emptyKeys)
            Data.Stock.Remove(key);
    }

    private static void ApplyUpdateItem(ItemUpdateData update)
    {
        var target = FindItem(update.Id);
        if (target?.GetItem() is not { } stocked)
            return;
        stocked.Stack = Math.Clamp(stocked.Stack + update.StackDelta, 1, stocked.maximumStackSize());
        target.RefreshSnapshot();
        target.Price = update.Price;
    }
}
