using Microsoft.Xna.Framework.Graphics;

namespace Red_Panda_Bazaar_Code.Framework.UI;

public class UiRow : UiElement
{
    public List<UiElement> Children { get; } = new();
    public int Spacing { get; set; } = 10;

    public UiRow Add(UiElement child)
    {
        Children.Add(child);
        return this;
    }

    public override void Arrange()
    {
        var currentX = 0;
        var maxH = 0;
        foreach (var child in Children)
        {
            child.Arrange();
            child.X = X + currentX;
            child.Y = Y;
            maxH = Math.Max(maxH, child.Height);
            currentX += child.Width + Spacing;
        }
        Width = currentX > 0 ? currentX - Spacing : 0;

        // 垂直居中对齐
        foreach (var child in Children)
            child.Y = Y + (maxH - child.Height) / 2;

        Height = maxH;
    }

    public override void Draw(SpriteBatch b)
    {
        if (!Visible) return;
        foreach (var child in Children)
            child.Draw(b);
    }

    public override bool HandleClick(int x, int y)
    {
        if (!Visible) return false;
        foreach (var child in Children)
            if (child.HandleClick(x, y)) return true;
        return false;
    }
}
