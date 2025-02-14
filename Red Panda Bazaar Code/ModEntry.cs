using HarmonyLib;
using Red_Panda_Bazaar_Code.Compatibility;
using Red_Panda_Bazaar_Code.Config;
using Red_Panda_Bazaar_Code.Controller;
using Red_Panda_Bazaar_Code.HarmonyPatch;
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

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        ControllerInit();
        HarmonyPatch();
        ModCompat.Init();
    }

    private static void ControllerInit()
    {
        DataController.Init();
        EntranceController.Init();
        CritterController.Init();
        FestivalController.Init();
        MenuController.Init();
        QuestController.Init();
        BuffController.Init();
    }

    private void HarmonyPatch()
    {
        var harmony = new Harmony(Tools.ModManifest.UniqueID);
        HarmonyPatch_FishingGameEvent.ApplyPatch(harmony);
        HarmonyPatch_CustomBuffEffects.ApplyPatch(harmony);
    }
}