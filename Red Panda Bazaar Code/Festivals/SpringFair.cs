using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Config;
using Red_Panda_Bazaar_Code.Festivals.MiniGames;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Network;

namespace Red_Panda_Bazaar_Code.Festivals;

public class SpringFair
{
    private static bool Enabled { get; set; } = false;

    private static ModConfig Config { get; set; } = null;

    private static IModHelper Helper { get; set; } = null;

    private static IMonitor Monitor { get; set; } = null;

    /// <summary>启用春8的一些效果</summary>
    public static void Enable(IModHelper helper, IMonitor monitor, ModConfig modConfig)
    {
        // 如果未启用
        if (!Enabled && helper != null && monitor != null && modConfig != null)
        {
            Helper = helper;
            Monitor = monitor;
            Config = modConfig;

            Helper.Events.GameLoop.DayStarted += OnDayStarted;

            added = false;
            Enabled = true;
            Monitor.Log("SpringFairFunctions Enabled", LogLevel.Debug);
        }
    }

    private static void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (Game1.CurrentEvent?.FestivalName == "SpringFair")
        {
            var b = Game1.spriteBatch;
            e.SpriteBatch.Draw(Game1.fadeToBlackRect,
                new Rectangle(16, 16, 128 + (Game1.player.festivalScore > 999 ? 16 : 0), 64),
                Color.Black * 0.75f);
            e.SpriteBatch.Draw(Game1.mouseCursors, new Vector2(32f, 32f),
                new Rectangle?(new Rectangle(338, 400, 8, 8)),
                Color.White,
                0.0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
            Game1.drawWithBorder(Game1.player.festivalScore.ToString() ?? "", Color.Black, Color.White,
                new Vector2(72f,
                    (float)(21 + (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.en
                        ? 8
                        : (LocalizedContentManager.CurrentLanguageLatin ? 16 : 8)))), 0.0f, 1f, 1f, false);
        }
    }

    private static void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (Game1.CurrentEvent == null || Game1.CurrentEvent.FestivalName != "SpringFair" ||
            Game1.activeClickableMenu != null || !Context.CanPlayerMove)
        {
            return;
        }

        if (e.Button.IsActionButton())
        {
            // 进行钓鱼小游戏
            if (e.Cursor.GrabTile is { X: 62, Y: 75 })
            {
                //suppressClick();
                if (CheckMoneyAndCharge(50))
                {
                    Game1.currentLocation.createQuestionDialogue(Helper.Translation.Get("FishingGame_Question"),
                        new Response[]
                        {
                            new Response("Positive", Helper.Translation.Get("Response_Positive")),
                            new Response("Negative", Helper.Translation.Get("Response_Negative"))
                        },
                        (f, answer) =>
                        {
                            if (answer == "Positive")
                            {
                                Game1.globalFadeToBlack(new Game1.afterFadeFunction(RPBFishingGame.startMe), 0.01f);
                            }
                        });
                }
            }

            // 进行弹弓小游戏
            if (e.Cursor.GrabTile is { X: 72, Y: 75 })
            {
                //suppressClick();
                if (CheckMoneyAndCharge(50))
                {
                    Game1.currentLocation.createQuestionDialogue(Helper.Translation.Get("TargetGame_Question"),
                        new Response[]
                        {
                            new Response("Positive", Helper.Translation.Get("Response_Positive")),
                            new Response("Negative", Helper.Translation.Get("Response_Negative"))
                        },
                        (f, answer) =>
                        {
                            if (answer == "Positive")
                            {
                                Game1.globalFadeToBlack(new Game1.afterFadeFunction(RPBTargetGame.startMe), 0.01f);
                            }
                        });
                }
            }

            // 进行轮盘赌小游戏
            if (e.Cursor.GrabTile is { X: 67, Y: 75 } or { X: 68, Y: 75 })
            {
                //SuppressClick();
                Response[] answerChoices = new Response[3]
                {
                    new Response("Orange",
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1645")),
                    new Response("Green",
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1647")),
                    new Response("I",
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1650"))
                };
                Game1.currentLocation.createQuestionDialogue(
                    Game1.parseText(
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1652")),
                    answerChoices, "wheelBet");
            }
        }
    }

    private static void SuppressClick()
    {
        Helper.Input.Suppress(Game1.options.actionButton[0].ToSButton());
        Helper.Input.Suppress(Game1.options.useToolButton[0].ToSButton());
        Helper.Input.Suppress(SButton.MouseLeft);
        Helper.Input.Suppress(SButton.MouseRight);
    }

    private static bool CheckMoneyAndCharge(int cost)
    {
        if (Game1.player.Money >= cost)
        {
            Game1.player.Money -= cost;
            return true;
        }
        else
        {
            Game1.drawObjectDialogue(Helper.Translation.Get("Money_NotEnough"));
            return false;
        }
    }

    private static bool added = false;

    [EventPriority(EventPriority.High)]
    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        // 如果今天是春7, 调整第二天天气为微风
        if (Game1.Date.Season == Season.Spring && Game1.Date.DayOfMonth == 7)
        {
            Game1.weatherForTomorrow = Game1.weather_debris;
        }

        // 如果今天是春8, 添加小游戏触发条件, 覆盖今天天气为微风
        if (Game1.Date.Season == Season.Spring && Game1.Date.DayOfMonth == 8)
        {
            if (!added)
            {
                Helper.Events.Input.ButtonPressed += OnButtonPressed;
                Helper.Events.Display.RenderedWorld += OnRenderedWorld;
                added = true;
            }

            LocationWeather weatherForLocation = Game1.netWorldState.Value.GetWeatherForLocation("Default");
            weatherForLocation.isDebrisWeather.Value = true;
            Game1.ApplyWeatherForNewDay();
        }
        // 如果今天不是春8且小游戏触发条件未移除, 则移除小游戏触发条件, 防止重复判定影响性能
        else
        {
            if (added)
            {
                Helper.Events.Input.ButtonPressed -= OnButtonPressed;
                Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
                added = false;
            }
        }
    }
}