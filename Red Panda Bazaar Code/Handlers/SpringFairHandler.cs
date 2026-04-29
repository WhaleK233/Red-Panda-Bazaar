using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Custom;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace Red_Panda_Bazaar_Code.Handlers;

public static class SpringFairHandler
{
    private const int FishingGameTileX = 62, FishingGameTileY = 75;
    private const int TargetGameTileX = 72, TargetGameTileY = 75;
    private const int WheelBetTileX1 = 67, WheelBetTileY1 = 75;
    private const int WheelBetTileX2 = 68, WheelBetTileY2 = 75;
    private const int PrizeMachineTileX = 40, PrizeMachineTileY = 62;
    private const int BuyBackpackTileX = 26, BuyBackpackTileY = 77;
    public static void Init()
    {
        Tools.Log("Spring Fair Initializing.");

        Tools.Helper.Events.GameLoop.DayStarted += OnDayStarted;
        Tools.Helper.Events.Input.ButtonPressed += OnButtonPressed;
        Tools.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
        Tools.Helper.Events.Player.Warped += OnPlayerWarped;

        Tools.Log("Spring Fair Initialized.");
    }

    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        // 如果今天是春7, 调整第二天天气为微风
        if (Game1.Date.Season == Season.Spring && Game1.Date.DayOfMonth == 7)
        {
            Game1.weatherForTomorrow = Game1.weather_debris;
        }

