using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Framework.UI.Enums;

namespace Red_Panda_Bazaar_Code.Framework.UI.Components;

public class UiRow : UiElement
{
    public List<UiElement> Children { get; } = new();
    public int Spacing { get; set; } = 10;

    /// <summary>子元素垂直对齐方式。</summary>
    public UiAlign VerticalAlignment { get; set; } = UiAlign.Center;

    /// <summary>是否撑满父容器宽度（仅当父容器宽度大于自然宽度时有效）。</summary>
    public bool Stretch { get; set; }

    /// <summary>子元素水平分布方式（需配合 Stretch 使用）。</summary>
    public UiJustify JustifyContent { get; set; } = UiJustify.Start;

    public override int ChildCount => Children.Count;
    public override UiElement? GetChild(int index) => index >= 0 && index < Children.Count ? Children[index] : null;

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

        var naturalW = totalW > 0 ? totalW - Spacing : 0;
        Height = maxH;

        // 撑满父容器
        if (Stretch && Parent != null && Parent.Width > naturalW)
            Width = Parent.Width;
        else
            Width = naturalW;

        // 第二遍：设置正确坐标后向下传播
        var extra = Width - naturalW;

        switch (JustifyContent)
        {
            case UiJustify.SpaceBetween when Children.Count > 1:
            {
                var perGap = extra / (float)(Children.Count - 1);
                var cx = 0f;
                foreach (var child in Children)
                {
                    child.X = X + (int)cx;
                    child.Y = VerticalAlignment switch
                    {
                        UiAlign.Top => Y,
                        UiAlign.Bottom => Y + maxH - child.Height,
                        _ => Y + (maxH - child.Height) / 2,
                    };
                    child.Arrange();
                    cx += child.Width + Spacing + perGap;
                }
                break;
            }
            case UiJustify.End:
            {
                var cx = 0;
                foreach (var child in Children)
                {
                    child.X = X + (int)(extra) + cx;
                    child.Y = VerticalAlignment switch
                    {
                        UiAlign.Top => Y,
                        UiAlign.Bottom => Y + maxH - child.Height,
                        _ => Y + (maxH - child.Height) / 2,
                    };
                    child.Arrange();
                    cx += child.Width + Spacing;
                }
                break;
            }
            case UiJustify.Center:
            {
                var cx = 0;
                foreach (var child in Children)
                {
                    child.X = X + (int)(extra / 2f) + cx;
                    child.Y = VerticalAlignment switch
                    {
                        UiAlign.Top => Y,
                        UiAlign.Bottom => Y + maxH - child.Height,
                        _ => Y + (maxH - child.Height) / 2,
                    };
                    child.Arrange();
                    cx += child.Width + Spacing;
                }
                break;
            }
            default: // Start
            {
                var cx = 0;
                foreach (var child in Children)
                {
                    child.X = X + cx;
                    child.Y = VerticalAlignment switch
                    {
                        UiAlign.Top => Y,
                        UiAlign.Bottom => Y + maxH - child.Height,
                        _ => Y + (maxH - child.Height) / 2,
                    };
                    child.Arrange();
                    cx += child.Width + Spacing;
                }
                break;
            }
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
