using HarmonyLib;
using Red_Panda_Bazaar_Code.Controller;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewValley;

namespace Red_Panda_Bazaar_Code.HarmonyPatch;

public class HarmonyPatch_CustomBuffEffects
{
    private static bool Applied { get; set; } = false;

    public static void ApplyPatch(Harmony harmony)
    {
        if (!Applied)
        {
            Tools.Monitor.Log(
                $"Applying Harmony patch \"{nameof(HarmonyPatch_CustomBuffEffects)}\": postfixing SDV method \"Farmer.doneEating()\".",
                LogLevel.Trace);
            harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), "doneEating"),
                prefix: new HarmonyMethod(typeof(HarmonyPatch_CustomBuffEffects), nameof(Prefix_Farmer_doneEating))
            );

            Applied = true;
        }
    }

    private static bool Prefix_Farmer_doneEating(Farmer __instance)
    {
        try
        {
            var buffDict = BuffController.buffDict;

            if (buffDict.ContainsKey(__instance.itemToEat.ItemId))
            {
                __instance.applyBuff(buffDict[__instance.itemToEat.ItemId]);
            }

            return true;
        }
        catch (Exception e)
        {
            Tools.LogOnce(
                $"Harmony patch \"{nameof(HarmonyPatch_CustomBuffEffects)}\" has encountered an error. Custom Buffs might not work properly. Full error message: \n{e.ToString()}",
                LogLevel.Error);
            throw;
        }
    }
}