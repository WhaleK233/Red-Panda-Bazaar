using Red_Panda_Bazaar_Code.Compatibility;
using Red_Panda_Bazaar_Code.VisualEffects;
using RedPandaBazaarCode.Config;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Red_Panda_Bazaar_Code;

public class ModEntry : Mod
{
    private ModConfig _modConfig;

    public override void Entry(IModHelper helper)
    {
        Monitor.Log($"Red Panda Bazaar Code Initializing...", LogLevel.Debug);

        _modConfig = this.Helper.ReadConfig<ModConfig>();

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;

        FireFlyEffects.Enable(helper, Monitor, _modConfig);
    }
    
    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
        InitializeGenericModConfigMenu(sender, e);
    }
    
    #region Generic Mod Config Menu
    private void InitializeGenericModConfigMenu(object sender, GameLaunchedEventArgs e)
    {
        // 获取通用模组配置菜单的api
        var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(ModCompat.GenericModConfigMenu);
        if (configMenu is null) return;

        // 注册模组
        configMenu.Register(
            mod: this.ModManifest,
            reset: () => _modConfig = new ModConfig(),
            save: () => this.Helper.WriteConfig(_modConfig)
        );

        // 添加选项
        configMenu.AddBoolOption(
            mod: this.ModManifest,
            name: () => this.Helper.Translation.Get("enabled"),
            tooltip: () => "The switch of the mod",
            getValue: () => _modConfig.Enabled,
            setValue: value => _modConfig.Enabled = value
        );

        configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => this.Helper.Translation.Get("number-of-firefly"),
            getValue: () => _modConfig.NumberOfFireFly,
            setValue: value => _modConfig.NumberOfFireFly = value,
            min: 0,
            max: 2048
        );
    }
    #endregion
}