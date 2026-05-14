using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace Red_Panda_Bazaar_Code.Features.Furniture;

public static class Furniture
{
    private static bool _initialized;

    public static void Init()
    {
        Tools.Log("Furniture Initializing.");

        Tools.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        Tools.Helper.Events.Player.Warped += OnPlayerWarped;

        Tools.Log("Furniture Initialized.");
    }

    /// <summary>存档加载时由主机创建鱼缸。</summary>
    private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsMainPlayer) return;
        EnsureTanksExist();
    }

    /// <summary>若 SaveLoaded 时地图尚未加载，由主机首次进入时补充创建。</summary>
    private static void OnPlayerWarped(object? sender, WarpedEventArgs e)
    {
        if (!Context.IsMainPlayer || _initialized) return;
        if (e.NewLocation.Name != "Custom_MarlinShop1") return;
        EnsureTanksExist();
    }

    private static void EnsureTanksExist()
    {
        var loc = Game1.getLocationFromName("Custom_MarlinShop1");
        if (loc == null) return;
        if (loc.furniture.Any(f => f.QualifiedItemId == "(F)" + ItemsKeys.Furniture.MarlinFishTank1))
            return;

        CreateFishTank(loc, 17f, 12f);
        CreateFishTank(loc, 20f, 17f);
        _initialized = true;
    }

    /// <summary>在指定位置创建一个展示鱼缸（不可交互、不可拆除）。</summary>
    private static void CreateFishTank(GameLocation loc, float x, float y)
    {
        var tank = new FishTankFurniture(ItemsKeys.Furniture.MarlinFishTank1, new Vector2(x, y))
        {
            CanBeGrabbed = false,
            AllowLocalRemoval = false,
            Fragility = 2
        };
        tank.heldItems.Add(ItemRegistry.Create("(O)143"));
        tank.heldItems.Add(ItemRegistry.Create("(O)145"));
        loc.furniture.Add(tank);
    }
}