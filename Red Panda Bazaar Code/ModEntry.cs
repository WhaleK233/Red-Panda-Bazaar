using HarmonyLib;
using Red_Panda_Bazaar_Code.Compatibility;
using Red_Panda_Bazaar_Code.Config;
using Red_Panda_Bazaar_Code.DeBug;
using Red_Panda_Bazaar_Code.Features.Buffs;
using Red_Panda_Bazaar_Code.Features.ButterflyNight;
using Red_Panda_Bazaar_Code.Features.Critters;
using Red_Panda_Bazaar_Code.Features.FishingMiniGame;
using Red_Panda_Bazaar_Code.Features.Furniture;
using Red_Panda_Bazaar_Code.Features.PlayerStall;
using Red_Panda_Bazaar_Code.Features.PrizeMachines;
using Red_Panda_Bazaar_Code.Features.SpecialOrders;
using Red_Panda_Bazaar_Code.Features.SpringFair;
using Red_Panda_Bazaar_Code.Features.Bank;
using Red_Panda_Bazaar_Code.Features.Transportation;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Red_Panda_Bazaar_Code;

public class ModEntry : Mod {
    public override void Entry(IModHelper helper) {
        Tools.Init(helper, helper.ReadConfig<ModConfig>(), Monitor, ModManifest);

        Tools.LogInfo("Red Panda Bazaar Code Initializing...");

        Tools.Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private static void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
        Integrations.Init();
        FeatureInit();
        HarmonyPatch();
    }

    private static void FeatureInit() {
        Bank.Init();
        Buffs.Init();
        Critter.Init();
        Furniture.Init();
        SpringFair.Init();
        PlayerStall.Init();
        DebugOverlay.Init();
        PrizeMachines.Init();
        SpecialOrders.Init();
        ButterflyNight.Init();
        Transportation.Init();
    }

    private static void HarmonyPatch() {
        var harmony = new Harmony(Tools.ModManifest.UniqueID);
        FishingMiniGamePatch.ApplyPatch(harmony);
        BuffsPatch.ApplyPatch(harmony);
        SpecialOrdersPatch.ApplyPatch(harmony);
        FurniturePatch.ApplyPatch(harmony);
    }
}