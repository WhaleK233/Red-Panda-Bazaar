using HarmonyLib;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Custom;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Patches;

public static class HarmonyPatch_CustomFishingGame
{
    private const string NAME = nameof(HarmonyPatch_CustomFishingGame);

    public static void ApplyPatch(Harmony harmony)
    {
        Tools.LogPatch(NAME, "Event.caughtFish()", PatchType.Prefix);
        harmony.Patch(
            original: AccessTools.Method(typeof(Event), "caughtFish"),
            prefix: new HarmonyMethod(typeof(HarmonyPatch_CustomFishingGame), nameof(Prefix_Event_caughtFish))
        );

        Tools.LogPatch(NAME, "Event.perfectFishing()", PatchType.Prefix);
        harmony.Patch(
            original: AccessTools.Method(typeof(Event), "perfectFishing"),
            prefix: new HarmonyMethod(typeof(HarmonyPatch_CustomFishingGame), nameof(Prefix_Event_perfectFishing))
        );
    }

    private static bool Prefix_Event_caughtFish(Event __instance, int size)
    {
        try
        {
            if (Game1.currentMinigame is RPB_FishingGame currentMinigame &&
                Game1.CurrentEvent?.FestivalName == "SpringFair")
            {
                currentMinigame.score += size > 0 ? size + 5 : 1;
                if (size > 0)
                {
                    ++currentMinigame.fishCaught;
                }

                Game1.player.FarmerSprite.PauseForSingleAnimation = false;
                Game1.player.FarmerSprite.StopAnimation();
            }

            return true;
        }
        catch (Exception e)
        {
            Tools.LogPatchErr(NAME, e);
            throw;
        }
    }

    private static bool Prefix_Event_perfectFishing(Event __instance)
    {
        try
        {
            if (Game1.currentMinigame is RPB_FishingGame currentMinigame &&
                Game1.CurrentEvent?.FestivalName == "SpringFair")
            {
                ++currentMinigame.perfections;
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