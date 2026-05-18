using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Framework.UI;

public class UiButton : UiElement
{
    private const int PadX = 16;
    private const int PadY = 10;

    public string Text { get; set; }
    public Action? OnClick { get; set; }
    public bool Enabled { get; set; } = true;

    /// <summary>悬停时背景色。</summary>
    public Color HoverColor { get; set; } = Color.Wheat;

    /// <summary>点击播放的音效名，null 表示不播放。</summary>
    public string? ClickSound { get; set; } = "bigDeSelect";

    public UiButton(string text, Action? onClick = null)
    {
        Text = text;
        OnClick = onClick;
    }

    public override bool IsFocusable => Enabled && Visible;

    public override void Arrange()
    {
        var size = Game1.smallFont.MeasureString(Text);
        Width = (int)size.X + PadX * 2;
        Height = (int)size.Y + PadY * 2;
    }

    public override void Draw(SpriteBatch b)
    {
        if (!Visible) return;

        var bgColor = Enabled
            ? (IsHovered || Focused ? HoverColor : Color.White)
            : Color.Gray;
        var textColor = Enabled
            ? (IsHovered || Focused ? Color.Black : Game1.textColor)
            : Color.DarkGray;

        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9),
            X, Y, Width, Height, bgColor, 4f);
        var textSize = Game1.smallFont.MeasureString(Text);
        Utility.drawTextWithShadow(b, Text, Game1.smallFont,
            new Vector2(X + (Width - textSize.X) / 2, Y + (Height - textSize.Y) / 2),
            textColor);
    }

    public override bool HandleClick(int x, int y)
    {
        if (!Visible || !Enabled || !Bounds.Contains(x, y)) return false;
        if (ClickSound != null)
            Game1.playSound(ClickSound);
        OnClick?.Invoke();
        return true;
    }
}
