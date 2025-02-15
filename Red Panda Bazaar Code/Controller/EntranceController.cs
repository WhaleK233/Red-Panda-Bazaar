using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Compatibility;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Controller;

public static class EntranceController
{
    public static void Init()
    {
        if (!Integrations.Installed.CentralStation)
        {
            Tools.Helper.Events.Input.ButtonPressed += OnButtonPressed;
        }

        Tools.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        Tools.Log("Entrance Initialized.");
    }

    private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        RegisterActions();
    }

    private static void RegisterActions()
    {
        GameLocation.RegisterTouchAction("RedPandaBazaarBus", (_, _, _, _) =>
            Game1.currentLocation.createQuestionDialogue(
                Game1.content.LoadString("Strings\\Locations:Desert_Return_Question"),
                new Response[]
                {
                    new("Positive", Tools.GetI18n(I18nKeys.Dialogue_PositiveResponse)),
                    new("Negative", Tools.GetI18n(I18nKeys.Dialogue_NegativeResponse))
                },
                (_, answer) =>
                {
                    if (answer == "Positive")
                    {
                        Game1.pauseThenMessage(1500, null);
                        Game1.currentLocation.localSound("busDriveOff");
                        Game1.warpFarmer("BusStop", 22, 10, 2);
                    }
                }
            )
        );

        SetStations(out var stations, out var responses);
        GameLocation.RegisterTileAction("RedPandaBazaar_TicketStation", (_, _, _, _) =>
            {
                Game1.currentLocation.createQuestionDialogue(
                    Tools.GetI18n(I18nKeys.Dialogue_WhereToGo),
                    responses,
                    (_, answer) =>
                    {
                        if (answer != "Cancel")
                        {
                            if (answer == "Desert")
                            {
                                if (!Game1.MasterPlayer.mailReceived.Contains("ccVault"))
                                {
                                    Game1.drawObjectDialogue(
                                        Game1.content.LoadString("Strings\\Locations:BusStop_DesertOutOfService"));
                                }
                                else if (Tools.Charge(500))
                                {
                                    Game1.pauseThenMessage(1500, null);
                                    Game1.currentLocation.localSound("busDriveOff");
                                    Game1.warpFarmer("Desert", 18, 28, 2);
                                }
                            }
                            else
                            {
                                foreach (var station in stations)
                                {
                                    if (answer == station.mapName && Tools.Charge(station.price))
                                    {
                                        Game1.pauseThenMessage(1500, null);
                                        Game1.currentLocation.localSound("busDriveOff");
                                        Game1.warpFarmer(station.mapName, station.tile.X, station.tile.Y,
                                            station.facingDirection);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                );
                return false;
            }
        );
    }

    private static void SetStations(out Station[] stations, out Response[] responses)
    {
        var StationList = new List<Station>();
        var ResponseList = new List<Response>();

        StationList.Add(new Station("BusStop", new Point(22, 9), 2, 0));
        ResponseList.Add(new Response("BusStop", Tools.GetI18n(I18nKeys.Text_PelicanTown)));

        if (Integrations.Installed.CentralStation)
        {
            StationList.Add(new Station("Pathoschild.CentralStation_CentralStation", new Point(60, 13), 2, 0));
            ResponseList.Add(new Response("Pathoschild.CentralStation_CentralStation",
                Tools.GetI18n(I18nKeys.Text_CentralStation)));
        }

        ResponseList.Add(new Response("Desert",
            Tools.GetI18n(I18nKeys.Text_CalicoDesert) + " (500" + Tools.GetI18n(I18nKeys.Text_Gold) + ")"));
        ResponseList.Add(new Response("Cancel", "Cancel"));

        stations = StationList.ToArray();
        responses = ResponseList.ToArray();
    }

    private static void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || !e.Button.IsActionButton() || Game1.player.hasMenuOpen.Value ||
            !Game1.player.canMove)
            return;

        if (Constants.TargetPlatform == GamePlatform.Android && e.Button != SButton.MouseLeft)
            return;

        if (!Game1.currentLocation.Name.Contains("BusStop"))
            return;

        var tile = e.Cursor.GrabTile;

        if (tile is { X: 19 } and ({ Y: 10 } or { Y: 11 } or { Y: 12 }))
        {
            Tools.Helper.Input.Suppress(e.Button);
            Game1.currentLocation.createQuestionDialogue(Tools.GetI18n(I18nKeys.Dialogue_EntranceQuestion),
                new Response[]
                {
                    new("Positive", Tools.GetI18n(I18nKeys.Dialogue_PositiveResponse)),
                    new("Negative", Tools.GetI18n(I18nKeys.Dialogue_NegativeResponse))
                },
                (_, answer) =>
                {
                    if (answer == "Positive" && Tools.Charge(300))
                    {
                        Game1.pauseThenMessage(1500, null);
                        Game1.currentLocation.localSound("busDriveOff");
                        Game1.warpFarmer("Custom_MapleBridge", 27, 40, 2);
                    }
                }
            );
        }
    }

    private class Station
    {
        public readonly int facingDirection;
        public readonly string mapName;
        public readonly int price;
        public readonly Point tile;

        public Station(string mapName, Point tile, int facingDirection, int price)
        {
            this.mapName = mapName;
            this.tile = tile;
            this.facingDirection = facingDirection;
            this.price = price;
        }
    }
}