using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.Bank;

public static class Bank
{
    private const string BankLocation = "Custom_RedPandaBazaarBank1";

    public static void Init()
    {
        Tools.Log("[Bank] Initializing.");
        GameLocation.RegisterTileAction("RedPandaBazaar_BankMenu", OnTileAction);
        ModInfo.Helper.Events.Input.ButtonPressed += OnButtonPressed;
        Tools.Log("[Bank] Initialized.");
    }

    private static bool OnTileAction(GameLocation location, string[] args, Farmer who, Point tile)
    {
        ShowBankServiceDialogue(location);
        return false;
    }

    private static void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Tools.IsValidButtonAction(e))
            return;
        if (Game1.currentLocation is not { Name: BankLocation })
            return;

        var tile = e.Cursor.GrabTile;
        var action = Game1.currentLocation.doesTileHaveProperty(
            (int)tile.X, (int)tile.Y, "Action", "Buildings");
        if (action != "RedPandaBazaar_BankMenu")
            return;

        ShowBankServiceDialogue(Game1.currentLocation);
        ModInfo.Helper.Input.Suppress(e.Button);
    }

    /// <summary>弹出业务选择对话框。</summary>
    private static void ShowBankServiceDialogue(GameLocation location)
    {
        location.createQuestionDialogue(
            Tools.GetI18n(I18nKeys.Bank_ServiceQuestion).ToString(),
            new Response[]
            {
                new("Investment", Tools.GetI18n(I18nKeys.Bank_ServiceInvestment).ToString()),
                new("Cancel", Tools.GetI18n(I18nKeys.Bank_ServiceCancel).ToString()),
            },
            OnBankDialogResponse);
    }

    private static void OnBankDialogResponse(Farmer who, string whichAnswer)
    {
        switch (whichAnswer)
        {
            case "Investment":
                Game1.activeClickableMenu = new BankInvestmentMenu();
                break;
        }
    }
}
