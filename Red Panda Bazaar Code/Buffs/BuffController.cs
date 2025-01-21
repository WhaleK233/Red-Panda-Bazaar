using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buffs;

namespace Red_Panda_Bazaar_Code.Buffs;

public static class BuffController
{
    public static Dictionary<string, Buff> buffDict = new Dictionary<string, Buff>();
    private static bool Enabled { get; set; } = false;

    /// <summary>启用自定义Buff</summary>
    public static void Init()
    {
        // 如果未启用
        if (!Enabled)
        {
            InitCustomBuffs();

            Tools.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

            Enabled = true;
            Tools.Log("Custom Buffs Enabled");
        }
    }

    private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Context.IsWorldReady && Context.IsGameLaunched)
        {
            if (Game1.player.hat?.Get()?.Name == Constants.NameKeys.Hat.GamblerHat)
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
    }

    private static void InitCustomBuffs()
    {
        int index = 0;
        buffDict[Constants.NameKeys.Food.Golden_Delight] = new Buff(
            id: "RedPandaBazaar_ExquisitelyStuffed",
            displaySource: Tools.I18n.Get(I18nKeys.Display_RedPandaBazaar_Golden_Delight_BuffDisplaySource),
            displayName: Tools.I18n.Get(I18nKeys.Display_RedPandaBazaar_Golden_Delight_BuffDisplayName),
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

        buffDict[Constants.NameKeys.Food.Golden_Cupcake] = new Buff(
            id: "RedPandaBazaar_Golden_Cupcake",
            displaySource: Tools.I18n.Get(I18nKeys.Display_RedPandaBazaar_Golden_Cupcake_BuffDisplaySource).ToString(),
            displayName: Tools.I18n.Get(I18nKeys.Display_RedPandaBazaar_Golden_Cupcake_BuffDisplayName).ToString(),
            iconTexture: Tools.Helper.ModContent.Load<Texture2D>("assets/RedPandaBazaar_Buffs.png"),
            iconSheetIndex: index++,
            duration: 240000,
            effects: new BuffEffects()
            {
                LuckLevel = { 6 },
            }
        );
        buffDict[Constants.NameKeys.Food.Golden_Flavor_Popsicle] = new Buff(
            id: "RedPandaBazaar_Golden_Flavor_Popsicle",
            displaySource: Tools.I18n.Get(I18nKeys.Display_RedPandaBazaar_Golden_Flavor_Popsicle_BuffDisplaySource).ToString(),
            displayName: Tools.I18n.Get(I18nKeys.Display_RedPandaBazaar_Golden_Flavor_Popsicle_BuffDisplayName).ToString(),
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
        buffDict[Constants.NameKeys.Food.Coffee_Popsicle] = new Buff(
            id: "RedPandaBazaar_Coffee_Popsicle",
            displaySource: Tools.I18n.Get(I18nKeys.Display_RedPandaBazaar_Coffee_Popsicle_BuffDisplaySource).ToString(),
            displayName: Tools.I18n.Get(I18nKeys.Display_RedPandaBazaar_Coffee_Popsicle_BuffDisplayName).ToString(),
            iconTexture: Tools.Helper.ModContent.Load<Texture2D>("assets/RedPandaBazaar_Buffs.png"),
            iconSheetIndex: index++,
            duration: 300000,
            effects: new BuffEffects()
            {
                Speed = { 1 }
            }
        );
        buffDict[Constants.NameKeys.Food.Fern_Popsicle] = new Buff(
            id: "RedPandaBazaar_Fern_Popsicle",
            displaySource: Tools.I18n.Get(I18nKeys.Display_RedPandaBazaar_Fern_Popsicle_BuffDisplaySource).ToString(),
            displayName: Tools.I18n.Get(I18nKeys.Display_RedPandaBazaar_Fern_Popsicle_BuffDisplayName).ToString(),
            iconTexture: Tools.Helper.ModContent.Load<Texture2D>("assets/RedPandaBazaar_Buffs.png"),
            iconSheetIndex: index++,
            duration: 300000,
            effects: new BuffEffects()
            {
                FarmingLevel = { 2 },
                ForagingLevel = { 5 }
            }
        );
        buffDict[Constants.NameKeys.Food.Mango_Popsicle] = new Buff(
            id: "RedPandaBazaar_Mango_Popsicle",
            displaySource: Tools.I18n.Get(I18nKeys.Display_RedPandaBazaar_Mango_Popsicle_BuffDisplaySource).ToString(),
            displayName: Tools.I18n.Get(I18nKeys.Display_RedPandaBazaar_Mango_Popsicle_BuffDisplayName).ToString(),
            iconTexture: Tools.Helper.ModContent.Load<Texture2D>("assets/RedPandaBazaar_Buffs.png"),
            iconSheetIndex: index++,
            duration: 300000,
            effects: new BuffEffects()
            {
                ForagingLevel = { 2 }
            }
        );
        buffDict[Constants.NameKeys.Food.Peach_Popsicle] = new Buff(
            id: "RedPandaBazaar_Peach_Popsicle",
            displaySource: Tools.I18n.Get(I18nKeys.Display_RedPandaBazaar_Peach_Popsicle_BuffDisplaySource).ToString(),
            displayName: Tools.I18n.Get(I18nKeys.Display_RedPandaBazaar_Peach_Popsicle_BuffDisplayName).ToString(),
            iconTexture: Tools.Helper.ModContent.Load<Texture2D>("assets/RedPandaBazaar_Buffs.png"),
            iconSheetIndex: index++,
            duration: 300000,
            effects: new BuffEffects()
            {
                MiningLevel = { 1 },
                Defense = { 1 }
            }
        );
        buffDict[Constants.NameKeys.Food.Pumpkin_Popsicle] = new Buff(
            id: "RedPandaBazaar_Pumpkin_Popsicle",
            displaySource: Tools.I18n.Get(I18nKeys.Display_RedPandaBazaar_Pumpkin_Popsicle_BuffDisplaySource).ToString(),
            displayName: Tools.I18n.Get(I18nKeys.Display_RedPandaBazaar_Pumpkin_Popsicle_BuffDisplayName).ToString(),
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