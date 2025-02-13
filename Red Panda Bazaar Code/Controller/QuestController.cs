using Red_Panda_Bazaar_Code.Data;
using Red_Panda_Bazaar_Code.Menus;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Controller;

public static class QuestController
{
    private static bool Enabled { get; set; } = false;

    /// <summary>启用自定义任务</summary>
    public static void Init()
    {
        // 如果未启用
        if (!Enabled)
        {
            Tools.Helper.Events.GameLoop.DayStarted += OnDayStarted;

            RegisterActions();

            Enabled = true;
            Tools.Log("Quests Initialized.");
        }
    }

    private static void RegisterActions()
    {
        GameLocation.RegisterTileAction("RedPandaBazaar_PrizeTicketReward",
            (location, strings, arg3, arg4) =>
            {
                if (RPBData.PrizeTicketReward > 0U)
                {
                    if (Game1.player.couldInventoryAcceptThisItem(
                            ItemRegistry.Create("(O)RedPandaBazaar_Prize_Ticket_1")))
                    {
                        Game1.player.addItemToInventoryBool(
                            ItemRegistry.Create("(O)RedPandaBazaar_Prize_Ticket_1"));
                        RPBData.PrizeTicketRewardDecrement();
                        Game1.playSound("coin");
                    }
                    else
                    {
                        Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
                    }
                }
                else
                {
                    Game1.drawObjectDialogue("这里什么都没有，要不去给陈小茗供点货？");
                }

                return true;
            }
        );
    }

    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (Game1.dayOfMonth % 7 == 1 && Game1.player.IsMainPlayer)
        {
            RPB_SpecialOrderBoard.UpdateAvailableSpecialOrders(true);
            Tools.Log("Fresh RPB Special Orders");
        }
    }
}