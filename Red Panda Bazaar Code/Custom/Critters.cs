using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace Red_Panda_Bazaar_Code.Custom;

public static class Critters
{
    public const int Firefly = 0;
    public const int Butterfly = 1;

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

    internal static Critter GetNewCritter(GameLocation loc, Vector2 tile, int cType)
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
    internal static int GetNumber(GameLocation loc, int type)
    {
        var area = loc.Map.GetLayer("Back").LayerSize.Area;

        var p = Tools.ModConfig.CritterMultiplier;
        p *= type switch
        {
            0 => 0.03f,
            1 => 0.02f,
            _ => 0.0f
        };

        return (int)(area * p);
    }
}