        // 如果今天是春8, 覆盖今天天气为微风
        if (Game1.Date.Season == Season.Spring && Game1.Date.DayOfMonth == 8)
        {
            Game1.netWorldState.Value.GetWeatherForLocation("Default").isDebrisWeather.Value = true;
            Game1.ApplyWeatherForNewDay();
        }
    }

    private static void OnPlayerWarped(object? sender, WarpedEventArgs e)
    {
        if (Game1.CurrentEvent?.FestivalName == "SpringFair")
        {
            RPB_Critters.spawns(Game1.currentLocation, RPB_Critters.Firefly);
        }
    }

    /// <summary>渲染星星币数量</summary>
    private static void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (Game1.CurrentEvent?.FestivalName != "SpringFair") return;

        e.SpriteBatch.Draw(Game1.fadeToBlackRect,
            new Rectangle(16, 16, 128 + (Game1.player.festivalScore > 999 ? 16 : 0), 64),
            Color.Black * 0.75f);
        e.SpriteBatch.Draw(Game1.mouseCursors, new Vector2(32f, 32f),
            (new Rectangle(338, 400, 8, 8)),
            Color.White,
            0.0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
        Game1.drawWithBorder(Game1.player.festivalScore.ToString() ?? "", Color.Black, Color.White,
            new Vector2(72f,
                (21 + (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.en
                    ? 8
                    : (LocalizedContentManager.CurrentLanguageLatin ? 16 : 8)))), 0.0f, 1f, 1f, false);
    }

    /// <summary>检测是否为节日交互图块</summary>
    private static void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Tools.IsValidButtonAction(e) || Game1.CurrentEvent?.FestivalName != "SpringFair") return;

        var tile = e.Cursor.GrabTile;

        switch (tile.X, tile.Y)
        {
            case (FishingGameTileX, FishingGameTileY):
                HandleFishingGame();
                break;
            case (TargetGameTileX, TargetGameTileY):
                HandleTargetGame();
                break;
            case (WheelBetTileX1, WheelBetTileY1):
            case (WheelBetTileX2, WheelBetTileY2):
                HandleWheelBetGame();
                break;
            case (PrizeMachineTileX, PrizeMachineTileY):
                Game1.activeClickableMenu = new RPB_ClassicMachineMenu();
                break;
            case (BuyBackpackTileX, BuyBackpackTileY):
                BuyBackpack();
                break;
        }
    }

    private static void HandleWheelBetGame()
    {
        Game1.currentLocation.createQuestionDialogue(Tools.GetI18n(I18nKeys.Dialogue_WheelBetChargeQuestion),
            new Response[]
            {
                new("Positive", Tools.GetI18n(I18nKeys.Dialogue_PositiveResponse)),
                new("Negative", Tools.GetI18n(I18nKeys.Dialogue_NegativeResponse))
            },
            (_, answer) =>
            {
                if (answer == "Positive" && Tools.Charge(50))
                {
                    Response[] answerChoices = new Response[]
                    {
                        new("Orange", Tools.GetI18n(I18nKeys.Dialogue_WheelBet_WhiteResponse)),
                        new("Green", Tools.GetI18n(I18nKeys.Dialogue_WheelBet_RedResponse)),
                        new("I", Tools.GetI18n(I18nKeys.Dialogue_WheelBet_GiveUpResponse))
                    };
                    Game1.currentLocation.createQuestionDialogue(
                        Game1.parseText(
                            Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1652")),
                        answerChoices, "wheelBet");
                }
            });
    }

    private static void HandleTargetGame()
    {
        if (Tools.Charge(50))
        {
            Game1.currentLocation.createQuestionDialogue(Tools.GetI18n(I18nKeys.Dialogue_TargetGameQuestion),
                new Response[]
                {
                    new("Positive", Tools.GetI18n(I18nKeys.Dialogue_PositiveResponse)),
                    new("Negative", Tools.GetI18n(I18nKeys.Dialogue_NegativeResponse))
                },
                (_, answer) =>
                {
                    if (answer == "Positive")
                    {
                        Game1.globalFadeToBlack(RPB_TargetGame.startMe, 0.01f);
                    }
                });
        }
    }

    private static void HandleFishingGame()
    {
        if (Tools.Charge(50))
        {
            Game1.currentLocation.createQuestionDialogue(Tools.GetI18n(I18nKeys.Dialogue_FishingGameQuestion),
                new Response[]
                {
                    new("Positive", Tools.GetI18n(I18nKeys.Dialogue_PositiveResponse)),
                    new("Negative", Tools.GetI18n(I18nKeys.Dialogue_NegativeResponse))
                },
                (_, answer) =>
                {
                    if (answer == "Positive")
                    {
                        Game1.globalFadeToBlack(RPB_FishingGame.startMe, 0.01f);
                    }
                });
        }
    }

    private static void BuyBackpack()
    {
        var response1 = new Response("Purchase",
            Tools.GetI18n(I18nKeys.Dialogue_BuyBackpack_PositiveResponseTo24Slots));
        var response2 = new Response("Purchase",
            Tools.GetI18n(I18nKeys.Dialogue_BuyBackpack_PositiveResponseTo36Slots));
        var response3 = new Response("Not",
            Game1.content.LoadString("Strings\\Locations:SeedShop_BuyBackpack_ResponseNo"));
        switch (Game1.player.maxItems.Value)
        {
            case 12:
                Game1.currentLocation.createQuestionDialogue(
                    Game1.content.LoadString("Strings\\Locations:SeedShop_BuyBackpack_Question24"), new[]
                    {
                        response1,
                        response3
                    }, (_, answer) =>
                    {
                        if (answer == "Purchase" && Tools.Charge(1999))
                        {
                            Game1.player.increaseBackpackSize(12);
                            Game1.player.holdUpItemThenMessage(new SpecialItem(99,
                                Game1.content.LoadString("Strings\\StringsFromCSFiles:GameLocation.cs.8708")));
                            Tools.Helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue()
                                .globalChatInfoMessage("BackpackLarge", Game1.player.Name);
                        }
                    }
                );
                break;
            case < 36:
                Game1.currentLocation.createQuestionDialogue(
                    Game1.content.LoadString("Strings\\Locations:SeedShop_BuyBackpack_Question36"), new[]
                    {
                        response2,
                        response3
                    }, (_, answer) =>
                    {
                        if (answer == "Purchase" && Tools.Charge(9999))
                        {
                            Game1.player.maxItems.Value += 12;
                            Game1.player.holdUpItemThenMessage(new SpecialItem(99,
                                Game1.content.LoadString("Strings\\StringsFromCSFiles:GameLocation.cs.8709")));
                            for (var index = 0; index < Game1.player.maxItems.Value; ++index)
                            {
                                if (Game1.player.Items.Count <= index)
                                    Game1.player.Items.Add(null);
                            }

                            Tools.Helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue()
                                .globalChatInfoMessage("BackpackDeluxe", Game1.player.Name);
                        }
                    }
                );
                break;
        }
    }
}