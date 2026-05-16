using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Features.Bank;

public class BankFixedMenu : IClickableMenu
{
    private const int ContentPadding = 24;
    private const int TopPadding = 40;
    private readonly List<ClickableComponent> _actionButtons = new();
    private readonly List<ClickableComponent> _fixedActionButtons = new();

    private static int BtnH() => (int)(Game1.smallFont.MeasureString("X").Y + 20);

    private static int ActionBtnW()
    {
        var font = Game1.smallFont;
        var redeemW = font.MeasureString(Tools.GetI18n(I18nKeys.Bank_Redeem).ToString()).X;
        var earlyW = font.MeasureString(Tools.GetI18n(I18nKeys.Bank_EarlyWithdraw).ToString()).X;
        return (int)Math.Max(redeemW, earlyW) + 32;
    }

    private static int DurationButtonsTotalW()
    {
        var font = Game1.smallFont;
        var days = Tools.GetI18n(I18nKeys.Bank_Days).ToString();
        var total = 0;
        foreach (var term in BankCalculator.FixedTermOptions)
            total += (int)font.MeasureString($"{term}{days}").X + 32 + 10;
        return total - 10; // last gap removed
    }

    private static int CalcWidth()
    {
        var font = Game1.smallFont;
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();
        var lineW = font.MeasureString(
            $"[1] {Tools.GetI18n(I18nKeys.Bank_FixedAmount).Tokens(new { amount = 9999999, gold })} | 112 {Tools.GetI18n(I18nKeys.Bank_Days).ToString()} | {Tools.GetI18n(I18nKeys.Bank_FixedStatusActive).ToString()}"
        ).X;

        var totalW = Math.Max(lineW + ActionBtnW() + 40, DurationButtonsTotalW());
        return Math.Clamp((int)(totalW + ContentPadding * 3 + 40), 600, Game1.uiViewport.Width - 40);
    }

    private static int CalcHeight()
    {
        var baseH = TopPadding + ContentPadding;
        var listH = 56 + 40 * Math.Min(Bank.GetFixedDeposits().Count + 1, 5) + 60;
        var h = baseH + Math.Max(120, listH) + ContentPadding + 40;
        return Math.Clamp(h, 200, Game1.uiViewport.Height - 40);
    }

    public BankFixedMenu()
        : base(
            (Game1.uiViewport.Width - CalcWidth()) / 2,
            (Game1.uiViewport.Height - CalcHeight()) / 2,
            CalcWidth(), CalcHeight(),
            showUpperRightCloseButton: true)
    {
        RefreshActionButtons();
    }

    private void RefreshActionButtons()
    {
        _actionButtons.Clear();
        _fixedActionButtons.Clear();

        var cx = xPositionOnScreen + ContentPadding;
        var cy = yPositionOnScreen + TopPadding;
        var bh = BtnH();

        var terms = BankCalculator.FixedTermOptions;
        var days = Tools.GetI18n(I18nKeys.Bank_Days).ToString();
        var offsetX = 0;
        for (var t = 0; t < terms.Length; t++)
        {
            var label = $"{terms[t]}{days}";
            var btnW = (int)Game1.smallFont.MeasureString(label).X + 32;
            _actionButtons.Add(new ClickableComponent(
                new Rectangle(cx + offsetX, cy, btnW, bh), $"newFixed_{terms[t]}"));
            offsetX += btnW + 10;
        }

        var deposits = Bank.GetFixedDeposits();
        var listY = cy + 56;
        var contentW = width - ContentPadding * 2;
        var actionW = ActionBtnW();
        for (var i = 0; i < deposits.Count; i++)
        {
            var d = deposits[i];
            if (d.Withdrawn) continue;

            var elapsed = (int)Game1.stats.DaysPlayed - d.StartDay;
            var name = elapsed >= d.TermDays ? $"redeem_{i}" : $"early_{i}";
            _fixedActionButtons.Add(new ClickableComponent(
                new Rectangle(cx + contentW - actionW, listY, actionW, (int)bh), name));
            listY += 40;
        }
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (Game1.activeClickableMenu != this) return;

        foreach (var btn in _actionButtons)
        {
            if (!btn.bounds.Contains(x, y)) continue;
            HandleActionButton(btn.name);
            return;
        }

        foreach (var btn in _fixedActionButtons)
        {
            if (!btn.bounds.Contains(x, y)) continue;
            HandleFixedAction(btn.name);
            return;
        }
    }

