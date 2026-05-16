using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Features.Bank;

public class BankTaxMenu : IClickableMenu
{
    private const int ContentPadding = 24;
    private const int TopPadding = 40;

    private static int CalcWidth()
    {
        var font = Game1.smallFont;
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();
        var w = font.MeasureString(
            Tools.GetI18n(I18nKeys.Bank_TotalTax).Tokens(new { amount = 9999999, gold }).ToString()
        ).X + ContentPadding * 3 + 40;
        return Math.Clamp((int)w, 400, Game1.uiViewport.Width - 40);
    }

    private static int CalcHeight()
    {
        return Math.Clamp(140, 120, Game1.uiViewport.Height - 40);
    }

    public BankTaxMenu()
        : base(
            (Game1.uiViewport.Width - CalcWidth()) / 2,
            (Game1.uiViewport.Height - CalcHeight()) / 2,
            CalcWidth(), CalcHeight(),
            showUpperRightCloseButton: true)
    { }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
    }

    public override void receiveRightClick(int x, int y, bool playSound = true) { }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        width = CalcWidth();
        height = CalcHeight();
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
        initializeUpperRightCloseButton();
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
            xPositionOnScreen, yPositionOnScreen, width, height, Color.White, 4f);

        var title = Tools.GetI18n(I18nKeys.Bank_TaxTab).ToString();
        var titleSize = Game1.dialogueFont.MeasureString(title);
        Utility.drawTextWithShadow(b, title, Game1.dialogueFont,
            new Vector2(xPositionOnScreen + (width - titleSize.X) / 2, yPositionOnScreen - 32), Color.Black);

        var cx = xPositionOnScreen + ContentPadding;
        var cy = yPositionOnScreen + TopPadding + 20;
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();
        var totalTax = PlayerStall.PlayerStall.TotalTax;

        Utility.drawTextWithShadow(b,
            Tools.GetI18n(I18nKeys.Bank_TotalTax).Tokens(new { amount = totalTax, gold }).ToString(),
            Game1.dialogueFont, new Vector2(cx, cy), Color.Black);

        base.draw(b);
        drawMouse(b);
    }
}
