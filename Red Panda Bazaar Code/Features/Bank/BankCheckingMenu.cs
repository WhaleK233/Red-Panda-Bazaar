using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Framework.UI;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewModdingAPI;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Features.Bank;

public class BankCheckingMenu : UiBaseMenu {
    protected override Point CalcContentSize() => new(400, 200);

    protected override void BuildUi() {
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();
        var balance = Bank.GetCheckingBalance();
        var todayInterest = (long)(balance * BankCalculator.GetDailyCheckingRate());
        var rate = BankCalculator.GetDailyCheckingRate();

        Root.Add(
            new UiText(Tools.GetI18n(I18nKeys.Bank_CheckingBalance)
                .Tokens(new { amount = balance, gold }).ToString(), Game1.dialogueFont),
            new UiText(Tools.GetI18n(I18nKeys.Bank_TodayInterest)
                .Tokens(new { amount = todayInterest, gold }).ToString()),
            new UiText(Tools.GetI18n(I18nKeys.Bank_TodayRate)
                .Tokens(new { rate = (rate * 100).ToString("F2") }).ToString(), color: Color.Gray),
            new UiRow { Spacing = 20 }
                .Add(new UiButton(Tools.GetI18n(I18nKeys.Bank_Deposit).ToString(), () => OpenDeposit()))
                .Add(new UiButton(Tools.GetI18n(I18nKeys.Bank_Withdraw).ToString(), () => OpenWithdraw()))
        );
    }

    private void OpenDeposit() {
        Game1.activeClickableMenu = new NumberSelectionMenu(
            Tools.GetI18n(I18nKeys.Bank_DepositTitle).ToString(),
            (number, price, who) => {
                if (number > 0) Bank.Deposit(number);
                Game1.exitActiveMenu();
                if (Context.IsMainPlayer && number > 0)
                    Game1.activeClickableMenu = new BankCheckingMenu();
            },
            price: -1, minValue: 1, maxValue: Math.Max(1, Game1.player.Money),
            defaultNumber: Math.Min(100, Game1.player.Money));
    }

    private void OpenWithdraw() {
        Game1.activeClickableMenu = new NumberSelectionMenu(
            Tools.GetI18n(I18nKeys.Bank_WithdrawTitle).ToString(),
            (number, price, who) => {
                if (number > 0) Bank.Withdraw(number);
                Game1.exitActiveMenu();
                if (Context.IsMainPlayer && number > 0)
                    Game1.activeClickableMenu = new BankCheckingMenu();
            },
            price: -1, minValue: 1, maxValue: (int)Math.Max(1L, Bank.GetCheckingBalance()),
            defaultNumber: (int)Math.Min(100L, Bank.GetCheckingBalance()));
    }
}