using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Constant;
using SharedBikesFeature = Red_Panda_Bazaar_Code.Features.SharedBikes.SharedBikes;
using Red_Panda_Bazaar_Code.Utils;
using StardewUI;
using StardewUI.Graphics;
using StardewUI.Layout;
using StardewUI.Widgets;
using StardewValley;
using WhaleK.Common;

namespace Red_Panda_Bazaar_Code.Features.Bank;

/// <summary>银行项目投资菜单：付费开通集市公共项目（当前：共享单车）。</summary>
public class BankInvestmentMenu : ViewMenu
{
    private Label? _statusLabel;
    private Button? _unlockButton;

    protected override IView CreateView()
    {
        CloseButtonSprite = UiSprites.CloseButton;

        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();

        _statusLabel = MenuUi.Text(string.Empty);
        _unlockButton = MenuUi.Button(
            Tools.GetI18n(I18nKeys.Bank_InvestmentUnlock)
                .Tokens(new { amount = SharedBikesFeature.UnlockPrice, gold })
                .ToString(),
            OnUnlockBike,
            new(Left: 12));

        var content = MenuUi.Column();
        content.Children.Add(
            MenuUi.TextRow(Tools.GetI18n(I18nKeys.Bank_InvestmentTitle).ToString(), Game1.dialogueFont)
        );
        content.Children.Add(
            MenuUi.TextRow(Tools.GetI18n(I18nKeys.Bank_InvestmentBike).ToString(), topMargin: MenuUi.RowGap)
        );
        content.Children.Add(
            MenuUi.TextRow(
                Tools.GetI18n(I18nKeys.Bank_InvestmentBikeDesc).ToString(),
                color: Color.Gray,
                topMargin: MenuUi.RowGap
            )
        );

        var statusRow = MenuUi.Row(topMargin: MenuUi.RowGap);
        statusRow.Children.Add(_statusLabel);
        statusRow.Children.Add(_unlockButton);
        content.Children.Add(statusRow);

        RefreshStatus();

        return MenuUi.Window(content);
    }

    private void OnUnlockBike()
    {
        SharedBikesFeature.TryUnlock();
        RefreshStatus();
    }

    /// <summary>按解锁状态刷新状态文本与解锁按钮（替代原框架的整体重建）。</summary>
    private void RefreshStatus()
    {
        if (_statusLabel is null || _unlockButton is null)
        {
            return;
        }
        var unlocked = SharedBikesFeature.IsUnlocked();
        _statusLabel.Text = Tools.GetI18n(
            unlocked ? I18nKeys.Bank_InvestmentStatusUnlocked : I18nKeys.Bank_InvestmentStatusLocked
        ).ToString();
        _statusLabel.Color = unlocked ? Color.Green : Game1.textColor;
        _unlockButton.Visibility = unlocked ? Visibility.Hidden : Visibility.Visible;
    }
}
