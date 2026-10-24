using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewUI;
using StardewUI.Graphics;
using StardewUI.Layout;
using StardewUI.Widgets;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.PlayerStall;

/// <summary>结算账单菜单：汇总所有摊位已售出物品，一键收取。</summary>
public class SettlementMenu : ViewMenu
{
    /// <summary>Grid 以六列平均分配可用宽度，窗口变宽时同步伸展、窄屏时保持列间距而不溢出。</summary>
    private const int ColumnCount = 6;

    /// <summary>各列水平对齐：文字列靠左，数字与金额列靠右。</summary>
    private static readonly Alignment[] ColumnAligns =
    {
        Alignment.Start,
        Alignment.Start,
        Alignment.End,
        Alignment.End,
        Alignment.End,
        Alignment.End,
    };

    /// <summary>滚动区高度上限：行数多时表格内部滚动，菜单整体不超出屏幕。</summary>
    private const int ScrollMaxH = 400;

    /// <summary>客机收取请求的回执等待上限（游戏帧，约 10 秒）：超时解除按钮禁用，允许玩家重试。</summary>
    private const int PendingTimeoutTicks = 600;

    /// <summary>单元格之间的横向留白。</summary>
    private const int CellGap = 8;

    private Lane? _content;

    /// <summary>客机收取请求已发出、等待主机回执期间为真：按钮转为禁用的「收取中」。</summary>
    private bool _collectPending;

    /// <summary>已等待的帧数，用于回执超时判定。</summary>
    private int _pendingTicks;

    public override void update(GameTime time)
    {
        base.update(time);
        // 客机收取是异步请求-回执：轮询收取状态，主机广播到达后刷新为「已收取」
        if (_collectPending && PlayerStall.IsCollectedToday)
        {
            _collectPending = false;
            BuildContent();
        }
        else if (_collectPending && ++_pendingTicks >= PendingTimeoutTicks)
        {
            // 回执一直未到（主机掉线或消息丢弃）：解除禁用允许重试；主机结算按天幂等，重试不会重复入账
            _collectPending = false;
            _pendingTicks = 0;
            Game1.addHUDMessage(new HUDMessage(
                Tools.GetI18n(I18nKeys.PlayerStall_CollectTimeout).ToString(), HUDMessage.error_type));
            BuildContent();
        }
    }

    private sealed class SoldEntry
    {
        public string StallName = "";
        public string ItemName = "";
        public int Amount;
        public int Price;
        public int UnitPrice;
        public string? SoldDate;
    }

    protected override IView CreateView()
    {
        CloseButtonSprite = UiSprites.CloseButton;

        _content = MenuUi.Column();
        _content.Children.Add(MenuUi.Header(Tools.GetI18n(I18nKeys.PlayerStall_BillTitle).ToString()));
        BuildContent();
        return MenuUi.Window(_content);
    }

    /// <summary>按当前已售记录重建内容（收取后调用）。</summary>
    private void BuildContent()
    {
        var content = _content;
        if (content is null)
        {
            return;
        }
        while (content.Children.Count > 1)
        {
            content.Children.RemoveAt(content.Children.Count - 1);
        }

        var entries = LoadEntries();
        if (entries.Count == 0)
        {
            content.Children.Add(
                MenuUi.Card(
                    MenuUi.TextRow(Tools.GetI18n(I18nKeys.PlayerStall_BillEmpty).ToString(), color: MenuUi.Muted),
                    topMargin: MenuUi.SectionGap
                )
            );
            return;
        }

        BuildTable(content, entries, entries.Sum(e => e.Price));
    }

