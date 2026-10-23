using HarmonyLib;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.SpecialOrders;

namespace Red_Panda_Bazaar_Code.Features.SpecialOrders;

public static class SpecialOrdersPatch
{
    private const string NAME = nameof(SpecialOrdersPatch);

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
            // 原版只在主机推进完成状态；订单完成后仍会被目标重复调用 CheckCompletion。
            if (!Context.IsMainPlayer ||
                __instance.questState.Value != SpecialOrderStatus.Complete ||
                !__instance.orderType.Value.Contains(QuestsKeys.CXM_OrderType))
            {
                return;
            }

            foreach (var playerId in __instance.participants.Keys)
            {
                var player = Game1.GetPlayer(playerId);
                player?.stats.Increment(StatsKeys.ChenXiaomingRewardCount);
            }
            Tools.Log("A SpecialOrders of Chen Xiaoming has been completed.");
        }
        catch (Exception e)
        {
            Tools.LogPatchErr(NAME, e);
        }
    }
}
