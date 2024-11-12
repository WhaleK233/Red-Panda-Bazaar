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
            if (Game1.player.hat?.Get()?.Name == Hat.GamblerHat)
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
        buffDict[Food.Golden_Delight] = new Buff(
            id: "RedPandaBazaar_ExquisitelyStuffed",
            iconTexture: Tools.Helper.ModContent.Load<Texture2D>("assets/RedPandaBazaar_Buffs.png"),
            iconSheetIndex: 0,
            duration: Buff.ENDLESS,
            effects: new BuffEffects()
            {
                Speed = { 2 },
                Defense = { 4 },
                Attack = { 10 },
                CriticalChanceMultiplier = { 4 },
                FishingLevel = { 6 },
                ForagingLevel = { 4 },
                FarmingLevel = { 4 }
            }
        );
    }

    public static class Food
    {
        public const string Golden_Delight = "RedPandaBazaar_Golden_Delight";
    }

    public static class Hat
    {
        public const string GamblerHat = "RedPandaBazaar_GamblerHat";
    }
}