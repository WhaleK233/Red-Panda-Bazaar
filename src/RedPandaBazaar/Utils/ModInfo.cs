using Red_Panda_Bazaar_Code.Config;
using Red_Panda_Bazaar_Code.Constant;
using StardewModdingAPI;

namespace Red_Panda_Bazaar_Code.Utils;

/// <summary>本 mod 专属的运行时信息（Helper/配置/Monitor/翻译/清单）；通用函数见本目录的 Tools。</summary>
public static class ModInfo
{
    public static IModHelper Helper { get; set; } = null!;
    public static ModConfig ModConfig { get; set; } = null!;
    public static IMonitor Monitor { get; set; } = null!;
    public static ITranslationHelper I18n { get; set; } = null!;
    public static IManifest ModManifest { get; set; } = null!;

    /// <summary>ModEntry 启动时调用一次，注入本 mod 的运行时信息。</summary>
    public static void Init(IModHelper helper, ModConfig modConfig, IMonitor monitor, IManifest manifest)
    {
        Helper = helper;
        ModConfig = modConfig;
        Monitor = monitor;
        ModManifest = manifest;
        I18n = helper.Translation;
        Tools.Init(helper, monitor, manifest.UniqueID,
            () => Tools.GetI18n(I18nKeys.Dialogue_MoneyNotEnough).ToString());

        NormalizeConfig();
    }

    /// <summary>归一化配置值。GMCM 有范围校验，但直接手改 config.json 会绕过它：
    /// 抽奖机动画倍率为 0/NaN 时出奖计时永不结束，菜单会卡死到无法关闭。</summary>
    private static void NormalizeConfig()
    {
        if (!float.IsFinite(ModConfig.AnimationSpeed_PrizeMenu_Multiplier) ||
            ModConfig.AnimationSpeed_PrizeMenu_Multiplier < 0.1f)
            ModConfig.AnimationSpeed_PrizeMenu_Multiplier = 1.0f;
        if (!float.IsFinite(ModConfig.CritterMultiplier) || ModConfig.CritterMultiplier < 0f)
            ModConfig.CritterMultiplier = 1.0f;
        else if (ModConfig.CritterMultiplier > 10f)
            ModConfig.CritterMultiplier = 10f;
    }
}
