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

    /// <summary>当前玩家是否在可生成生物的合法位置。</summary>
    private static readonly PerScreen<bool> IsRightLoc = new();

    /// <summary>当前地图蝴蝶数量上限。</summary>
    private static readonly PerScreen<int> ButterflyCount = new();

    /// <summary>当前地图萤火虫数量上限。</summary>
    private static readonly PerScreen<int> FireflyCount = new();

    /// <summary>启用粒子效果。</summary>
    public static void Init()
    {
        Tools.Log("CritterEffects Initializing.");

        Tools.Helper.Events.Player.Warped += OnPlayerWarped;
        Tools.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

        Tools.Log("CritterEffects Initialized.");
    }

    /// <summary>计数器控制生成频率。每 30 tick ≈ 0.5 秒检查一次是否需要生成/移除氛围生物。</summary>
    private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady) return;
        if (Counter.Value < 30) { Counter.Value++; return; }
        Counter.Value = 0;

        if (!IsRightLoc.Value) return;

        var loc = Game1.currentLocation;
        if (loc is null) return;

        // 白天补蝴蝶，直到达到上限
        if (Tools.IsDayTime(loc) && loc.critters.Count < ButterflyCount.Value)
        {
            var tile = loc.getRandomTile();
            loc.critters.Add(CrittersSpawner.GetNewCritter(loc, tile, CrittersSpawner.Butterfly));
        }

        // 黄昏逐渐移除
        if (Tools.IsDuskTime(loc) && loc.critters.Count > 0)
        {
            loc.critters.RemoveAt(loc.critters.Count - 1);
        }

        // 入夜补萤火虫
        if (Game1.timeOfDay > Game1.getTrulyDarkTime(loc) - 100 &&
            loc.critters.Count < FireflyCount.Value)
        {
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
            IsRightLoc.Value = false;
            return;
        }

        // 检查雕像
        var hasStatue = false;
        if (!Tools.IsDuskTime(loc))
        {
            foreach (var pair in loc.Objects.Pairs)
            {
                var bc = pair.Value;
                if (!bc.bigCraftable.Value || !BCs.Contains(bc.ItemId)) continue;
                if (Tools.IsDayTime(loc)) CrittersSpawner.spawns(loc, CrittersSpawner.Butterfly);
                else if (Tools.IsNightTime(loc)) CrittersSpawner.spawns(loc, CrittersSpawner.Firefly);
                hasStatue = true;
                break;
            }
        }

        // 地图列表
        var inMapList = Maps.Contains(loc.Name);

        // 雕像或地图列表，满足其一即可
        IsRightLoc.Value = hasStatue || inMapList;

        // 夜晚且在地图列表里，额外补萤火虫
        if (Tools.IsNightTime(loc) && inMapList)
            CrittersSpawner.spawns(loc, CrittersSpawner.Firefly);
    }
}
