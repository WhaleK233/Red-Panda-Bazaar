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

    public abstract void Draw(SpriteBatch b);
    public virtual bool HandleClick(int x, int y) => false;
    public virtual bool HandleScroll(int direction) => false;

    /// <summary>测量内容大小并设置子元素位置。</summary>
    public virtual void Arrange() { }
}
