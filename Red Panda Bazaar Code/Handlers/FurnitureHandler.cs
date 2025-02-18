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
        Tools.Helper.Events.Display.RenderedWorld += OnRenderedWorld;

        Tools.Log("Furniture Initialized.");
    }

    private static void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        /*var b = e.SpriteBatch;
        var loc = Game1.currentLocation;
        if (loc.Name != "Custom_MarlinShop1") return;

        Furniture.isDrawingLocationFurniture = true;
        foreach (Furniture furniture in loc.furniture)
        {
            if (furniture.QualifiedItemId == "(F)" + ItemsKeys.Furniture.MarlinFishTank1)
                furniture.draw(b, -1, -1, 1f);
        }

        Furniture.isDrawingLocationFurniture = false;*/
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
        FishTankFurniture fishTankFurniture = new(ItemsKeys.Furniture.MarlinFishTank1, new Vector2(16f, 11f))
        {
            CanBeGrabbed = false,
            AllowLocalRemoval = false,
            Fragility = 2
        };
        fishTankFurniture.heldItems.Add(ItemRegistry.Create("(O)143"));
        fishTankFurniture.heldItems.Add(ItemRegistry.Create("(O)145"));
        loc.furniture.Add(fishTankFurniture);
    }
}