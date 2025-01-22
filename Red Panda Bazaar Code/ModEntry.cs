using HarmonyLib;
using Red_Panda_Bazaar_Code.Buffs;
using Red_Panda_Bazaar_Code.Compatibility;
using Red_Panda_Bazaar_Code.Config;
using Red_Panda_Bazaar_Code.Data;
using Red_Panda_Bazaar_Code.Festivals;
using Red_Panda_Bazaar_Code.Menus;
using Red_Panda_Bazaar_Code.MiniGames;
using Red_Panda_Bazaar_Code.Quests;
using Red_Panda_Bazaar_Code.Utils;
using Red_Panda_Bazaar_Code.VisualEffects;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code;

public class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        Tools.Init(helper, helper.ReadConfig<ModConfig>(), Monitor);

        Tools.Log($"Red Panda Bazaar Code Initializing...");

        Tools.Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        Tools.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || !e.Button.IsActionButton())
            return;

        Tools.Helper.Input.Suppress(e.Button);
        var tile = e.Cursor.GrabTile;
        if (Game1.currentLocation.Name.Contains("BusStop") && tile.X == 19 && tile.Y == 11)
        {
            Game1.currentLocation.createQuestionDialogue(Tools.I18n.Get(I18nKeys.Dialogue_EntranceQuestion),
                new Response[]
                {
                    new Response("Positive", Tools.I18n.Get(I18nKeys.Dialogue_PositiveResponse)),
                    new Response("Negative", Tools.I18n.Get(I18nKeys.Dialogue_NegativeResponse))
                },
                (f, answer) =>
                {
                    if (answer == "Positive" && SpringFairController.CheckMoneyAndCharge(500))
                    {
                        Game1.player.Halt();
                        Game1.player.freezePause = 700;
                        Game1.warpFarmer("Custom_MapleBridge", 27, 40, 2);
                    }
                }
            );
        }
    }

    // 包含i18n的初始化
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        BuffController.Init();
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        InitializeGenericModConfigMenu();
        RPBData.Init();
        ControllerInit();
        EntranceInit();
        PatchHarmony();
    }

    private void EntranceInit()
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

    private static void ControllerInit()
    {
        FireFlyController.Init();
        SpringFairController.Init();
        MenuController.Init();
        QuestsController.Init();
    }

    private void PatchHarmony()
    {
        var harmony = new Harmony(this.ModManifest.UniqueID);
        HarmonyPatch_FishingGameEvent.ApplyPatch(harmony);
        HarmonyPatch_CustomBuffEffects.ApplyPatch(harmony);
    }

    #region Generic Mod Config Menu

    private void InitializeGenericModConfigMenu()
    {
        // 获取通用模组配置菜单的API
        var configMenu = Tools.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(ModCompat.GenericModConfigMenu);
        if (configMenu is null) return;

        // 注册模组
        configMenu.Register(
            mod: this.ModManifest,
            reset: () => Tools.ModConfig = new ModConfig(),
            save: () => Tools.Helper.WriteConfig(Tools.ModConfig)
        );

        // 萤火虫数量
        configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => Tools.I18n.Get(I18nKeys.Config_NumberOfFirefly),
            getValue: () => Tools.ModConfig.NumberOfFireFly,
            setValue: value => Tools.ModConfig.NumberOfFireFly = value,
            min: 0,
            max: 2048
        );

        //动画速度
        configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => Tools.I18n.Get(I18nKeys.Config_AnimeSpeed_PrizeMenu),
            getValue: () => Tools.ModConfig.AnimationSpeed_PrizeMenu,
            setValue: value => Tools.ModConfig.AnimationSpeed_PrizeMenu = (float)Math.Round(value, 1),
            min: 0.5f,
            max: 5.0f,
            formatValue: value => Math.Round(value, 1) + "x"
        );
    }

    #endregion
}