using HarmonyLib;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.SpecialOrders;

namespace Red_Panda_Bazaar_Code.Features.SpecialOrders;

public static class SpecialOrdersPatch
{
    private const string NAME = nameof(SpecialOrdersPatch);

    /// <summary>前缀记录的「本次调用前订单是否已完成」。CheckCompletion 对非 InProgress 订单直接早退，
    /// 但后缀照样执行、状态值也区分不了「刚完成」与「早已完成」，只能靠调用前后的差值判定；
    /// 按 InProgress → Complete 迁移发奖，Repeatable 订单再次完成会再次迁移而重新发奖，
    /// 也无需跨存档持久化的去重集合。</summary>
    private static bool _wasCompleteBeforeCheck;

    public static void ApplyPatch(Harmony harmony)
    {
        Tools.LogPatch(NAME, "SpecialOrder.CheckCompletion()", PatchType.Prefix);
        Tools.LogPatch(NAME, "SpecialOrder.CheckCompletion()", PatchType.Postfix);
        harmony.Patch(
            original: AccessTools.Method(typeof(SpecialOrder), "CheckCompletion"),
            prefix: new HarmonyMethod(typeof(SpecialOrdersPatch), nameof(Prefix_SpecialOrder_CheckCompletion)),
            postfix: new HarmonyMethod(typeof(SpecialOrdersPatch), nameof(Postfix_SpecialOrder_CheckCompletion))
        );
    }

    private static void Prefix_SpecialOrder_CheckCompletion(SpecialOrder __instance)
    {
        try
        {
            _wasCompleteBeforeCheck = __instance.questState.Value == SpecialOrderStatus.Complete;
        }
        catch (Exception e)
        {
            // 状态不明时按已完成处理：宁可漏发一次补奖，也不冒重复发奖的风险
            _wasCompleteBeforeCheck = true;
            Tools.LogPatchErr(NAME, e);
        }
    }

    private static void Postfix_SpecialOrder_CheckCompletion(SpecialOrder __instance)
    {
        try
        {
            if (!Context.IsMainPlayer ||
                _wasCompleteBeforeCheck ||
                __instance.questState.Value != SpecialOrderStatus.Complete ||
                !__instance.orderType.Value.Contains(QuestsKeys.CXM_OrderType))
            {
                return;
            }

            foreach (var playerId in __instance.participants.Keys)
            {
                var player = Game1.GetPlayer(playerId);
                player?.stats.Increment(StatsKeys.ChenXiaomingRewardCount);
                if (player is { IsLocalPlayer: false })
                    ModInfo.Helper.Multiplayer.SendMessage(
                        player.UniqueMultiplayerID, MPMessageType.SpecialOrders_RewardSync,
                        modIDs: new[] { ModInfo.ModManifest.UniqueID },
                        playerIDs: new[] { player.UniqueMultiplayerID });
            }
            Tools.Log("A SpecialOrders of Chen Xiaoming has been completed.");
        }
        catch (Exception e)
        {
            Tools.LogPatchErr(NAME, e);
        }
    }
}
