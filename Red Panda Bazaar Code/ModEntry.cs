using WhaleK.Common;
using HarmonyLib;
using Red_Panda_Bazaar_Code.Compatibility;
using Red_Panda_Bazaar_Code.Config;
using Red_Panda_Bazaar_Code.Features.Bank;
using Red_Panda_Bazaar_Code.Features.Buffs;
using Red_Panda_Bazaar_Code.Features.ButterflyNight;
using Red_Panda_Bazaar_Code.Features.Critters;
using Red_Panda_Bazaar_Code.Features.FishingMiniGame;
using Red_Panda_Bazaar_Code.Features.Furniture;
using Red_Panda_Bazaar_Code.Features.PlayerStall;
using Red_Panda_Bazaar_Code.Features.PrizeMachines;
using Red_Panda_Bazaar_Code.Features.SpecialOrders;
using Red_Panda_Bazaar_Code.Features.SpringFair;
using Red_Panda_Bazaar_Code.Features.SharedBikes;
using Red_Panda_Bazaar_Code.Features.Tetris;
using Red_Panda_Bazaar_Code.Features.Transportation;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Red_Panda_Bazaar_Code;

public class ModEntry : Mod {
    public override void Entry(IModHelper helper) {
        ModInfo.Init(helper, helper.ReadConfig<ModConfig>(), Monitor, ModManifest);

        // StardewUI.Core 是编译进本 mod 的 UI 框架，必须在 Entry 里初始化（内部会订阅 GameLoop 事件）
        StardewUI.UI.Initialize(helper, Monitor);

        // 内容注册必须在游戏启动加载资产前就位（Data/AudioChanges 启动早期即被缓存）
        SharedBikes.RegisterContent();

        ModInfo.Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private static void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
        Tools.LogInfo("Red Panda Bazaar Code Initializing...");

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
        PrizeMachines.Init();
        SpecialOrders.Init();
        ButterflyNight.Init();
        Transportation.Init();
        SharedBikes.Init();
        TetrisFeature.Init();
    }

    private static void HarmonyPatch() {
        var harmony = new Harmony(ModInfo.ModManifest.UniqueID);
        FishingMiniGamePatch.ApplyPatch(harmony);
        BuffsPatch.ApplyPatch(harmony);
        SpecialOrdersPatch.ApplyPatch(harmony);
        FurniturePatch.ApplyPatch(harmony);
        SharedBikesPatch.ApplyPatch(harmony);
    }
}