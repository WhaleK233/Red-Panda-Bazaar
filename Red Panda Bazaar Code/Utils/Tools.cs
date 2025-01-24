using Red_Panda_Bazaar_Code.Config;
using StardewModdingAPI;
using StardewValley;

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

    public static void Log(string message, LogLevel level = LogLevel.Trace) => Monitor.Log(message, level);

    public static void LogOnce(string message, LogLevel level = LogLevel.Trace) =>
        Monitor.LogOnce(message, level);

    public static bool Charge(int cost)
    {
        if (Game1.player.Money >= cost)
        {
            Game1.player.Money -= cost;
            return true;
        }
        else
        {
            Game1.drawObjectDialogue(I18n.Get(I18nKeys.Dialogue_MoneyNotEnough));
            return false;
        }
    }
}