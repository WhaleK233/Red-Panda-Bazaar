using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Framework.UI;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.PlayerStall;

/// <summary>结算账单菜单：汇总所有摊位已售出物品，一键收取。</summary>
public class SettlementMenu : UiBaseMenu
{
    private const int ColPadding = 8;
    private const int ColGap = 8;
    private const int MinItemW = 120;
    private const int LineH = 28;
    private const int ScrollMaxH = 400;

    private readonly List<SoldEntry> _entries = new();
    private readonly int _totalEarnings;
    private int[] _colWidths = Array.Empty<int>();

    private sealed class SoldEntry
    {
        public string StallName;
        public string ItemName;
        public int Amount;
        public int Price;
        public int UnitPrice;
        public string? SoldDate;
    }

    public SettlementMenu()
    {
        var allSold = PlayerStall.GetSoldItems();
        for (var i = 0; i < allSold.Count; i++)
        {
            var item = allSold[i];
            var obj = ItemRegistry.Create(item.ItemId);
            _entries.Add(new SoldEntry
            {
                StallName = (i + 1).ToString(),
                ItemName = obj.DisplayName,
                Amount = item.Amount,
                UnitPrice = item.Price,
                Price = item.Price * item.Amount,
                SoldDate = item.SoldDate
            });
        }
        _totalEarnings = _entries.Sum(e => e.Price);

        Rebuild();
    }

    protected override void BuildUi()
    {
        var title = Tools.GetI18n(I18nKeys.PlayerStall_BillTitle).ToString();
        Root.Add(new UiText(title, Game1.dialogueFont) { HorizontalAlignment = 0.5f });

        if (_entries.Count == 0)
        {
            BuildEmptyState();
            return;
        }

        BuildTable();
    }

    private void BuildEmptyState()
    {
        var emptyMsg = Tools.GetI18n(I18nKeys.PlayerStall_BillEmpty).ToString();
        Root.Add(new UiText(emptyMsg) { Color = Game1.textColor * 0.6f });

        if (PlayerStall.TotalTax <= 0) return;
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();
        var taxText = Tools.GetI18n(I18nKeys.PlayerStall_TotalTax)
            .Tokens(new { amount = PlayerStall.TotalTax, gold }).ToString();
        Root.Add(new UiText(taxText) { HorizontalAlignment = 1f, Color = Color.Black });
    }

