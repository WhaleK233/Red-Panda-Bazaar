using HarmonyLib;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley.Objects;

namespace Red_Panda_Bazaar_Code.Features.Furniture;

/// <summary>阻止玩家打开马林商店展示鱼缸的菜单（防偷鱼）。</summary>
public static class FurniturePatch
{
    private const string NAME = nameof(FurniturePatch);

    public static void ApplyPatch(Harmony harmony)
    {
        Tools.LogPatch(NAME, "FishTankFurniture.checkForAction()", PatchType.Prefix);
        harmony.Patch(
            original: AccessTools.Method(typeof(FishTankFurniture), nameof(FishTankFurniture.checkForAction)),
            prefix: new HarmonyMethod(typeof(FurniturePatch), nameof(Prefix_FishTankFurniture_checkForAction))
        );
    }

    private static bool Prefix_FishTankFurniture_checkForAction(FishTankFurniture __instance)
    {
        try
        {
            if (__instance.Location?.Name != "Custom_MarlinShop1") return true;
            var pos = __instance.TileLocation;
            if (pos is not ({ X: 17f, Y: 12f } or { X: 20f, Y: 17f })) return true;

            // 阻止打开鱼缸菜单
            return false;
        }
        catch (Exception e)
        {
            Tools.LogPatchErr(NAME, e);
            return true;
        }
    }
}
