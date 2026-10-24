using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.Critters;

/// <summary>在集市地图生成氛围生物（蝴蝶、萤火虫）。使用 PerScreen 隔离多人状态。</summary>
public static class Critter
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

    /// <summary>计数器，控制生成频率。UpdateTicked 每 30 tick（≈ 0.5 秒）执行一次。</summary>
    private static readonly PerScreen<int> Counter = new();

    /// <summary>最近一次判定为可生成生物的地图名；null 表示当前位置不生成。</summary>
    private static readonly PerScreen<string?> RightLocName = new();

    /// <summary>当前地图蝴蝶数量上限。</summary>
    private static readonly PerScreen<int> ButterflyCount = new();

    /// <summary>当前地图萤火虫数量上限。</summary>
    private static readonly PerScreen<int> FireflyCount = new();

    /// <summary>启用粒子效果。</summary>
    public static void Init()
    {
        Tools.Log("CritterEffects Initializing.");

        ModInfo.Helper.Events.Player.Warped += OnPlayerWarped;
        ModInfo.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

        Tools.Log("CritterEffects Initialized.");
    }

    /// <summary>计数器控制生成频率。每 30 tick ≈ 0.5 秒检查一次是否需要生成/移除氛围生物。</summary>
    private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady) return;
        if (Counter.Value < 30) { Counter.Value++; return; }
        Counter.Value = 0;

        var loc = Game1.currentLocation;
        if (loc?.Map is null) return;

        // Warped 事件比本事件晚一 tick 触发（warp 发生在两者之间的游戏更新里），
        // 此时缓存的地图名还停留在上一张地图，不比对就会拿到未初始化 critters 的新地图
        if (RightLocName.Value != loc.Name) return;

        // 原版只对带 Outdoors 属性的地图初始化 critters，自定义地图缺该属性时列表为 null，直接访问会空引用
        loc.instantiateCrittersList();

        // 按类型计数：黄昏移除与入夜补萤火虫的窗口（19:00–20:00）重叠，
        // 不分类型就会「移除列表尾部恰好是刚补的萤火虫」，且直接移除会泄漏它挂在场景里的光源
        var butterflies = 0;
        var fireflies = 0;
        foreach (var c in loc.critters) {
            if (c is StardewValley.BellsAndWhistles.Firefly) fireflies++;
            else butterflies++;
        }

        // 白天补蝴蝶，直到达到上限
        if (TimeUtils.IsDayTime(loc) && butterflies < ButterflyCount.Value) {
            var tile = loc.getRandomTile();
            loc.critters.Add(CrittersSpawner.GetNewCritter(loc, tile, CrittersSpawner.Butterfly));
        }

        // 黄昏逐渐移除蝴蝶（不动萤火虫）
        if (TimeUtils.IsDuskTime(loc) && butterflies > 0) {
            for (var i = loc.critters.Count - 1; i >= 0; i--) {
                if (loc.critters[i] is StardewValley.BellsAndWhistles.Firefly) continue;
                loc.critters.RemoveAt(i);
                break;
            }
        }

        // 入夜补萤火虫
        if (Game1.timeOfDay > Game1.getTrulyDarkTime(loc) - 100 &&
            fireflies < FireflyCount.Value) {
            var tile = loc.getRandomTile();
            loc.critters.Add(CrittersSpawner.GetNewCritter(loc, tile, CrittersSpawner.Firefly));
        }
    }

    /// <summary>进入新地图时初始化生物参数。季节/天气不符时关闭生成。</summary>
    private static void OnPlayerWarped(object? sender, WarpedEventArgs e)
    {
        var loc = e.NewLocation;
        ButterflyCount.Value = CrittersSpawner.GetNumber(loc, CrittersSpawner.Butterfly);
        FireflyCount.Value = CrittersSpawner.GetNumber(loc, CrittersSpawner.Firefly);
        Counter.Value = 0;

        // 季节或天气不符 → 不生成
        if (Game1.season is Season.Winter or Season.Fall || !Tools.IsGoodWeather())
        {
            RightLocName.Value = null;
            return;
        }

        // 检查雕像
        var hasStatue = false;
        if (!TimeUtils.IsDuskTime(loc))
        {
            foreach (var pair in loc.Objects.Pairs)
            {
                var bc = pair.Value;
                if (!bc.bigCraftable.Value || !BCs.Contains(bc.ItemId)) continue;
                if (TimeUtils.IsDayTime(loc)) CrittersSpawner.spawns(loc, CrittersSpawner.Butterfly);
                else if (TimeUtils.IsNightTime(loc)) CrittersSpawner.spawns(loc, CrittersSpawner.Firefly);
                hasStatue = true;
                break;
            }
        }

        // 地图列表
        var inMapList = Maps.Contains(loc.Name);

        // 雕像或地图列表，满足其一即可
        RightLocName.Value = hasStatue || inMapList ? loc.Name : null;

        // 夜晚且在地图列表里，额外补萤火虫
        if (TimeUtils.IsNightTime(loc) && inMapList)
            CrittersSpawner.spawns(loc, CrittersSpawner.Firefly);
    }
}
