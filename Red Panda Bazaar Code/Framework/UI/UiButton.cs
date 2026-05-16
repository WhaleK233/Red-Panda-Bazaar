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
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9),
            X, Y, Width, Height, Enabled ? Color.White : Color.Gray, 4f);
        var textSize = Game1.smallFont.MeasureString(Text);
        Utility.drawTextWithShadow(b, Text, Game1.smallFont,
            new Vector2(X + (Width - textSize.X) / 2, Y + (Height - textSize.Y) / 2),
            Enabled ? Game1.textColor : Color.DarkGray);
    }

    public override bool HandleClick(int x, int y)
    {
        if (!Visible || !Enabled || !Bounds.Contains(x, y)) return false;
        OnClick?.Invoke();
        return true;
    }
}
