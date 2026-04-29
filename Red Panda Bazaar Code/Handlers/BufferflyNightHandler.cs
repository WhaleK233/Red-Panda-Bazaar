using Red_Panda_Bazaar_Code.Custom;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Handlers;

public class BufferflyNightHandler
{
    public static void Init()
    {
        Tools.Log("Bufferfly Night Initializing.");

        Tools.Helper.Events.GameLoop.DayStarted += OnDayStarted;
        Tools.Helper.Events.Player.Warped += OnPlayerWarped;

        Tools.Log("Bufferfly Night Initialized.");
    }

    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        // 如果今天是秋10, 调整第二天天气为大风
        if (Game1.Date.Season == Season.Fall && Game1.Date.DayOfMonth == 10)
        {
            Game1.weatherForTomorrow = Game1.weather_debris;
        }

        // 如果今天是秋11, 覆盖今天天气为大风
        if (Game1.Date.Season == Season.Fall && Game1.Date.DayOfMonth == 11)
        {
            Game1.netWorldState.Value.GetWeatherForLocation("Default").isDebrisWeather.Value = true;
            Game1.ApplyWeatherForNewDay();
        }
    }

    private static void OnPlayerWarped(object? sender, WarpedEventArgs e)
    {
        if (Game1.season is Season.Fall && Game1.dayOfMonth == 11 && Game1.timeOfDay >= 1600)
        {
            RPB_Critters.spawns(Game1.currentLocation, RPB_Critters.Butterfly);
        }
    }
}