using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.Buffs;

namespace Red_Panda_Bazaar_Code.Features.Buffs;

/// <summary>管理自定义食物 Buff。赌徒帽的加速走装备效果，见 <see cref="BuffsPatch"/>。</summary>
public static class Buffs
{
    public static readonly Dictionary<string, Buff> BuffDict = new();
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
        _buffIconTexture = Tools.Helper.ModContent.Load<Texture2D>("assets/RedPandaBazaar_Buffs.png");

        var index = 0;
        BuffDict[ItemsKeys.Food.Golden_Delight] = CreateBuff(
            id: "RedPandaBazaar_ExquisitelyStuffed",
            displayNameKey: I18nKeys.Display_RedPandaBazaar_Golden_Delight_BuffDisplayName,
            displaySourceKey: I18nKeys.Display_RedPandaBazaar_Golden_Delight_BuffDisplaySource,
            iconIndex: index++, duration: Buff.ENDLESS,
            effects: new BuffEffects
            {
                Speed = { 2 }, Defense = { 4 }, Attack = { 10 },
                LuckLevel = { 3 }, CriticalChanceMultiplier = { 4 },
                FishingLevel = { 6 }, ForagingLevel = { 4 }, FarmingLevel = { 4 }
            });

        BuffDict[ItemsKeys.Food.Golden_Cupcake] = CreateBuff(
            id: "RedPandaBazaar_Golden_Cupcake",
            displayNameKey: I18nKeys.Display_RedPandaBazaar_Golden_Cupcake_BuffDisplayName,
            displaySourceKey: I18nKeys.Display_RedPandaBazaar_Golden_Cupcake_BuffDisplaySource,
            iconIndex: index++, duration: 240000,
            effects: new BuffEffects { LuckLevel = { 6 } });

        BuffDict[ItemsKeys.Food.Golden_Flavor_Popsicle] = CreateBuff(
            id: "RedPandaBazaar_Golden_Flavor_Popsicle",
            displayNameKey: I18nKeys.Display_RedPandaBazaar_Golden_Flavor_Popsicle_BuffDisplayName,
            displaySourceKey: I18nKeys.Display_RedPandaBazaar_Golden_Flavor_Popsicle_BuffDisplaySource,
            iconIndex: index++, duration: 300000,
            effects: new BuffEffects
            {
                LuckLevel = { 5 }, ForagingLevel = { 5 },
                MagneticRadius = { 50 }, Attack = { 3 }
            });

        BuffDict[ItemsKeys.Food.Coffee_Popsicle] = CreateBuff(
            id: "RedPandaBazaar_Coffee_Popsicle",
            displayNameKey: I18nKeys.Display_RedPandaBazaar_Coffee_Popsicle_BuffDisplayName,
            displaySourceKey: I18nKeys.Display_RedPandaBazaar_Coffee_Popsicle_BuffDisplaySource,
            iconIndex: index++, duration: 300000,
            effects: new BuffEffects { Speed = { 1 } });

        BuffDict[ItemsKeys.Food.Fern_Popsicle] = CreateBuff(
            id: "RedPandaBazaar_Fern_Popsicle",
            displayNameKey: I18nKeys.Display_RedPandaBazaar_Fern_Popsicle_BuffDisplayName,
            displaySourceKey: I18nKeys.Display_RedPandaBazaar_Fern_Popsicle_BuffDisplaySource,
            iconIndex: index++, duration: 300000,
            effects: new BuffEffects { FarmingLevel = { 2 }, ForagingLevel = { 5 } });

        BuffDict[ItemsKeys.Food.Mango_Popsicle] = CreateBuff(
            id: "RedPandaBazaar_Mango_Popsicle",
            displayNameKey: I18nKeys.Display_RedPandaBazaar_Mango_Popsicle_BuffDisplayName,
            displaySourceKey: I18nKeys.Display_RedPandaBazaar_Mango_Popsicle_BuffDisplaySource,
            iconIndex: index++, duration: 300000,
            effects: new BuffEffects { ForagingLevel = { 2 } });

        BuffDict[ItemsKeys.Food.Peach_Popsicle] = CreateBuff(
            id: "RedPandaBazaar_Peach_Popsicle",
            displayNameKey: I18nKeys.Display_RedPandaBazaar_Peach_Popsicle_BuffDisplayName,
            displaySourceKey: I18nKeys.Display_RedPandaBazaar_Peach_Popsicle_BuffDisplaySource,
            iconIndex: index++, duration: 300000,
            effects: new BuffEffects { MiningLevel = { 1 }, Defense = { 1 } });

        BuffDict[ItemsKeys.Food.Pumpkin_Popsicle] = CreateBuff(
            id: "RedPandaBazaar_Pumpkin_Popsicle",
            displayNameKey: I18nKeys.Display_RedPandaBazaar_Pumpkin_Popsicle_BuffDisplayName,
            displaySourceKey: I18nKeys.Display_RedPandaBazaar_Pumpkin_Popsicle_BuffDisplaySource,
            iconIndex: index++, duration: 300000,
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
