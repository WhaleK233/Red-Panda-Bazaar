using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Constants;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Locations;
using StardewValley.Objects;

namespace Red_Panda_Bazaar_Code.Controller;

public static class FireFlyController
{
    /// <summary>启用萤火虫效果</summary>
    public static void Init()
    {
        Tools.Helper.Events.Player.Warped += OnPlayerWarped;

        Tools.Log("FireFlyEffects Initialized.");
    }

    private static void OnPlayerWarped(object? sender, WarpedEventArgs e)
    {
        var location = e.NewLocation;
        if (Game1.timeOfDay > Game1.getTrulyDarkTime(location)
            /*&& location.IsOutdoors*/ && !Game1.isRaining && !Game1.isLightning && !Game1.isSnowing)
        {
            List<Furniture> furnitureList = new List<Furniture>();
            if (location is Farm or DecoratableLocation)
            {
                furnitureList = new List<Furniture>(location.furniture);
            }

            foreach (var furniture in furnitureList)
            {
                if (furniture.name == ItemKeys.Furniture.FireFlyFurniture)
                {
                    var area = location.Map.GetLayer("Back").LayerSize.Area;
                    spawns(location, (int)(area * 0.05));
                    return;
                }
            }
        }

        spawnFireFly(location);
    }

    /// <summary>根据位置生成萤火虫</summary>
    public static void spawnFireFly(GameLocation location)
    {
        if (location == null || Game1.timeOfDay < Game1.getStartingToGetDarkTime(location) ||
            Game1.season is Season.Winter or Season.Fall || Game1.isRaining || Game1.isLightning ||
            Game1.isSnowing) return;
        var targetNumber = GetNumberOfFireFly(location);

        spawns(location, targetNumber);
    }

    private static void spawns(GameLocation location, int targetNumber)
    {
        location.instantiateCrittersList();
        while (targetNumber > 0)
        {
            // 生成一个萤火虫
            var tile = location.getRandomTile();

            location.critters.Add(SpawnNewFireFly(tile));
            targetNumber--;

            var chance = Game1.random.NextDouble();
            // 20%的概率在附近生成一个萤火虫
            if (chance < 0.2 && targetNumber >= 1)
            {
                var nearTile = GetNearTile(location, tile);

                location.critters.Add(SpawnNewFireFly(nearTile));
                targetNumber--;
            }

            chance = Game1.random.NextDouble();
            // 10%的概率在附近生成两个萤火虫
            if (chance < 0.1 && targetNumber >= 2)
            {
                var nearTile = GetNearTile(location, tile);
                location.critters.Add(SpawnNewFireFly(nearTile));
                nearTile = GetNearTile(location, tile);
                location.critters.Add(SpawnNewFireFly(nearTile));
                targetNumber -= 2;
            }
        }
    }

    private static Vector2 GetNearTile(GameLocation location, Vector2 tile)
    {
        Vector2 nearTile;

        do
        {
            nearTile = new Vector2(tile.X + Game1.random.Next(-2, 3), tile.Y + Game1.random.Next(-2, 3));
        } while (nearTile.X < 0 || nearTile.Y < 0 || nearTile.X >= location.Map.DisplayWidth ||
                 nearTile.Y >= location.Map.DisplayHeight);

        return nearTile;
    }

    /// <summary>生成萤火虫</summary>
    private static Critter SpawnNewFireFly(Vector2 tile)
    {
        return new Firefly(tile);
    }

    /// <summary>根据玩家当前位置获取要生成的萤火虫的数量</summary>
    private static int GetNumberOfFireFly(GameLocation location)
    {
        var fireFlyDict = new Dictionary<string, double>
        {
            { "Custom_RedPandaBazaar", 1.0 },
            { "Custom_RedPandaLake", 0.7 },
            { "Custom_RedPandaBridge", 0.6 },
            { "Custom_MirrorLake", 0.5 },
            { "Custom_MapleBridge", 0.4 },
            { "Custom_LiQingFengCourtyard", 0.3 },
            { "Custom_BazaarWest", 0.2 }
        };

        if (fireFlyDict.TryGetValue(location.Name, out double multiplier))
        {
            return (int)(Tools.ModConfig.NumberOfFireFly * multiplier);
        }

        if (location.Name == "Temp" && Game1.CurrentEvent.FestivalName == "SpringFair")
        {
            return (int)(Tools.ModConfig.NumberOfFireFly * 0.6);
        }

        return 0;
    }
}