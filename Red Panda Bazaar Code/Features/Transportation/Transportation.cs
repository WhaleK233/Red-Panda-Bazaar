using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Compatibility;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.Transportation;

/// <summary>小熊猫集市交通系统：入口传送 + 票务站目的地选择。</summary>
public static class Transportation
{
    /// <summary>注册 Tile Action 和事件。</summary>
    public static void Init()
    {
        Tools.Log("Entrance Initializing.");

        // 没装 CentralStation 时用按钮交互进集市
        if (!Integrations.Installed.CentralStation)
        {
            Tools.Helper.Events.Input.ButtonPressed += OnButtonPressed;
        }

        RegisterActions();

        Tools.Log("Entrance Initialized.");
    }

    /// <summary>注册 TouchAction 和 TileAction。</summary>
    private static void RegisterActions()
    {
        // 巴士站返回按钮
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
                        TravelTo("BusStop", 22, 10, 2);
                }
            )
        );

        // 票务站目的地选择菜单
        GameLocation.RegisterTileAction("RedPandaBazaar_TicketStation", (_, _, _, _) =>
        {
            var choices = GetDestinationChoices();
            if (choices.Length == 0)
                return false;

            Game1.currentLocation.ShowPagedResponses(
                Tools.GetI18n(I18nKeys.Dialogue_WhereToGo),
                choices.Select(c => KeyValuePair.Create(c.id, c.label)).ToList(),
                selectedId => OnDestinationPicked(selectedId, choices),
                itemsPerPage: 6
            );
            return false;
        });
    }

    /// <summary>处理目的地选择结果。</summary>
    /// <param name="selectedId">玩家选择的目的地 ID。</param>
    /// <param name="choices">由 <see cref="GetDestinationChoices"/> 构建，与菜单打开时一致。</param>
    private static void OnDestinationPicked(string selectedId, DestinationChoice[] choices)
    {
        // 沙漠需要额外检查巴士是否修好
        if (selectedId == "Desert")
        {
            if (!Game1.MasterPlayer.mailReceived.Contains("ccVault"))
            {
                Game1.drawObjectDialogue(
                    Game1.content.LoadString("Strings\\Locations:BusStop_DesertOutOfService"));
                return;
            }
            if (!Tools.TryCharge(500))
                return;

            TravelTo("Desert", 18, 28, 2);
            return;
        }

        // 普通目的地
        foreach (var choice in choices)
        {
            if (choice.id == selectedId && Tools.TryCharge(choice.price))
            {
                TravelTo(choice.mapName, choice.tile.X, choice.tile.Y, choice.facingDirection);
                return;
            }
        }
    }

    /// <summary>构建目的地列表。</summary>
    private static DestinationChoice[] GetDestinationChoices()
    {
        var list = new List<DestinationChoice>();

        list.Add(new DestinationChoice("BusStop", Tools.GetI18n(I18nKeys.Text_PelicanTown),
            "BusStop", new Point(22, 9), 2, 0));

        if (Integrations.Installed.CentralStation)
        {
            list.Add(new DestinationChoice("CentralStation", Tools.GetI18n(I18nKeys.Text_CentralStation),
                "Pathoschild.CentralStation_CentralStation", new Point(60, 13), 2, 0));
        }

        list.Add(new DestinationChoice("Desert",
            Tools.GetI18n(I18nKeys.Text_CalicoDesert) + " (500" + Tools.GetI18n(I18nKeys.Text_Gold) + ")",
            "Desert", new Point(18, 28), 2, 500));

        return list.ToArray();
    }

    /// <summary>传送玩家到指定位置。</summary>
    private static void TravelTo(string location, int x, int y, int facing)
    {
        Game1.pauseThenMessage(1500, null);
        Game1.currentLocation.localSound("busDriveOff");

        var request = Game1.getLocationRequest(location);
        Game1.warpFarmer(request, x, y, facing);
    }

    /// <summary>巴士站按钮交互：进入小熊猫集市。</summary>
    private static void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Tools.IsValidButtonAction(e) || !Game1.currentLocation.Name.Contains("BusStop")) return;

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
                    if (answer == "Positive" && Tools.TryCharge(300))
                        TravelTo("Custom_MapleBridge", 27, 40, 2);
                }
            );
        }
    }

    /// <summary>目的地数据模型。</summary>
    /// <param name="id">唯一标识，用于回调匹配。</param>
    /// <param name="label">菜单显示文本。</param>
    /// <param name="mapName">目标地图内部名。</param>
    /// <param name="tile">目标坐标。</param>
    /// <param name="facingDirection">传送后朝向。</param>
    /// <param name="price">费用，0 为免费。</param>
    private readonly record struct DestinationChoice(
        string id,
        string label,
        string mapName,
        Point tile,
        int facingDirection,
        int price
    );
}
