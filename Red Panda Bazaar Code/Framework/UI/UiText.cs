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

    /// <summary>最小宽度，0 表不限制。用于表格列对齐。</summary>
    public int MinWidth { get; set; }

    /// <summary>水平对齐方式。</summary>
    public UiAlign HorizontalAlignment { get; set; }

    public UiText(string text, SpriteFont? font = null, Color? color = null)
    {
        Text = text;
        Font = font ?? Game1.smallFont;
        if (color.HasValue) Color = color.Value;
    }

    public override void Arrange()
    {
        var size = Font.MeasureString(Text);
        Width = Math.Max((int)size.X, MinWidth);
        Height = (int)size.Y;
    }

    public override void Draw(SpriteBatch b)
    {
        if (!Visible || string.IsNullOrEmpty(Text)) return;

        var textSize = Font.MeasureString(Text);
        var xOff = HorizontalAlignment switch
        {
            UiAlign.Center => (Width - textSize.X) / 2,
            UiAlign.Right => Width - textSize.X,
            _ => 0,
        };

        if (Shadow)
            Utility.drawTextWithShadow(b, Text, Font, new Vector2(X + xOff, Y), Color);
        else
            b.DrawString(Font, Text, new Vector2(X + xOff, Y), Color);
    }
}
