using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Framework.UI;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Features.Bank;

public class BankFixedMenu : UiBaseMenu
{
    protected override Point CalcContentSize() => new(600, 120);

    protected override void BuildUi()
    {
        // 新建定期（横排）
        var row = new UiRow { Spacing = 10 };
        foreach (var term in BankCalculator.FixedTermOptions)
        {
            var label = $"{term}{Tools.GetI18n(I18nKeys.Bank_Days).ToString()}";
            row.Add(new UiButton(label, () => OpenNewFixed(term)));
        }
        Root.Add(row);

        // 现有定期列表
        var deposits = Bank.GetFixedDeposits();
        if (deposits.Count == 0)
        {
            Root.Add(new UiText(Tools.GetI18n(I18nKeys.Bank_FixedEmpty).ToString(), color: Color.Gray));
        }
        else
        {
            var displayIndex = 0;
            var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();
            for (var i = 0; i < deposits.Count; i++)
            {
                var d = deposits[i];
                if (d.Withdrawn) continue;

                displayIndex++;
                var idx = i; // capture
                var elapsed = (int)Game1.stats.DaysPlayed - d.StartDay;
                var matured = elapsed >= d.TermDays;

                var line = $"[{displayIndex}] ";
                line += Tools.GetI18n(I18nKeys.Bank_FixedAmount).Tokens(new { amount = d.Amount, gold }).ToString();
                line += $" | {d.TermDays} {Tools.GetI18n(I18nKeys.Bank_Days).ToString()}";
                line += " | ";
                line += matured
                    ? Tools.GetI18n(I18nKeys.Bank_FixedStatusMature).ToString()
                    : Tools.GetI18n(I18nKeys.Bank_FixedStatusActive)
                        .Tokens(new { remaining = d.TermDays - elapsed }).ToString();

                var btnLabel = matured
                    ? Tools.GetI18n(I18nKeys.Bank_Redeem).ToString()
                    : Tools.GetI18n(I18nKeys.Bank_EarlyWithdraw).ToString();
                var action = matured
                    ? (Action)(() => HandleRedeem(idx))
                    : () => HandleEarly(idx);

                Root.Add(new UiRow { Stretch = true, JustifyContent = UiJustify.SpaceBetween }
                    .Add(new UiText(line))
                    .Add(new UiButton(btnLabel, action)));
            }
        }
    }

    private void OpenNewFixed(int termDays)
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

    private void HandleRedeem(int index)
    {
        Bank.RedeemFixedDeposit(index);
        Game1.playSound("coin");
        if (Context.IsMainPlayer) Rebuild();
        else Game1.exitActiveMenu();
    }

    private void HandleEarly(int index)
    {
        Bank.EarlyWithdrawFixedDeposit(index);
        Game1.playSound("coin");
        if (Context.IsMainPlayer) Rebuild();
        else Game1.exitActiveMenu();
    }
}
