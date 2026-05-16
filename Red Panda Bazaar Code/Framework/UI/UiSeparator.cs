using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Framework.UI;

/// <summary>水平分隔线。</summary>
public class UiSeparator : UiElement
{
    public int Thickness { get; set; } = 3;
    public Color SeparatorColor { get; set; } = Color.Black * 0.5f;

    public override void Arrange()
    {
        Height = Thickness;
    }

    public override void Draw(SpriteBatch b)
    {
        if (!Visible) return;
        b.Draw(Game1.fadeToBlackRect, Bounds, SeparatorColor);
    }
}
