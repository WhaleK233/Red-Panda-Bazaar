using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewUI;
using StardewUI.Graphics;
using StardewUI.Widgets;
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

        var content = MenuUi.Column();
        content.Children.Add(MenuUi.Header(Tools.GetI18n(I18nKeys.Bank_ServiceChecking).ToString()));
        content.Children.Add(MenuUi.Card(BuildSummary(), topMargin: MenuUi.SectionGap));

        var actions = MenuUi.Row(topMargin: MenuUi.SectionGap);
        actions.Children.Add(
            MenuUi.PrimaryButton(Tools.GetI18n(I18nKeys.Bank_Deposit).ToString(), OpenDeposit, new(Right: 12))
        );
        actions.Children.Add(MenuUi.SecondaryButton(Tools.GetI18n(I18nKeys.Bank_Withdraw).ToString(), OpenWithdraw));
        content.Children.Add(actions);

        return MenuUi.Window(content);
    }

    /// <summary>账户概览卡：大号余额打头，分隔线之下是今日利息与日利率（弱化显示）。</summary>
    private static Lane BuildSummary()
    {
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();
        var balance = Bank.GetCheckingBalance();
        var rate = BankCalculator.GetDailyCheckingRate();
        var todayInterest = (long)(balance * rate);

        var summary = MenuUi.Column();
        summary.Children.Add(
            MenuUi.TextRow(
                Tools.GetI18n(I18nKeys.Bank_CheckingBalance).Tokens(new { amount = balance, gold }).ToString(),
                Game1.dialogueFont,
                color: Color.Black
            )
        );
        summary.Children.Add(MenuUi.Divider());
        summary.Children.Add(
            MenuUi.TextRow(
                Tools.GetI18n(I18nKeys.Bank_TodayInterest).Tokens(new { amount = todayInterest, gold }).ToString()
            )
        );
        summary.Children.Add(
            MenuUi.TextRow(
                Tools.GetI18n(I18nKeys.Bank_TodayRate).Tokens(new { rate = (rate * 100).ToString("F2") }).ToString(),
                color: MenuUi.Muted,
                topMargin: MenuUi.RowGap
            )
        );
        return summary;
    }

    private void OpenDeposit()
    {
        Game1.activeClickableMenu = new NumberSelectionMenu(
            Tools.GetI18n(I18nKeys.Bank_DepositTitle).ToString(),
            (number, price, who) =>
            {
                if (number > 0)
                    Bank.Deposit(number);
                Game1.exitActiveMenu();
                if (Context.IsMainPlayer && number > 0)
                    Game1.activeClickableMenu = new BankCheckingMenu();
            },
            price: -1,
            minValue: 1,
            maxValue: Math.Max(1, Game1.player.Money),
            defaultNumber: Math.Min(100, Game1.player.Money)
        );
    }

    private void OpenWithdraw()
    {
        Game1.activeClickableMenu = new NumberSelectionMenu(
            Tools.GetI18n(I18nKeys.Bank_WithdrawTitle).ToString(),
            (number, price, who) =>
            {
                if (number > 0)
                    Bank.Withdraw(number);
                Game1.exitActiveMenu();
                if (Context.IsMainPlayer && number > 0)
                    Game1.activeClickableMenu = new BankCheckingMenu();
            },
            price: -1,
            minValue: 1,
            maxValue: (int)Math.Max(1L, Bank.GetCheckingBalance()),
            defaultNumber: (int)Math.Min(100L, Bank.GetCheckingBalance())
        );
    }
}