    private void BuildTable(Lane content, List<SoldEntry> entries, int totalEarnings)
    {
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();
        var unknownDate = Tools.GetI18n(I18nKeys.PlayerStall_DateUnknown).ToString();

        // 单一 Grid 承载表头与数据行；Count(6) 让列宽随窗口可用宽度自适应并始终对齐。
        var table = new Grid
        {
            ItemLayout = new GridItemLayout.Count(ColumnCount),
            ItemSpacing = new Vector2(CellGap, MenuUi.RowGap),
            HorizontalItemAlignment = Alignment.Start,
            VerticalItemAlignment = Alignment.Middle,
            Layout = new() { Width = Length.Stretch(), Height = Length.Content() },
        };
        AddTableCells(
            table,
            new[]
            {
                Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Stall).ToString(),
                Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Item).ToString(),
                Tools.GetI18n(I18nKeys.PlayerStall_UnitPrice).ToString(),
                Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Qty).ToString(),
                Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Price).ToString(),
                Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Time).ToString(),
            },
            MenuUi.Accent,
            bold: true
        );
        foreach (var entry in entries)
        {
            AddTableCells(
                table,
                new[]
                {
                    entry.StallName,
                    entry.ItemName,
                    $"{entry.UnitPrice}{gold}",
                    entry.Amount.ToString(),
                    $"{entry.Price}{gold}",
                    CalcDaysSinceSold(entry.SoldDate)?.ToString() ?? unknownDate,
                },
                Color.Black,
                topMargin: MenuUi.RowGap
            );
        }

        var naturalH = (entries.Count + 1) * (Game1.smallFont.LineSpacing + MenuUi.RowGap);
        content.Children.Add(
            MenuUi.Card(
                new ScrollableView
                {
                    Layout = new()
                    {
                        Width = Length.Stretch(),
                        Height = Length.Px(Math.Min(naturalH, ScrollMaxH)),
                    },
                    Content = table,
                    Peeking = 16,
                },
                topMargin: MenuUi.SectionGap
            )
        );

        // 合计：右对齐大字，与下方收取按钮形成“确认收款”的动线。
        content.Children.Add(
            MenuUi.TextRow(
                Tools.GetI18n(I18nKeys.PlayerStall_Total).Tokens(new { amount = totalEarnings, gold }).ToString(),
                Game1.dialogueFont,
                align: Alignment.End,
                topMargin: MenuUi.SectionGap
            )
        );

        var collected = PlayerStall.IsCollectedToday;
        var pending = _collectPending && !collected;
        var button = collected
            ? MenuUi.SecondaryButton(Tools.GetI18n(I18nKeys.PlayerStall_Collected).ToString(), OnCollect)
            : pending
                ? MenuUi.SecondaryButton(Tools.GetI18n(I18nKeys.PlayerStall_CollectPending).ToString(), OnCollect)
                : MenuUi.PrimaryButton(Tools.GetI18n(I18nKeys.PlayerStall_CollectAll).ToString(), OnCollect);
        if (collected || pending)
        {
            button.PointerEventsEnabled = false;
        }
        var buttonRow = MenuUi.Row(topMargin: MenuUi.RowGap);
        buttonRow.Children.Add(button);
        content.Children.Add(buttonRow);
    }

    /// <summary>把一行单元格加入统一 Grid；每个 Label 的宽度撑满自己的响应式网格格子。</summary>
    private static void AddTableCells(Grid table, string[] cells, Color color, int topMargin = 0, bool bold = false)
    {
        for (var i = 0; i < cells.Length; i++)
        {
            var label = MenuUi.Text(cells[i], color: color, maxLines: 2, bold: bold, topMargin: topMargin);
            label.Layout = new() { Width = Length.Stretch(), Height = Length.Content() };
            label.HorizontalAlignment = ColumnAligns[i];
            if (i < cells.Length - 1)
            {
                label.Padding = new(Right: CellGap);
            }
            table.Children.Add(label);
        }
    }

    private static List<SoldEntry> LoadEntries()
    {
        var entries = new List<SoldEntry>();
        foreach (var item in PlayerStall.GetSoldItems())
        {
            var obj = item.GetItem();
            if (obj == null)
                continue; // 快照失效的记录直接跳过

            entries.Add(
                new SoldEntry
                {
                    StallName = GetStallLabel(item.ActionId),
                    ItemName = obj.DisplayName,
                    Amount = obj.Stack,
                    UnitPrice = item.Price,
                    Price = item.Price * obj.Stack,
                    SoldDate = item.SoldDate,
                }
            );
        }
        return entries;
    }

    /// <summary>摊位号取 Action ID 尾段（RedPandaBazaar_PlayerStall_{n}）；解析失败原样展示 ID。</summary>
    private static string GetStallLabel(string actionId)
    {
        var idx = actionId.LastIndexOf('_');
        return idx >= 0 && int.TryParse(actionId[(idx + 1)..], out var n) ? n.ToString() : actionId;
    }

    private void OnCollect()
    {
        var netEarnings = PlayerStall.TryCollectToday();
        if (netEarnings >= 0)
        {
            Game1.player.Money += netEarnings;
            Game1.playSound("coin");
        }
        else if (netEarnings == PlayerStall.CollectPending)
        {
            // 请求已发往主机：按钮转「收取中」，入账与刷新由 update 轮询完成
            _collectPending = true;
            _pendingTicks = 0;
        }
        else
        {
            Game1.playSound("smallSelect");
        }
        BuildContent();
    }

    private static int? CalcDaysSinceSold(string? soldDate)
    {
        if (string.IsNullOrEmpty(soldDate))
            return null;
        var parts = soldDate.Split('_');
        if (parts.Length < 3)
            return null;
        if (!int.TryParse(parts[1], out var day) || !int.TryParse(parts[2], out var year))
            return null;

        var seasonIdx = -1;
        if (int.TryParse(parts[0], out var idx))
            seasonIdx = idx;
        else
        {
            var season = parts[0].Trim();
            seasonIdx = season.Equals("Spring", StringComparison.OrdinalIgnoreCase) ? 0
                : season.Equals("Summer", StringComparison.OrdinalIgnoreCase) ? 1
                : season.Equals("Fall", StringComparison.OrdinalIgnoreCase) ? 2
                : season.Equals("Winter", StringComparison.OrdinalIgnoreCase) ? 3
                : -1;
        }
        if (seasonIdx is < 0 or > 3)
            return null;

        var totalDays = (year - 1) * 112 + seasonIdx * 28 + day;
        return (int)Game1.stats.DaysPlayed - totalDays;
    }
}
