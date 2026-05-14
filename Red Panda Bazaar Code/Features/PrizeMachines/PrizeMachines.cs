using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.PrizeMachines;

/// <summary>注册抽奖机的 Tile Action 并初始化奖池物品列表。</summary>
public static class PrizeMachines
{
    private const int CouponCount = 18;

    /// <summary>经典抽奖机普通物品池（36 种，等概率）。</summary>
    public static List<Tuple<string, int>> CommonPrizeList;

    /// <summary>经典抽奖机兑换券池（1~18 号各 1 张）。</summary>
    public static List<Tuple<string, int>> CouponPrizeList;

    /// <summary>Joja 抽奖机普通物品池（18 种，等概率）。</summary>
    public static List<Tuple<string, int>> JojaCommonPrizeList;

    /// <summary>Joja 抽奖机兑换券池（1~18 号各 2 张）。</summary>
    public static List<Tuple<string, int>> JojaCouponPrizeList;

    /// <summary>注册 Tile Action 并初始化奖池。</summary>
    public static void Init()
    {
        Tools.Log("PrizeMachine Initializing.");

        InitCustomMenus();
        InitPrizeList();

        Tools.Log("PrizeMachine Initialized.");
    }

    /// <summary>注册经典抽奖机和 Joja 抽奖机的 Tile Action。</summary>
    private static void InitCustomMenus()
    {
        GameLocation.RegisterTileAction("RedPandaBazaar_PrizeMachine_1", (_, _, _, _) =>
        {
            Game1.activeClickableMenu = new ClassicPrizeMachineMenu();
            return false;
        });
        GameLocation.RegisterTileAction("RedPandaBazaar_PrizeMachine_2", (_, _, _, _) =>
        {
            Game1.activeClickableMenu = new JojaPrizeMachineMenu();
            return false;
        });
    }

    /// <summary>初始化四组奖池数据。</summary>
    private static void InitPrizeList()
    {
        CommonPrizeList = new List<Tuple<string, int>>()
        {
            new("(BC)9", 2), // 避雷针
            new("(BC)21", 1), // 宝石复制机
            new("(BC)231", 1), // 太阳能板
            new("(BC)MushroomLog", 4), // 蘑菇树桩
            new("(BC)Dehydrator", 1), // 烘干机
            new("(BC)FishSmoker", 1), // 熏鱼机
            new("(O)446", 1), // 兔子的脚
            new("(O)695", 1), // 软木塞浮标
            new("(O)MixedFlowerSeeds", 15), // 混合花卉种子
            new("(O)72", 5), // 钻石
            new("(O)166", 1), // 宝藏箱
            new("(O)253", 5), // 浓缩咖啡
            new("(O)275", 4), // 宝藏盒
            new("(O)275", 5), // 宝藏盒
            new("(O)279", 1), // 魔法糖冰棍
            new("(O)286", 20), // 樱桃炸弹
            new("(O)287", 12), // 炸弹
            new("(O)287", 15), // 炸弹
            new("(O)288", 6), // 超级炸弹
            new("(O)288", 8), // 超级炸弹
            new("(O)337", 5), // 铱锭
            new("(O)621", 4), // 优质洒水器
            new("(O)630", 1), // 橘子树种
            new("(O)631", 1), // 桃子树种
            new("(O)632", 1), // 石榴树种
            new("(O)633", 1), // 苹果树种
            new("(O)645", 1), // 铱制洒水器
            new("(O)732", 5), // 蟹饼
            new("(O)749", 8), // 万象晶球
            new("(O)770", 10), // 混合种子
            new("(O)872", 1), // 仙尘
            new("(O)872", 2), // 仙尘
            new("(O)872", 3), // 仙尘
            new("(BC)10", 4), // 避雷针
            new("(BC)12", 4), // 蜂箱
            new("(BC)15", 4) // 小桶
        };
        CouponPrizeList = new List<Tuple<string, int>>();
        for (var i = 1; i <= CouponCount; i++)
        {
            CouponPrizeList.Add(new($"(O)RedPandaBazaar_Redemption_Coupon_{i}", 1));
        }

        JojaCommonPrizeList = new List<Tuple<string, int>>()
        {
            new("(O)MixedFlowerSeeds", 25), // 混合花卉种子
            new("(O)72", 5), // 钻石
            new("(O)166", 1), // 宝藏箱
            new("(O)253", 5), // 浓缩咖啡
            new("(O)279", 1), // 魔法糖冰棍
            new("(O)288", 8), // 超级炸弹
            new("(O)337", 5), // 铱锭
            new("(O)630", 2), // 橘子树种
            new("(O)631", 2), // 桃子树种
            new("(O)632", 2), // 石榴树种
            new("(O)633", 2), // 苹果树种
            new("(O)645", 1), // 铱制洒水器
            new("(O)749", 8), // 万象晶球
            new("(O)770", 10), // 混合种子
            new("(O)872", 3), // 仙尘
            new("(O)167", 10), // Joja可乐
            new("(O)390", 50), // 石头
            new("(O)388", 50), // 木头
        };
        JojaCouponPrizeList = new List<Tuple<string, int>>();
        for (var i = 1; i <= CouponCount; i++)
        {
            JojaCouponPrizeList.Add(new($"(O)RedPandaBazaar_Redemption_Coupon_{i}", 2));
        }
    }
}
