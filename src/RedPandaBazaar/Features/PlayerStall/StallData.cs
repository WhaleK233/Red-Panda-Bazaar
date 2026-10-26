using Red_Panda_Bazaar_Code.Utils;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.PlayerStall;

public class StallSaveData
{
    /// <summary>各摊位的库存，key 为 ActionId。</summary>
    public Dictionary<string, List<StallItem>> Stock { get; set; } = new();

    /// <summary>已售出记录（未领取），结算后清空。</summary>
    public List<SoldRecord> SoldRecords { get; set; } = new();

    /// <summary>最近一次已领取的售出记录副本，收取后当天仍可查看账单明细。</summary>
    public List<SoldRecord>? LastSoldRecords { get; set; }

    public int CollectDay { get; set; } = -1;
}

/// <summary>
/// 以原版 XmlSerializer 快照持有物品，完整保留品质等全部状态。
/// 数量不单独存字段，一律以快照的 <see cref="Item.Stack"/> 为准，避免两份状态不一致。
/// </summary>
public abstract class ItemSnapshot
{
    /// <summary>物品的 XML 快照，唯一参与序列化的成员。</summary>
    public string ItemXml { get; set; } = "";

    private Item? _item;
    private bool _resolved;

    /// <summary>
    /// 还原出的物品，原地缓存。快照为空或损坏时为 null。
    /// 用方法而非属性，避免被 SMAPI 的 JSON 序列化器当成数据成员写回存档。
    /// </summary>
    public Item? GetItem()
    {
        if (!_resolved)
        {
            _item = ItemSerializer.Deserialize(ItemXml);
            _resolved = true;
        }
        return _item;
    }

    /// <summary>就地修改 <see cref="GetItem"/> 返回的物品后调用，把变更同步回 XML 快照。</summary>
    public void RefreshSnapshot()
    {
        if (_item != null)
            ItemXml = ItemSerializer.Serialize(_item) ?? ItemXml;
    }

    /// <summary>快照里记录的权威数量。部分取回路径中原版会原地改小缓存实例，
    /// 数量运算必须以快照为基准，否则会把已扣除的部分再扣一次。</summary>
    public int SnapshotStack()
    {
        return ItemSerializer.Deserialize(ItemXml)?.Stack ?? _item?.Stack ?? 0;
    }
}

/// <summary>摊位中的在售物品。</summary>
public class StallItem : ItemSnapshot
{
    public string Id { get; set; } = "";
    public string ActionId { get; set; } = "";
    public int Price { get; set; }
}

/// <summary>夜间售出记录，供结算菜单展示。快照的 Stack 即该次售出数量。</summary>
public class SoldRecord : ItemSnapshot
{
    public string ActionId { get; set; } = "";
    public int Price { get; set; }
    public string SoldDate { get; set; } = "";
}

/// <summary>摊位配置（注册时设定）。</summary>
public class StallConfig
{
    public string ActionId { get; set; } = "";
    public double BaseSellChance { get; set; } = 0.3;
}
