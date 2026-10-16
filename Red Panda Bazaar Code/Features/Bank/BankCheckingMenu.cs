using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewUI;
using StardewUI.Graphics;
using StardewValley;
using StardewValley.Menus;
using WhaleK.Common;

namespace Red_Panda_Bazaar_Code.Features.Bank;

/// <summary>银行活期账户菜单：展示余额、今日利息与利率，提供存入/取出入口。</summary>
public class BankCheckingMenu : ViewMenu
{
    protected override IView CreateView()
    {
        CloseButtonSprite = UiSprites.CloseButton;

        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();
        var balance = Bank.GetCheckingBalance();
        var rate = BankCalculator.GetDailyCheckingRate();
        var todayInterest = (long)(balance * rate);

        var content = MenuUi.Column();
        content.Children.Add(
            MenuUi.TextRow(
                Tools.GetI18n(I18nKeys.Bank_CheckingBalance).Tokens(new { amount = balance, gold }).ToString(),
                Game1.dialogueFont
            )
        );
        content.Children.Add(
            MenuUi.TextRow(
                Tools.GetI18n(I18nKeys.Bank_TodayInterest).Tokens(new { amount = todayInterest, gold }).ToString(),
                topMargin: MenuUi.RowGap
            )
        );
        content.Children.Add(
            MenuUi.TextRow(
                Tools.GetI18n(I18nKeys.Bank_TodayRate).Tokens(new { rate = (rate * 100).ToString("F2") }).ToString(),
                color: Color.Gray,
                topMargin: MenuUi.RowGap
            )
        );

        var buttons = MenuUi.Row(topMargin: MenuUi.RowGap);
        buttons.Children.Add(
            MenuUi.Button(Tools.GetI18n(I18nKeys.Bank_Deposit).ToString(), OpenDeposit, new(Right: 20))
        );
        buttons.Children.Add(MenuUi.Button(Tools.GetI18n(I18nKeys.Bank_Withdraw).ToString(), OpenWithdraw));
        content.Children.Add(buttons);

        return MenuUi.Window(content);
    }

    private void OpenDeposit()
    {
        Game1.activeClickableMenu = new NumberSelectionMenu(
            Tools.GetI18n(I18nKeys.Bank_DepositTitle).ToString(),
            (number, price, who) =>
            {
                if (number > 0) Bank.Deposit(number);
                Game1.exitActiveMenu();
                if (Context.IsMainPlayer && number > 0)
                    Game1.activeClickableMenu = new BankCheckingMenu();
            },
            price: -1, minValue: 1, maxValue: Math.Max(1, Game1.player.Money),
            defaultNumber: Math.Min(100, Game1.player.Money));
    }

    private void OpenWithdraw()
    {
        Game1.activeClickableMenu = new NumberSelectionMenu(
            Tools.GetI18n(I18nKeys.Bank_WithdrawTitle).ToString(),
            (number, price, who) =>
            {
                if (number > 0) Bank.Withdraw(number);
                Game1.exitActiveMenu();
                if (Context.IsMainPlayer && number > 0)
                    Game1.activeClickableMenu = new BankCheckingMenu();
            },
            price: -1, minValue: 1, maxValue: (int)Math.Max(1L, Bank.GetCheckingBalance()),
            defaultNumber: (int)Math.Min(100L, Bank.GetCheckingBalance()));
    }
}
