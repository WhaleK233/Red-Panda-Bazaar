using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Custom;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace Red_Panda_Bazaar_Code.Controller;

public static class CritterController
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

    private static int Counter = 0;

    private static bool IsRightLoc = false;

    /// <summary>启用萤火虫效果</summary>
    public static void Init()
    {
        Tools.Helper.Events.Player.Warped += OnPlayerWarped;
        Tools.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

        Tools.Log("CritterEffects Initialized.");
    }

    private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!IsRightLoc) return;
        Counter++;
        var loc = Game1.currentLocation;
        if (Tools.IsDuskTime(loc) && Counter >= 30 && loc.critters.Count > 0)
        {
            loc.critters.RemoveAt(loc.critters.Count - 1);
            Tools.Log("Remove one critter");
            Counter = 0;
        }

        if (Game1.timeOfDay > Game1.getTrulyDarkTime(loc) - 100 &&
            loc.critters.Count <= Critters.GetNumber(loc, Critters.Firefly) && Counter >= 30)
        {
            var tile = loc.getRandomTile();
            loc.critters.Add(Critters.GetNewCritter(loc, tile, Critters.Firefly));
            Tools.Log("Spawn one critter");
            Counter = 0;
        }
    }

    private static void OnPlayerWarped(object? sender, WarpedEventArgs e)
    {
        if (Game1.season is Season.Winter or Season.Fall) return;
        var loc = e.NewLocation;
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