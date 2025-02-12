using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Compatibility;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Controller;

public static class EntranceController
{
    private static bool HasCentralStation = false;

    public static void Init()
    {
        AddCentralStationDestination();
        Tools.Helper.Events.Input.ButtonPressed += OnButtonPressed;
        Tools.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
    }

    private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        RegisterActions();
    }

    private static void RegisterActions()
    {
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
                        Game1.pauseThenMessage(1500, null);
                        Game1.currentLocation.localSound("busDriveOff");
                        Game1.warpFarmer("BusStop", 22, 10, 2);
                    }
                }
            )
        );

        Station[] stations;
        Response[] responses;
        SetStations(out stations, out responses);
        GameLocation.RegisterTileAction("RedPandaBazaar_TicketStation", (location, strings, arg3, arg4) =>
            {
                Game1.currentLocation.createQuestionDialogue(
                    Tools.I18n.Get(I18nKeys.Dialogue_WhereToGo),
                    responses,
                    (f, answer) =>
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
        List<Station> StationList = new List<Station>();
        List<Response> ResponseList = new List<Response>();

        StationList.Add(new Station("BusStop", new Point(22, 9), 2, 0));
        ResponseList.Add(new Response("BusStop", Tools.I18n.Get(I18nKeys.Text_PelicanTown)));

        if (HasCentralStation)
        {
            StationList.Add(new Station("Pathoschild.CentralStation_CentralStation", new Point(60, 13), 2, 0));
            ResponseList.Add(new Response("Pathoschild.CentralStation_CentralStation",
                Tools.I18n.Get(I18nKeys.Text_CentralStation)));
        }

        ResponseList.Add(new Response("Desert",
            Tools.I18n.Get(I18nKeys.Text_CalicoDesert) + " (500" + Tools.I18n.Get(I18nKeys.Text_Gold) + ")"));
        ResponseList.Add(new Response("Cancel", "Cancel"));

        stations = StationList.ToArray();
        responses = ResponseList.ToArray();
    }

    private static void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || !e.Button.IsActionButton() || Game1.player.hasMenuOpen.Value)
            return;

        if (!Game1.currentLocation.Name.Contains("BusStop"))
            return;

        if (StardewModdingAPI.Constants.TargetPlatform == GamePlatform.Android && e.Button != SButton.MouseLeft)
            return;

        var tile = e.Cursor.GrabTile;

        if (Game1.player.canMove && tile.X == 19 && (tile.Y == 10 || tile.Y == 11 || tile.Y == 12))
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
                        Game1.pauseThenMessage(1500, null);
                        Game1.currentLocation.localSound("busDriveOff");
                        Game1.warpFarmer("Custom_MapleBridge", 27, 40, 2);
                    }
                }
            );
        }
    }

    private static void AddCentralStationDestination()
    {
        var centralStation = Tools.Helper.ModRegistry.GetApi<ICentralStationApi>("Pathoschild.CentralStation");
        if (centralStation != null)
        {
            HasCentralStation = true;
        }

        centralStation?.RegisterStop(
            id: "RedPandaBazaarStation",
            displayName: () => "Red Panda Bazaar",
            toLocation: "Custom_MapleBridge",
            toTile: new Point(27, 40),
            toFacingDirection: Game1.down,
            cost: 300,
            network: "Bus",
            condition: null
        );
    }

    private class Station
    {
        public int facingDirection;
        public string mapName;
        public int price;
        public Point tile;

        public Station(string mapName, Point tile, int facingDirection, int price)
        {
            this.mapName = mapName;
            this.tile = tile;
            this.facingDirection = facingDirection;
            this.price = price;
        }
    }
}