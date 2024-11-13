using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Menus;

public static class MenuController
{
    public static List<Tuple<string, int>> CommonPrizeList;
    public static List<Tuple<string, int>> CouponPrizeList;
    private static bool Enabled { get; set; } = false;

    /// <summary>启用自定义菜单</summary>
    public static void Init()
    {
        // 如果未启用
        if (!Enabled)
        {
            InitCustomMenus();
            InitPrizeList();

            Enabled = true;
            Tools.Log("Custom Menus Enabled");
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
        GameLocation.RegisterTileAction("RedPandaBazaar_SpecialOrdersBoard", (location, strings, arg3, arg4) =>
            {
                Game1.activeClickableMenu = (IClickableMenu)new RPB_SpecialOrderBoard("RPB");
                return false;
            }
        );
    }

    private static void InitPrizeList()
    {
        Random uniqueRandom =
            Utility.CreateRandom((double)Game1.uniqueIDForThisGame, (double)Game1.player.UniqueMultiplayerID);
        CouponPrizeList = new List<Tuple<string, int>>();
        for (int i = 1; i <= 17; i++)
        {
            CouponPrizeList.Add(new($"(O)RedPandaBazaar_Redemption_Coupon_{i}", 1));
        }

        CommonPrizeList = new List<Tuple<string, int>>()
        {
            // 避雷针
            new("(BC)9", 2),
            // 宝石复制机
            new("(BC)21", 1),
            // 太阳能板
            new("(BC)231", 1),
            // 蘑菇树桩
            new("(BC)MushroomLog", 4),
            // 混合花卉种子
            new("(O)MixedFlowerSeeds", 15),
            // 熏鱼机
            new("(BC)FishSmoker", 1),
            // 兔子的脚
            new("(O)446", 1),
            // 软木塞浮标
            new("(O)695", 1),
            // 其他
            new("(O)Book_Friendship", 1),
            new("(O)631", 1),
            new("(O)630", 1),
            new("(O)770", 10),
            new("(O)621", 4),
            new("(BC)15", 4),
            new("(O)633", 1),
            new("(O)632", 1),
            new("(O)286", 20),
            new("(O)287", 12),
            new("(O)288", 6),
            new("(BC)Dehydrator", 1),
            new("(O)275", 4),
            new("(O)872", 2),
            new("(F)FancyHousePlant1", 1),
            new("(F)FancyHousePlant2", 1),
            new("(F)FancyHousePlant3", 1),
            new("(O)SkillBook_" + uniqueRandom.Next(5), 1),
            new("(F)CowDecal", 1),
            new("(O)749", 8),
            new("(BC)10", 4),
            new("(BC)12", 4),
            new("(O)72", 5),
            new("(O)337", 5),
            new("(O)226", 5),
            new("(O)253", 5),
            new("(O)732", 5),
            new("(O)279", 1),
            new("(O)872", 1),
            new("(F)FancyHousePlant1", 1),
            new("(F)FancyHousePlant2", 1),
            new("(F)FancyHousePlant3", 1),
            new("(O)275", 5),
            new("(O)166", 1),
            new("(O)645", 1),
            new("(F)FancyTree1", 1),
            new("(F)FancyTree2", 1),
            new("(F)FancyTree3", 1),
            new("(F)PigPainting", 1),
            new("(O)287", 15),
            new("(O)872", 3),
            new("(O)288", 8),
            new(Game1.player.HouseUpgradeLevel > 0 ? "(F)BluePinstripeDoubleBed" : "(F)BluePinstripeBed", 1),
        };
    }
}