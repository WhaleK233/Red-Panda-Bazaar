using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewUI;
using StardewUI.Graphics;
using StardewUI.Layout;
using StardewUI.Widgets;
using StardewValley;
using SharedBikesFeature = Red_Panda_Bazaar_Code.Features.SharedBikes.SharedBikes;

namespace Red_Panda_Bazaar_Code.Features.Bank;

/// <summary>银行项目投资菜单：付费开通集市公共项目（当前：共享单车）。</summary>
public class BankInvestmentMenu : ViewMenu
{
    private Label? _statusLabel;
    private Button? _unlockButton;
    private bool? _shownUnlocked;

    public override void update(GameTime time)
    {
        base.update(time);
        RefreshStatusIfChanged();
    }

    protected override IView CreateView()
    {
        CloseButtonSprite = UiSprites.CloseButton;

        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();
        _statusLabel = MenuUi.Text(string.Empty, bold: true);
        _unlockButton = MenuUi.PrimaryButton(
            Tools.GetI18n(I18nKeys.Bank_InvestmentUnlock)
                .Tokens(new { amount = SharedBikesFeature.UnlockPrice, gold })
                .ToString(),
            OnUnlockBike
        );

        // 项目卡：项目名与说明打头，分隔线之下是解锁状态与开通入口
        var project = MenuUi.Column();
        project.Children.Add(
            MenuUi.TextRow(Tools.GetI18n(I18nKeys.Bank_InvestmentBike).ToString(), color: Color.Black, bold: true)
        );
        project.Children.Add(
            MenuUi.TextRow(
                Tools.GetI18n(I18nKeys.Bank_InvestmentBikeDesc).ToString(),
                color: MenuUi.Muted,
                topMargin: MenuUi.RowGap
            )
        );
        project.Children.Add(MenuUi.Divider());
        project.Children.Add(MenuUi.RowBetween(_statusLabel, _unlockButton));

        var content = MenuUi.Column();
        content.Children.Add(MenuUi.Header(Tools.GetI18n(I18nKeys.Bank_InvestmentTitle).ToString()));
        content.Children.Add(MenuUi.Card(project, topMargin: MenuUi.SectionGap));

        RefreshStatus();

        return MenuUi.Window(content);
    }

    private void OnUnlockBike()
    {
        SharedBikesFeature.TryUnlock();
        RefreshStatusIfChanged();
    }

    /// <summary>客机收到主机广播后没有菜单回调，在更新循环中检测状态变化。</summary>
    private void RefreshStatusIfChanged()
    {
        var unlocked = SharedBikesFeature.IsUnlocked();
        if (_shownUnlocked == unlocked)
        {
            return;
        }

        _shownUnlocked = unlocked;
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
        _statusLabel.Color = unlocked ? MenuUi.Positive : MenuUi.Locked;
        _unlockButton.Visibility = unlocked ? Visibility.Hidden : Visibility.Visible;
    }
}
