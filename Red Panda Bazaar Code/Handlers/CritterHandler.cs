using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Custom;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace Red_Panda_Bazaar_Code.Handlers;

public static class CritterHandler
{
    private static readonly List<string> Maps = new()
    {
        "Custom_RedPandaBazaar",
        "Custom_RedPandaLake",
        "Custom_RedPandaBridge",
        "Custom_MirrorLake",
        "Custom_MapleBridge",
        "Custom_LiQingFengCourtyard",
        "Custom_BazaarWest"
    };

    private static int Counter;

    private static bool IsRightLoc;

    private static int ButterflyCount;

    private static int FireflyCount;

    /// <summary>启用粒子效果</summary>
    public static void Init()
    {
        Tools.Log("CritterEffects Initializing.", LogLevel.Info);

        Tools.Helper.Events.Player.Warped += OnPlayerWarped;
        Tools.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

        Tools.Log("CritterEffects Initialized.", LogLevel.Info);
    }

    private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!IsRightLoc || Counter < 30) return;
        Counter++;
        var loc = Game1.currentLocation;
        if (Tools.IsDayTime(loc) && loc.critters.Count < ButterflyCount)
        {
            var tile = loc.getRandomTile();
            loc.critters.Add(Critters.GetNewCritter(loc, tile, Critters.Butterfly));
            Tools.Log("Spawn one critter");
            Counter = 0;
        }

        if (Tools.IsDuskTime(loc) && loc.critters.Count > 0)
        {
            loc.critters.RemoveAt(loc.critters.Count - 1);
            Tools.Log("Remove one critter");
            Counter = 0;
        }

        if (Game1.timeOfDay > Game1.getTrulyDarkTime(loc) - 100 &&
            loc.critters.Count <= FireflyCount)
        {
            var tile = loc.getRandomTile();
            loc.critters.Add(Critters.GetNewCritter(loc, tile, Critters.Firefly));
            Tools.Log("Spawn one critter");
            Counter = 0;
        }
    }

    private static void OnPlayerWarped(object? sender, WarpedEventArgs e)
    {
        var loc = e.NewLocation;
        ButterflyCount = Critters.GetNumber(loc, Critters.Butterfly);
        FireflyCount = Critters.GetNumber(loc, Critters.Firefly);

        if (Game1.season is Season.Winter or Season.Fall) return;
        if (Tools.IsGoodWeather() && !Tools.IsDuskTime(loc))
        {
            var furnitureList = new List<Furniture>(loc.furniture);
            foreach (var furniture in furnitureList)
            {
                if (furniture.name != ItemKeys.Furniture.FireFlyFurniture) continue;
                if (Tools.IsDayTime(loc))
                {
                    Critters.spawns(loc, Critters.Butterfly);
                    IsRightLoc = true;
                }
                else if (Tools.IsNightTime(loc))
                {
                    Critters.spawns(loc, Critters.Firefly);
                    IsRightLoc = true;
                }
            }
        }
        else if ((!Tools.IsDayTime(loc) && Tools.IsGoodWeather() &&
                  Maps.Contains(loc.Name)) ||
                 loc.Name == "Temp" && Game1.CurrentEvent.FestivalName == "SpringFair")
        {
            Critters.spawns(loc, Critters.Firefly);
            IsRightLoc = true;
        }
        else
        {
            IsRightLoc = false;
        }
    }
}