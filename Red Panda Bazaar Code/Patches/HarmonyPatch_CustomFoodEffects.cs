using HarmonyLib;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Handlers;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Patches;

public static class HarmonyPatch_CustomFoodEffects
{
    private const string NAME = nameof(HarmonyPatch_CustomFoodEffects);

    public static void ApplyPatch(Harmony harmony)
    {
        Tools.LogPatch(NAME, "Farmer.doneEating()", PatchType.Prefix);
        harmony.Patch(
            original: AccessTools.Method(typeof(Farmer), "doneEating"),
            prefix: new HarmonyMethod(typeof(HarmonyPatch_CustomFoodEffects), nameof(Prefix_Farmer_doneEating))
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

            if (__instance.itemToEat.ItemId.Contains(ItemsKeys.Food.Milk_Pudding))
            {
                var ex = Game1.random.Next(40, 60);
                Game1.player.gainExperience(SkillsKeys.Farming, ex);
            }

            return true;
        }
        catch (Exception e)
        {
            Tools.LogPatchErr(NAME, e);
            throw;
        }
    }
}