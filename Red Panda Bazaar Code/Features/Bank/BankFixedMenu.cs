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

/// <summary>银行定期理财菜单：列出三档存期与已有定期存款的支取入口。</summary>
public class BankFixedMenu : ViewMenu
{
    private Lane? _content;

    protected override IView CreateView()
    {
        CloseButtonSprite = UiSprites.CloseButton;

        _content = MenuUi.Column();
        BuildContent();
        return MenuUi.Window(_content);
    }

    /// <summary>按当前存款数据重建内容（等价于原框架的 <c>Rebuild</c>）。</summary>
    private void BuildContent()
    {
        if (_content is null)
        {
            return;
        }
        _content.Children.Clear();

        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();
        var days = Tools.GetI18n(I18nKeys.Bank_Days).ToString();

        // 三种定期方案，每种一行
        foreach (var term in BankCalculator.FixedTermOptions)
        {
            var rate = BankCalculator.GetFixedTermRate(term);
            var dailyRate = rate / term;
            var t = term; // capture
            var desc = $"{term}{days} {Tools.GetI18n(I18nKeys.Bank_DailyRateLabel).Tokens(new { rate = (dailyRate * 100).ToString("F2") })}";
            _content.Children.Add(
                MenuUi.RowBetween(
                    desc,
                    MenuUi.Button(Tools.GetI18n(I18nKeys.Bank_Apply).ToString(), () => OpenNewFixed(t))
                )
            );
        }

        // 分割线
        _content.Children.Add(MenuUi.Divider());

        // 现有定期列表
        var deposits = Bank.GetFixedDeposits();
        if (deposits.Count == 0)
        {
            _content.Children.Add(
                MenuUi.TextRow(
                    Tools.GetI18n(I18nKeys.Bank_FixedEmpty).ToString(),
                    color: Color.Gray,
                    topMargin: MenuUi.RowGap
                )
            );
        }
        else
        {
            var displayIndex = 0;
            for (var i = 0; i < deposits.Count; i++)
            {
                var d = deposits[i];
                if (d.Withdrawn) continue;

                displayIndex++;
                var idx = i; // capture
                var elapsed = (int)Game1.stats.DaysPlayed - d.StartDay;
                var matured = elapsed >= d.TermDays;

                var rate = BankCalculator.GetFixedTermRate(d.TermDays);
                var interest = (long)(d.Amount * rate * Math.Min(elapsed, d.TermDays) / d.TermDays);
                var line = $"[{displayIndex}] ";
                line += Tools.GetI18n(I18nKeys.Bank_FixedAmount).Tokens(new { amount = d.Amount, gold }).ToString();
                line += $" | {Tools.GetI18n(I18nKeys.Bank_LoanInterest).Tokens(new { amount = interest, gold })}";
                line += $" | {d.TermDays} {days}";
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

                _content.Children.Add(MenuUi.RowBetween(line, MenuUi.Button(btnLabel, action)));
            }
        }

        // 底部提示
        _content.Children.Add(
            MenuUi.TextRow(
                Tools.GetI18n(I18nKeys.Bank_FixedEarlyWithdrawTip).ToString(),
                color: Color.Gray,
                topMargin: MenuUi.RowGap
            )
        );
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
        if (Context.IsMainPlayer) BuildContent();
        else Game1.exitActiveMenu();
    }

    private void HandleEarly(int index)
    {
        Bank.EarlyWithdrawFixedDeposit(index);
        Game1.playSound("coin");
        if (Context.IsMainPlayer) BuildContent();
        else Game1.exitActiveMenu();
    }
}
