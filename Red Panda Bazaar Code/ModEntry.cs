using HarmonyLib;
using Red_Panda_Bazaar_Code.Buffs;
using Red_Panda_Bazaar_Code.Compatibility;
using Red_Panda_Bazaar_Code.Config;
using Red_Panda_Bazaar_Code.Data;
using Red_Panda_Bazaar_Code.Festivals;
using Red_Panda_Bazaar_Code.Menus;
using Red_Panda_Bazaar_Code.MiniGames;
using Red_Panda_Bazaar_Code.Quests;
using Red_Panda_Bazaar_Code.Utils;
using Red_Panda_Bazaar_Code.VisualEffects;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Red_Panda_Bazaar_Code;

public class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        Tools.Init(helper, helper.ReadConfig<ModConfig>(), Monitor);

        Tools.Log($"Red Panda Bazaar Code Initializing...");

        Tools.Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        InitializeGenericModConfigMenu();
        RPBData.Init();
        ControllerInit();
        PatchHarmony();
    }

    private static void ControllerInit()
    {
        FireFlyController.Init();
        SpringFairController.Init();
        BuffController.Init();
        MenuController.Init();
        QuestsController.Init();
    }

    private void PatchHarmony()
    {
        var harmony = new Harmony(this.ModManifest.UniqueID);
        HarmonyPatch_FishingGameEvent.ApplyPatch(harmony);
        HarmonyPatch_CustomBuffEffects.ApplyPatch(harmony);
        HarmonyPatch_CustomSpecialOrder.ApplyPatch(harmony);
    }

    #region Generic Mod Config Menu

    private void InitializeGenericModConfigMenu()
    {
        // 获取通用模组配置菜单的API
        var configMenu = Tools.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(ModCompat.GenericModConfigMenu);
        if (configMenu is null) return;

        // 注册模组
        configMenu.Register(
            mod: this.ModManifest,
            reset: () => Tools.ModConfig = new ModConfig(),
            save: () => Tools.Helper.WriteConfig(Tools.ModConfig)
        );

        // 萤火虫数量
        configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => Tools.I18n.Get(I18nKeys.Config_NumberOfFirefly),
            getValue: () => Tools.ModConfig.NumberOfFireFly,
            setValue: value => Tools.ModConfig.NumberOfFireFly = value,
            min: 0,
            max: 2048
        );
    }

    #endregion
}