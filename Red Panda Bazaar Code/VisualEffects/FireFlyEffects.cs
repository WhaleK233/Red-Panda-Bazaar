using Microsoft.Xna.Framework;
using RedPandaBazaarCode.Config;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace Red_Panda_Bazaar_Code.VisualEffects;

public class FireFlyEffects
{
    private static bool Enabled { get; set; } = false;

    private static ModConfig Config { get; set; } = null;

    private static IModHelper Helper { get; set; } = null;

    private static IMonitor Monitor { get; set; } = null;

    /// <summary>启用萤火虫效果</summary>
    public static void Enable(IModHelper helper, IMonitor monitor, ModConfig modConfig)
    {
        // 如果未启用
        if (!Enabled && helper != null && monitor != null && modConfig != null)
        {
            Helper = helper;
            Monitor = monitor;
            Config = modConfig;
            
            Helper.Events.Player.Warped += OnPlayerWarped;
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

            Enabled = true;
            Monitor.Log("FireFlyEffects Enabled", LogLevel.Debug);
        }
    }

    /// <summary>判断前一tick是否在事件中</summary>
    private static bool wasEvent = false;

    private static int ticksUntilSpawn = -1;

    private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (ticksUntilSpawn == 0)
        {
            spawnFireFly(Game1.player.currentLocation);
            ticksUntilSpawn = -1;
        }
        else if (wasEvent == true && Game1.CurrentEvent == null) // 如果此tick有事件结束
        {
            ticksUntilSpawn = 10;
        }

        wasEvent = Game1.CurrentEvent != null;

        if (ticksUntilSpawn > 0) ticksUntilSpawn--;
    }

    private static void OnPlayerWarped(object? sender, WarpedEventArgs e)
    {
        spawnFireFly(e.NewLocation);
    }

    /// <summary>根据位置生成萤火虫</summary>
    private static void spawnFireFly(GameLocation location)
    {
        if (Config.Enabled != true || location == null ||
            Game1.timeOfDay < Game1.getStartingToGetDarkTime(location)) return;

        var locationName = location.name.Value ?? "";

        location.instantiateCrittersList();

        int targetNumber = GetNumberOfFireFly(locationName);

        while (targetNumber > 0)
        {
            var tile = location.getRandomTile();
            location.critters.Add(SpawnNewFireFly(locationName, tile));
            targetNumber--;

            var chance = Game1.random.NextDouble();
            if (chance < 0.2 && targetNumber >= 1)
            {
                var nearTile = new Vector2(tile.X + Game1.random.Next(-2, 3), tile.Y + Game1.random.Next(-2, 3));
                location.critters.Add(SpawnNewFireFly(locationName, nearTile));
                targetNumber--;
            }

            if (chance < 0.1 && targetNumber >= 2)
            {
                var nearTile = new Vector2(tile.X + Game1.random.Next(-2, 3), tile.Y + Game1.random.Next(-2, 3));
                location.critters.Add(SpawnNewFireFly(locationName, nearTile));
                nearTile = new Vector2(tile.X + Game1.random.Next(-2, 3), tile.Y + Game1.random.Next(-2, 3));
                location.critters.Add(SpawnNewFireFly(locationName, nearTile));
                targetNumber -= 2;
            }
        }
    }

    /// <summary>生成萤火虫</summary>
    private static Critter SpawnNewFireFly(string locationName, Vector2 tile)
    {
        return new Firefly(tile);
    }

    /// <summary>根据玩家当前位置获取要生成的萤火虫的数量</summary>
    private static int GetNumberOfFireFly(string locationName)
    {
        /*switch (locationName)
        {
            case "Custom_RedPandaBazaar": return Config.NumberOfFireFly;
            case "Custom_RedPandaLake": return (int)(Config.NumberOfFireFly * 0.7);
            case "Custom_RedPandaBridge": return (int)(Config.NumberOfFireFly * 0.6);
            case "Custom_MirrorLake": return (int)(Config.NumberOfFireFly * 0.5);
            case "Custom_MapleBridge": return (int)(Config.NumberOfFireFly * 0.4);
            case "Custom_LiQingFengCourtyard": return (int)(Config.NumberOfFireFly * 0.3);
            case "Custom_BazaarWest": return (int)(Config.NumberOfFireFly * 0.2);
            default: return 0;
        }*/
        
        var fireFlyCounts = new Dictionary<string, double>
        {
            { "Custom_RedPandaBazaar", 1.0 },
            { "Custom_RedPandaLake", 0.7 },
            { "Custom_RedPandaBridge", 0.6 },
            { "Custom_MirrorLake", 0.5 },
            { "Custom_MapleBridge", 0.4 },
            { "Custom_LiQingFengCourtyard", 0.3 },
            { "Custom_BazaarWest", 0.2 }
        };

        if (fireFlyCounts.TryGetValue(locationName, out double multiplier))
        {
            return (int)(Config.NumberOfFireFly * multiplier);
        }

        return 0;
    }
}