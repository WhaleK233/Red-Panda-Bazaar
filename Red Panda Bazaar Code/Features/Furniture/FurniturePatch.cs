using HarmonyLib;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley.Objects;

namespace Red_Panda_Bazaar_Code.Features.Furniture;

/// <summary>阻止玩家与马林商店展示鱼缸的一切交互（打开菜单、拿起、移动、用工具敲）。</summary>
public static class FurniturePatch
{
    private const string NAME = nameof(FurniturePatch);

    public static void ApplyPatch(Harmony harmony)
    {
        Tools.LogPatch(NAME, "FishTankFurniture.checkForAction()", PatchType.Prefix);
        harmony.Patch(
            original: AccessTools.Method(typeof(FishTankFurniture), nameof(FishTankFurniture.checkForAction)),
            prefix: new HarmonyMethod(typeof(FurniturePatch), nameof(Prefix_checkForAction))
        );

        // clicked 由 Furniture 声明，FishTankFurniture 未重写，需补丁基类
        Tools.LogPatch(NAME, "Furniture.clicked()", PatchType.Prefix);
        harmony.Patch(
            original: AccessTools.Method(typeof(StardewValley.Objects.Furniture), nameof(StardewValley.Objects.Furniture.clicked)),
            prefix: new HarmonyMethod(typeof(FurniturePatch), nameof(Prefix_clicked))
        );

        // canBeRemoved 由 StorageFurniture 重写，FishTankFurniture 未重写，需补丁基类
        Tools.LogPatch(NAME, "StorageFurniture.canBeRemoved()", PatchType.Prefix);
        harmony.Patch(
            original: AccessTools.Method(typeof(StorageFurniture), nameof(StorageFurniture.canBeRemoved)),
            prefix: new HarmonyMethod(typeof(FurniturePatch), nameof(Prefix_canBeRemoved))
        );
    }

    /// <returns>false = 跳过原方法（阻止右键菜单/放鱼等交互）。</returns>
    private static bool Prefix_checkForAction(FishTankFurniture __instance)
    {
        try
        {
            if (!IsTargetTank(__instance)) return true;
            return false;
        }
        catch (Exception e)
        {
            Tools.LogPatchErr(NAME, e);
            return true;
        }
    }

    /// <returns>false = 跳过原方法（阻止左键点击造成的拿起/移动）。</returns>
    private static bool Prefix_clicked(FishTankFurniture __instance)
    {
        try
        {
            if (!IsTargetTank(__instance)) return true;
            return false;
        }
        catch (Exception e)
        {
            Tools.LogPatchErr(NAME, e);
            return true;
        }
    }

    /// <returns>false = 跳过原方法（阻止任何方式移除鱼缸）。</returns>
    private static bool Prefix_canBeRemoved(FishTankFurniture __instance)
    {
        try
        {
            if (!IsTargetTank(__instance)) return true;
            return false;
        }
        catch (Exception e)
        {
            Tools.LogPatchErr(NAME, e);
            return true;
        }
    }

    /// <summary>判断是否为我们放置在 Custom_MarlinShop1 中的特定鱼缸。</summary>
    private static bool IsTargetTank(FishTankFurniture tank)
    {
        if (tank.Location?.Name != "Custom_MarlinShop1") return false;
        var pos = tank.TileLocation;
        return pos is ({ X: 17f, Y: 12f } or { X: 20f, Y: 17f });
    }
}
