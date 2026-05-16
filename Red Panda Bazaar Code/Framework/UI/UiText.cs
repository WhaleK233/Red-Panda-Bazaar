using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Framework.UI;

public class UiText : UiElement
{
    public string Text { get; set; }
    public SpriteFont Font { get; set; }
    public Color Color { get; set; } = Color.Black;
    public bool Shadow { get; set; } = true;

    public UiText(string text, SpriteFont? font = null, Color? color = null)
    {
        Text = text;
        Font = font ?? Game1.smallFont;
        if (color.HasValue) Color = color.Value;
    }

    public override void Arrange()
    {
        var size = Font.MeasureString(Text);
        Width = (int)size.X;
        Height = (int)size.Y;
    }

    public override void Draw(SpriteBatch b)
    {
        if (!Visible || string.IsNullOrEmpty(Text)) return;
        if (Shadow)
            Utility.drawTextWithShadow(b, Text, Font, new Vector2(X, Y), Color);
        else
            b.DrawString(Font, Text, new Vector2(X, Y), Color);
    }
}
