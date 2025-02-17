using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Custom;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI.Events;
using StardewValley;

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

    private static readonly List<string> BCs = new()
    {
        ItemsKeys.Machines.StatuePlants1,
        ItemsKeys.Machines.StatuePlants2,
        ItemsKeys.Machines.StatuePlants3
    };

    private static int Counter;

    private static bool IsRightLoc;

    private static int ButterflyCount;

    private static int FireflyCount;

    /// <summary>启用粒子效果</summary>
    public static void Init()
    {
        Tools.Log("CritterEffects Initializing.");

        Tools.Helper.Events.Player.Warped += OnPlayerWarped;
        Tools.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

        Tools.Log("CritterEffects Initialized.");
    }

    private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!IsRightLoc || Counter < 30) return;
        Counter++;
        var loc = Game1.currentLocation;
        if (Tools.IsDayTime(loc) && loc.critters.Count < ButterflyCount)
        {
            var tile = loc.getRandomTile();
            loc.critters.Add(RPB_Critters.GetNewCritter(loc, tile, RPB_Critters.Butterfly));
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
            loc.critters.Add(RPB_Critters.GetNewCritter(loc, tile, RPB_Critters.Firefly));
            Tools.Log("Spawn one critter");
            Counter = 0;
        }
    }

    private static void OnPlayerWarped(object? sender, WarpedEventArgs e)
    {
        var loc = e.NewLocation;
        ButterflyCount = RPB_Critters.GetNumber(loc, RPB_Critters.Butterfly);
        FireflyCount = RPB_Critters.GetNumber(loc, RPB_Critters.Firefly);

        if (Game1.season is Season.Winter or Season.Fall || !Tools.IsGoodWeather()) return;
        if (!Tools.IsDuskTime(loc))
        {
            foreach (var pair in loc.Objects.Pairs)
            {
                var bc = pair.Value;
                if (!bc.bigCraftable.Value || !BCs.Contains(bc.name)) continue;
                if (Tools.IsDayTime(loc)) RPB_Critters.spawns(loc, RPB_Critters.Butterfly);
                else if (Tools.IsNightTime(loc)) RPB_Critters.spawns(loc, RPB_Critters.Firefly);
                IsRightLoc = true;
                break;
            }
        }
        else if (Tools.IsNightTime(loc) && Maps.Contains(loc.Name))
        {
            RPB_Critters.spawns(loc, RPB_Critters.Firefly);
            IsRightLoc = true;
        }
        else
        {
            IsRightLoc = false;
        }
    }
}