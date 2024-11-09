using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.Buffs;

namespace Red_Panda_Bazaar_Code.Buffs;

public static class RPB_BuffManager
{
    private static bool Enabled { get; set; } = false;

    public static Dictionary<string, Buff> buffDict = new Dictionary<string, Buff>();

    /// <summary>启用自定义Buff</summary>
    public static void Enable()
    {
        // 如果未启用
        if (!Enabled)
        {
            InitCustomBuffs();

            Enabled = true;
            Tools.Monitor.Log("Custom Buffs Enabled");
        }
    }

    private static void InitCustomBuffs()
    {
        buffDict["RedPandaBazaar_Golden_Delight"] = new Buff(
            id: "RedPandaBazaar_ExquisitelyStuffed",
            iconTexture: Tools.Helper.ModContent.Load<Texture2D>("assets/RedPandaBazaar_Buffs.png"),
            iconSheetIndex: 0,
            duration: Buff.ENDLESS,
            effects: new BuffEffects()
            {
                Speed = { 2 },
                Defense = { 4 },
                Attack = { 10 },
                WeaponSpeedMultiplier = { 10 },
                CriticalChanceMultiplier = { 4 },
                FishingLevel = { 6 },
                ForagingLevel = { 4 },
                FarmingLevel = { 4 }
            }
        );
    }
}