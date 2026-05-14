using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.DeBug;

public static class DebugOverlay {
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