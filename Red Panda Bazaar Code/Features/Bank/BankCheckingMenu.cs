using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Features.Bank;

public class BankCheckingMenu : IClickableMenu
{
    private const int ContentPadding = 24;
    private const int TopPadding = 40;

    private static int CalcWidth()
    {
        var font = Game1.smallFont;
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();
        var w = font.MeasureString(
            Tools.GetI18n(I18nKeys.Bank_CheckingBalance).Tokens(new { amount = 9999999, gold }).ToString()
        ).X + ContentPadding * 3 + 200;
        return Math.Clamp((int)w, 600, Game1.uiViewport.Width - 40);
    }

    private static int CalcHeight()
    {
        return Math.Clamp(240, 200, Game1.uiViewport.Height - 40);
    }

    public BankCheckingMenu()
        : base(
            (Game1.uiViewport.Width - CalcWidth()) / 2,
            (Game1.uiViewport.Height - CalcHeight()) / 2,
            CalcWidth(), CalcHeight(),
            showUpperRightCloseButton: true)
    { }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (Game1.activeClickableMenu != this) return;

        var cx = xPositionOnScreen + ContentPadding;
        var cy = yPositionOnScreen + TopPadding;

        if (Bank.GetInterestEarned() > 0)
        {
            var claimBounds = new Rectangle(cx + 300, cy + 48, 100, 40);
            if (claimBounds.Contains(x, y))
            {
                Bank.ClaimInterest();
                Game1.playSound("coin");
                exitThisMenu();
                return;
            }
        }

        var depositBounds = new Rectangle(cx, cy + 120, 120, 40);
        if (depositBounds.Contains(x, y))
        {
            Game1.activeClickableMenu = new NumberSelectionMenu(
                Tools.GetI18n(I18nKeys.Bank_DepositTitle).ToString(),
                (number, price, who) =>
                {
                    if (number > 0)
                    {
                        Bank.Deposit(number);
                        Game1.playSound("coin");
                    }
                    Game1.exitActiveMenu();
                    if (Context.IsMainPlayer && number > 0)
                        Game1.activeClickableMenu = new BankCheckingMenu();
                },
                price: -1, minValue: 1, maxValue: Math.Max(1, Game1.player.Money),
                defaultNumber: Math.Min(100, Game1.player.Money));
            return;
        }

        var withdrawBounds = new Rectangle(cx + 140, cy + 120, 120, 40);
        if (withdrawBounds.Contains(x, y))
        {
            Game1.activeClickableMenu = new NumberSelectionMenu(
                Tools.GetI18n(I18nKeys.Bank_WithdrawTitle).ToString(),
                (number, price, who) =>
                {
                    if (number > 0)
                    {
                        Bank.Withdraw(number);
                        Game1.playSound("coin");
                    }
                    Game1.exitActiveMenu();
                    if (Context.IsMainPlayer && number > 0)
                        Game1.activeClickableMenu = new BankCheckingMenu();
                },
                price: -1, minValue: 1, maxValue: (int)Math.Max(1L, Bank.GetCheckingBalance()),
                defaultNumber: (int)Math.Min(100L, Bank.GetCheckingBalance()));
        }
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

        var title = Tools.GetI18n(I18nKeys.Bank_CheckingTab).ToString();
        var titleSize = Game1.dialogueFont.MeasureString(title);
        Utility.drawTextWithShadow(b, title, Game1.dialogueFont,
            new Vector2(xPositionOnScreen + (width - titleSize.X) / 2, yPositionOnScreen - 32), Color.Black);

        var cx = xPositionOnScreen + ContentPadding;
        var cy = yPositionOnScreen + TopPadding;

        var balance = Bank.GetCheckingBalance();
        var interest = Bank.GetInterestEarned();
        var rate = BankCalculator.GetDailyCheckingRate();
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();

        Utility.drawTextWithShadow(b,
            Tools.GetI18n(I18nKeys.Bank_CheckingBalance).Tokens(new { amount = balance, gold }).ToString(),
            Game1.dialogueFont, new Vector2(cx, cy), Color.Black);

        Utility.drawTextWithShadow(b,
            Tools.GetI18n(I18nKeys.Bank_InterestEarned).Tokens(new { amount = interest, gold }).ToString(),
            Game1.smallFont, new Vector2(cx, cy + 52), Color.Black);

        if (interest > 0)
        {
            var claimBounds = new Rectangle(cx + 300, cy + 48, 100, 40);
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9),
                claimBounds.X, claimBounds.Y, claimBounds.Width, claimBounds.Height, Color.White, 4f);
            var claimLabel = Tools.GetI18n(I18nKeys.Bank_ClaimInterest).ToString();
            Utility.drawTextWithShadow(b, claimLabel, Game1.smallFont,
                new Vector2(claimBounds.X + (claimBounds.Width - Game1.smallFont.MeasureString(claimLabel).X) / 2,
                    claimBounds.Y + 8), Game1.textColor);
        }

        Utility.drawTextWithShadow(b,
            Tools.GetI18n(I18nKeys.Bank_TodayRate).Tokens(new { rate = (rate * 100).ToString("F4") }).ToString(),
            Game1.smallFont, new Vector2(cx, cy + 90), Color.Gray);

        DrawButton(b, cx, cy + 120, 120, 40, Tools.GetI18n(I18nKeys.Bank_Deposit).ToString());
        DrawButton(b, cx + 140, cy + 120, 120, 40, Tools.GetI18n(I18nKeys.Bank_Withdraw).ToString());

        base.draw(b);
        drawMouse(b);
    }

    private static void DrawButton(SpriteBatch b, int x, int y, int w, int h, string label)
    {
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9),
            x, y, w, h, Color.White, 4f);
        Utility.drawTextWithShadow(b, label, Game1.smallFont,
            new Vector2(x + (w - Game1.smallFont.MeasureString(label).X) / 2, y + 8), Game1.textColor);
    }
}
