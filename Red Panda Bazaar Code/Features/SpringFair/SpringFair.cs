using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Features.Critters;
using Red_Panda_Bazaar_Code.Features.PrizeMachines;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace Red_Panda_Bazaar_Code.Features.SpringFair;

/// <summary>春季展览会（Spring 8）的交互逻辑：天气、小游戏、抽奖机、背包购买。</summary>
public static class SpringFair
{
    /// <summary>各交互点在图块上的坐标。</summary>
    private const int FishingGameTileX = 62, FishingGameTileY = 75;
    private const int TargetGameTileX = 72, TargetGameTileY = 75;
    private const int WheelBetTileX1 = 67, WheelBetTileY1 = 75;
    private const int WheelBetTileX2 = 68, WheelBetTileY2 = 75;
    private const int PrizeMachineTileX = 40, PrizeMachineTileY = 62;
    private const int BuyBackpackTileX = 26, BuyBackpackTileY = 77;

    /// <summary>是否处于春 8 节日状态（兼容 CJB 跳日期，不依赖 Game1.CurrentEvent）。</summary>
    private static bool IsSpringFairActive =>
        Game1.Date.Season == Season.Spring && Game1.Date.DayOfMonth == 8;

    /// <summary>注册节日事件。</summary>
    public static void Init()
    {
        Tools.Log("Spring Fair Initializing.");

        Tools.Helper.Events.GameLoop.DayStarted += OnDayStarted;
        Tools.Helper.Events.Input.ButtonPressed += OnButtonPressed;
        Tools.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
        Tools.Helper.Events.Player.Warped += OnPlayerWarped;

        Tools.Log("Spring Fair Initialized.");
    }

    /// <summary>春 7 设次日微风，春 8 覆盖当天为微风。</summary>
    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!Context.IsMainPlayer) return;

        if (Game1.Date.Season == Season.Spring && Game1.Date.DayOfMonth == 7)
        {
            Game1.weatherForTomorrow = Game1.weather_debris;
        }

        if (Game1.Date.Season == Season.Spring && Game1.Date.DayOfMonth == 8)
        {
            Game1.netWorldState.Value.GetWeatherForLocation("Default").isDebrisWeather.Value = true;
            Game1.ApplyWeatherForNewDay();
        }
    }

    /// <summary>进入室外地图时生成氛围生物（白天蝴蝶/入夜萤火虫），并确保微风天气（兼容 CJB 跳日期）。</summary>
    private static void OnPlayerWarped(object? sender, WarpedEventArgs e)
    {
        if (!IsSpringFairActive) return;
        if (!e.NewLocation.IsOutdoors) return;

        var critterType = TimeUtils.IsDayTime(e.NewLocation)
            ? CrittersSpawner.Butterfly
            : CrittersSpawner.Firefly;
        CrittersSpawner.spawns(e.NewLocation, critterType);

        // 无论通过何种方式到达春8，确保天气为微风（仅主机）
        if (Context.IsMainPlayer && !Game1.isDebrisWeather)
            Game1.netWorldState.Value.GetWeatherForLocation("Default").isDebrisWeather.Value = true;
    }

    /// <summary>渲染星星币数量（左上角）。</summary>
    private static void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!IsSpringFairActive) return;
        if (Game1.CurrentEvent == null && !Game1.currentLocation.Name.Contains("SpringFair")) return;

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

    /// <summary>根据点击坐标派发到对应的交互处理。</summary>
    private static void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Tools.IsValidButtonAction(e) || !IsSpringFairActive) return;

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
                Game1.activeClickableMenu = new ClassicPrizeMachineMenu();
                break;
            case (BuyBackpackTileX, BuyBackpackTileY):
                BuyBackpack();
                break;
        }
    }

    /// <summary>轮盘赌：付 50g 后选择颜色下注。</summary>
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
                if (answer == "Positive" && Tools.TryCharge(50))
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

    /// <summary>打靶游戏：付 50g 后进入。限时射击获得星星币。</summary>
    private static void HandleTargetGame()
    {
        if (Tools.TryCharge(50))
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
                        Game1.globalFadeToBlack(TargetMiniGame.TargetMiniGame.startMe, 0.01f);
                    }
                });
        }
    }

    /// <summary>钓鱼游戏：付 50g 后进入。限时钓鱼获得星星币。</summary>
    private static void HandleFishingGame()
    {
        if (Tools.TryCharge(50))
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
                        Game1.globalFadeToBlack(FishingMiniGame.FishingMiniGame.startMe, 0.01f);
                    }
                });
        }
    }

    /// <summary>购买背包：根据当前容量（12/24/36）提供升级选项。</summary>
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
                        if (answer == "Purchase" && Tools.TryCharge(1999))
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
                        if (answer == "Purchase" && Tools.TryCharge(9999))
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
