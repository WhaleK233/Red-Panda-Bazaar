using Red_Panda_Bazaar_Code.Config;
using StardewModdingAPI;

namespace Red_Panda_Bazaar_Code.Utils;

public static class Tools
{
    public static IModHelper Helper { get; set; } = null;
    public static ModConfig ModConfig { get; set; } = null;
    public static IMonitor Monitor { get; set; } = null;
    public static ITranslationHelper I18n { get; set; } = null;

    public static void Init(IModHelper helper, ModConfig modConfig, IMonitor monitor)
    {
        Helper = helper;
        ModConfig = modConfig;
        Monitor = monitor;
        
        I18n = Helper.Translation;
    }
}