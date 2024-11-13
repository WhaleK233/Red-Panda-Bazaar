using HarmonyLib;
using Red_Panda_Bazaar_Code.Data;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.SpecialOrders;

namespace Red_Panda_Bazaar_Code.Quests;

public class HarmonyPatch_CustomSpecialOrder
{
    private static bool Applied { get; set; } = false;

    public static void ApplyPatch(Harmony harmony)
    {
        if (!Applied)
        {
            Tools.Log(
                $"Applying Harmony patch \"{nameof(HarmonyPatch_CustomSpecialOrder)}\": postfixing SDV method \"SpecialOrder.Update()\".");
            harmony.Patch(
                original: AccessTools.Method(typeof(SpecialOrder), "Update"),
                prefix: new HarmonyMethod(typeof(HarmonyPatch_CustomSpecialOrder), nameof(Prefix_SpecialOrder_Update))
            );

            Applied = true;
        }
    }

    public static bool Prefix_SpecialOrder_Update(SpecialOrder __instance)
    {
        if (!__instance.readyForRemoval.Value)
        {
            switch (__instance.questState.Value)
            {
                case SpecialOrderStatus.InProgress:
                    __instance.participants.TryAdd(Game1.player.UniqueMultiplayerID, true);
                    break;
                case SpecialOrderStatus.Complete:
                    if (__instance.unclaimedRewards.Remove(Game1.player.UniqueMultiplayerID))
                    {
                        ++Game1.stats.QuestsCompleted;
                        Game1.playSound("questcomplete");
                        if (__instance.orderType.Value == "RPB")
                        {
                            RPBData.PrizeTicketRewardIncrement();
                        }
                    }
                    break;
            }
        }

        return true;
    }
}