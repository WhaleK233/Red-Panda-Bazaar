using System.Xml.Serialization;
using StardewModdingAPI;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Utils;

/// <summary>
/// 用原版存档同款 XmlSerializer 序列化物品，完整保留品质、加工来源、染色、orderData、modData 等全部状态。
/// </summary>
/// <remarks>
/// 与 SaveGame.farmerSerializer 保持一致：Item / Object / Tool 三层 [XmlInclude] 已覆盖全部物品派生类，
/// quality、stack、preservedParentSheetIndex 等均为 [XmlElement] 公共字段，可完整往返。
/// </remarks>
public static class ItemSerializer {
    // XmlSerializer 构造时会动态生成程序集，开销大，全程只建一次
    private static readonly XmlSerializer Serializer = new(typeof(Item));

    /// <summary>预热：提前完成 serializer 构造，避免首次读档或上架时的一次性卡顿。</summary>
    public static void WarmUp() { }

    /// <summary>序列化为 XML 快照。失败返回 null（如 XmlInclude 未覆盖的物品类型）。</summary>
    public static string? Serialize(Item? item) {
        if (item == null) return null;

        try {
            using var writer = new StringWriter();
            Serializer.Serialize(writer, item);
            return writer.ToString();
        } catch (Exception e) {
            Tools.Log($"物品序列化失败（{item.QualifiedItemId}）：{e}", LogLevel.Error);
            return null;
        }
    }

    /// <summary>从 XML 快照还原物品。快照损坏或为空时返回 null。</summary>
    public static Item? Deserialize(string? xml) {
        if (string.IsNullOrEmpty(xml)) return null;

        try {
            using var reader = new StringReader(xml);
            return Serializer.Deserialize(reader) as Item;
        } catch (Exception e) {
            Tools.Log($"物品反序列化失败：{e}", LogLevel.Error);
            return null;
        }
    }
}
