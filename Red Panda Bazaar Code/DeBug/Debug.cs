using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.DeBug;

public static class Debug {
    private static bool _debugMode;

    public static bool IsEnabled => _debugMode;

    public static void SetEnabled(bool value) {
        _debugMode = value;
    }

    public static void Init() {
        Tools.Helper.Events.Input.ButtonPressed += OnButtonPressed;
        Tools.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
    }

    private static void OnButtonPressed(object? sender, ButtonPressedEventArgs e) {
        if (!Context.IsWorldReady) return;

        if (Enum.TryParse<SButton>(Tools.ModConfig.DebugToggleKey, ignoreCase: true, out var toggleBtn)
            && e.Button == toggleBtn) {
            _debugMode = !_debugMode;
        }

        if (Enum.TryParse<SButton>(Tools.ModConfig.DebugMenuKey, ignoreCase: true, out var menuBtn)
            && e.Button == menuBtn) {
            if (Game1.activeClickableMenu is DebugMenu) {
                Game1.exitActiveMenu();
            } else {
                Game1.activeClickableMenu = new DebugMenu();
            }
        }

        if (Enum.TryParse<SButton>(Tools.ModConfig.DebugTeleportKey, ignoreCase: true, out var tpBtn)
            && e.Button == tpBtn) {
            TeleportToCursor();
        }
    }

    private static void TeleportToCursor() {
        var tile = Game1.currentCursorTile;
        var tileX = (int)tile.X;
        var tileY = (int)tile.Y;
        var loc = Game1.currentLocation;

        if (loc?.Map?.Layers == null || loc.Map.Layers.Count == 0) return;
        if (tileX < 0 || tileX >= loc.Map.Layers[0].LayerWidth) return;
        if (tileY < 0 || tileY >= loc.Map.Layers[0].LayerHeight) return;

        Game1.player.Position = new Vector2(tileX * 64f + 32f, tileY * 64f + 32f);
        Game1.player.currentLocation = loc;
        Game1.exitActiveMenu();
    }

    private static void OnRenderedWorld(object? sender, RenderedWorldEventArgs e) {
        if (!_debugMode || !Context.IsWorldReady) return;

        var b = e.SpriteBatch;
        var loc = Game1.currentLocation;
        if (loc?.Map?.Layers == null || loc.Map.Layers.Count == 0) return;

        var width = loc.Map.Layers[0].LayerWidth;
        var height = loc.Map.Layers[0].LayerHeight;

        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++) {
            var action = loc.doesTileHaveProperty(x, y, "Action", "Buildings");
            if (action == null) continue;

            var screenPos = Game1.GlobalToLocal(Game1.viewport,
                new Vector2(x * 64f, y * 64f));

            var rect = new Rectangle((int)screenPos.X, (int)screenPos.Y, 64, 64);
            b.Draw(Game1.fadeToBlackRect, rect, Color.LimeGreen * 0.15f);
            DrawBorder(b, rect, Color.LimeGreen, 2);
        }

        // 鼠标悬浮时在 tile 上方显示 Action ID
        var cursorTile = Game1.currentCursorTile;
        var tileX = (int)cursorTile.X;
        var tileY = (int)cursorTile.Y;
        if (tileX >= 0 && tileX < width && tileY >= 0 && tileY < height) {
            var cursorAction = loc.doesTileHaveProperty(tileX, tileY, "Action", "Buildings");
            if (cursorAction != null) {
                var textPos = Game1.GlobalToLocal(Game1.viewport,
                    new Vector2(tileX * 64f + 32f, tileY * 64f - 24f));
                var textSize = Game1.smallFont.MeasureString(cursorAction);
                var drawPos = new Vector2(textPos.X - textSize.X / 2, textPos.Y);
                b.DrawString(Game1.smallFont, cursorAction, drawPos + Vector2.One, Color.Black);
                b.DrawString(Game1.smallFont, cursorAction, drawPos, Color.White);
            }
        }
    }

    private static void DrawBorder(SpriteBatch b, Rectangle rect, Color color, int thickness) {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness),
            color);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height),
            color);
    }
}