    private void HandleActionButton(string name)
    {
        if (name.StartsWith("newFixed_") && int.TryParse(name.Replace("newFixed_", ""), out var termDays))
        {
            Game1.activeClickableMenu = new NumberSelectionMenu(
                Tools.GetI18n(I18nKeys.Bank_NewFixedAmountTitle).ToString(),
                (number, price, who) =>
                {
                    if (number > 0)
                    {
                        Bank.CreateFixedDeposit(number, termDays);
                        Game1.playSound("coin");
                    }
                    Game1.exitActiveMenu();
                    if (Context.IsMainPlayer && number > 0)
                        Game1.activeClickableMenu = new BankFixedMenu();
                },
                price: -1, minValue: 1, maxValue: Math.Max(1, Game1.player.Money),
                defaultNumber: Math.Min(100, Game1.player.Money));
        }
    }

    private void HandleFixedAction(string name)
    {
        var parts = name.Split('_');
        if (parts.Length < 2 || !int.TryParse(parts[1], out var index)) return;

        switch (parts[0])
        {
            case "redeem":
                Bank.RedeemFixedDeposit(index);
                Game1.playSound("coin");
                if (Context.IsMainPlayer)
                    Game1.activeClickableMenu = new BankFixedMenu();
                else
                    Game1.exitActiveMenu();
                break;
            case "early":
                Bank.EarlyWithdrawFixedDeposit(index);
                Game1.playSound("coin");
                if (Context.IsMainPlayer)
                    Game1.activeClickableMenu = new BankFixedMenu();
                else
                    Game1.exitActiveMenu();
                break;
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
        RefreshActionButtons();
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
            xPositionOnScreen, yPositionOnScreen, width, height, Color.White, 4f);

        var title = Tools.GetI18n(I18nKeys.Bank_FixedTab).ToString();
        var titleSize = Game1.dialogueFont.MeasureString(title);
        Utility.drawTextWithShadow(b, title, Game1.dialogueFont,
            new Vector2(xPositionOnScreen + (width - titleSize.X) / 2, yPositionOnScreen - 32), Color.Black);

        var cx = xPositionOnScreen + ContentPadding;
        var cy = yPositionOnScreen + TopPadding;

        var terms = BankCalculator.FixedTermOptions;
        for (var t = 0; t < terms.Length; t++)
        {
            var btn = _actionButtons.FirstOrDefault(a => a.name == $"newFixed_{terms[t]}");
            if (btn == null) continue;
            var label = $"{terms[t]}{Tools.GetI18n(I18nKeys.Bank_Days).ToString()}";
            DrawButton(b, btn.bounds.X, btn.bounds.Y, btn.bounds.Width, btn.bounds.Height, label);
        }

        var deposits = Bank.GetFixedDeposits();
        var listY = cy + 56;
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();

        if (deposits.Count == 0)
        {
            Utility.drawTextWithShadow(b, Tools.GetI18n(I18nKeys.Bank_FixedEmpty).ToString(),
                Game1.smallFont, new Vector2(cx, listY), Color.Gray);
        }
        else
        {
            var displayIndex = 0;
            var btnIndex = 0;
            for (var i = 0; i < deposits.Count; i++)
            {
                var d = deposits[i];
                if (d.Withdrawn) continue;

                displayIndex++;
                var elapsed = (int)Game1.stats.DaysPlayed - d.StartDay;
                var matured = elapsed >= d.TermDays;

                var line = $"[{displayIndex}] ";
                line += Tools.GetI18n(I18nKeys.Bank_FixedAmount).Tokens(new { amount = d.Amount, gold }).ToString();
                line += $" | {d.TermDays} {Tools.GetI18n(I18nKeys.Bank_Days).ToString()}";
                line += " | ";

                if (matured)
                    line += Tools.GetI18n(I18nKeys.Bank_FixedStatusMature).ToString();
                else
                {
                    var remaining = d.TermDays - elapsed;
                    line += Tools.GetI18n(I18nKeys.Bank_FixedStatusActive).Tokens(new { remaining }).ToString();
                }

                Utility.drawTextWithShadow(b, line, Game1.smallFont, new Vector2(cx, listY), Color.Black);

                if (btnIndex < _fixedActionButtons.Count)
                {
                    var actionBtn = _fixedActionButtons[btnIndex++];
                    var label = actionBtn.name.StartsWith("redeem")
                        ? Tools.GetI18n(I18nKeys.Bank_Redeem).ToString()
                        : Tools.GetI18n(I18nKeys.Bank_EarlyWithdraw).ToString();
                    DrawButton(b, actionBtn.bounds.X, actionBtn.bounds.Y,
                        actionBtn.bounds.Width, actionBtn.bounds.Height, label);
                }

                listY += 40;
            }
        }

        base.draw(b);
        drawMouse(b);
    }

    private static void DrawButton(SpriteBatch b, int x, int y, int w, int h, string label)
    {
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9),
            x, y, w, h, Color.White, 4f);
        Utility.drawTextWithShadow(b, label, Game1.smallFont,
            new Vector2(x + (w - Game1.smallFont.MeasureString(label).X) / 2, y + (h - Game1.smallFont.MeasureString(label).Y) / 2), Game1.textColor);
    }
}
