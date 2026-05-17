using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace Red_Panda_Bazaar_Code.Features.Critters;

/// <summary>集市氛围生物（蝴蝶、萤火虫）的生成与数量计算。</summary>
public static class CrittersSpawner
{
    public const int Firefly = 0;
    public const int Butterfly = 1;

    /// <summary>在地图上批量生成指定类型的氛围生物，附带随机成团效果。</summary>
    /// <param name="loc">目标地图。</param>
    /// <param name="type"><see cref="Firefly"/> 或 <see cref="Butterfly"/>。</param>
    public static void spawns(GameLocation loc, int type)
    {
        var number = GetNumber(loc, type);
        loc.instantiateCrittersList();
        while (number > 0)
        {
            var tile = loc.getRandomTile();

            // 主体
            loc.critters.Add(GetNewCritter(loc, tile, type));
            number--;

            // 20% 概率在主体附近额外生成一只
            var chance = RandomUtils.NextDouble();
            if (chance is >= 0.1 and < 0.3 && number >= 1)
            {
                var nearTile = GetNearTile(loc, tile);
                loc.critters.Add(GetNewCritter(loc, nearTile, type));
                number--;
            }

            // 10% 概率在主体附近额外生成两只（独立掷骰）
            chance = RandomUtils.NextDouble();
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

    /// <summary>创建一只氛围生物实例。</summary>
    /// <param name="loc">所在地图。</param>
    /// <param name="tile">生成坐标（像素）。</param>
    /// <param name="cType">生物类型：0 = 萤火虫，1 = 蝴蝶。</param>
    internal static StardewValley.BellsAndWhistles.Critter GetNewCritter(GameLocation loc, Vector2 tile, int cType)
    {
        return cType switch
        {
            0 => new Firefly(tile),
            1 => new Butterfly(loc, tile),
            _ => throw new ArgumentOutOfRangeException(nameof(cType), cType, null)
        };
    }

    /// <summary>在目标附近 ±2 格内找一个合法坐标。搜索范围小，不会真的死循环。</summary>
    /// <param name="loc">目标地图。</param>
    /// <param name="tile">中心坐标。</param>
    private static Vector2 GetNearTile(GameLocation loc, Vector2 tile)
    {
        var layer = loc.Map.GetLayer("Back");
        Vector2 nearTile;

        do
        {
            nearTile = new Vector2(tile.X + RandomUtils.Next(-2, 3), tile.Y + RandomUtils.Next(-2, 3));
        } while (nearTile.X < 0 || nearTile.Y < 0 || nearTile.X >= layer.LayerWidth ||
                 nearTile.Y >= layer.LayerHeight);

        return nearTile;
    }

    /// <summary>根据地图面积和配置倍率计算应生成的生物数量。</summary>
    /// <param name="loc">目标地图。</param>
    /// <param name="type"><see cref="Firefly"/> 或 <see cref="Butterfly"/>。</param>
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
