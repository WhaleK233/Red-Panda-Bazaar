using Red_Panda_Bazaar_Code.Compatibility;
using Red_Panda_Bazaar_Code.Config;
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
        Monitor.Log($"Red Panda Bazaar Code Initializing...", LogLevel.Debug);

        Helper2 = helper;
        Config = this.Helper2.ReadConfig<ModConfig>();

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
        // 初始化通用模组配置菜单
        InitializeGenericModConfigMenu(sender, e);

        FireFlyEffects.Enable(Helper2, Monitor, Config);
        WeatherController.Enable(Helper2, Monitor, Config);
    }

    #region Generic Mod Config Menu

    private void InitializeGenericModConfigMenu(object sender, GameLaunchedEventArgs e)
    {
        // 获取通用模组配置菜单的API
        var configMenu = this.Helper2.ModRegistry.GetApi<IGenericModConfigMenuApi>(ModCompat.GenericModConfigMenu);
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
            name: () => this.Helper2.Translation.Get("enabled"),
            getValue: () => Config.Enabled,
            setValue: value => Config.Enabled = value
        );

        configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => this.Helper2.Translation.Get("number-of-firefly"),
            getValue: () => Config.NumberOfFireFly,
            setValue: value => Config.NumberOfFireFly = value,
            min: 0,
            max: 2048
        );
    }

    #endregion
}