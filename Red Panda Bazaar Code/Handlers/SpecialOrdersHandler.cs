using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Custom;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Handlers;

public static class SpecialOrdersHandler
{
    private static Texture2D ItemsT2D;

    /// <summary>启用自定义任务</summary>
    public static void Init()
    {
        Tools.Log("Quests Initializing.");
        ItemsT2D = Tools.Helper.ModContent.Load<Texture2D>("assets/RedPandaBazaar_Items.png");

        Tools.Helper.Events.GameLoop.DayStarted += OnDayStarted;
        Tools.Helper.Events.Input.ButtonPressed += OnButtonPressed;
        Tools.Helper.Events.Display.RenderedWorld += OnRenderedWorld;

        Tools.Log("Quests Initialized.");
    }

    private static void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (Game1.currentLocation.Name != "Custom_ChenQiShop1" ||
            Game1.player.stats.Get(StatsKeys.ChenXiaomingRewardCount) <= 0U) return;

        var spriteBatch = e.SpriteBatch;

        int dx = 10;
        int dy = 5;
        float vx = 64 * dx - 8;
        float vy = 64 * dy - 48;

        float num =
            (float)(4.0 * Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2));
        spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(vx, vy + num)),
            new Rectangle(141, 465, 20, 24),
            Color.White * 0.75f, 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 0.98f);
        spriteBatch.Draw(ItemsT2D,
            Game1.GlobalToLocal(Game1.viewport, new Vector2(vx + 40, vy + 40 + num)),
            new Rectangle(16, 0, 16, 16),
            Color.White * 0.75f, 0.0f, new Vector2(8f, 8f), 4f, SpriteEffects.None, 1f);
    }

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

    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (Game1.dayOfMonth % 7 == 1 && Game1.player.IsMainPlayer)
        {
            RPB_SpecialOrderBoard.UpdateAvailableSpecialOrders(true);
        }
    }
}