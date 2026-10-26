using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Red_Panda_Bazaar_Code.Features.PlayerStall;

/// <summary>
/// 摊位系统的多人同步：请求 / 广播的收发、主机权威校验回执、客机乐观操作的回滚登记。
/// 入口与地图交互见 PlayerStall.cs，数据读写见 PlayerStallData.cs，夜间结算见 PlayerStallSettlement.cs。
/// </summary>
public static partial class PlayerStall
{
    private class ItemUpdateData
    {
        public string? RequestId { get; set; }
        public string Id { get; set; } = "";
        public int StackDelta { get; set; }
        public int Price { get; set; }
    }

    private class CollectResultData
    {
        public long PlayerId { get; set; }
        public int NetEarnings { get; set; }
        public int CollectDay { get; set; }
    }

    private class StallItemRequestData
    {
        public long PlayerId { get; set; }
        public string? RequestId { get; set; }
        public StallItem? Item { get; set; }
    }

    private class StallItemIdRequestData
    {
        public long PlayerId { get; set; }
        public string? RequestId { get; set; }
        public string Id { get; set; } = "";
    }

    private class StallItemUpdateRequestData
    {
        public long PlayerId { get; set; }
        public string? RequestId { get; set; }
        public ItemUpdateData Update { get; set; } = new();
    }

    /// <summary>客机乐观操作的回滚动作，键为请求 ID；主机拒绝时执行，确认时丢弃。</summary>
    private static readonly Dictionary<string, Action> PendingRollbacks = new();

    /// <summary>
    /// 分屏客机待转交主机屏执行的动作。SMAPI 消息在分屏客机上下文发出时 FromPlayerID 是分屏玩家，
    /// 远端客机对广播的信任检查只认主机玩家，会被整体忽略；改由主机屏代发即可带上主机身份。
    /// 队列只在同进程内传递，游戏循环单线程，无需加锁。
    /// </summary>
    private static readonly Queue<Action> HostScreenRelay = new();

    /// <summary>把动作放到主机屏执行：主机屏上下文立即执行，分屏客机上下文入队等待下一 tick 转发。</summary>
    private static void RelayOnHostScreen(Action action)
    {
        if (Context.IsMainPlayer)
            action();
        else
            HostScreenRelay.Enqueue(action);
    }

    private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        // 仅主机屏处理转发队列（见 HostScreenRelay 的说明）
        if (!Context.IsMainPlayer || HostScreenRelay.Count == 0)
            return;

