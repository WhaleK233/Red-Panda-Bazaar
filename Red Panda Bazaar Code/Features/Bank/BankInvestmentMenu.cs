using WhaleK.Common;
using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Constant;
using SharedBikesFeature = Red_Panda_Bazaar_Code.Features.SharedBikes.SharedBikes;
using WhaleK.Common.UI;
using WhaleK.Common.UI.Controls;
using WhaleK.Common.UI.Layout;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.Bank;

/// <summary>银行项目投资菜单：付费开通集市公共项目（当前：共享单车）。</summary>
public class BankInvestmentMenu : UiBaseMenu
{
    protected override void BuildUi()
    {
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();

        Root.Add(
            new UiText(Tools.GetI18n(I18nKeys.Bank_InvestmentTitle).ToString(), Game1.dialogueFont),
            new UiText(Tools.GetI18n(I18nKeys.Bank_InvestmentBike).ToString()),
            new UiText(Tools.GetI18n(I18nKeys.Bank_InvestmentBikeDesc).ToString(), color: Color.Gray),
            SharedBikesFeature.IsUnlocked()
                ? new UiText(Tools.GetI18n(I18nKeys.Bank_InvestmentStatusUnlocked).ToString(), color: Color.Green)
                : new UiRow { Spacing = 12 }
                    .Add(new UiText(Tools.GetI18n(I18nKeys.Bank_InvestmentStatusLocked).ToString()))
                    .Add(new UiButton(Tools.GetI18n(I18nKeys.Bank_InvestmentUnlock)
                        .Tokens(new { amount = SharedBikesFeature.UnlockPrice, gold }).ToString(), OnUnlockBike))
        );
    }

    private void OnUnlockBike()
    {
        SharedBikesFeature.TryUnlock();
        Rebuild();
    }
}
