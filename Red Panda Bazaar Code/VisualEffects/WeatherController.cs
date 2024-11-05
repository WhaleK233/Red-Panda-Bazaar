using Red_Panda_Bazaar_Code.Config;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Network;

namespace Red_Panda_Bazaar_Code.VisualEffects;

public class WeatherController
{
    private static bool Enabled { get; set; } = false;

    private static ModConfig Config { get; set; } = null;

    private static IModHelper Helper { get; set; } = null;

    private static IMonitor Monitor { get; set; } = null;

    /// <summary>启用部分日期的天气修改</summary>
    public static void Enable(IModHelper helper, IMonitor monitor, ModConfig modConfig)
    {
        // 如果未启用
        if (!Enabled && helper != null && monitor != null && modConfig != null)
        {
            Helper = helper;
            Monitor = monitor;
            Config = modConfig;

            Helper.Events.GameLoop.DayStarted += OnDayStarted;

            Enabled = true;
            Monitor.Log("FireFlyEffects Enabled", LogLevel.Debug);
        }
    }

    [EventPriority(EventPriority.High)]
    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        // 如果今天是春7, 调整第二天天气为微风
        if (Game1.Date.Season == Season.Spring && Game1.Date.DayOfMonth == 7)
        {
            Game1.weatherForTomorrow = Game1.weather_debris;
        }

        // 如果今天是春8, 覆盖今天天气为微风
        if (Game1.Date.Season == Season.Spring && Game1.Date.DayOfMonth == 8)
        {
            LocationWeather weatherForLocation = Game1.netWorldState.Value.GetWeatherForLocation("Default");
            weatherForLocation.isDebrisWeather.Value = true;
            Game1.ApplyWeatherForNewDay();
        }
    }
}