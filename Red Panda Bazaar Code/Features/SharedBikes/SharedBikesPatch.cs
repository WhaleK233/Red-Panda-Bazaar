using System;
using WhaleK.Common;
using HarmonyLib;
using StardewValley.Characters;

namespace Red_Panda_Bazaar_Code.Features.SharedBikes;

/// <summary>共享单车补丁：原版下马只给有 Stable（马厩）的马重新落位，无 Stable 的共享单车会变成孤儿实体消失。</summary>
public static class SharedBikesPatch
{
    private const string NAME = nameof(SharedBikesPatch);

    public static void ApplyPatch(Harmony harmony)
    {
        Tools.LogPatch(NAME, "Horse.dismount()", PatchType.Postfix);
        harmony.Patch(
            original: AccessTools.Method(typeof(Horse), nameof(Horse.dismount)),
            postfix: new HarmonyMethod(typeof(SharedBikesPatch), nameof(Postfix_Horse_dismount))
        );
    }

    /// <summary>下马后把共享单车补回骑手当前地图的 characters，恢复实体的更新与绘制。</summary>
    private static void Postfix_Horse_dismount(Horse __instance)
    {
        try
        {
            // 先判身份：不是共享单车的马立刻放行
            if (!__instance.modData.ContainsKey(SharedBikes.BikeMarkerKey))
                return;

            // 骑乘期间马的 currentLocation 由骑手 update 持续喂入（Farmer.update → mount.update），
            // dismount 结束时它就是骑手脚下那张图，直接落位即可
            if (__instance.currentLocation != null && !__instance.currentLocation.characters.Contains(__instance))
            {
                __instance.currentLocation.characters.Add(__instance);
            }
        }
        catch (Exception e)
        {
            Tools.LogPatchErr(NAME, e);
        }
    }
}
