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
        // 第一遍：测量所有子元素的高度
        var totalH = 0;
        var maxW = 0;
        foreach (var child in Children)
        {
            child.Arrange();
            maxW = Math.Max(maxW, child.Width);
            totalH += child.Height + Spacing;
        }
        Width = maxW;
        Height = totalH > 0 ? totalH - Spacing : 0;

        // 第二遍：设置正确坐标后向下传播
        var currentY = 0;
        foreach (var child in Children)
        {
            child.X = X;
            child.Y = Y + currentY;
            child.Arrange(); // 子容器用正确坐标重排后代
            currentY += child.Height + Spacing;
        }
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
