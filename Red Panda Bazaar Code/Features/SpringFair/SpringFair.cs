using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Features.Critters;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Minigames;

namespace Red_Panda_Bazaar_Code.Features.SpringFair;

/// <summary>春季展览会（Spring 8）的交互逻辑：天气、小游戏、抽奖机、背包购买。</summary>
public static class SpringFair
{
    /// <summary>各交互点在图块上的坐标。</summary>
    private const int FishingGameTileX = 62, FishingGameTileY = 75;
    private const int TargetGameTileX = 72, TargetGameTileY = 75;
    private const int WheelBetTileX1 = 67, WheelBetTileY1 = 75;
    private const int WheelBetTileX2 = 68, WheelBetTileY2 = 75;
    private static readonly PerScreen<Vector2?> MiniGameReturnPosition = new();

    /// <summary>是否处于春 8 节日状态（兼容 CJB 跳日期，不依赖 Game1.CurrentEvent）。</summary>
    private static bool IsSpringFairActive =>
        Game1.Date.Season == Season.Spring && Game1.Date.DayOfMonth == 8;

    /// <summary>是否位于春季展览会地图，避免春 8 在其他地图误触相同坐标。</summary>
    private static bool IsSpringFairLocation =>
        Game1.currentLocation?.Name.Contains("SpringFair", StringComparison.OrdinalIgnoreCase) == true ||
        Game1.CurrentEvent?.FestivalName == "SpringFair";

    /// <summary>注册节日事件。</summary>
    public static void Init()
    {
        Tools.Log("Spring Fair Initializing.");

        ModInfo.Helper.Events.GameLoop.DayStarted += OnDayStarted;
        ModInfo.Helper.Events.Input.ButtonPressed += OnButtonPressed;
        ModInfo.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
        ModInfo.Helper.Events.Player.Warped += OnPlayerWarped;

        Tools.Log("Spring Fair Initialized.");
    }

    internal static void StartTargetGame()
    {
        MiniGameReturnPosition.Value = Game1.player.Position;
        Game1.globalFadeToBlack(TargetGame.startMe, 0.01f);
    }

    internal static void StartFishingGame()
    {
        MiniGameReturnPosition.Value = Game1.player.Position;
        Game1.globalFadeToBlack(FishingGame.startMe, 0.01f);
    }

    internal static void TryRestoreMiniGamePosition()
    {
        if (!MiniGameReturnPosition.Value.HasValue)
            return;

        Game1.player.Position = MiniGameReturnPosition.Value.Value;
        Game1.player.TemporaryPassableTiles.Add(new Rectangle(
            Game1.player.TilePoint.X * 64,
            Game1.player.TilePoint.Y * 64,
            64,
            64));
        MiniGameReturnPosition.Value = null;
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
        if (!e.NewLocation.IsOutdoors || !IsSpringFairLocation) return;

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
        if (!IsSpringFairLocation) return;

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
        if (!Tools.IsValidButtonAction(e) || !IsSpringFairActive || !IsSpringFairLocation) return;

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
        Game1.currentLocation.createQuestionDialogue(Tools.GetI18n(I18nKeys.Dialogue_TargetGameQuestion),
            new Response[]
            {
                new("Positive", Tools.GetI18n(I18nKeys.Dialogue_PositiveResponse)),
                new("Negative", Tools.GetI18n(I18nKeys.Dialogue_NegativeResponse))
            },
            (_, answer) =>
            {
                if (answer == "Positive" && Tools.TryCharge(50))
                {
                    StartTargetGame();
                }
            });
    }

    /// <summary>钓鱼游戏：付 50g 后进入。限时钓鱼获得星星币。</summary>
    private static void HandleFishingGame()
    {
        Game1.currentLocation.createQuestionDialogue(Tools.GetI18n(I18nKeys.Dialogue_FishingGameQuestion),
            new Response[]
            {
                new("Positive", Tools.GetI18n(I18nKeys.Dialogue_PositiveResponse)),
                new("Negative", Tools.GetI18n(I18nKeys.Dialogue_NegativeResponse))
            },
            (_, answer) =>
            {
                if (answer == "Positive" && Tools.TryCharge(50))
                {
                    StartFishingGame();
                }
            });
    }

}
