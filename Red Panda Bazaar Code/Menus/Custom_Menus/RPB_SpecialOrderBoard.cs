using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Menus.Custom_Menus;

public class RPB_SpecialOrderBoard : SpecialOrdersBoard
{
    public RPB_SpecialOrderBoard(string board_type = "") : base(board_type)
    {
        Tools.Helper.Reflection.GetField<Texture2D>(this, "billboardTexture")
            .SetValue(Tools.Helper.ModContent.Load<Texture2D>("assets/RPB_SpecialOrderBoard.png"));
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        Game1.activeClickableMenu = (IClickableMenu) new RPB_SpecialOrderBoard(this.boardType);
    }
}