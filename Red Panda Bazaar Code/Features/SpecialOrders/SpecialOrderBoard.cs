using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.SpecialOrders;
using StardewValley.Menus;
using StardewValley.SpecialOrders;

namespace Red_Panda_Bazaar_Code.Features.SpecialOrders;

public class SpecialOrderBoard : SpecialOrdersBoard
{
    public SpecialOrderBoard(string board_type = "") : base(board_type)
    {
        Tools.Helper.Reflection.GetField<Texture2D>(this, "billboardTexture")
            .SetValue(Tools.Helper.ModContent.Load<Texture2D>("assets/RPB_SpecialOrderBoard.png"));
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        Game1.activeClickableMenu = (IClickableMenu)new SpecialOrderBoard(this.boardType);
    }

    public static void UpdateAvailableSpecialOrders(bool forceRefresh)
    {
        if (Game1.player.team.availableSpecialOrders is not null)
        {
            foreach (SpecialOrder order in Game1.player.team.availableSpecialOrders)
            {
                if ((order.questDuration.Value == QuestDuration.TwoDays ||
                     order.questDuration.Value == QuestDuration.ThreeDays) &&
                    !Game1.player.team.acceptedSpecialOrderTypes.Contains(order.orderType.Value))
                {
                    order.SetDuration(order.questDuration.Value);
                }
            }
        }

        if (!forceRefresh)
        {
            foreach (SpecialOrder availableSpecialOrder in Game1.player.team.availableSpecialOrders)
            {
                if (availableSpecialOrder.orderType.Value == QuestsKeys.CXM_OrderType)
                    return;
            }
        }

        SpecialOrder.RemoveAllSpecialOrders(QuestsKeys.CXM_OrderType);
        List<string> stringList = new List<string>();
        foreach (KeyValuePair<string, SpecialOrderData> specialOrder in DataLoader.SpecialOrders(Game1.content))
        {
            if (specialOrder.Value.OrderType == QuestsKeys.CXM_OrderType &&
                SpecialOrder.CanStartOrderNow(specialOrder.Key, specialOrder.Value))
                stringList.Add(specialOrder.Key);
        }

        List<string> collection = new List<string>((IEnumerable<string>)stringList);
        Random random = new RandomSeed("SpecialOrderBoard");
        for (int index = 0; index < 2; ++index)
        {
            if (stringList.Count == 0)
            {
                if (collection.Count == 0)
                    break;
                stringList = new List<string>((IEnumerable<string>)collection);
            }

            string key = random.ChooseFrom<string>((IList<string>)stringList);
            Game1.player.team.availableSpecialOrders.Add(SpecialOrder.GetSpecialOrder(key, new int?(random.Next())));
            stringList.Remove(key);
            collection.Remove(key);
        }
    }
}