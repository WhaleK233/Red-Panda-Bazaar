using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Framework.UI.Components;

public class UiScrollContainer : UiElement
{
    private const int ScrollBarWidth = 12;
    private const int ScrollSpeed = 60;
    private const int MinThumbHeight = 20;

    private UiElement? _child;
    public UiElement? Child
    {
        get => _child;
        set
        {
            if (_child != null)
                _child.Parent = null;
            _child = value;
            if (_child != null)
                _child.Parent = this;
        }
    }

    /// <summary>视口最大高度，0 表示不限制（不启用滚动）。</summary>
    public int MaxHeight { get; set; }

    public override int ChildCount => Child != null ? 1 : 0;
    public override UiElement? GetChild(int index) => index == 0 ? Child : null;

    private int _scrollY;
    private int _contentHeight;

    public bool CanScroll => MaxHeight > 0 && _contentHeight > MaxHeight;

    public override void Measure()
    {
        if (Child == null)
        {
            Width = Height = 0;
            return;
        }

        Child.X = X;
        Child.Y = Y - _scrollY;
        Child.Measure();

        _contentHeight = Child.Height;
        Width = Child.Width;
        Height = MaxHeight > 0 ? Math.Min(MaxHeight, _contentHeight) : _contentHeight;

        _scrollY = Math.Clamp(_scrollY, 0, Math.Max(0, _contentHeight - Height));
    }

    public override void Arrange()
    {
        if (Child == null) return;

        Child.X = X;
        Child.Y = Y - _scrollY;
        Child.Arrange();

        _contentHeight = Child.Height;
        Width = Child.Width;
        Height = MaxHeight > 0 ? Math.Min(MaxHeight, _contentHeight) : _contentHeight;

        _scrollY = Math.Clamp(_scrollY, 0, Math.Max(0, _contentHeight - Height));
    }

    public override void Update(int mouseX, int mouseY)
    {
        IsHovered = Bounds.Contains(mouseX, mouseY);
        Child?.Update(mouseX, mouseY);
    }

    public override void Draw(SpriteBatch b)
    {
        if (!Visible || Child == null) return;

        if (!CanScroll)
        {
            Child.Draw(b);
            return;
        }

        var prevScissor = b.GraphicsDevice.ScissorRectangle;
        var scissorRect = new Rectangle(X, Y, Width, Height);

        b.End();
        b.GraphicsDevice.ScissorRectangle = scissorRect;
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
            SamplerState.PointClamp, null, new RasterizerState { ScissorTestEnable = true });

        Child.Draw(b);

        b.End();
        b.GraphicsDevice.ScissorRectangle = prevScissor;
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
            SamplerState.PointClamp, null, null);

        DrawScrollbar(b);
    }

    private void DrawScrollbar(SpriteBatch b)
    {
        var trackRect = new Rectangle(X + Width - ScrollBarWidth, Y, ScrollBarWidth, Height);

        // 轨道：半透明黑色背景（原版风格）
        b.Draw(Game1.fadeToBlackRect, trackRect, Color.Black * 0.35f);

        // 滑块拇指尺寸
        var thumbHeight = Math.Max(MinThumbHeight, Height * Height / _contentHeight);
        var trackSpace = Height - thumbHeight;
        var scrollRange = _contentHeight - Height;
        var thumbY = Y + (scrollRange > 0
            ? (int)((float)_scrollY / scrollRange * trackSpace)
            : 0);

        var thumbRect = new Rectangle(X + Width - ScrollBarWidth + 2, thumbY, ScrollBarWidth - 4, thumbHeight);

        // 滑块拇指：原版边框贴图风格
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9),
            thumbRect.X, thumbRect.Y, thumbRect.Width, thumbRect.Height, Color.Wheat, 1f);
    }

    public void Scroll(int direction)
    {
        if (!CanScroll) return;

        _scrollY = Math.Clamp(_scrollY + (direction > 0 ? -ScrollSpeed : ScrollSpeed), 0,
            _contentHeight - Height);

        if (Child == null) return;
        Child.Y = Y - _scrollY;
        Child.Arrange();
    }

    /// <summary>滚动使指定子元素在视口中可见。</summary>
    public void EnsureChildVisible(UiElement childElement)
    {
        if (!CanScroll || Child == null) return;

        var childBounds = childElement.Bounds;
        var containerBounds = Bounds;

        int delta = 0;
        if (childBounds.Top < containerBounds.Top)
            delta = containerBounds.Top - childBounds.Top;
        else if (childBounds.Bottom > containerBounds.Bottom)
            delta = containerBounds.Bottom - childBounds.Bottom;

        if (delta == 0) return;

        _scrollY -= delta;
        _scrollY = Math.Clamp(_scrollY, 0, Math.Max(0, _contentHeight - Height));

        Child.Y = Y - _scrollY;
        Child.Arrange();
    }

    public override bool HandleScroll(int direction)
    {
        if (!Visible || Child == null || !CanScroll) return false;
        if (!Bounds.Contains(Game1.getMouseX(), Game1.getMouseY())) return false;

        // 先问子元素能否处理（如内部有滑块），没处理再自己滚动
        if (Child.HandleScroll(direction))
            return true;

        Scroll(direction);
        return true;
    }

    public override bool HandleClick(int x, int y)
    {
        if (!Visible || Child == null) return false;
        if (!Bounds.Contains(x, y)) return false;

        // 点击滚动条跳转到对应位置
        if (CanScroll && x >= Bounds.Right - ScrollBarWidth)
        {
            var ratio = (y - Y) / (float)Height;
            _scrollY = (int)(ratio * (_contentHeight - Height));
            _scrollY = Math.Clamp(_scrollY, 0, Math.Max(0, _contentHeight - Height));

            if (Child == null) return true;
            Child.Y = Y - _scrollY;
            Child.Arrange();
            return true;
        }

        return Child.HandleClick(x, y);
    }
}
