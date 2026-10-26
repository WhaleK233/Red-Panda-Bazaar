namespace Red_Panda_Bazaar_Code.Constant;

public static class ItemsKeys
{
    public static class Food
    {
        public const string Golden_Delight = "RedPandaBazaar_Golden_Delight";
        public const string Golden_Cupcake = "RedPandaBazaar_Golden_Cupcake";
        public const string Golden_Flavor_Popsicle = "RedPandaBazaar_Golden_Flavor_Popsicle";
        public const string Coffee_Popsicle = "RedPandaBazaar_Coffee_Popsicle";
        public const string Fern_Popsicle = "RedPandaBazaar_Fern_Popsicle";
        public const string Mango_Popsicle = "RedPandaBazaar_Mango_Popsicle";
        public const string Peach_Popsicle = "RedPandaBazaar_Peach_Popsicle";
        public const string Pumpkin_Popsicle = "RedPandaBazaar_Pumpkin_Popsicle";
        public const string Milk_Pudding = "RedPandaBazaar_Milk_Pudding";
    }

    public static class Tickets
    {
        public const string ClassicTicket = "RedPandaBazaar_Prize_Ticket_1";
        public const string GenericTicket = "PrizeTicket";
        public const string JojaTicket = "RedPandaBazaar_Prize_Ticket_4";

        /// <summary>JOJA 兑换券。兑奖机 10% 奖池。</summary>
        public const string JojaRedemptionCoupon = "(O)RedPandaBazaar_Redemption_Coupon_19";
    }

    public static class Furniture
    {
        public const string MarlinFishTank1 = "RedPandaBazaar_MarlinShop_FishTank1";
        public const string MarlinFishTank2 = "RedPandaBazaar_MarlinShop_FishTank2";
    }

    public static class Machines
    {
        public const string StatuePlants1 = "RedPandaBazaar_StatuePlants1";
        public const string StatuePlants2 = "RedPandaBazaar_StatuePlants2";
        public const string StatuePlants3 = "RedPandaBazaar_StatuePlants3";
    }

    public static class Hats
    {
        public const string GamblerHat = "RedPandaBazaar_GamblerHat";
    }

    /// <summary>
    /// 原版游戏物品的限定 ID（含类型前缀），用于兑奖池与展示鱼缸等需要引用原版物品的场合。
    /// 集中在此便于核对，避免在业务代码里散落裸字符串。
    /// </summary>
    public static class Vanilla
    {
        /// <summary>远古种子。1% 头奖。</summary>
        public const string AncientSeed = "(O)114";

        /// <summary>五彩碎片。2% 次奖。</summary>
        public const string PrismaticShard = "(O)74";

        /// <summary>星之果茶。10% 次奖。</summary>
        public const string StardropTea = "(O)StardropTea";

        /// <summary>自动抚摸机。1% 头奖。</summary>
        public const string AutoPetter = "(BC)272";

        /// <summary>鲶鱼。马林鱼缸的展示鱼之一。</summary>
        public const string Catfish = "(O)143";

        /// <summary>太阳鱼。马林鱼缸的展示鱼之一。</summary>
        public const string Sunfish = "(O)145";
    }
}
