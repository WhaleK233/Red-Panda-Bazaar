using WhaleK.Common;
using WhaleK.Common.Integrations;
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
        InitDevDebugKit();
        InitializeGenericModConfigMenu();
        AddCentralStationDestination();
    }

    /// <summary>调试通用功能拆分到了独立模组 Dev Debug Kit，这里取它的 API 供 Debug.IsEnabled 查询；未安装时保持 null（视为关闭）。</summary>
    private static void InitDevDebugKit()
    {
        Installed.DevDebugKitApi = ModInfo.Helper.ModRegistry.GetApi<IDevDebugKitApi>(ID.DevDebugKit);
    }

    private static void AddCentralStationDestination()
    {
        var centralStationApi = ModInfo.Helper.ModRegistry.GetApi<ICentralStationApi>(ID.CentralStation);
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
        var configMenuApi = ModInfo.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(ID.GenericModConfigMenu);
        if (configMenuApi == null) return;

        Installed.GenericModConfigMenu = true;

        // 注册模组
        configMenuApi.Register(
            mod: ModInfo.ModManifest,
            reset: () => ModInfo.ModConfig = new ModConfig(),
            save: () => ModInfo.Helper.WriteConfig(ModInfo.ModConfig)
        );

        // 萤火虫数量
        configMenuApi.AddNumberOption(
            mod: ModInfo.ModManifest,
            name: () => Tools.GetI18n(I18nKeys.Config_NumberOfCritterMultiplier),
            getValue: () => ModInfo.ModConfig.CritterMultiplier,
            setValue: value => ModInfo.ModConfig.CritterMultiplier = (float)Math.Round(value, 1),
            min: 0.5f,
            max: 2.0f,
            formatValue: value => Math.Round(value, 1) + "×"
        );

        //动画速度
        configMenuApi.AddNumberOption(
            mod: ModInfo.ModManifest,
            name: () => Tools.GetI18n(I18nKeys.Config_AnimeSpeed_PrizeMenu),
            getValue: () => ModInfo.ModConfig.AnimationSpeed_PrizeMenu_Multiplier,
            setValue: value => ModInfo.ModConfig.AnimationSpeed_PrizeMenu_Multiplier = (float)Math.Round(value, 1),
            min: 0.5f,
            max: 5.0f,
            formatValue: value => Math.Round(value, 1) + "×"
        );
    }

    public static class Installed
    {
        public static bool GenericModConfigMenu { get; set; } = false;
        public static bool CentralStation { get; set; } = false;

        /// <summary>Dev Debug Kit 的调试状态 API，未安装该模组时为 null。</summary>
        public static IDevDebugKitApi? DevDebugKitApi { get; set; }
    }

    private static class ID
    {
        public const string GenericModConfigMenu = "spacechase0.GenericModConfigMenu";
        public const string CentralStation = "Pathoschild.CentralStation";
        public const string DevDebugKit = "WhaleK233.DevDebugKit";
    }
}