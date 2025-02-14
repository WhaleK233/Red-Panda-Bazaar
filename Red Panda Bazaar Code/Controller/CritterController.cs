using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
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

    public static int counter = 0;

    public static bool IsRightLoc = false;

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
        counter++;
        var loc = Game1.currentLocation;
        if (IsDuskTime(loc) && counter >= 30 && loc.critters.Count > 0)
        {
            loc.critters.RemoveAt(loc.critters.Count - 1);
            Tools.Log("Remove one critter");
            counter = 0;
        }

        if (Game1.timeOfDay > Game1.getTrulyDarkTime(loc) - 100 &&
            loc.critters.Count <= GetNumber(loc, CType.Firefly) && counter >= 30)
        {
            var tile = loc.getRandomTile();
            loc.critters.Add(GetNewCritter(loc, tile, CType.Firefly));
            Tools.Log("Spawn one critter");
            counter = 0;
        }
    }

    private static void OnPlayerWarped(object? sender, WarpedEventArgs e)
    {
        if (Game1.season is Season.Winter or Season.Fall) return;
        var loc = e.NewLocation;
        if (IsGoodWeather() && !IsDuskTime(loc))
        {
            List<Furniture> furnitureList = new List<Furniture>(loc.furniture);
            foreach (var furniture in furnitureList)
            {
                if (furniture.name == ItemKeys.Furniture.FireFlyFurniture)
                {
                    if (IsDayTime(loc))
                    {
                        spawns(loc, CType.Butterfly);
                        IsRightLoc = true;
                    }
                    else if (IsNightTime(loc))
                    {
                        spawns(loc, CType.Firefly);
                        IsRightLoc = true;
                    }
                }
            }
        }
        else if ((!IsDayTime(loc) && IsGoodWeather() &&
                  Maps.Contains(loc.Name)) ||
                 loc.Name == "Temp" && Game1.CurrentEvent.FestivalName == "SpringFair")
        {
            spawns(loc, CType.Firefly);
            IsRightLoc = true;
        }
        else
        {
            IsRightLoc = false;
        }
    }

    public static void spawns(GameLocation loc, int type)
    {
        var number = GetNumber(loc, type);
        loc.instantiateCrittersList();
        while (number > 0)
        {
            // 生成一个萤火虫
            var tile = loc.getRandomTile();

            loc.critters.Add(GetNewCritter(loc, tile, type));
            number--;

            var chance = Game1.random.NextDouble();
            // 20%的概率在附近生成一个萤火虫
            if (chance is >= 0.1 and < 0.3 && number >= 1)
            {
                var nearTile = GetNearTile(loc, tile);
                loc.critters.Add(GetNewCritter(loc, nearTile, type));
                number--;
            }

            chance = Game1.random.NextDouble();
            // 10%的概率在附近生成两个萤火虫
            if (chance < 0.1 && number >= 2)
            {
                var nearTile = GetNearTile(loc, tile);
                loc.critters.Add(GetNewCritter(loc, nearTile, type));
                nearTile = GetNearTile(loc, tile);
                loc.critters.Add(GetNewCritter(loc, nearTile, type));
                number -= 2;
            }
        }
    }

    private static Critter GetNewCritter(GameLocation loc, Vector2 tile, int cType)
    {
        return cType switch
        {
            0 => new Firefly(tile),
            1 => new Butterfly(loc, tile),
            _ => throw new ArgumentOutOfRangeException(nameof(cType), cType, null)
        };
    }

    private static Vector2 GetNearTile(GameLocation loc, Vector2 tile)
    {
        Vector2 nearTile;

        do
        {
            nearTile = new Vector2(tile.X + Game1.random.Next(-2, 3), tile.Y + Game1.random.Next(-2, 3));
        } while (nearTile.X < 0 || nearTile.Y < 0 || nearTile.X >= loc.Map.DisplayWidth ||
                 nearTile.Y >= loc.Map.DisplayHeight);

        return nearTile;
    }

    /// <summary>根据玩家当前位置获取要生成的萤火虫的数量</summary>
    private static int GetNumber(GameLocation loc, int type)
    {
        var area = loc.Map.GetLayer("Back").LayerSize.Area;
        return (int)(area * GetPercentage(type));
    }

    private static float GetPercentage(int type)
    {
        var b = Tools.ModConfig.CritterMultiplier;
        return type switch
        {
            0 => 0.03f * b,
            1 => 0.02f * b,
            _ => 0.0f
        };
    }

    private static bool IsGoodWeather()
    {
        return !Game1.isRaining && !Game1.isLightning && !Game1.isSnowing;
    }

    private static bool IsDayTime(GameLocation loc)
    {
        return Game1.timeOfDay < Game1.getStartingToGetDarkTime(loc);
    }

    private static bool IsDuskTime(GameLocation loc)
    {
        return !IsDayTime(loc) && !IsNightTime(loc);
    }

    private static bool IsNightTime(GameLocation loc)
    {
        return Game1.timeOfDay > Game1.getTrulyDarkTime(loc);
    }

    public static class CType
    {
        public static readonly int Firefly = 0;
        public static readonly int Butterfly = 1;
    }
}