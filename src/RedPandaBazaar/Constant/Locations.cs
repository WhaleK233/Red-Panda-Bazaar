namespace Red_Panda_Bazaar_Code.Constant;

/// <summary>
/// 本 mod 依赖的自定义地图名。这些名称由 CP 内容包（content/[CP]Red Panda Bazaar/）
/// 通过 CustomLocations 注册，C# 侧只读取，改名必须两侧同步。
/// </summary>
public static class Locations
{
    /// <summary>小熊猫集市主区域。</summary>
    public const string Bazaar = "Custom_RedPandaBazaar";

    /// <summary>集市西侧扩展区域。</summary>
    public const string BazaarWest = "Custom_BazaarWest";

    /// <summary>集市湖泊。</summary>
    public const string RedPandaLake = "Custom_RedPandaLake";

    /// <summary>集市西侧的桥。</summary>
    public const string RedPandaBridge = "Custom_RedPandaBridge";

    /// <summary>镜湖。</summary>
    public const string MirrorLake = "Custom_MirrorLake";

    /// <summary>枫木桥。共享单车解锁点与中央车站集成都依赖此地图。</summary>
    public const string MapleBridge = "Custom_MapleBridge";

    /// <summary>李清风的庭院。</summary>
    public const string LiQingFengCourtyard = "Custom_LiQingFengCourtyard";

    /// <summary>集市银行。</summary>
    public const string Bank = "Custom_RedPandaBazaarBank1";

    /// <summary>农夫商店。玩家摊位所在的室内地图。</summary>
    public const string FarmerShop = "Custom_FarmerShop1";

    /// <summary>陈记商店。</summary>
    public const string ChenQiShop = "Custom_ChenQiShop1";

    /// <summary>马林商店。展示鱼缸所在的室内地图。</summary>
    public const string MarlinShop = "Custom_MarlinShop1";
}
