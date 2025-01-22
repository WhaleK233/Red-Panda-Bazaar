using HarmonyLib;
using Red_Panda_Bazaar_Code.Compatibility;
using Red_Panda_Bazaar_Code.Config;
using Red_Panda_Bazaar_Code.Controller;
using Red_Panda_Bazaar_Code.Data;
using Red_Panda_Bazaar_Code.HarmonyPatch;
using Red_Panda_Bazaar_Code.Utils;
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
        Tools.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
    }


    // 包含i18n的初始化
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        BuffController.Init();
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
        EntranceController.Init();
        FireFlyController.Init();
        FestivalController.Init();
        MenuController.Init();
        QuestController.Init();
    }

    private void PatchHarmony()
    {
        var harmony = new Harmony(this.ModManifest.UniqueID);
        HarmonyPatch_FishingGameEvent.ApplyPatch(harmony);
        HarmonyPatch_CustomBuffEffects.ApplyPatch(harmony);
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

        //动画速度
        configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => Tools.I18n.Get(I18nKeys.Config_AnimeSpeed_PrizeMenu),
            getValue: () => Tools.ModConfig.AnimationSpeed_PrizeMenu,
            setValue: value => Tools.ModConfig.AnimationSpeed_PrizeMenu = (float)Math.Round(value, 1),
            min: 0.5f,
            max: 5.0f,
            formatValue: value => Math.Round(value, 1) + "x"
        );
    }

    #endregion
}