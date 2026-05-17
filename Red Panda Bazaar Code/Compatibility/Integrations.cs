using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Compatibility.ModApi;
using Red_Panda_Bazaar_Code.Config;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Compatibility;

public static class Integrations
{
    public static void Init()
    {
        InitializeGenericModConfigMenu();
        AddCentralStationDestination();
    }

    private static void AddCentralStationDestination()
    {
        var centralStationApi = Tools.Helper.ModRegistry.GetApi<ICentralStationApi>(ID.CentralStation);
        if (centralStationApi == null) return;

        Installed.CentralStation = true;

        centralStationApi?.RegisterStop(
            id: "RedPandaBazaarStation",
            displayName: () => Tools.GetI18n(I18nKeys.Text_RedPandaBazaar), //"Red Panda Bazaar"
            toLocation: "Custom_MapleBridge",
            toTile: new Point(27, 40),
            toFacingDirection: Game1.down,
            cost: 300,
            network: "Bus",
            condition: null
        );
    }

    private static void InitializeGenericModConfigMenu()
    {
        // 获取通用模组配置菜单的API
        var configMenuApi = Tools.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(ID.GenericModConfigMenu);
        if (configMenuApi == null) return;

        Installed.GenericModConfigMenu = true;

        // 注册模组
        configMenuApi.Register(
            mod: Tools.ModManifest,
            reset: () => Tools.ModConfig = new ModConfig(),
            save: () => Tools.Helper.WriteConfig(Tools.ModConfig)
        );

        // 税率
        configMenuApi.AddNumberOption(
            mod: Tools.ModManifest,
            name: () => Tools.GetI18n(I18nKeys.Config_TaxRate),
            getValue: () => Tools.ModConfig.TaxRate,
            setValue: value => Tools.ModConfig.TaxRate = value,
            min: 0.0f,
            max: 1.0f,
            interval: 0.01f,
            formatValue: value => (int)Math.Round(value * 100) + "%"
        );

        // 萤火虫数量
        configMenuApi.AddNumberOption(
            mod: Tools.ModManifest,
            name: () => Tools.GetI18n(I18nKeys.Config_NumberOfCritterMultiplier),
            getValue: () => Tools.ModConfig.CritterMultiplier,
            setValue: value => Tools.ModConfig.CritterMultiplier = (float)Math.Round(value, 1),
            min: 0.5f,
            max: 2.0f,
            formatValue: value => Math.Round(value, 1) + "×"
        );

        //动画速度
        configMenuApi.AddNumberOption(
            mod: Tools.ModManifest,
            name: () => Tools.GetI18n(I18nKeys.Config_AnimeSpeed_PrizeMenu),
            getValue: () => Tools.ModConfig.AnimationSpeed_PrizeMenu_Multiplier,
            setValue: value => Tools.ModConfig.AnimationSpeed_PrizeMenu_Multiplier = (float)Math.Round(value, 1),
            min: 0.5f,
            max: 5.0f,
            formatValue: value => Math.Round(value, 1) + "×"
        );

        // 调试快捷键
        configMenuApi.AddKeybind(
            mod: Tools.ModManifest,
            name: () => Tools.GetI18n(I18nKeys.Config_DebugKey),
            getValue: () => Enum.TryParse<SButton>(Tools.ModConfig.DebugToggleKey, out var btn) ? btn : SButton.OemTilde,
            setValue: value => Tools.ModConfig.DebugToggleKey = value.ToString()
        );

        // 调试菜单快捷键
        configMenuApi.AddKeybind(
            mod: Tools.ModManifest,
            name: () => Tools.GetI18n(I18nKeys.Config_DebugMenuKey),
            getValue: () => Enum.TryParse<SButton>(Tools.ModConfig.DebugMenuKey, out var btn) ? btn : SButton.F12,
            setValue: value => Tools.ModConfig.DebugMenuKey = value.ToString()
        );

        // 调试传送快捷键
        configMenuApi.AddKeybind(
            mod: Tools.ModManifest,
            name: () => Tools.GetI18n(I18nKeys.Config_DebugTeleportKey),
            getValue: () => Enum.TryParse<SButton>(Tools.ModConfig.DebugTeleportKey, out var btn) ? btn : SButton.None,
            setValue: value => Tools.ModConfig.DebugTeleportKey = value.ToString()
        );
    }

    public static class Installed
    {
        public static bool GenericModConfigMenu { get; set; } = false;
        public static bool CentralStation { get; set; } = false;
    }

    private static class ID
    {
        public const string GenericModConfigMenu = "spacechase0.GenericModConfigMenu";
        public const string CentralStation = "Pathoschild.CentralStation";
    }
}