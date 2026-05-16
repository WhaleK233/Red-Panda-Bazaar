using Microsoft.Xna.Framework.Graphics;

namespace Red_Panda_Bazaar_Code.Framework.UI;

public class UiRow : UiElement
{
    public List<UiElement> Children { get; } = new();
    public int Spacing { get; set; } = 10;

    public UiRow Add(UiElement child)
    {
        child.Parent = this;
        Children.Add(child);
        return this;
    }

    public UiRow Add(params UiElement[] children)
    {
        foreach (var child in children)
            child.Parent = this;
        Children.AddRange(children);
        return this;
    }

    public UiRow Clear()
    {
        foreach (var child in Children)
            child.Parent = null;
        Children.Clear();
        return this;
    }

    public override void Arrange()
    {
        // 第一遍：测量所有子元素的尺寸
        var maxH = 0;
        var totalW = 0;
        foreach (var child in Children)
        {
            child.Arrange();
            maxH = Math.Max(maxH, child.Height);
            totalW += child.Width + Spacing;
        }
        Width = totalW > 0 ? totalW - Spacing : 0;
        Height = maxH;

        // 第二遍：设置正确坐标后向下传播（含垂直居中）
        var currentX = 0;
        foreach (var child in Children)
        {
            child.X = X + currentX;
            child.Y = Y + (maxH - child.Height) / 2;
            child.Arrange(); // 子容器用正确坐标重排后代
            currentX += child.Width + Spacing;
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

    public override bool HandleScroll(int direction)
    {
        if (!Visible) return false;
        for (var i = Children.Count - 1; i >= 0; i--)
            if (Children[i].HandleScroll(direction)) return true;
        return false;
    }
}
