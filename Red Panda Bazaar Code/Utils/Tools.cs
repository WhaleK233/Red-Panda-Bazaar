using Red_Panda_Bazaar_Code.Config;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Utils;

public static class Tools
{
    public static IModHelper Helper { get; set; } = null;
    public static ModConfig ModConfig { get; set; } = null;
    public static IMonitor Monitor { get; set; } = null;
    public static ITranslationHelper I18n { get; set; } = null;

    public static int PrizeRandomInt { get; set; }

    public static void Init(IModHelper helper, ModConfig modConfig, IMonitor monitor)
    {
        Helper = helper;
        ModConfig = modConfig;
        Monitor = monitor;

        I18n = Helper.Translation;

        Tools.Helper.Events.GameLoop.DayStarted += OnDayStarted;
        Tools.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        Tools.Helper.Events.GameLoop.Saving += OnSaving;
    }

    private static void OnSaving(object? sender, SavingEventArgs e)
    {
        Tools.Helper.Data.WriteSaveData(Keys.PrizeRandomIntKey, PrizeRandomInt.ToString());
        Tools.Log($"Saving Prize Random Number");
    }

    private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        PrizeRandomInt = int.Parse(Tools.Helper.Data.ReadSaveData<string>(Keys.PrizeRandomIntKey) ??
                                   $"{Game1.random.Next()}");
        Tools.Log($"Loaded Prize Random Number");
    }

    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (Game1.dayOfMonth % 7 == 1)
        {
            PrizeRandomInt = Game1.random.Next();
        }

        Tools.Log($"Today's Prize Random Number is {PrizeRandomInt}");
    }

    public static int PrizeIncrement()
    {
        return ++PrizeRandomInt;
    }

    public static void Log(string message, LogLevel level = LogLevel.Trace) => Tools.Monitor.Log(message, level);

    public static void LogOnce(string message, LogLevel level = LogLevel.Trace) =>
        Tools.Monitor.LogOnce(message, level);

    internal static class Keys
    {
        public const string PrizeRandomIntKey = "PrizeRandomIntPerWeek";
    }
}