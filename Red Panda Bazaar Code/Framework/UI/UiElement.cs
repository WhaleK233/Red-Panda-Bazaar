using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Red_Panda_Bazaar_Code.Framework.UI;

public abstract class UiElement
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Visible { get; set; } = true;

    public Rectangle Bounds => new(X, Y, Width, Height);

    /// <summary>父容器引用，用于向上遍历（焦点导航、滚动容器查找）。</summary>
    public UiElement? Parent { get; set; }

    public virtual bool IsFocusable => false;

    /// <summary>鼠标是否悬停在此元素上（每帧由父容器更新）。</summary>
    public bool IsHovered { get; set; }

    /// <summary>是否获得键盘/游戏杆焦点（由 UiBaseMenu 管理）。</summary>
    public bool Focused { get; set; }

    /// <summary>悬停提示文本，null 表示无提示。</summary>
    public Func<string>? Tooltip { get; set; }

    /// <summary>每帧更新逻辑（鼠标位置、状态刷新等）。</summary>
    /// <param name="mouseX">当前鼠标 X 坐标。</param>
    /// <param name="mouseY">当前鼠标 Y 坐标。</param>
    public virtual void Update(int mouseX, int mouseY)
    {
        IsHovered = Bounds.Contains(mouseX, mouseY);
    }

    public abstract void Draw(SpriteBatch b);
    public virtual bool HandleClick(int x, int y) => false;
    public virtual bool HandleScroll(int direction) => false;

    /// <summary>测量内容大小并设置子元素位置。</summary>
    public virtual void Arrange() { }
}
