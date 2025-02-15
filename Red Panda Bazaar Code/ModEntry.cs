using HarmonyLib;
using Red_Panda_Bazaar_Code.Compatibility;
using Red_Panda_Bazaar_Code.Config;
using Red_Panda_Bazaar_Code.Handlers;
using Red_Panda_Bazaar_Code.Patches;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Red_Panda_Bazaar_Code;

public class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        Tools.Init(helper, helper.ReadConfig<ModConfig>(), Monitor, ModManifest);

        Tools.Log($"Red Panda Bazaar Code Initializing...");

        Tools.Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private static void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        Integrations.Init();
        ControllerInit();
        HarmonyPatch();
    }

    private static void ControllerInit()
    {
        DataHandler.Init();
        EntranceHandler.Init();
        CritterHandler.Init();
        FestivalHandler.Init();
        MenuHandler.Init();
        QuestHandler.Init();
        BuffHandler.Init();
    }

    private static void HarmonyPatch()
    {
        var harmony = new Harmony(Tools.ModManifest.UniqueID);
        HarmonyPatch_CustomFishingGame.ApplyPatch(harmony);
        HarmonyPatch_CustomBuffs.ApplyPatch(harmony);
    }
}