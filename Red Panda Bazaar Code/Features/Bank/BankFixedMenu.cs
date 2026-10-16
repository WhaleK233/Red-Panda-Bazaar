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
        _content.Children.Add(MenuUi.Header(Tools.GetI18n(I18nKeys.Bank_ServiceFixed).ToString()));
        BuildContent();
        return MenuUi.Window(_content);
    }

    /// <summary>按当前存款数据重建内容（标题条保留，其余整体重建）。</summary>
    private void BuildContent()
    {
        if (_content is null)
        {
            return;
        }
        while (_content.Children.Count > 1)
        {
            _content.Children.RemoveAt(_content.Children.Count - 1);
        }

        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();
        var days = Tools.GetI18n(I18nKeys.Bank_Days).ToString();

        _content.Children.Add(MenuUi.Card(BuildPlans(days), topMargin: MenuUi.SectionGap));
        _content.Children.Add(MenuUi.Card(BuildHoldings(gold, days), topMargin: MenuUi.SectionGap));
        _content.Children.Add(
            MenuUi.TextRow(
                Tools.GetI18n(I18nKeys.Bank_FixedEarlyWithdrawTip).ToString(),
                color: MenuUi.Muted,
                topMargin: MenuUi.SectionGap
            )
        );
    }

    /// <summary>可开立的三档存期：每行"存期 + 日利率"，右侧一键申请。</summary>
    private Lane BuildPlans(string days)
    {
        var plans = MenuUi.Column();
        var first = true;
        foreach (var term in BankCalculator.FixedTermOptions)
        {
            var termDays = term;
            var dailyRate = BankCalculator.GetFixedTermRate(termDays) / termDays;
            var desc =
                $"{termDays} {days}　"
                + Tools.GetI18n(I18nKeys.Bank_DailyRateLabel)
                    .Tokens(new { rate = (dailyRate * 100).ToString("F2") })
                    .ToString();
            plans.Children.Add(
                MenuUi.RowBetween(
                    desc,
                    MenuUi.PrimaryButton(Tools.GetI18n(I18nKeys.Bank_Apply).ToString(), () => OpenNewFixed(termDays)),
                    topMargin: first ? 0 : MenuUi.RowGap
                )
            );
            first = false;
        }
        return plans;
    }

    /// <summary>已有定期：每笔以"金额 + 利息"为主行（右侧支取/赎回），存期与状态作为次行弱化显示。</summary>
    private Lane BuildHoldings(string gold, string days)
    {
        var holdings = MenuUi.Column();
        var deposits = Bank.GetFixedDeposits();
        if (deposits.Count == 0)
        {
            holdings.Children.Add(
                MenuUi.TextRow(Tools.GetI18n(I18nKeys.Bank_FixedEmpty).ToString(), color: MenuUi.Muted)
            );
            return holdings;
        }

        var first = true;
        var displayIndex = 0;
        for (var i = 0; i < deposits.Count; i++)
        {
            var deposit = deposits[i];
            if (deposit.Withdrawn)
            {
                continue;
            }

            displayIndex++;
            var index = i;
            var elapsed = (int)Game1.stats.DaysPlayed - deposit.StartDay;
            var matured = elapsed >= deposit.TermDays;
            var rate = BankCalculator.GetFixedTermRate(deposit.TermDays);
            var interest = (long)(deposit.Amount * rate * Math.Min(elapsed, deposit.TermDays) / deposit.TermDays);
            var status = matured
                ? Tools.GetI18n(I18nKeys.Bank_FixedStatusMature).ToString()
                : Tools.GetI18n(I18nKeys.Bank_FixedStatusActive)
                    .Tokens(new { remaining = deposit.TermDays - elapsed })
                    .ToString();
            var buttonLabel = matured
                ? Tools.GetI18n(I18nKeys.Bank_Redeem).ToString()
                : Tools.GetI18n(I18nKeys.Bank_EarlyWithdraw).ToString();
            var action = matured ? (Action)(() => HandleRedeem(index)) : () => HandleEarly(index);
            var button = matured
                ? MenuUi.PrimaryButton(buttonLabel, action, new(Left: 16))
                : MenuUi.SecondaryButton(buttonLabel, action, new(Left: 16));

            var main =
                $"[{displayIndex}] "
                + Tools.GetI18n(I18nKeys.Bank_FixedAmount).Tokens(new { amount = deposit.Amount, gold }).ToString()
                + "　"
                + Tools.GetI18n(I18nKeys.Bank_LoanInterest).Tokens(new { amount = interest, gold }).ToString();

            holdings.Children.Add(
                MenuUi.RowBetween(main, button, topMargin: first ? 0 : MenuUi.SectionGap)
            );
            holdings.Children.Add(
                MenuUi.TextRow(
                    $"{deposit.TermDays} {days}　{status}",
                    color: matured ? MenuUi.Positive : MenuUi.Muted,
                    topMargin: MenuUi.RowGap,
                    bold: matured
                )
            );
            first = false;
        }
        return holdings;
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
            price: -1,
            minValue: 1,
            maxValue: Math.Max(1, Game1.player.Money),
            defaultNumber: Math.Min(100, Game1.player.Money)
        );
    }

    private void HandleRedeem(int index)
    {
        Bank.RedeemFixedDeposit(index);
        Game1.playSound("coin");
        if (Context.IsMainPlayer)
            BuildContent();
        else
            Game1.exitActiveMenu();
    }

    private void HandleEarly(int index)
    {
        Bank.EarlyWithdrawFixedDeposit(index);
        Game1.playSound("coin");
        if (Context.IsMainPlayer)
            BuildContent();
        else
            Game1.exitActiveMenu();
    }
}
