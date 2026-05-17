using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.SpecialOrders;

/// <summary>陈小明特殊订单系统：7 天周期刷新、奖励领取、商店提示图标。</summary>
public static class SpecialOrders
{
    private static Texture2D? _itemsTexture;

    /// <summary>注册事件和 Tile Action。</summary>
    public static void Init()
    {
        Tools.Log("Quests Initializing.");

        _itemsTexture = Tools.Helper.ModContent.Load<Texture2D>("assets/RedPandaBazaar_Items.png");
        Tools.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        Tools.Helper.Events.GameLoop.DayStarted += OnDayStarted;
        Tools.Helper.Events.Input.ButtonPressed += OnButtonPressed;
        Tools.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
        GameLocation.RegisterTileAction("RedPandaBazaar_SpecialOrdersBoard", (_, _, _, _) =>
        {
            Game1.activeClickableMenu = new SpecialOrderBoard(QuestsKeys.CXM_OrderType);
            return false;
        });

        Tools.Log("Quests Initialized.");
    }

    private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        SpecialOrdersPatch.Reset();
    }

    /// <summary>陈小明商店内有可领取奖励时，渲染浮动提示图标。</summary>
    private static void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (Game1.currentLocation.Name != "Custom_ChenQiShop1" ||
            Game1.player.stats.Get(StatsKeys.ChenXiaomingRewardCount) <= 0U)
            return;

        var spriteBatch = e.SpriteBatch;
        int dx = 10;
        int dy = 5;
        float vx = 64 * dx - 8;
        float vy = 64 * dy - 48;
        var floatOffset = (float)(4.0 * Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2));

        spriteBatch.Draw(Game1.mouseCursors,
            Game1.GlobalToLocal(Game1.viewport, new Vector2(vx, vy + floatOffset)),
            new Rectangle(141, 465, 20, 24),
            Color.White * 0.75f, 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 0.98f);
        spriteBatch.Draw(_itemsTexture,
            Game1.GlobalToLocal(Game1.viewport, new Vector2(vx + 40, vy + 40 + floatOffset)),
            new Rectangle(16, 0, 16, 16),
            Color.White * 0.75f, 0.0f, new Vector2(8f, 8f), 4f, SpriteEffects.None, 1f);
    }

    /// <summary>陈小明商店内点击领取位置时，发放经典抽奖券奖励。</summary>
    private static void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Tools.IsValidButtonAction(e) || !Game1.currentLocation.Name.Contains("Custom_ChenQiShop1")) return;

        var tile = e.Cursor.GrabTile;
        if (tile is not { X: 10, Y: 6 }) return;

        if (Game1.stats.Get(StatsKeys.ChenXiaomingRewardCount) > 0)
        {
            Game1.player.addItemToInventory(ItemRegistry.Create(ItemsKeys.Tickets.ClassicTicket));
            Game1.stats.Decrement(StatsKeys.ChenXiaomingRewardCount);
            Game1.drawObjectDialogue(Tools.GetI18n(I18nKeys.Dialogue_GetXiaoMingReward));
        }
        else
        {
            Game1.drawObjectDialogue(Tools.GetI18n(I18nKeys.Dialogue_NoXiaoMingReward));
        }
    }

    /// <summary>每 7 天（每月 1、8、15、22 日）由主机刷新 CXM 特殊订单。</summary>
    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (Game1.dayOfMonth % 7 == 1 && Game1.player.IsMainPlayer)
        {
            SpecialOrderBoard.UpdateAvailableSpecialOrders(true);
        }
    }
}
