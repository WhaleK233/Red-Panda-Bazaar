using Red_Panda_Bazaar_Code.Config;
using Red_Panda_Bazaar_Code.Constant;
using StardewModdingAPI;
using WhaleK.Common;

namespace Red_Panda_Bazaar_Code.Utils;

/// <summary>本 mod 专属的运行时信息（Helper/配置/Monitor/翻译/清单）；通用函数在 WhaleK.Common.Tools。</summary>
public static class ModInfo {
    public static IModHelper Helper { get; set; } = null!;
    public static ModConfig ModConfig { get; set; } = null!;
    public static IMonitor Monitor { get; set; } = null!;
    public static ITranslationHelper I18n { get; set; } = null!;
    public static IManifest ModManifest { get; set; } = null!;

    /// <summary>ModEntry 启动时调用一次：注入本 mod 信息，并把公共运行时交给 WhaleK.Common.Tools。</summary>
    public static void Init(IModHelper helper, ModConfig modConfig, IMonitor monitor, IManifest manifest) {
        Helper = helper;
        ModConfig = modConfig;
        Monitor = monitor;
        ModManifest = manifest;
        I18n = helper.Translation;
        Tools.Init(helper, monitor, manifest.UniqueID,
            () => Tools.GetI18n(I18nKeys.Dialogue_MoneyNotEnough).ToString());
    }
}
