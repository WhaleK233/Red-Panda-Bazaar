using HarmonyLib;
using Red_Panda_Bazaar_Code.Buffs;
using Red_Panda_Bazaar_Code.Compatibility;
using Red_Panda_Bazaar_Code.Config;
using Red_Panda_Bazaar_Code.Festivals;
using Red_Panda_Bazaar_Code.Festivals.MiniGames;
using Red_Panda_Bazaar_Code.Menus;
using Red_Panda_Bazaar_Code.Utils;
using Red_Panda_Bazaar_Code.VisualEffects;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Red_Panda_Bazaar_Code;

public class ModEntry : Mod
{
    private ModConfig Config { get; set; } = null;

    private IModHelper Helper2 { get; set; } = null;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();

        Tools.Init(helper, Config, Monitor);

        Tools.Log($"Red Panda Bazaar Code Initializing...");

        Tools.Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        InitializeGenericModConfigMenu();
        EnableEffects();
        PatchHarmony();
    }

    private static void EnableEffects()
    {
        FireFlyEffects.Enable();
        SpringFairEffects.Enable();
        BuffEffects.Enable();
        MenuEffects.Enable();
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
            reset: () => Config = new ModConfig(),
            save: () => this.Helper2.WriteConfig(Config)
        );

        // 添加选项
        configMenu.AddBoolOption(
            mod: this.ModManifest,
            name: () => Tools.I18n.Get(I18nKeys.Config_Enable),
            getValue: () => Config.Enabled,
            setValue: value => Config.Enabled = value
        );

        configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => Tools.I18n.Get(I18nKeys.Config_NumberOfFirefly),
            getValue: () => Config.NumberOfFireFly,
            setValue: value => Config.NumberOfFireFly = value,
            min: 0,
            max: 2048
        );
    }

    #endregion
}