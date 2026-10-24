using HarmonyLib;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.SpecialOrders;

namespace Red_Panda_Bazaar_Code.Features.SpecialOrders;

public static class SpecialOrdersPatch
{
    private const string NAME = nameof(SpecialOrdersPatch);
    private static readonly HashSet<string> RewardedOrderIds = new();

    public static void ApplyPatch(Harmony harmony)
    {
        Tools.LogPatch(NAME, "SpecialOrder.CheckCompletion()", PatchType.Postfix);
        harmony.Patch(
            original: AccessTools.Method(typeof(SpecialOrder), "CheckCompletion"),
            postfix: new HarmonyMethod(typeof(SpecialOrdersPatch),
                nameof(Postfix_SpecialOrder_CheckCompletion))
        );
    }

    private static void Postfix_SpecialOrder_CheckCompletion(SpecialOrder __instance)
    {
        try
        {
            // 订单完成后 CheckCompletion 仍会被目标反复调用，只能按订单 ID 发一次奖励。
            if (!Context.IsMainPlayer ||
                __instance.questState.Value != SpecialOrderStatus.Complete ||
                !__instance.orderType.Value.Contains(QuestsKeys.CXM_OrderType))
            {
                return;
            }

            if (!RewardedOrderIds.Add(__instance.questKey.Value))
                return;

            foreach (var playerId in __instance.participants.Keys)
            {
                var player = Game1.getAllFarmers().FirstOrDefault(p => p.UniqueMultiplayerID == playerId);
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
