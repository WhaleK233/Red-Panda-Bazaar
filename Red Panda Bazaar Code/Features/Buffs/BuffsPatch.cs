using HarmonyLib;
using Red_Panda_Bazaar_Code.Constant;

using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Objects;

namespace Red_Panda_Bazaar_Code.Features.Buffs;

public static class BuffsPatch
{
    private const string NAME = nameof(BuffsPatch);

    public static void ApplyPatch(Harmony harmony)
    {
        Tools.LogPatch(NAME, "Farmer.doneEating()", PatchType.Prefix);
        harmony.Patch(
            original: AccessTools.Method(typeof(Farmer), "doneEating"),
            prefix: new HarmonyMethod(typeof(BuffsPatch), nameof(Prefix_Farmer_doneEating))
        );

        Tools.LogPatch(NAME, "Item.AddEquipmentEffects()", PatchType.Postfix);
        harmony.Patch(
            original: AccessTools.Method(typeof(Item), nameof(Item.AddEquipmentEffects)),
            postfix: new HarmonyMethod(typeof(BuffsPatch), nameof(Postfix_Item_AddEquipmentEffects))
        );
    }

    /// <summary>赌徒帽的加速走装备效果通道：不占 Buff 栏、不与任何 Buff 互相覆盖。</summary>
    private static void Postfix_Item_AddEquipmentEffects(Item __instance, BuffEffects effects)
    {
        try
        {
            if (__instance is Hat { ItemId: ItemsKeys.Hats.GamblerHat })
            {
                effects.Speed.Value += 1f;
            }
        }
        catch (Exception e)
        {
            Tools.LogPatchErr(NAME, e);
        }
    }

    private static bool Prefix_Farmer_doneEating(Farmer __instance)
    {
        try
        {
            var buffDict = Buffs.BuffDict;

            if (buffDict.ContainsKey(__instance.itemToEat.ItemId))
            {
                __instance.applyBuff(buffDict[__instance.itemToEat.ItemId]);
            }

            if (__instance.itemToEat.ItemId == ItemsKeys.Food.Milk_Pudding)
            {
                var ex = RandomUtils.Next(40, 60);
                Game1.player.gainExperience(SkillsKeys.Farming, ex);
            }

            return true;
        }
        catch (Exception e)
        {
            Tools.LogPatchErr(NAME, e);
            return true;
        }
    }
}