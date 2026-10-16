using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewUI;
using StardewUI.Graphics;
using StardewUI.Layout;
using StardewUI.Widgets;
using StardewValley;
using WhaleK.Common;

namespace Red_Panda_Bazaar_Code.Features.PlayerStall;

/// <summary>结算账单菜单：汇总所有摊位已售出物品，一键收取。</summary>
public class SettlementMenu : ViewMenu
{
    private const int ColPadding = 8;
    private const int ColGap = 8;
    private const int MinItemW = 120;
    private const int ScrollMaxH = 400;

    /// <summary>表格可视宽度；右侧额外留出滚动条位置。</summary>
    private const int TableW = 540;

    /// <summary>内容区宽度上限：表格宽度 + 滚动条预留。</summary>
    private const int ContentW = 600;

    /// <summary>表头文字色（对齐原框架主题的 Accent）。</summary>
    private static readonly Color HeaderColor = new(87, 58, 24);

    private Lane? _content;

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

        _content = MenuUi.Column(ContentW, 640);
        BuildContent();
        return MenuUi.Window(_content);
    }

    /// <summary>按当前已售记录重建内容（收取后调用）。</summary>
    private void BuildContent()
    {
        if (_content is null)
        {
            return;
        }
        _content.Children.Clear();

        var entries = LoadEntries();

        _content.Children.Add(
            MenuUi.TextRow(
                Tools.GetI18n(I18nKeys.PlayerStall_BillTitle).ToString(),
                Game1.dialogueFont,
                align: Alignment.Middle
            )
        );

        if (entries.Count == 0)
        {
            _content.Children.Add(
                MenuUi.TextRow(
                    Tools.GetI18n(I18nKeys.PlayerStall_BillEmpty).ToString(),
                    color: Game1.textColor * 0.6f,
                    topMargin: MenuUi.RowGap
                )
            );
            return;
        }

        BuildTable(entries, entries.Sum(e => e.Price));
    }

    private void BuildTable(List<SoldEntry> entries, int totalEarnings)
    {
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();
        var unknownDate = Tools.GetI18n(I18nKeys.PlayerStall_DateUnknown).ToString();

        // 列标题
        var stallH = Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Stall).ToString();
        var itemH = Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Item).ToString();
        var unitH = Tools.GetI18n(I18nKeys.PlayerStall_UnitPrice).ToString();
        var qtyH = Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Qty).ToString();
        var priceH = Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Price).ToString();
        var timeH = Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Time).ToString();

        // 计算列宽
        var stallW = CalcMaxWidth(entries, stallH, e => e.StallName) + ColPadding;
        var qtyW = CalcMaxWidth(entries, qtyH, e => e.Amount.ToString()) + ColPadding;
        var unitW = CalcMaxWidth(entries, unitH, e => $"{e.UnitPrice}{gold}") + ColPadding;
        var priceW = CalcMaxWidth(entries, priceH, e => $"{e.Price}{gold}") + ColPadding;
        var dateW = CalcMaxWidth(entries, timeH, e => CalcDaysSinceSold(e.SoldDate)?.ToString() ?? unknownDate) + ColPadding;

        var totalKnownW = stallW + qtyW + unitW + priceW + dateW + ColGap * 4;
        var itemW = Math.Max(MinItemW, entries.Max(e => (int)Game1.smallFont.MeasureString(e.ItemName).X + ColPadding));
        var surplus = totalKnownW + itemW - TableW;
        if (surplus > 0)
        {
            itemW = Math.Max(MinItemW, itemW - surplus);
        }

        var widths = new[] { stallW, itemW, unitW, qtyW, priceW, dateW };

        var table = new Lane
        {
            Orientation = Orientation.Vertical,
            Layout = new() { Width = Length.Stretch(), Height = Length.Content() },
        };
        table.Children.Add(
            BuildTableRow(new[] { stallH, itemH, unitH, qtyH, priceH, timeH }, widths, HeaderColor)
        );
        table.Children.Add(MenuUi.Divider(0));
        foreach (var entry in entries)
        {
            var dateStr = CalcDaysSinceSold(entry.SoldDate)?.ToString() ?? unknownDate;
            table.Children.Add(
                BuildTableRow(
                    new[]
                    {
                        entry.StallName,
                        entry.ItemName,
                        $"{entry.UnitPrice}{gold}",
                        entry.Amount.ToString(),
                        $"{entry.Price}{gold}",
                        dateStr,
                    },
                    widths,
                    Game1.textColor,
                    brightFrom: 4
                )
            );
        }

        // 表格可滚动区：高度不超过上限，超出部分滚动
        var naturalH = (entries.Count + 1) * Game1.smallFont.LineSpacing;
        var tableRow = MenuUi.Row(topMargin: MenuUi.RowGap);
        tableRow.Children.Add(
            new ScrollableView
            {
                Layout = new() { Width = Length.Px(TableW), Height = Length.Px(Math.Min(naturalH, ScrollMaxH)) },
                Content = table,
                Peeking = 16,
            }
        );
        _content!.Children.Add(tableRow);

        // 分隔线
        _content.Children.Add(MenuUi.Divider());

        // 合计
        var totalText = Tools.GetI18n(I18nKeys.PlayerStall_Total)
            .Tokens(new { amount = totalEarnings, gold })
            .ToString();
        _content.Children.Add(
            MenuUi.TextRow(totalText, Game1.dialogueFont, align: Alignment.End, topMargin: MenuUi.RowGap)
        );

        // 按钮
        var collected = PlayerStall.IsCollectedToday;
        var btnText = collected
            ? Tools.GetI18n(I18nKeys.PlayerStall_Collected).ToString()
            : Tools.GetI18n(I18nKeys.PlayerStall_CollectAll).ToString();
        var button = MenuUi.Button(btnText, OnCollect);
        if (collected)
        {
            button.PointerEventsEnabled = false;
        }
        var buttonRow = MenuUi.Row(topMargin: MenuUi.RowGap);
        buttonRow.Children.Add(button);
        _content.Children.Add(buttonRow);
    }

    /// <summary>构建一行表格：单元格按列宽居中；<paramref name="brightFrom"/> 起的列用亮黑（对齐原框架的加粗列）。</summary>
    private static Lane BuildTableRow(string[] cells, int[] widths, Color color, int brightFrom = int.MaxValue)
    {
        var row = MenuUi.Row();
        for (var i = 0; i < cells.Length; i++)
        {
            var label = MenuUi.Text(cells[i], color: i >= brightFrom ? Color.Black : color);
            label.Layout = new() { Width = Length.Px(widths[i]), Height = Length.Content() };
            label.HorizontalAlignment = Alignment.Middle;
            label.Margin = new(Right: ColGap);
            row.Children.Add(label);
        }
        return row;
    }

    private static List<SoldEntry> LoadEntries()
    {
        var entries = new List<SoldEntry>();
        foreach (var item in PlayerStall.GetSoldItems())
        {
            var obj = item.GetItem();
            if (obj == null) continue; // 快照失效的记录直接跳过

            entries.Add(
                new SoldEntry
                {
                    StallName = (entries.Count + 1).ToString(),
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

    private void OnCollect()
    {
        var netEarnings = PlayerStall.TryCollectToday();
        if (netEarnings >= 0)
        {
            Game1.player.Money += netEarnings;
            Game1.playSound("coin");
        }
        else
        {
            Game1.playSound("smallSelect");
        }
        BuildContent();
    }

    // ---- 辅助方法 ----

    private static int CalcMaxWidth(List<SoldEntry> entries, string header, Func<SoldEntry, string> valueFn)
    {
        var w = (int)Game1.smallFont.MeasureString(header).X;
        foreach (var entry in entries)
        {
            var m = (int)Game1.smallFont.MeasureString(valueFn(entry)).X;
            if (m > w) w = m;
        }
        return w;
    }

    private static int? CalcDaysSinceSold(string? soldDate)
    {
        if (string.IsNullOrEmpty(soldDate)) return null;
        var parts = soldDate.Split('_');
        if (parts.Length < 3) return null;
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
        if (seasonIdx is < 0 or > 3) return null;

        var totalDays = (year - 1) * 112 + seasonIdx * 28 + day;
        return (int)Game1.stats.DaysPlayed - totalDays;
    }
}
