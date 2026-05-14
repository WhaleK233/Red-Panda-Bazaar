using Red_Panda_Bazaar_Code.Features.Critters;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.ButterflyNight;

public static class ButterflyNight
{
    public static void Init()
    {
        Tools.Log("Butterfly Night Initializing.");

        Tools.Helper.Events.GameLoop.DayStarted += OnDayStarted;
        Tools.Helper.Events.Player.Warped += OnPlayerWarped;

        Tools.Log("Butterfly Night Initialized.");
    }

    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!Context.IsMainPlayer) return;

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
        if (Game1.Date.Season != Season.Fall || Game1.dayOfMonth != 11) return;

        if (Game1.timeOfDay >= 1600)
            CrittersSpawner.spawns(Game1.currentLocation, CrittersSpawner.Butterfly);

        // 无论通过何种方式到达秋11，确保天气为大风（仅主机）
        if (Context.IsMainPlayer && !Game1.isDebrisWeather)
            Game1.netWorldState.Value.GetWeatherForLocation("Default").isDebrisWeather.Value = true;
    }
}