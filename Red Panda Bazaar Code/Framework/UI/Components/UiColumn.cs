using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Framework.UI.Enums;

namespace Red_Panda_Bazaar_Code.Framework.UI.Components;

public class UiColumn : UiElement
{
    public List<UiElement> Children { get; } = new();
    public int Spacing { get; set; } = 6;

    /// <summary>子元素水平对齐方式。</summary>
    public UiAlign HorizontalAlignment { get; set; }

    public override int ChildCount => Children.Count;
    public override UiElement? GetChild(int index) => index >= 0 && index < Children.Count ? Children[index] : null;

    public UiColumn Add(UiElement child)
    {
        child.Parent = this;
        Children.Add(child);
        return this;
    }

    public UiColumn Add(params UiElement[] children)
    {
        foreach (var child in children)
            child.Parent = this;
        Children.AddRange(children);
        return this;
    }

    public UiColumn Clear()
    {
        foreach (var child in Children)
            child.Parent = null;
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
            child.X = HorizontalAlignment switch
            {
                UiAlign.Center => X + (maxW - child.Width) / 2,
                UiAlign.Right => X + maxW - child.Width,
                _ => X,
            };
            child.Y = Y + currentY;
            child.Arrange(); // 子容器用正确坐标重排后代
            currentY += child.Height + Spacing;
        }
    }

    public override void Update(int mouseX, int mouseY)
    {
        IsHovered = Bounds.Contains(mouseX, mouseY);
        foreach (var child in Children)
            child.Update(mouseX, mouseY);
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
