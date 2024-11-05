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

    private static ModConfig _modConfig { get; set; } = null;

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
            _modConfig = modConfig;
            
            // 玩家移动到新位置
            Helper.Events.Player.Warped += OnPlayerWarped;

            Enabled = true;
            Monitor.Log("FireFlyEffects Enabled", LogLevel.Debug);
        }
    }

    private static void OnPlayerWarped(object? sender, WarpedEventArgs e)
    {
        spawnFireFly(e.NewLocation);
    }

    private static void spawnFireFly(GameLocation location)
    {
        if (_modConfig.Enabled != true || location == null ||
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
            if (chance < 0.3 && targetNumber >= 1)
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

    private static Critter SpawnNewFireFly(string locationName, Vector2 tile)
    {
        return new Firefly(tile);
    }

    private static int GetNumberOfFireFly(string locationName)
    {
        switch (locationName)
        {
            case "Custom_RedPandaBazaar": return _modConfig.NumberOfFireFly;
            case "Custom_RedPandaLake": return (int)(_modConfig.NumberOfFireFly * 0.7);
            case "Custom_RedPandaBridge": return (int)(_modConfig.NumberOfFireFly * 0.6);
            case "Custom_MirrorLake": return (int)(_modConfig.NumberOfFireFly * 0.5);
            case "Custom_MapleBridge": return (int)(_modConfig.NumberOfFireFly * 0.4);
            case "Custom_LiQingFengCourtyard": return (int)(_modConfig.NumberOfFireFly * 0.3);
            case "Custom_BazaarWest": return (int)(_modConfig.NumberOfFireFly * 0.2);
            default: return 0;
        }
    }
}