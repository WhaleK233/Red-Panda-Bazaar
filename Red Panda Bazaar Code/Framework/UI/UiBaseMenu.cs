using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Framework.UI;

public abstract class UiBaseMenu : IClickableMenu
{
    protected const int ContentPadding = 24;
    protected const int TopPadding = 24;

    protected UiColumn Root { get; } = new();
    protected abstract void BuildUi();

    /// <summary>子类在此返回内容区域的期望宽高（不含边框）。</summary>
    protected abstract Point CalcContentSize();

    public UiBaseMenu()
    {
        Rebuild();
    }

    protected void Rebuild()
    {
        Root.Children.Clear();
        BuildUi();
        Root.Arrange();

        var content = CalcContentSize();
        var w = Math.Max(content.X, Root.Width) + ContentPadding * 2;
        var h = Math.Max(content.Y, Root.Height) + ContentPadding + TopPadding;

        width = Math.Clamp(w + 24, 300, Game1.uiViewport.Width - 40);
        height = Math.Clamp(h + 40, 120, Game1.uiViewport.Height - 40);

        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;

        initializeUpperRightCloseButton();

        Root.X = xPositionOnScreen + ContentPadding;
        Root.Y = yPositionOnScreen + TopPadding;
        Root.Arrange();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (Game1.activeClickableMenu != this) return;
        Root.HandleClick(x, y);
    }

    public override void receiveRightClick(int x, int y, bool playSound = true) { }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        Rebuild();
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
            xPositionOnScreen, yPositionOnScreen, width, height, Color.White, 4f);

        Root.Draw(b);

        base.draw(b);
        drawMouse(b);
    }
}
