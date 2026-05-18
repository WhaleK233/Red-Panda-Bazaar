using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Framework.UI.Components;

/// <summary>水平分隔线。</summary>
public class UiSeparator : UiElement
{
    public int Thickness { get; set; } = 3;
    public Color SeparatorColor { get; set; } = Color.Black * 0.5f;

    public override void Arrange()
    {
        Height = Thickness;
        // 默认撑满父容器宽度（列布局常用），不覆盖显式设置的 Width
        if (Width <= 0 && Parent != null)
            Width = Parent.Width;
    }

    public override void Update(int mouseX, int mouseY)
    {
        IsHovered = Bounds.Contains(mouseX, mouseY);
    }

    public override void Draw(SpriteBatch b)
    {
        if (!Visible) return;
        b.Draw(Game1.fadeToBlackRect, Bounds, SeparatorColor);
    }
}
