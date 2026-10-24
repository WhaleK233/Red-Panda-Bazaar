using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.Buffs;

namespace Red_Panda_Bazaar_Code.Features.Buffs;

/// <summary>管理自定义食物 Buff。赌徒帽的加速走装备效果，见 <see cref="BuffsPatch"/>。</summary>
public static class Buffs
{
    /// <summary>食物 Buff 工厂表：键为食物 ItemId，值按缓存参数新建 Buff 实例。
    /// Buff 的计时状态（millisecondsDuration）随帧递减、归零即被管理器丢弃，
    /// 复用同一实例会让再次食用拿到已耗尽的计时器而不生效，必须每次调用都新建。</summary>
    public static readonly Dictionary<string, Func<Buff>> BuffFactories = new();
    private static Texture2D? _buffIconTexture;

    /// <summary>启用自定义 Buff。</summary>
    public static void Init()
    {
        Tools.Log("Buffs Initializing.");

        InitCustomBuffs();

        Tools.Log("Buffs Initialized.");
    }

    private static void InitCustomBuffs()
    {
        _buffIconTexture = ModInfo.Helper.ModContent.Load<Texture2D>("assets/Textures/RedPandaBazaar_Buffs.png");

        // iconIndex 对应 RedPandaBazaar_Buffs.png 中的图标序号，与注册顺序一一对应
        BuffFactories[ItemsKeys.Food.Golden_Delight] = () => CreateBuff(
            id: "RedPandaBazaar_ExquisitelyStuffed",
            displayNameKey: I18nKeys.Display_RedPandaBazaar_Golden_Delight_BuffDisplayName,
            displaySourceKey: I18nKeys.Display_RedPandaBazaar_Golden_Delight_BuffDisplaySource,
            iconIndex: 0, duration: Buff.ENDLESS,
            effects: new BuffEffects
            {
                Speed = { 2 }, Defense = { 4 }, Attack = { 10 },
                LuckLevel = { 3 }, CriticalChanceMultiplier = { 4 },
                FishingLevel = { 6 }, ForagingLevel = { 4 }, FarmingLevel = { 4 }
            });

        BuffFactories[ItemsKeys.Food.Golden_Cupcake] = () => CreateBuff(
            id: "RedPandaBazaar_Golden_Cupcake",
            displayNameKey: I18nKeys.Display_RedPandaBazaar_Golden_Cupcake_BuffDisplayName,
            displaySourceKey: I18nKeys.Display_RedPandaBazaar_Golden_Cupcake_BuffDisplaySource,
            iconIndex: 1, duration: 240000,
            effects: new BuffEffects { LuckLevel = { 6 } });

        BuffFactories[ItemsKeys.Food.Golden_Flavor_Popsicle] = () => CreateBuff(
            id: "RedPandaBazaar_Golden_Flavor_Popsicle",
            displayNameKey: I18nKeys.Display_RedPandaBazaar_Golden_Flavor_Popsicle_BuffDisplayName,
            displaySourceKey: I18nKeys.Display_RedPandaBazaar_Golden_Flavor_Popsicle_BuffDisplaySource,
            iconIndex: 2, duration: 300000,
            effects: new BuffEffects
            {
                LuckLevel = { 5 }, ForagingLevel = { 5 },
                MagneticRadius = { 50 }, Attack = { 3 }
            });

        BuffFactories[ItemsKeys.Food.Coffee_Popsicle] = () => CreateBuff(
            id: "RedPandaBazaar_Coffee_Popsicle",
            displayNameKey: I18nKeys.Display_RedPandaBazaar_Coffee_Popsicle_BuffDisplayName,
            displaySourceKey: I18nKeys.Display_RedPandaBazaar_Coffee_Popsicle_BuffDisplaySource,
            iconIndex: 3, duration: 300000,
            effects: new BuffEffects { Speed = { 1 } });

        BuffFactories[ItemsKeys.Food.Fern_Popsicle] = () => CreateBuff(
            id: "RedPandaBazaar_Fern_Popsicle",
            displayNameKey: I18nKeys.Display_RedPandaBazaar_Fern_Popsicle_BuffDisplayName,
            displaySourceKey: I18nKeys.Display_RedPandaBazaar_Fern_Popsicle_BuffDisplaySource,
            iconIndex: 4, duration: 300000,
            effects: new BuffEffects { FarmingLevel = { 2 }, ForagingLevel = { 5 } });

        BuffFactories[ItemsKeys.Food.Mango_Popsicle] = () => CreateBuff(
            id: "RedPandaBazaar_Mango_Popsicle",
            displayNameKey: I18nKeys.Display_RedPandaBazaar_Mango_Popsicle_BuffDisplayName,
            displaySourceKey: I18nKeys.Display_RedPandaBazaar_Mango_Popsicle_BuffDisplaySource,
            iconIndex: 5, duration: 300000,
            effects: new BuffEffects { ForagingLevel = { 2 } });

        BuffFactories[ItemsKeys.Food.Peach_Popsicle] = () => CreateBuff(
            id: "RedPandaBazaar_Peach_Popsicle",
            displayNameKey: I18nKeys.Display_RedPandaBazaar_Peach_Popsicle_BuffDisplayName,
            displaySourceKey: I18nKeys.Display_RedPandaBazaar_Peach_Popsicle_BuffDisplaySource,
            iconIndex: 6, duration: 300000,
            effects: new BuffEffects { MiningLevel = { 1 }, Defense = { 1 } });

        BuffFactories[ItemsKeys.Food.Pumpkin_Popsicle] = () => CreateBuff(
            id: "RedPandaBazaar_Pumpkin_Popsicle",
            displayNameKey: I18nKeys.Display_RedPandaBazaar_Pumpkin_Popsicle_BuffDisplayName,
            displaySourceKey: I18nKeys.Display_RedPandaBazaar_Pumpkin_Popsicle_BuffDisplaySource,
            iconIndex: 7, duration: 300000,
            effects: new BuffEffects { FishingLevel = { 2 } });
    }

    /// <summary>创建 Buff 实例的工厂方法。</summary>
    private static Buff CreateBuff(string id, string displayNameKey, string displaySourceKey,
        int iconIndex, int duration, BuffEffects effects)
    {
        return new Buff(
            id: id,
            displaySource: Tools.GetI18n(displaySourceKey).ToString(),
            displayName: Tools.GetI18n(displayNameKey).ToString(),
            iconTexture: _buffIconTexture,
            iconSheetIndex: iconIndex,
            duration: duration,
            effects: effects
        );
    }
}