    private void BuildTable()
    {
        var gold = Tools.GetI18n(I18nKeys.Text_Gold).ToString();

        // 计算列宽
        var stallH = Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Stall).ToString();
        var itemH = Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Item).ToString();
        var unitH = Tools.GetI18n(I18nKeys.PlayerStall_UnitPrice).ToString();
        var qtyH = Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Qty).ToString();
        var priceH = Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Price).ToString();
        var timeH = Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Time).ToString();
        var unknownDate = Tools.GetI18n(I18nKeys.PlayerStall_DateUnknown).ToString();

        var stallW = CalcMaxWidth(stallH, e => e.StallName) + ColPadding;
        var qtyW = CalcMaxWidth(qtyH, e => e.Amount.ToString()) + ColPadding;
        var unitW = CalcMaxWidth(unitH, e => $"{e.UnitPrice}{gold}") + ColPadding;
        var priceW = CalcMaxWidth(priceH, e => $"{e.Price}{gold}") + ColPadding;
        var dateW = CalcMaxWidth(timeH,
            e => CalcDaysSinceSold(e.SoldDate)?.ToString() ?? unknownDate) + ColPadding;

        var totalKnownW = stallW + qtyW + unitW + priceW + dateW + ColGap * 4;
        var itemW = Math.Max(MinItemW, _entries.Max(e =>
            (int)Game1.smallFont.MeasureString(e.ItemName).X + ColPadding));
        // 如果总宽超出合理范围，压缩 itemW
        var maxExpectedW = (int)(Game1.uiViewport.Width * 0.55);
        var surplus = totalKnownW + itemW - maxExpectedW;
        if (surplus > 0)
            itemW = Math.Max(MinItemW, itemW - surplus);

        _colWidths = new[] { stallW, itemW, unitW, qtyW, priceW, dateW };

        // 表头
        Root.Add(new UiRow { Spacing = ColGap }
            .Add(Cell(stallH, stallW))
            .Add(Cell(itemH, itemW))
            .Add(Cell(unitH, unitW, right: true))
            .Add(Cell(qtyH, qtyW, right: true))
            .Add(Cell(priceH, priceW, right: true))
            .Add(Cell(timeH, dateW)));

        // 数据行
        var listColumn = new UiColumn();
        foreach (var entry in _entries)
        {
            var dateStr = CalcDaysSinceSold(entry.SoldDate)?.ToString() ?? unknownDate;
            listColumn.Add(new UiRow { Spacing = ColGap }
                .Add(Cell(entry.StallName, stallW))
                .Add(Cell(entry.ItemName, itemW))
                .Add(Cell($"{entry.UnitPrice}{gold}", unitW, right: true))
                .Add(Cell(entry.Amount.ToString(), qtyW, right: true))
                .Add(Cell($"{entry.Price}{gold}", priceW, right: true, bold: true))
                .Add(Cell(dateStr, dateW, bold: true)));
        }
        Root.Add(new UiScrollContainer { Child = listColumn, MaxHeight = ScrollMaxH });

        // 分隔线
        var totalW = _colWidths.Sum() + ColGap * (_colWidths.Length - 1);
        Root.Add(new UiSeparator { Width = totalW });

        // 税收
        var taxAmount = (int)Math.Round(_totalEarnings * Tools.ModConfig.TaxRate);
        if (Tools.ModConfig.EnableTax && taxAmount > 0)
        {
            var taxPct = (int)(Tools.ModConfig.TaxRate * 100);
            var taxText = Tools.GetI18n(I18nKeys.PlayerStall_TaxLine)
                .Tokens(new { amount = taxAmount, gold, rate = taxPct }).ToString();
            Root.Add(new UiText(taxText) { HorizontalAlignment = 1f, Color = Color.Red });
        }

        // 合计
        var net = Tools.ModConfig.EnableTax ? _totalEarnings - taxAmount : _totalEarnings;
        var totalText = Tools.GetI18n(I18nKeys.PlayerStall_Total)
            .Tokens(new { amount = net, gold }).ToString();
        Root.Add(new UiText(totalText, Game1.dialogueFont) { HorizontalAlignment = 1f });

        // 按钮
        var collected = PlayerStall.IsCollectedToday;
        var btnText = collected
            ? Tools.GetI18n(I18nKeys.PlayerStall_Collected).ToString()
            : Tools.GetI18n(I18nKeys.PlayerStall_CollectAll).ToString();
        Root.Add(new UiButton(btnText, OnCollect) { Enabled = !collected });

        // 累计已交税额
        if (PlayerStall.TotalTax > 0)
        {
            var totalTaxText = Tools.GetI18n(I18nKeys.PlayerStall_TotalTax)
                .Tokens(new { amount = PlayerStall.TotalTax, gold }).ToString();
            Root.Add(new UiText(totalTaxText) { HorizontalAlignment = 1f, Color = Color.Black });
        }
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
        Rebuild();
    }

    protected override Point CalcContentSize()
    {
        if (_entries.Count == 0)
            return new Point(300, 80);

        var totalW = _colWidths.Sum() + ColGap * (_colWidths.Length - 1);
        return new Point(totalW + ContentPadding * 2, 120 + ScrollMaxH);
    }

    // ---- 辅助方法 ----

    private static UiText Cell(string text, int width, bool right = false, bool bold = false)
    {
        return new UiText(text)
        {
            MinWidth = width,
            HorizontalAlignment = right ? 1f : 0f,
            Color = bold ? Color.Black : Game1.textColor
        };
    }

    private int CalcMaxWidth(string header, Func<SoldEntry, string> valueFn)
    {
        var w = (int)Game1.smallFont.MeasureString(header).X;
        foreach (var entry in _entries)
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
