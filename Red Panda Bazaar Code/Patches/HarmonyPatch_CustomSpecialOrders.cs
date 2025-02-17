using HarmonyLib;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.SpecialOrders;

namespace Red_Panda_Bazaar_Code.Patches;

public static class HarmonyPatch_CustomSpecialOrders
{
    private static bool extraRewardGiven;

    public static void ApplyPatch(Harmony harmony)
    {
        Tools.LogPatch(nameof(HarmonyPatch_CustomSpecialOrders), "SpecialOrder.CheckCompletion()", PatchType.Postfix);
        harmony.Patch(
            original: AccessTools.Method(typeof(SpecialOrder), "CheckCompletion"),
            postfix: new HarmonyMethod(typeof(HarmonyPatch_CustomSpecialOrders),
                nameof(Postfix_SpecialOrder_CheckCompletion))
        );

        Tools.LogPatch(nameof(HarmonyPatch_CustomSpecialOrders), "SpecialOrder.Update()", PatchType.Prefix);
        harmony.Patch(
            original: AccessTools.Method(typeof(SpecialOrder), "Update"),
            prefix: new HarmonyMethod(typeof(HarmonyPatch_CustomSpecialOrders),
                nameof(Prefix_SpecialOrder_Update))
        );
    }

    private static bool Prefix_SpecialOrder_Update(SpecialOrder __instance)
    {
        try
        {
            if (extraRewardGiven && Game1.player.stats.Get("specialOrderPrizeTickets") > 0U)
            {
                Game1.player.stats.Decrement("specialOrderPrizeTickets");
                extraRewardGiven = false;
            }

            return true;
        }
        catch (Exception e)
        {
            Tools.LogPatchErr(nameof(HarmonyPatch_CustomSpecialOrders), e);
            throw;
        }
    }

    private static void Postfix_SpecialOrder_CheckCompletion(SpecialOrder __instance)
    {
        try
        {
            if (__instance.questState.Value == SpecialOrderStatus.Complete &&
                __instance.orderType.Value.Contains(QuestsKeys.CXM_OrderType))
            {
                Game1.player.stats.Increment(StatsKeys.ChenXiaomingRewardCount);
                extraRewardGiven = true;
                Tools.Log("A SpecialOrders of Chen Xiaoming has been completed.");
            }
        }
        catch (Exception e)
        {
            Tools.LogPatchErr(nameof(HarmonyPatch_CustomSpecialOrders), e);
            throw;
        }
    }
}