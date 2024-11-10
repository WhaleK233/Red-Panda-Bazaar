using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Festivals.MiniGames;
using Red_Panda_Bazaar_Code.Menus.Custom_Menus;
using Red_Panda_Bazaar_Code.Utils;
using Red_Panda_Bazaar_Code.VisualEffects;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;

namespace Red_Panda_Bazaar_Code.Festivals;

public static class SpringFairEffects
{
    private static bool added = false;
    private static bool Enabled { get; set; } = false;

    /// <summary>启用春8的一些效果</summary>
    public static void Enable()
    {
        // 如果未启用
        if (!Enabled)
        {
            Tools.Helper.Events.GameLoop.DayStarted += OnDayStarted;

            added = false;
            Enabled = true;
            Tools.Monitor.Log("SpringFairFunctions Enabled");
        }
    }

    private static void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (Game1.CurrentEvent?.FestivalName == "SpringFair")
        {
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
                //SuppressClick();
                if (CheckMoneyAndCharge(50))
                {
                    Game1.currentLocation.createQuestionDialogue(Tools.I18n.Get(I18nKeys.Dialogue_FishingGameQuestion),
                        new Response[]
                        {
                            new Response("Positive", Tools.I18n.Get(I18nKeys.Dialogue_PositiveResponse)),
                            new Response("Negative", Tools.I18n.Get(I18nKeys.Dialogue_NegativeResponse))
                        },
                        (f, answer) =>
                        {
                            if (answer == "Positive")
                            {
                                Game1.globalFadeToBlack(new Game1.afterFadeFunction(RPB_FishingGame.startMe), 0.01f);
                            }
                        });
                }
            } // 进行弹弓小游戏
            else if (e.Cursor.GrabTile is { X: 72, Y: 75 })
            {
                if (CheckMoneyAndCharge(50))
                {
                    Game1.currentLocation.createQuestionDialogue(Tools.I18n.Get(I18nKeys.Dialogue_TargetGameQuestion),
                        new Response[]
                        {
                            new Response("Positive", Tools.I18n.Get(I18nKeys.Dialogue_PositiveResponse)),
                            new Response("Negative", Tools.I18n.Get(I18nKeys.Dialogue_NegativeResponse))
                        },
                        (f, answer) =>
                        {
                            if (answer == "Positive")
                            {
                                Game1.globalFadeToBlack(new Game1.afterFadeFunction(RPB_TargetGame.startMe), 0.01f);
                            }
                        });
                }
            } // 进行轮盘赌小游戏
            else if (e.Cursor.GrabTile is { X: 67, Y: 75 } or { X: 68, Y: 75 })
            {
                Game1.currentLocation.createQuestionDialogue(Tools.I18n.Get(I18nKeys.Dialogue_WheelBetChargeQuestion),
                    new Response[]
                    {
                        new Response("Positive", Tools.I18n.Get(I18nKeys.Dialogue_PositiveResponse)),
                        new Response("Negative", Tools.I18n.Get(I18nKeys.Dialogue_NegativeResponse))
                    },
                    (f, answer) =>
                    {
                        if (answer == "Positive" && CheckMoneyAndCharge(50))
                        {
                            Response[] answerChoices = new Response[3]
                            {
                                new Response("Orange", Tools.I18n.Get(I18nKeys.Dialogue_WheelBet_WhiteResponse)),
                                new Response("Green", Tools.I18n.Get(I18nKeys.Dialogue_WheelBet_RedResponse)),
                                new Response("I",
                                    Tools.I18n.Get(I18nKeys.Dialogue_WheelBet_GiveUpResponse))
                            };
                            Game1.currentLocation.createQuestionDialogue(
                                Game1.parseText(
                                    Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1652")),
                                answerChoices, "wheelBet");
                        }
                    });
            } // 打开兑奖机菜单
            else if (e.Cursor.GrabTile is { X: 40, Y: 62 })
            {
                Game1.activeClickableMenu = (IClickableMenu)new RPB_PrizeTicketMenu();
            } // 购买背包
            else if (e.Cursor.GrabTile is { X: 26, Y: 77 })
            {
                BuyBackpack();
            }
        }
    }

    private static void BuyBackpack()
    {
        Response response1 = new Response("Purchase",
            Tools.I18n.Get(I18nKeys.Dialogue_BuyBackpack_PositiveResponseTo24Slots));
        Response response2 = new Response("Purchase",
            Tools.I18n.Get(I18nKeys.Dialogue_BuyBackpack_PositiveResponseTo36Slots));
        Response response3 = new Response("Not",
            Game1.content.LoadString("Strings\\Locations:SeedShop_BuyBackpack_ResponseNo"));
        if (Game1.player.maxItems.Value == 12)
        {
            Game1.currentLocation.createQuestionDialogue(
                Game1.content.LoadString("Strings\\Locations:SeedShop_BuyBackpack_Question24"), new Response[2]
                {
                    response1,
                    response3
                }, (who, answer) =>
                {
                    if (answer == "Purchase" && CheckMoneyAndCharge(1999))
                    {
                        Game1.player.increaseBackpackSize(12);
                        Game1.player.holdUpItemThenMessage((Item)new SpecialItem(99,
                            Game1.content.LoadString("Strings\\StringsFromCSFiles:GameLocation.cs.8708")));
                    }
                }
            );
        }
        else if (Game1.player.maxItems.Value < 36)
        {
            Game1.currentLocation.createQuestionDialogue(
                Game1.content.LoadString("Strings\\Locations:SeedShop_BuyBackpack_Question36"), new Response[2]
                {
                    response2,
                    response3
                }, (who, answer) =>
                {
                    if (answer == "Purchase" && CheckMoneyAndCharge(9999))
                    {
                        Game1.player.maxItems.Value += 12;
                        Game1.player.holdUpItemThenMessage((Item)new SpecialItem(99,
                            Game1.content.LoadString("Strings\\StringsFromCSFiles:GameLocation.cs.8709")));
                        for (int index = 0; index < Game1.player.maxItems.Value; ++index)
                        {
                            if (Game1.player.Items.Count <= index)
                                Game1.player.Items.Add((Item)null);
                        }
                    }
                }
            );
        }
    }

    private static void SuppressClick()
    {
        Tools.Helper.Input.Suppress(Game1.options.actionButton[0].ToSButton());
        Tools.Helper.Input.Suppress(Game1.options.useToolButton[0].ToSButton());
        Tools.Helper.Input.Suppress(SButton.MouseLeft);
        Tools.Helper.Input.Suppress(SButton.MouseRight);
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
            Game1.drawObjectDialogue(Tools.I18n.Get(I18nKeys.Dialogue_MoneyNotEnough));
            return false;
        }
    }

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
                Tools.Helper.Events.Input.ButtonPressed += OnButtonPressed;
                Tools.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
                Tools.Helper.Events.Player.Warped += OnPlayerWarped;
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
                Tools.Helper.Events.Input.ButtonPressed -= OnButtonPressed;
                Tools.Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
                Tools.Helper.Events.Player.Warped -= OnPlayerWarped;
                added = false;
            }
        }
    }

    private static void OnPlayerWarped(object? sender, WarpedEventArgs e)
    {
        if (Game1.CurrentEvent?.FestivalName == "SpringFair")
        {
            FireFlyEffects.spawnFireFly(Game1.currentLocation);
        }
    }
}