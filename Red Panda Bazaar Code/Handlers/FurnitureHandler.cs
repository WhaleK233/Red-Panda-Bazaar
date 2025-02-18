using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace Red_Panda_Bazaar_Code.Handlers;

public static class FurnitureHandler
{
    public static void Init()
    {
        Tools.Log("Furniture Initializing.");

        Tools.Helper.Events.Player.Warped += OnPlayerWarped;

        Tools.Log("Furniture Initialized.");
    }

    private static void OnPlayerWarped(object? sender, WarpedEventArgs e)
    {
        var loc = e.NewLocation;
        if (loc.Name != "Custom_MarlinShop1") return;

        var flag = false;
        foreach (var furniture in loc.furniture.Where(furniture =>
                     furniture.QualifiedItemId == "(F)" + ItemsKeys.Furniture.MarlinFishTank1))
        {
            furniture.AllowLocalRemoval = false;
            flag = true;
            break;
        }

        if (flag)
            return;
        FishTankFurniture fishTankFurniture1 = new(ItemsKeys.Furniture.MarlinFishTank1, new Vector2(17f, 12f))
        {
            CanBeGrabbed = false,
            AllowLocalRemoval = false,
            Fragility = 2
        };
        FishTankFurniture fishTankFurniture2 = new(ItemsKeys.Furniture.MarlinFishTank1, new Vector2(20f, 17f))
        {
            CanBeGrabbed = false,
            AllowLocalRemoval = false,
            Fragility = 2
        };
        fishTankFurniture1.heldItems.Add(ItemRegistry.Create("(O)143"));
        fishTankFurniture2.heldItems.Add(ItemRegistry.Create("(O)143"));
        fishTankFurniture1.heldItems.Add(ItemRegistry.Create("(O)145"));
        fishTankFurniture2.heldItems.Add(ItemRegistry.Create("(O)145"));
        loc.furniture.Add(fishTankFurniture1);
        loc.furniture.Add(fishTankFurniture2);
    }
}