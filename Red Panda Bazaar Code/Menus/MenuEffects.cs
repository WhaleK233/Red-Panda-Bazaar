using Red_Panda_Bazaar_Code.Menus.Custom_Menus;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Menus;

public class MenuEffects
{
    private static bool Enabled { get; set; } = false;

    /// <summary>启用自定义Buff</summary>
    public static void Enable()
    {
        // 如果未启用
        if (!Enabled)
        {
            InitCustomMenus();

            Enabled = true;
            Tools.Log("Custom Buffs Enabled");
        }
    }

    private static void InitCustomMenus()
    {
        GameLocation.RegisterTileAction("RedPandaBazaar_PrizeMachine_1", (location, strings, arg3, arg4) =>
            {
                Game1.activeClickableMenu = (IClickableMenu)new RPB_PrizeTicketMenu();
                return false;
            }
        );
    }
}