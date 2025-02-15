using HarmonyLib;
using Red_Panda_Bazaar_Code.Handlers;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Patches;

public static class HarmonyPatch_CustomBuffs
{
    public static void ApplyPatch(Harmony harmony)
    {
        Tools.Monitor.Log(
            $"Applying Harmony patch \"{nameof(HarmonyPatch_CustomBuffs)}\": postfixing SDV method \"Farmer.doneEating()\".");
        harmony.Patch(
            original: AccessTools.Method(typeof(Farmer), "doneEating"),
            prefix: new HarmonyMethod(typeof(HarmonyPatch_CustomBuffs), nameof(Prefix_Farmer_doneEating))
        );
    }

    private static bool Prefix_Farmer_doneEating(Farmer __instance)
    {
        try
        {
            var buffDict = BuffHandler.BuffDict;

            if (buffDict.ContainsKey(__instance.itemToEat.ItemId))
            {
                __instance.applyBuff(buffDict[__instance.itemToEat.ItemId]);
            }

            return true;
        }
        catch (Exception e)
        {
            Tools.LogOnce(
                $"Harmony patch \"{nameof(HarmonyPatch_CustomBuffs)}\" has encountered an error. Custom Buffs might not work properly. Full error message: \n{e.ToString()}",
                LogLevel.Error);
            throw;
        }
    }
}