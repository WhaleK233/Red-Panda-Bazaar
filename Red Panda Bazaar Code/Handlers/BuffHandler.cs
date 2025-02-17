using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buffs;

namespace Red_Panda_Bazaar_Code.Handlers;

public static class BuffHandler
{
    public static readonly Dictionary<string, Buff> BuffDict = new();

    /// <summary>启用自定义Buff</summary>
    public static void Init()
    {
        Tools.Log("Buffs Initializing.");
        
        Tools.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        Tools.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

        Tools.Log("Buffs Initialized.");
    }

    private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        InitCustomBuffs();
    }

    private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsGameLaunched) return;

        if (Game1.player.hat?.Get()?.Name == ItemsKeys.Hats.GamblerHat)
        {
            Game1.player.applyBuff(new Buff(
                id: "speed",
                duration: 100,
                effects: new BuffEffects()
                {
                    Speed = { 1 }
                }
            ));
        }
    }

    private static void InitCustomBuffs()
    {
        var index = 0;
        BuffDict[ItemsKeys.Food.Golden_Delight] = new Buff(
            id: "RedPandaBazaar_ExquisitelyStuffed",
            displaySource: Tools.GetI18n(I18nKeys.Display_RedPandaBazaar_Golden_Delight_BuffDisplaySource),
            displayName: Tools.GetI18n(I18nKeys.Display_RedPandaBazaar_Golden_Delight_BuffDisplayName),
            iconTexture: Tools.Helper.ModContent.Load<Texture2D>("assets/RedPandaBazaar_Buffs.png"),
            iconSheetIndex: index++,
            duration: Buff.ENDLESS,
            effects: new BuffEffects()
            {
                Speed = { 2 },
                Defense = { 4 },
                Attack = { 10 },
                LuckLevel = { 3 },
                CriticalChanceMultiplier = { 4 },
                FishingLevel = { 6 },
                ForagingLevel = { 4 },
                FarmingLevel = { 4 }
            }
        );

        BuffDict[ItemsKeys.Food.Golden_Cupcake] = new Buff(
            id: "RedPandaBazaar_Golden_Cupcake",
            displaySource: Tools.GetI18n(I18nKeys.Display_RedPandaBazaar_Golden_Cupcake_BuffDisplaySource).ToString(),
            displayName: Tools.GetI18n(I18nKeys.Display_RedPandaBazaar_Golden_Cupcake_BuffDisplayName).ToString(),
            iconTexture: Tools.Helper.ModContent.Load<Texture2D>("assets/RedPandaBazaar_Buffs.png"),
            iconSheetIndex: index++,
            duration: 240000,
            effects: new BuffEffects()
            {
                LuckLevel = { 6 },
            }
        );
        BuffDict[ItemsKeys.Food.Golden_Flavor_Popsicle] = new Buff(
            id: "RedPandaBazaar_Golden_Flavor_Popsicle",
            displaySource: Tools.GetI18n(I18nKeys.Display_RedPandaBazaar_Golden_Flavor_Popsicle_BuffDisplaySource)
                .ToString(),
            displayName: Tools.GetI18n(I18nKeys.Display_RedPandaBazaar_Golden_Flavor_Popsicle_BuffDisplayName)
                .ToString(),
            iconTexture: Tools.Helper.ModContent.Load<Texture2D>("assets/RedPandaBazaar_Buffs.png"),
            iconSheetIndex: index++,
            duration: 300000,
            effects: new BuffEffects()
            {
                LuckLevel = { 5 },
                ForagingLevel = { 5 },
                MagneticRadius = { 50 },
                Attack = { 3 }
            }
        );
        BuffDict[ItemsKeys.Food.Coffee_Popsicle] = new Buff(
            id: "RedPandaBazaar_Coffee_Popsicle",
            displaySource: Tools.GetI18n(I18nKeys.Display_RedPandaBazaar_Coffee_Popsicle_BuffDisplaySource).ToString(),
            displayName: Tools.GetI18n(I18nKeys.Display_RedPandaBazaar_Coffee_Popsicle_BuffDisplayName).ToString(),
            iconTexture: Tools.Helper.ModContent.Load<Texture2D>("assets/RedPandaBazaar_Buffs.png"),
            iconSheetIndex: index++,
            duration: 300000,
            effects: new BuffEffects()
            {
                Speed = { 1 }
            }
        );
        BuffDict[ItemsKeys.Food.Fern_Popsicle] = new Buff(
            id: "RedPandaBazaar_Fern_Popsicle",
            displaySource: Tools.GetI18n(I18nKeys.Display_RedPandaBazaar_Fern_Popsicle_BuffDisplaySource).ToString(),
            displayName: Tools.GetI18n(I18nKeys.Display_RedPandaBazaar_Fern_Popsicle_BuffDisplayName).ToString(),
            iconTexture: Tools.Helper.ModContent.Load<Texture2D>("assets/RedPandaBazaar_Buffs.png"),
            iconSheetIndex: index++,
            duration: 300000,
            effects: new BuffEffects()
            {
                FarmingLevel = { 2 },
                ForagingLevel = { 5 }
            }
        );
        BuffDict[ItemsKeys.Food.Mango_Popsicle] = new Buff(
            id: "RedPandaBazaar_Mango_Popsicle",
            displaySource: Tools.GetI18n(I18nKeys.Display_RedPandaBazaar_Mango_Popsicle_BuffDisplaySource).ToString(),
            displayName: Tools.GetI18n(I18nKeys.Display_RedPandaBazaar_Mango_Popsicle_BuffDisplayName).ToString(),
            iconTexture: Tools.Helper.ModContent.Load<Texture2D>("assets/RedPandaBazaar_Buffs.png"),
            iconSheetIndex: index++,
            duration: 300000,
            effects: new BuffEffects()
            {
                ForagingLevel = { 2 }
            }
        );
        BuffDict[ItemsKeys.Food.Peach_Popsicle] = new Buff(
            id: "RedPandaBazaar_Peach_Popsicle",
            displaySource: Tools.GetI18n(I18nKeys.Display_RedPandaBazaar_Peach_Popsicle_BuffDisplaySource).ToString(),
            displayName: Tools.GetI18n(I18nKeys.Display_RedPandaBazaar_Peach_Popsicle_BuffDisplayName).ToString(),
            iconTexture: Tools.Helper.ModContent.Load<Texture2D>("assets/RedPandaBazaar_Buffs.png"),
            iconSheetIndex: index++,
            duration: 300000,
            effects: new BuffEffects()
            {
                MiningLevel = { 1 },
                Defense = { 1 }
            }
        );
        BuffDict[ItemsKeys.Food.Pumpkin_Popsicle] = new Buff(
            id: "RedPandaBazaar_Pumpkin_Popsicle",
            displaySource: Tools.GetI18n(I18nKeys.Display_RedPandaBazaar_Pumpkin_Popsicle_BuffDisplaySource)
                .ToString(),
            displayName: Tools.GetI18n(I18nKeys.Display_RedPandaBazaar_Pumpkin_Popsicle_BuffDisplayName).ToString(),
            iconTexture: Tools.Helper.ModContent.Load<Texture2D>("assets/RedPandaBazaar_Buffs.png"),
            iconSheetIndex: index++,
            duration: 300000,
            effects: new BuffEffects()
            {
                FishingLevel = { 2 }
            }
        );
    }
}