using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Controller;

public static class EntranceController
{
    public static void Init()
    {
        Tools.Helper.Events.Input.ButtonPressed += OnButtonPressed;
        GameLocation.RegisterTouchAction("RedPandaBazaarBus", (location, strings, arg3, arg4) =>
            Game1.currentLocation.createQuestionDialogue(
                Game1.content.LoadString("Strings\\Locations:Desert_Return_Question"),
                new Response[]
                {
                    new Response("Positive", Tools.I18n.Get(I18nKeys.Dialogue_PositiveResponse)),
                    new Response("Negative", Tools.I18n.Get(I18nKeys.Dialogue_NegativeResponse))
                },
                (f, answer) =>
                {
                    if (answer == "Positive")
                    {
                        Game1.player.Halt();
                        Game1.player.freezePause = 700;
                        Game1.warpFarmer("BusStop", 22, 10, 2);
                    }
                }
            )
        );
    }

    private static void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || !e.Button.IsActionButton())
            return;

        var tile = e.Cursor.GrabTile;
        if (Game1.currentLocation.Name.Contains("BusStop") && tile.X == 19 &&
            (tile.Y == 10 || tile.Y == 11 || tile.Y == 12))
        {
            Tools.Helper.Input.Suppress(e.Button);
            Game1.currentLocation.createQuestionDialogue(Tools.I18n.Get(I18nKeys.Dialogue_EntranceQuestion),
                new Response[]
                {
                    new Response("Positive", Tools.I18n.Get(I18nKeys.Dialogue_PositiveResponse)),
                    new Response("Negative", Tools.I18n.Get(I18nKeys.Dialogue_NegativeResponse))
                },
                (f, answer) =>
                {
                    if (answer == "Positive" && Tools.Charge(300))
                    {
                        Game1.player.Halt();
                        Game1.player.freezePause = 700;
                        Game1.warpFarmer("Custom_MapleBridge", 27, 40, 2);
                    }
                }
            );
        }
    }
}