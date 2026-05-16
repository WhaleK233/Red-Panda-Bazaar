using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Framework.UI;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.Bank;

public class BankTaxMenu : UiBaseMenu
{
    protected override Point CalcContentSize() => new(400, 60);

    protected override void BuildUi()
    {
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();
        var totalTax = PlayerStall.PlayerStall.TotalTax;

        Root.Add(new UiText(
            Tools.GetI18n(I18nKeys.Bank_TotalTax)
                .Tokens(new { amount = totalTax, gold }).ToString(),
            Game1.dialogueFont));
    }
}
