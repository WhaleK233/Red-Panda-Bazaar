using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.PlayerStall;

/// <summary>
/// 玩家摊位系统：支持多个 Action 摊位，各自独立概率和物品。
/// 按职责拆在四个文件：本文件放入口、共享状态与地图交互（点击 / 坐标扫描 / 图标绘制），
/// 数据读写见 PlayerStallData.cs，多人同步见 PlayerStallSync.cs，夜间结算与账单见 PlayerStallSettlement.cs。
/// </summary>
public static partial class PlayerStall
{
    public const int CollectPending = -2;

    /// <summary>已注册的摊位配置，key 为 ActionId。</summary>
    private static readonly Dictionary<string, StallConfig> RegisteredStalls = new();

    /// <summary>摊位存档数据。主机权威，客机通过同步消息覆盖。</summary>
    private static StallSaveData Data { get; set; } = new();

    /// <summary>摊位图块坐标缓存（进入地图时扫描 Action 属性获得）。</summary>
    private static readonly Dictionary<string, Vector2> StallTilePositions = new();

    private const float StallItemScale = 0.75f;

    public static void Init()
    {
        Tools.Log("[PlayerStall] Initializing.");
        ItemSerializer.WarmUp();
        RegisterAll();
        ModInfo.Helper.Events.Input.ButtonPressed += OnButtonPressed;
        ModInfo.Helper.Events.GameLoop.DayEnding += OnDayEnding;
        ModInfo.Helper.Events.GameLoop.Saving += OnSaving;
        ModInfo.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        ModInfo.Helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        ModInfo.Helper.Events.GameLoop.DayStarted += OnDayStarted;
        ModInfo.Helper.Events.Player.Warped += OnPlayerWarped;
        ModInfo.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
        ModInfo.Helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        ModInfo.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        Tools.Log("[PlayerStall] Initialized.");
    }

    /// <summary>基于鼠标位置打开摊位菜单，避免走 TileAction 的朝向判断。</summary>
    private static void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Tools.IsValidButtonAction(e))
            return;
        if (Game1.currentLocation is not { Name: "Custom_FarmerShop1" })
            return;

        var tile = e.Cursor.GrabTile;
        var action = Game1.currentLocation.doesTileHaveProperty(
            (int)tile.X, (int)tile.Y, "Action", "Buildings");
        if (action == null || !action.StartsWith("RedPandaBazaar_PlayerStall_"))
            return;

        // 鼠标交互绕过了 TileAction 路由，也就绕开了原版的贴近校验，这里手动补上
        if (!Utility.tileWithinRadiusOfPlayer((int)tile.X, (int)tile.Y, 2, Game1.player))
            return;

        Game1.activeClickableMenu = new PlayerStallMenu(action);
        ModInfo.Helper.Input.Suppress(e.Button);
    }

    /// <summary>进入摊位地图时扫描所有 tile，缓存 Action 对应的图块坐标。</summary>
    private static void OnPlayerWarped(object? sender, WarpedEventArgs e)
    {
        if (e.NewLocation.Name != "Custom_FarmerShop1")
        {
            // 离开地图时清理缓存
            if (StallTilePositions.Count > 0 && e.OldLocation?.Name == "Custom_FarmerShop1")
                StallTilePositions.Clear();
            return;
        }

        StallTilePositions.Clear();
        var map = e.NewLocation.Map;
        if (map?.Layers == null || map.Layers.Count == 0)
            return;

        var width = map.Layers[0].LayerWidth;
        var height = map.Layers[0].LayerHeight;

        for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
            {
                var action = e.NewLocation.doesTileHaveProperty(x, y, "Action", "Buildings");
                if (action == null || !action.StartsWith("RedPandaBazaar_PlayerStall_"))
                    continue;
                StallTilePositions[action] = new Vector2(x, y);
            }
    }

    /// <summary>在地图摊位 tile 中心绘制该摊位的物品图标和价格。</summary>
    private static void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;
        if (Game1.currentLocation?.Name != "Custom_FarmerShop1")
            return;
        if (StallTilePositions.Count == 0)
            return;

        var b = e.SpriteBatch;

        foreach (var kv in StallTilePositions)
        {
            var actionId = kv.Key;
            var tilePos = kv.Value;

            if (!Data.Stock.TryGetValue(actionId, out var stock))
                continue;
            var obj = stock.Select(i => i.GetItem()).FirstOrDefault(i => i != null);
            if (obj == null)
                continue;

            var screenPos = Game1.GlobalToLocal(Game1.viewport,
                new Vector2(tilePos.X * 64f - 8f, tilePos.Y * 64f - 16f));

            // tile 中心绘制物品（64x64 居中）
            var offset = (64 - 64 * StallItemScale) / 2;
            obj.drawInMenu(b, screenPos + new Vector2(offset, offset), StallItemScale);
        }
    }
}
