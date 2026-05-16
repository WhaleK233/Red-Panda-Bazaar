using Microsoft.Xna.Framework.Graphics;

namespace Red_Panda_Bazaar_Code.Framework.UI;

public class UiColumn : UiElement
{
    public List<UiElement> Children { get; } = new();
    public int Spacing { get; set; } = 6;

    public UiColumn Add(UiElement child)
    {
        Children.Add(child);
        return this;
    }

    public UiColumn Add(params UiElement[] children)
    {
        Children.AddRange(children);
        return this;
    }

    public UiColumn Clear()
    {
        Children.Clear();
        return this;
    }

    public override void Arrange()
    {
        var currentY = 0;
        var maxW = 0;
        foreach (var child in Children)
        {
            child.Arrange();
            child.X = X;
            child.Y = Y + currentY;
            maxW = Math.Max(maxW, child.Width);
            currentY += child.Height + Spacing;
        }
        Width = maxW;
        Height = currentY > 0 ? currentY - Spacing : 0;
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
