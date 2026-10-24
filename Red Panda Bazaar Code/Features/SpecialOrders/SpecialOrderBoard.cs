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
        ModInfo.Helper.Reflection.GetField<Texture2D>(this, "billboardTexture")
            .SetValue(ModInfo.Helper.ModContent.Load<Texture2D>("assets/Textures/RPB_SpecialOrderBoard.png"));
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        Game1.activeClickableMenu = (IClickableMenu)new SpecialOrderBoard(this.boardType);
    }

    public static void UpdateAvailableSpecialOrders(bool forceRefresh)
    {
        // 游戏内该列表不会为 null；防御极端时序下的空引用，取不到就直接跳过本次刷新
        var availableOrders = Game1.player.team.availableSpecialOrders;
        if (availableOrders is null)
            return;

        foreach (SpecialOrder order in availableOrders)
        {
            if ((order.questDuration.Value == QuestDuration.TwoDays ||
                 order.questDuration.Value == QuestDuration.ThreeDays) &&
                !Game1.player.team.acceptedSpecialOrderTypes.Contains(order.orderType.Value))
            {
                order.SetDuration(order.questDuration.Value);
            }
        }

        if (!forceRefresh)
        {
            foreach (SpecialOrder availableSpecialOrder in availableOrders)
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
        Random random = new RandomUtils.RandomSeed("SpecialOrderBoard");
        for (int index = 0; index < 2; ++index)
        {
            if (stringList.Count == 0)
            {
                if (collection.Count == 0)
                    break;
                stringList = new List<string>((IEnumerable<string>)collection);
            }

            string key = random.ChooseFrom<string>((IList<string>)stringList);
            availableOrders.Add(SpecialOrder.GetSpecialOrder(key, new int?(random.Next())));
            stringList.Remove(key);
            collection.Remove(key);
        }
    }
}