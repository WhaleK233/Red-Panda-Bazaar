using HarmonyLib;
using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using Object = StardewValley.Object;

namespace Red_Panda_Bazaar_Code.HarmonyPatch;

public class HarmonyPatch_CustomFertilizer
{
    private static bool Applied { get; set; } = false;

    public static void ApplyPatch(Harmony harmony)
    {
        if (!Applied)
        {
            Tools.Monitor.Log(
                $"Applying Harmony patch \"{nameof(HarmonyPatch_CustomFertilizer)}\": postfixing SDV method \"Crop.harvest()\".",
                LogLevel.Trace);
            harmony.Patch(
                original: AccessTools.Method(typeof(Crop), "harvest"),
                prefix: new HarmonyMethod(typeof(HarmonyPatch_CustomFertilizer), nameof(Prefix_Crop_harvest)),
                postfix: new HarmonyMethod(typeof(HarmonyPatch_CustomFertilizer), nameof(Postfix_Crop_harvest))
            );

            Applied = true;
        }
    }

    // 收获前记录化肥状态
    public static void Prefix_Crop_harvest(Crop __instance, HoeDirt soil, ref int __state)
    {
        __state = int.Parse(soil?.fertilizer?.Value ?? "0");
    }

    // 应用产量加成
    public static void Postfix_Crop_harvest(Crop __instance, HoeDirt soil, ref int __result, int __state)
    {
        if (__state == 10086 && __result > 0)
        {
            __result *= Game1.random.Next(1, 4);
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Object), nameof(Object.placementAction))]
    class FertilizerPlacementPatch
    {
        public static bool Prefix(Object __instance,
            GameLocation location,
            int x,
            int y,
            Farmer who,
            ref bool __result)
        {
            try
            {
                // 只处理我们的化肥
                if (__instance.ParentSheetIndex != 10086)
                    return true;

                Vector2 tilePos = new Vector2(x / 64, y / 64);

                // 验证目标地块
                if (!location.terrainFeatures.TryGetValue(tilePos, out var terrain) ||
                    !(terrain is HoeDirt dirt) ||
                    dirt.fertilizer.Value != "0")
                {
                    __result = false;
                    return false;
                }

                // 应用化肥
                dirt.fertilizer.Value = 10086.ToString();
                location.playSound("dirtyHit");

                // 消耗物品
                if (__instance.Stack > 1)
                    __instance.Stack--;
                else
                    who.removeItemFromInventory(__instance);

                __result = true;
                return false; // 跳过原版逻辑
            }
            catch
            {
                return true;
            }
        }
    }
}