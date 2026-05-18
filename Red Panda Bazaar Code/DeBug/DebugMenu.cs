using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Framework.UI;
using Red_Panda_Bazaar_Code.Framework.UI.Components;
using Red_Panda_Bazaar_Code.Framework.UI.Enums;
using StardewValley;

namespace Red_Panda_Bazaar_Code.DeBug;

public class DebugMenu : UiBaseMenu
{
    protected override void BuildUi()
    {
        Root.Add(
            new UiText("调试菜单", Game1.dialogueFont),
            new UiSeparator(),
            new UiRow { Spacing = 16, Stretch = true, JustifyContent = UiJustify.SpaceBetween }
                .Add(
                    new UiText("设置金钱为 0", color: Game1.textColor),
                    new UiButton("执行", () =>
                    {
                        Game1.player.Money = 0;
                        Game1.exitActiveMenu();
                    })
                )
        );
    }
}
