using System.Xml.Serialization;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Utils;

/// <summary>用原版存档同款 XmlSerializer 序列化物品，完整保留物品状态。</summary>
public static class ItemSerializer
{
    private static readonly XmlSerializer Serializer = new(typeof(Item));

    /// <summary>预热序列化器，避免首次读档或上架时产生一次性开销。</summary>
    public static void WarmUp() { }

    /// <summary>序列化为 XML 快照；不支持的物品类型返回 null。</summary>
    public static string? Serialize(Item? item)
    {
        if (item == null)
            return null;

        try
        {
            using var writer = new StringWriter();
            Serializer.Serialize(writer, item);
            return writer.ToString();
        }
        catch (Exception e)
        {
            Tools.LogErr($"物品序列化失败（{item.QualifiedItemId}）：{e}");
            return null;
        }
    }

    /// <summary>从 XML 快照还原物品；快照损坏或为空时返回 null。</summary>
    public static Item? Deserialize(string? xml)
    {
        if (string.IsNullOrEmpty(xml))
            return null;

        try
        {
            using var reader = new StringReader(xml);
            return Serializer.Deserialize(reader) as Item;
        }
        catch (Exception e)
        {
            Tools.LogErr($"物品反序列化失败：{e}");
            return null;
        }
    }
}
