namespace Red_Panda_Bazaar_Code.Features.PlayerStall;

public class StallSaveData {
    /// <summary>各摊位的库存，key 为 ActionId。</summary>
    public Dictionary<string, List<StallItem>> Stock { get; set; } = new();

    /// <summary>已售出记录（未领取），结算后清空。</summary>
    public List<SoldRecord> SoldRecords { get; set; } = new();

    /// <summary>最近一次已领取的售出记录副本，收取后当天仍可查看账单明细。</summary>
    public List<SoldRecord>? LastSoldRecords { get; set; }

    public int TotalEarnings { get; set; }
    public int TotalTax { get; set; }
    public int CollectDay { get; set; } = -1;
}

/// <summary>摊位中的在售物品。</summary>
public class StallItem {
    public string Id { get; set; } = "";
    public string ActionId { get; set; } = "";
    public string ItemId { get; set; } = "";
    public int Amount { get; set; }
    public int Price { get; set; }
}

/// <summary>摊位配置（注册时设定）。</summary>
public class StallConfig {
    public string ActionId { get; set; } = "";
    public double BaseSellChance { get; set; } = 0.3;
}

/// <summary>夜间售出记录，供结算菜单展示。</summary>
public class SoldRecord {
    public string ActionId { get; set; } = "";
    public string ItemId { get; set; } = "";
    public int Amount { get; set; }
    public int Price { get; set; }
    public string SoldDate { get; set; } = "";
}
