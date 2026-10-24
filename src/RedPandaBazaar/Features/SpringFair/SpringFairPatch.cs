using HarmonyLib;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.Minigames;

namespace Red_Panda_Bazaar_Code.Features.SpringFair;

/// <summary>复用原版春季展览会小游戏，并适配春季展览会的结算入口。</summary>
public static class SpringFairPatch
{
    private const string NAME = nameof(SpringFairPatch);

    public static void ApplyPatch(Harmony harmony)
    {
        Tools.LogPatch(NAME, "Event.caughtFish()", PatchType.Prefix);
        harmony.Patch(
            original: AccessTools.Method(typeof(Event), "caughtFish"),
            prefix: new HarmonyMethod(typeof(SpringFairPatch), nameof(Prefix_Event_caughtFish))
        );

        Tools.LogPatch(NAME, "Event.perfectFishing()", PatchType.Prefix);
        harmony.Patch(
            original: AccessTools.Method(typeof(Event), "perfectFishing"),
            prefix: new HarmonyMethod(typeof(SpringFairPatch), nameof(Prefix_Event_perfectFishing))
        );

        Tools.LogPatch(NAME, "FishingGame.unload()", PatchType.Postfix);
        harmony.Patch(
            original: AccessTools.Method(typeof(FishingGame), nameof(FishingGame.unload)),
            postfix: new HarmonyMethod(typeof(SpringFairPatch), nameof(Postfix_FishingGame_unload))
        );

        Tools.LogPatch(NAME, "TargetGame.unload()", PatchType.Postfix);
        harmony.Patch(
            original: AccessTools.Method(typeof(TargetGame), nameof(TargetGame.unload)),
            postfix: new HarmonyMethod(typeof(SpringFairPatch), nameof(Postfix_TargetGame_unload))
        );
    }

    private static bool Prefix_Event_caughtFish(int size)
    {
        try
        {
            if (Game1.currentMinigame is FishingGame currentMinigame &&
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
            return true;
        }
    }

    private static bool Prefix_Event_perfectFishing()
    {
        try
        {
            if (Game1.currentMinigame is FishingGame currentMinigame &&
                Game1.CurrentEvent?.FestivalName == "SpringFair")
            {
                ++currentMinigame.perfections;
            }

            return true;
        }
        catch (Exception e)
        {
            Tools.LogPatchErr(NAME, e);
            return true;
        }
    }

    private static void Postfix_FishingGame_unload()
    {
        try
        {
            if (Game1.currentMinigame is FishingGame)
            {
                SpringFair.TryRestoreMiniGamePosition();
            }
        }
        catch (Exception e)
        {
            Tools.LogPatchErr(NAME, e);
        }
    }

    private static void Postfix_TargetGame_unload()
    {
        try
        {
            if (Game1.currentMinigame is TargetGame)
            {
                SpringFair.TryRestoreMiniGamePosition();
            }
        }
        catch (Exception e)
        {
            Tools.LogPatchErr(NAME, e);
        }
    }
}