        while (HostScreenRelay.TryDequeue(out var action))
            action();
    }

    /// <summary>主机向请求方回执：accepted 丢弃回滚登记，rejected 执行回滚。</summary>
    private static void SendRequestResult(long playerId, string? requestId, bool accepted)
    {
        if (string.IsNullOrEmpty(requestId))
            return;
        ModInfo.Helper.Multiplayer.SendMessage(
            requestId, accepted ? MPMessageType.PlayerStall_RequestConfirm : MPMessageType.PlayerStall_RequestReject,
            modIDs: new[] { ModInfo.ModManifest.UniqueID },
            playerIDs: new[] { playerId });
    }

    private static void SendAddItemRequest(StallItem item, string? requestId)
    {
        var request = new StallItemRequestData { PlayerId = Game1.player.UniqueMultiplayerID, RequestId = requestId, Item = item };
        if (Context.IsMainPlayer)
        {
            BroadcastAddItem(item);
            return;
        }

        ModInfo.Helper.Multiplayer.SendMessage(
            request, MPMessageType.PlayerStall_AddItem,
            modIDs: new[] { ModInfo.ModManifest.UniqueID },
            playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
    }

    private static void SendRemoveItemRequest(string id, string? requestId)
    {
        var request = new StallItemIdRequestData { PlayerId = Game1.player.UniqueMultiplayerID, RequestId = requestId, Id = id };
        if (Context.IsMainPlayer)
        {
            BroadcastRemoveItem(id);
            return;
        }

        ModInfo.Helper.Multiplayer.SendMessage(
            request, MPMessageType.PlayerStall_RemoveItem,
            modIDs: new[] { ModInfo.ModManifest.UniqueID },
            playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
    }

    private static void SendUpdateItemRequest(ItemUpdateData update)
    {
        var request = new StallItemUpdateRequestData { PlayerId = Game1.player.UniqueMultiplayerID, RequestId = update.RequestId, Update = update };
        if (Context.IsMainPlayer)
        {
            BroadcastUpdateItem(new ItemUpdateData { Id = update.Id, StackDelta = update.StackDelta, Price = update.Price });
            return;
        }

        ModInfo.Helper.Multiplayer.SendMessage(
            request, MPMessageType.PlayerStall_UpdateItem,
            modIDs: new[] { ModInfo.ModManifest.UniqueID },
            playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
    }

    private static void BroadcastAddItem(StallItem item)
    {
        ModInfo.Helper.Multiplayer.SendMessage(item, MPMessageType.PlayerStall_AddItemBroadcast,
            modIDs: new[] { ModInfo.ModManifest.UniqueID });
    }

    private static void BroadcastRemoveItem(string id)
    {
        ModInfo.Helper.Multiplayer.SendMessage(id, MPMessageType.PlayerStall_RemoveItemBroadcast,
            modIDs: new[] { ModInfo.ModManifest.UniqueID });
    }

    private static void BroadcastUpdateItem(ItemUpdateData update)
    {
        ModInfo.Helper.Multiplayer.SendMessage(update, MPMessageType.PlayerStall_UpdateItemBroadcast,
            modIDs: new[] { ModInfo.ModManifest.UniqueID });
    }

    private static void SendSyncData(long playerId)
    {
        ModInfo.Helper.Multiplayer.SendMessage(
            Data, MPMessageType.PlayerStall_SyncData,
            modIDs: new[] { ModInfo.ModManifest.UniqueID },
            playerIDs: new[] { playerId });
    }

    /// <summary>新的一天开始，客机向主机请求最新内存数据。</summary>
    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        // 分屏客机与主机同进程共享 Data，无需也不能走网络请求（见 SendToHostOrBroadcast 的分屏说明）
        if (!Context.IsOnHostComputer)
        {
            ModInfo.Helper.Multiplayer.SendMessage(
                true, MPMessageType.PlayerStall_SyncRequest,
                modIDs: new[] { ModInfo.ModManifest.UniqueID },
                playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
        }
    }

    /// <summary>接收其他玩家的同步消息。主机负责接收客机请求并转发给所有客机；客机直接应用主机广播。
    /// 「是否主机」用进程级的 Game1.IsMasterGame 判断：混合联机（主机+分屏+远端客机）下，
    /// 消息可能在主机进程的任意屏上下文触发，屏级的 Context.IsMainPlayer 会误拒客机请求。</summary>
    private static void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != ModInfo.ModManifest.UniqueID)
            return;

        switch (e.Type)
        {
            case MPMessageType.PlayerStall_Collect:
                if (!Context.IsMainPlayer && e.FromPlayerID != Game1.MasterPlayer.UniqueMultiplayerID)
                    break;
                Data.CollectDay = e.ReadAs<int>();
                Data.LastSoldRecords = Data.SoldRecords.ToList();
                Data.SoldRecords.Clear();
                break;

            case MPMessageType.PlayerStall_CollectRequest:
                {
                    if (!Game1.IsMasterGame)
                        break;
                    var net = TryCollectTodayHost();
                    var result = new CollectResultData
                    {
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

            case MPMessageType.PlayerStall_CollectResult:
                {
                    if (Context.IsMainPlayer || e.FromPlayerID != Game1.MasterPlayer.UniqueMultiplayerID)
                        break;
                    var result = e.ReadAs<CollectResultData>();
                    if (result == null)
                        break;
                    if (result.PlayerId != Game1.player.UniqueMultiplayerID)
                        break;
                    if (result.NetEarnings >= 0)
                    {
                        Game1.player.Money += result.NetEarnings;
                        Game1.playSound("coin");
                    }

                    break;
                }

            case MPMessageType.PlayerStall_SyncRequest:
                {
                    if (!Game1.IsMasterGame)
                        break;
                    ModInfo.Helper.Multiplayer.SendMessage(
                        Data, MPMessageType.PlayerStall_SyncData,
                        modIDs: new[] { ModInfo.ModManifest.UniqueID },
                        playerIDs: new[] { e.FromPlayerID });
                    break;
                }

            case MPMessageType.PlayerStall_SyncData:
                {
                    if (Context.IsMainPlayer)
                        break;
                    if (e.FromPlayerID != Game1.MasterPlayer.UniqueMultiplayerID)
                        break;
                    var syncData = e.ReadAs<StallSaveData>();
                    if (syncData != null)
                        Data = syncData;
                    break;
                }

            case MPMessageType.PlayerStall_AddItem:
                {
                    if (Game1.IsMasterGame)
                    {
                        var request = e.ReadAs<StallItemRequestData>();
                        if (request?.Item == null || request.PlayerId != e.FromPlayerID)
                            break;
                        if (!ValidateAddItem(request.Item))
                        {
                            SendSyncData(e.FromPlayerID);
                            SendRequestResult(e.FromPlayerID, request.RequestId, accepted: false);
                            break;
                        }
                        ApplyAddItem(request.Item);
                        BroadcastAddItem(request.Item);
                        SendRequestResult(e.FromPlayerID, request.RequestId, accepted: true);
                    }

                    break;
                }

            case MPMessageType.PlayerStall_AddItemBroadcast:
                {
                    if (e.FromPlayerID != Game1.MasterPlayer.UniqueMultiplayerID)
                        break;
                    var newItem = e.ReadAs<StallItem>();
                    if (newItem != null)
                        ApplyAddItem(newItem);
                    break;
                }

            case MPMessageType.PlayerStall_RemoveItem:
                {
                    if (Game1.IsMasterGame)
                    {
                        var request = e.ReadAs<StallItemIdRequestData>();
                        if (request == null || request.PlayerId != e.FromPlayerID ||
                            string.IsNullOrWhiteSpace(request.Id) || FindItem(request.Id) == null)
                        {
                            if (request != null && request.PlayerId == e.FromPlayerID)
                            {
                                SendSyncData(e.FromPlayerID);
                                SendRequestResult(e.FromPlayerID, request.RequestId, accepted: false);
                            }
                            break;
                        }
                        ApplyRemoveItem(request.Id);
                        BroadcastRemoveItem(request.Id);
                        SendRequestResult(e.FromPlayerID, request.RequestId, accepted: true);
                    }

                    break;
                }

            case MPMessageType.PlayerStall_RemoveItemBroadcast:
                {
                    if (e.FromPlayerID != Game1.MasterPlayer.UniqueMultiplayerID)
                        break;
                    var removeId = e.ReadAs<string>();
                    if (!string.IsNullOrWhiteSpace(removeId))
                        ApplyRemoveItem(removeId);
                    break;
                }

            case MPMessageType.PlayerStall_UpdateItem:
                {
                    if (Game1.IsMasterGame)
                    {
                        var request = e.ReadAs<StallItemUpdateRequestData>();
                        if (request?.Update == null || request.PlayerId != e.FromPlayerID)
                            break;
                        if (!ValidateUpdateItem(request.Update))
                        {
                            SendSyncData(e.FromPlayerID);
                            SendRequestResult(e.FromPlayerID, request.RequestId, accepted: false);
                            break;
                        }
                        ApplyUpdateItem(request.Update);
                        BroadcastUpdateItem(new ItemUpdateData
                        {
                            Id = request.Update.Id,
                            StackDelta = request.Update.StackDelta,
                            Price = request.Update.Price
                        });
                        SendRequestResult(e.FromPlayerID, request.RequestId, accepted: true);
                    }

                    break;
                }

            case MPMessageType.PlayerStall_UpdateItemBroadcast:
                {
                    if (e.FromPlayerID != Game1.MasterPlayer.UniqueMultiplayerID)
                        break;
                    var update = e.ReadAs<ItemUpdateData>();
                    if (update != null)
                        ApplyUpdateItem(update);
                    break;
                }

            case MPMessageType.PlayerStall_RequestConfirm:
                {
                    if (Context.IsMainPlayer || e.FromPlayerID != Game1.MasterPlayer.UniqueMultiplayerID)
                        break;
                    var confirmedId = e.ReadAs<string>();
                    if (!string.IsNullOrEmpty(confirmedId))
                        PendingRollbacks.Remove(confirmedId);
                    break;
                }

            case MPMessageType.PlayerStall_RequestReject:
                {
                    if (Context.IsMainPlayer || e.FromPlayerID != Game1.MasterPlayer.UniqueMultiplayerID)
                        break;
                    var rejectedId = e.ReadAs<string>();
                    if (!string.IsNullOrEmpty(rejectedId) && PendingRollbacks.Remove(rejectedId, out var rollback))
                        rollback();
                    break;
                }
        }
    }
}
