using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Features.PlayerStall;

/// <summary>结算账单菜单：汇总所有摊位已售出物品，一键收取。列表支持滚轮滚动。</summary>
public class SettlementMenu : IClickableMenu {
    private const int MenuWidth = 520;
    private const int Pad = 32;
    private const int ColPadding = 8;
    private const int ColGap = 8;
    private const int RightPad = 48;
    private const int MinItemW = 120;
    private const int LineH = 28;
    private const int ScrollArrowSize = 24;

    private readonly ClickableComponent _collectBtn;
    private readonly ClickableComponent _scrollUpBtn;
    private readonly ClickableComponent _scrollDownBtn;
    private readonly List<SoldEntry> _entries = new();
    private readonly int _totalEarnings;
    private readonly int _maxVisible;
    private bool _collected;
    private int _scrollIndex;

    private class SoldEntry {
        public string StallName;
        public string ItemName;
        public int Amount;
        public int Price;
        public int UnitPrice;
        public string? SoldDate;
    }

    public SettlementMenu()
        : base(
            (int)Utility.getTopLeftPositionForCenteringOnScreen(MenuWidth, 0).X, 0,
            MenuWidth, 0, true) {
                var allSold = PlayerStall.GetSoldItems();
        for (var i = 0; i < allSold.Count; i++) {
            var item = allSold[i];
            var obj = ItemRegistry.Create(item.ItemId);
            _entries.Add(new SoldEntry {
                StallName = (i + 1).ToString(),
                ItemName = obj.DisplayName,
                Amount = item.Amount,
                UnitPrice = item.Price,
                Price = item.Price * item.Amount,
                SoldDate = item.SoldDate
            });
        }
        _totalEarnings = _entries.Sum(e => e.Price);

        _collected = PlayerStall.IsCollectedToday;

        // 自适应菜单宽度（按列宽计算）
        if (_entries.Count > 0) {
            var goldSuffix = Tools.GetI18n(I18nKeys.Text_Gold);
            var stallHeader = Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Stall).ToString();
            var qtyHeader = Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Qty).ToString();
            var unitHeader = Tools.GetI18n(I18nKeys.PlayerStall_UnitPrice).ToString();
            var priceHeader = Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Price).ToString();
            var timeHeader = Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Time).ToString();

            var stallW = (int)Math.Max(Game1.smallFont.MeasureString(stallHeader).X + ColPadding,
                _entries.Max(e => Game1.smallFont.MeasureString(e.StallName).X) + ColPadding);

            var qtyW = (int)Game1.smallFont.MeasureString(qtyHeader).X + ColPadding;
            foreach (var e in _entries) {
                var w = (int)Game1.smallFont.MeasureString(e.Amount.ToString()).X + ColPadding;
                if (w > qtyW) qtyW = w;
            }

            var unitW = (int)Game1.smallFont.MeasureString(unitHeader).X + ColPadding;
            foreach (var e in _entries) {
                var w = (int)Game1.smallFont.MeasureString($"{e.UnitPrice}{goldSuffix}").X + ColPadding;
                if (w > unitW) unitW = w;
            }

            var priceW = (int)Game1.smallFont.MeasureString(priceHeader).X + ColPadding;
            foreach (var e in _entries) {
                var w = (int)Game1.smallFont.MeasureString($"{e.Price}{goldSuffix}").X + ColPadding;
                if (w > priceW) priceW = w;
            }

            var unknownText = Tools.GetI18n(I18nKeys.PlayerStall_DateUnknown).ToString();
            var dateW = (int)Math.Max(Game1.smallFont.MeasureString(timeHeader).X + ColPadding,
                Game1.smallFont.MeasureString(unknownText).X + ColPadding);
            foreach (var e in _entries) {
                var d = CalcDaysSinceSold(e.SoldDate)?.ToString() ?? unknownText;
                var w = (int)Game1.smallFont.MeasureString(d).X + ColPadding;
                if (w > dateW) dateW = w;
            }

            var itemW = (int)Math.Max(MinItemW,
                _entries.Max(e => Game1.smallFont.MeasureString(e.ItemName).X) + ColPadding);
            var minContentWidth = stallW + itemW + unitW + qtyW + priceW + dateW + ColGap * 5;
            var neededWidth = minContentWidth + Pad + RightPad;
            if (neededWidth > width) {
                width = neededWidth;
                xPositionOnScreen = (int)Utility.getTopLeftPositionForCenteringOnScreen(width, 0).X;
            }
        }

        // 自适应高度
        var maxH = (int)(Game1.graphics.GraphicsDevice.Viewport.Height * 0.8);
        var topFixed = 100;
        var bottomFixed = 8 + 4 + 28 + 28 + 56 + 40;
        if (Tools.ModConfig.EnableTax) bottomFixed += 20 + 28;
        var desiredH = topFixed + Math.Max(_entries.Count, 1) * LineH + bottomFixed;
        height = Math.Min(desiredH, maxH);
        yPositionOnScreen = (int)Utility.getTopLeftPositionForCenteringOnScreen(MenuWidth, height).Y;

        // 是否需要滚动
        var needsScroll = desiredH > maxH;
        _maxVisible = needsScroll ? Math.Max(1, (height - topFixed - bottomFixed) / LineH) : _entries.Count;

        var btnText = Tools.GetI18n(I18nKeys.PlayerStall_CollectAll).ToString();
        var btnWidth = (int)Game1.smallFont.MeasureString(btnText).X + 24;
        _collectBtn = new ClickableComponent(
            new Rectangle(xPositionOnScreen + (width - btnWidth) / 2, yPositionOnScreen + height - 80,
                btnWidth, 40), "collect");

        var arrowX = xPositionOnScreen + width - 24;
        var arrowStartY = yPositionOnScreen + 72 + LineH;
        // 扩大点击范围方便触屏，绘制保持原大小
        _scrollUpBtn = new ClickableComponent(
            new Rectangle(arrowX - 12, arrowStartY - 8, ScrollArrowSize + 24, ScrollArrowSize + 16), "up");
        _scrollDownBtn = new ClickableComponent(
            new Rectangle(arrowX - 12, arrowStartY + ScrollArrowSize - 8, ScrollArrowSize + 24, ScrollArrowSize + 16), "down");

        // 关闭按钮移到右上角外侧
        if (upperRightCloseButton != null) {
            upperRightCloseButton.bounds.X = xPositionOnScreen + width - 16;
            upperRightCloseButton.bounds.Y = yPositionOnScreen - 24;
        }

        // 控制器/键盘导航
        const int IdCollect = 501;
        const int IdScrollUp = 502;
        const int IdScrollDown = 503;

        _collectBtn.myID = IdCollect;
        _collectBtn.upNeighborID = IdScrollDown;
        _collectBtn.downNeighborID = IdCollect;

        _scrollUpBtn.myID = IdScrollUp;
        _scrollUpBtn.downNeighborID = IdScrollDown;

        _scrollDownBtn.myID = IdScrollDown;
        _scrollDownBtn.upNeighborID = IdScrollUp;
        _scrollDownBtn.downNeighborID = IdCollect;

        allClickableComponents = new();
        allClickableComponents.Add(_collectBtn);
        allClickableComponents.Add(_scrollUpBtn);
        allClickableComponents.Add(_scrollDownBtn);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true) {
        if (_collectBtn.bounds.Contains(x, y) && !_collected) {
            var netEarnings = PlayerStall.TryCollectToday();
            if (netEarnings >= 0) {
                Game1.player.Money += netEarnings;
                Game1.playSound("coin");
                _collected = true;
            } else if (netEarnings == PlayerStall.CollectPending) {
                _collected = true;
                Game1.playSound("smallSelect");
            }
            if (_collected) return;
            return;
        }

        if (_scrollUpBtn.bounds.Contains(x, y) && _scrollIndex > 0) {
            _scrollIndex--;
            Game1.playSound("shiny4");
            return;
        }

        if (_scrollDownBtn.bounds.Contains(x, y) && _scrollIndex + _maxVisible < _entries.Count) {
            _scrollIndex++;
            Game1.playSound("shiny4");
            return;
        }

        base.receiveLeftClick(x, y, playSound);
    }

    public override void receiveScrollWheelAction(int direction) {
        if (direction > 0 && _scrollIndex > 0)
            _scrollIndex--;
        else if (direction < 0 && _scrollIndex + _maxVisible < _entries.Count)
            _scrollIndex++;
        base.receiveScrollWheelAction(direction);
    }

    public override void receiveGamePadButton(Buttons b) {
        switch (b) {
            case Buttons.LeftTrigger:
            case Buttons.LeftShoulder:
                if (_scrollIndex > 0) { _scrollIndex--; Game1.playSound("shiny4"); }
                break;
            case Buttons.RightTrigger:
            case Buttons.RightShoulder:
                if (_scrollIndex + _maxVisible < _entries.Count) { _scrollIndex++; Game1.playSound("shiny4"); }
                break;
            default: base.receiveGamePadButton(b); break;
        }
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) {
        Game1.activeClickableMenu = new SettlementMenu();
    }

    public override void draw(SpriteBatch b) {
        // 刷新全局状态，同步多人收取
        if (!_collected && PlayerStall.IsCollectedToday)
            _collected = true;

        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);

        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
            xPositionOnScreen, yPositionOnScreen, width, height, Color.White, 4f);

        var title = Tools.GetI18n(I18nKeys.PlayerStall_BillTitle).ToString();
        var titlePos = new Vector2(xPositionOnScreen + (width - Game1.dialogueFont.MeasureString(title).X) / 2,
            yPositionOnScreen + 28);
        Utility.drawTextWithShadow(b, title, Game1.dialogueFont, titlePos, Game1.textColor);

        if (_entries.Count == 0) {
            var emptyMsg = Tools.GetI18n(I18nKeys.PlayerStall_BillEmpty).ToString();
            var emptyPos = new Vector2(
                xPositionOnScreen + (width - Game1.smallFont.MeasureString(emptyMsg).X) / 2,
                yPositionOnScreen + 80);
            Utility.drawTextWithShadow(b, emptyMsg, Game1.smallFont, emptyPos, Game1.textColor * 0.6f);
            // 即使无账单也显示历史总税收
            if (PlayerStall.TotalTax > 0) {
                var g2 = Tools.GetI18n(I18nKeys.Text_Gold);
                var cr = xPositionOnScreen + width - Pad;
                var ttt = Tools.GetI18n(I18nKeys.PlayerStall_TotalTax)
                    .Tokens(new { amount = PlayerStall.TotalTax, gold = g2 }).ToString();
                Utility.drawTextWithShadow(b, ttt, Game1.smallFont,
                    new Vector2(cr - Game1.smallFont.MeasureString(ttt).X,
                        yPositionOnScreen + height - 28), Color.Black);
            }

            base.draw(b);
            drawMouse(b);
            return;
        }

        // 测量列宽
        var contentLeft = xPositionOnScreen + Pad;
        var contentRight = xPositionOnScreen + width - RightPad;

        var goldSuffix = Tools.GetI18n(I18nKeys.Text_Gold);
        var stallHeader = Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Stall).ToString();
        var itemHeader = Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Item).ToString();
        var qtyHeader = Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Qty).ToString();
        var unitHeader = Tools.GetI18n(I18nKeys.PlayerStall_UnitPrice).ToString();
        var priceHeader = Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Price).ToString();
        var timeHeader = Tools.GetI18n(I18nKeys.PlayerStall_ColHeader_Time).ToString();

        var stallW = (int)Math.Max(Game1.smallFont.MeasureString(stallHeader).X + ColPadding,
            _entries.Max(e => Game1.smallFont.MeasureString(e.StallName).X) + ColPadding);

        var qtyW = (int)Game1.smallFont.MeasureString(qtyHeader).X + ColPadding;
        foreach (var e in _entries) {
            var w = (int)Game1.smallFont.MeasureString(e.Amount.ToString()).X + ColPadding;
            if (w > qtyW) qtyW = w;
        }

        var unitW = (int)Game1.smallFont.MeasureString(unitHeader).X + ColPadding;
        foreach (var e in _entries) {
            var w = (int)Game1.smallFont.MeasureString($"{e.UnitPrice}{goldSuffix}").X + ColPadding;
            if (w > unitW) unitW = w;
        }

        var priceW = (int)Game1.smallFont.MeasureString(priceHeader).X + ColPadding;
        foreach (var e in _entries) {
            var w = (int)Game1.smallFont.MeasureString($"{e.Price}{goldSuffix}").X + ColPadding;
            if (w > priceW) priceW = w;
        }

        var unknownText = Tools.GetI18n(I18nKeys.PlayerStall_DateUnknown).ToString();
        var dateW = (int)Math.Max(Game1.smallFont.MeasureString(timeHeader).X + ColPadding,
            Game1.smallFont.MeasureString(unknownText).X + ColPadding);
        foreach (var e in _entries) {
            var d = CalcDaysSinceSold(e.SoldDate)?.ToString() ?? unknownText;
            var w = (int)Game1.smallFont.MeasureString(d).X + ColPadding;
            if (w > dateW) dateW = w;
        }

        var itemW = Math.Max(contentRight - contentLeft - ColGap * 5 - stallW - qtyW - unitW - priceW - dateW,
            _entries.Max(e => Game1.smallFont.MeasureString(e.ItemName).X) + ColPadding);
        if (itemW < MinItemW) itemW = MinItemW;

        var stallCol = contentLeft;
        var itemCol = stallCol + stallW + ColGap;
        var unitCol = itemCol + itemW + ColGap;
        var qtyCol = unitCol + unitW + ColGap;
        var priceCol = qtyCol + qtyW + ColGap;
        var dateCol = priceCol + priceW + ColGap;

        // 表头
        var yPos = yPositionOnScreen + 72;
        Utility.drawTextWithShadow(b, stallHeader, Game1.smallFont,
            new Vector2(stallCol, yPos), Game1.textColor * 0.7f);
        Utility.drawTextWithShadow(b, itemHeader, Game1.smallFont,
            new Vector2(itemCol, yPos), Game1.textColor * 0.7f);
        Utility.drawTextWithShadow(b, unitHeader, Game1.smallFont,
            new Vector2(unitCol, yPos), Game1.textColor * 0.7f);
        Utility.drawTextWithShadow(b, qtyHeader, Game1.smallFont,
            new Vector2(qtyCol, yPos), Game1.textColor * 0.7f);
        Utility.drawTextWithShadow(b, priceHeader, Game1.smallFont,
            new Vector2(priceCol, yPos), Game1.textColor * 0.7f);
        Utility.drawTextWithShadow(b, timeHeader, Game1.smallFont,
            new Vector2(dateCol, yPos), Game1.textColor * 0.7f);

        yPos += LineH;
        var listTop = yPos;

        // 可见条目
        var endIdx = Math.Min(_scrollIndex + _maxVisible, _entries.Count);
        for (var i = _scrollIndex; i < endIdx; i++) {
            var entry = _entries[i];
            Utility.drawTextWithShadow(b, entry.StallName, Game1.smallFont,
                new Vector2(stallCol, yPos), Game1.textColor);
            Utility.drawTextWithShadow(b, entry.ItemName, Game1.smallFont,
                new Vector2(itemCol, yPos), Game1.textColor);
            Utility.drawTextWithShadow(b, $"{entry.UnitPrice}{goldSuffix}", Game1.smallFont,
                new Vector2(unitCol, yPos), Game1.textColor);
            Utility.drawTextWithShadow(b, entry.Amount.ToString(), Game1.smallFont,
                new Vector2(qtyCol, yPos), Game1.textColor);
            Utility.drawTextWithShadow(b, $"{entry.Price}{goldSuffix}", Game1.smallFont,
                new Vector2(priceCol, yPos), Color.Black);
            var dateStr = CalcDaysSinceSold(entry.SoldDate)?.ToString() ?? unknownText;
            Utility.drawTextWithShadow(b, dateStr, Game1.smallFont,
                new Vector2(dateCol, yPos), Color.Black);
            yPos += LineH;
        }

        // 滚动箭头（以条目为单位）
        var canScrollUp = _scrollIndex > 0;
        var canScrollDown = _scrollIndex + _maxVisible < _entries.Count;
        _scrollUpBtn.bounds.Y = listTop;
        _scrollDownBtn.bounds.Y = listTop + LineH;

        if (canScrollUp)
            b.Draw(Game1.mouseCursors, new Vector2(_scrollUpBtn.bounds.X, _scrollUpBtn.bounds.Y),
                new Rectangle(421, 459, 11, 12), Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0.01f);
        if (canScrollDown)
            b.Draw(Game1.mouseCursors, new Vector2(_scrollDownBtn.bounds.X, _scrollDownBtn.bounds.Y),
                new Rectangle(421, 472, 11, 12), Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0.01f);

        // 分割线（文本渲染，与内容区等宽）
        yPos = Math.Max(yPos + 8, listTop + _maxVisible * LineH + 8);
        // 使用矩形绘制分割线，避免受字体宽度影响
        var lineWidth = (int)(contentRight - contentLeft);
        b.Draw(Game1.fadeToBlackRect,
            new Rectangle((int)contentLeft, (int)yPos, lineWidth, 3),
            Color.Black * 0.5f);

        // 税收行
        var taxAmount = (int)Math.Round(_totalEarnings * Tools.ModConfig.TaxRate);
        if (Tools.ModConfig.EnableTax && taxAmount > 0) {
            yPos += 20;
            var taxPct = (int)(Tools.ModConfig.TaxRate * 100);
            var taxText = Tools.GetI18n(I18nKeys.PlayerStall_TaxLine)
                .Tokens(new { amount = taxAmount, gold = goldSuffix, rate = taxPct }).ToString();
            Utility.drawTextWithShadow(b, taxText, Game1.smallFont,
                new Vector2(contentRight - Game1.smallFont.MeasureString(taxText).X, yPos), Color.Red);
        }

        // 合计
        yPos += 28;
        var netEarnings = Tools.ModConfig.EnableTax ? _totalEarnings - taxAmount : _totalEarnings;
        var totalText = Tools.GetI18n(I18nKeys.PlayerStall_Total)
            .Tokens(new { amount = netEarnings, gold = goldSuffix }).ToString();
        Utility.drawTextWithShadow(b, totalText, Game1.dialogueFont,
            new Vector2(contentRight - Game1.dialogueFont.MeasureString(totalText).X, yPos), Color.Black);

        // 按钮
        yPos += 56;
        _collectBtn.bounds.Y = yPos;
        var btnLabel = _collected
            ? Tools.GetI18n(I18nKeys.PlayerStall_Collected).ToString()
            : Tools.GetI18n(I18nKeys.PlayerStall_CollectAll).ToString();
        if (_collected) {
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9),
                _collectBtn.bounds.X, _collectBtn.bounds.Y,
                _collectBtn.bounds.Width, _collectBtn.bounds.Height, Color.White * 0.5f, 4f);
            Utility.drawTextWithShadow(b, btnLabel, Game1.smallFont,
                new Vector2(
                    _collectBtn.bounds.X + (_collectBtn.bounds.Width - Game1.smallFont.MeasureString(btnLabel).X) / 2,
                    _collectBtn.bounds.Y + 8), Color.Gray);
        } else {
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9),
                _collectBtn.bounds.X, _collectBtn.bounds.Y,
                _collectBtn.bounds.Width, _collectBtn.bounds.Height, Color.White, 4f);
            Utility.drawTextWithShadow(b, btnLabel, Game1.smallFont,
                new Vector2(
                    _collectBtn.bounds.X + (_collectBtn.bounds.Width - Game1.smallFont.MeasureString(btnLabel).X) / 2,
                    _collectBtn.bounds.Y + 8), Game1.textColor);
        }

        // 历史总税收（按钮下方）
        if (PlayerStall.TotalTax > 0) {
            var totalTaxText = Tools.GetI18n(I18nKeys.PlayerStall_TotalTax)
                .Tokens(new { amount = PlayerStall.TotalTax, gold = goldSuffix }).ToString();
            Utility.drawTextWithShadow(b, totalTaxText, Game1.smallFont,
                new Vector2(contentRight - Game1.smallFont.MeasureString(totalTaxText).X,
                    yPositionOnScreen + height - 48), Color.Black);
        }

        base.draw(b);
        drawMouse(b);
    }

    private static int? CalcDaysSinceSold(string? soldDate) {
        if (string.IsNullOrEmpty(soldDate)) return null;
        var parts = soldDate.Split('_');
        if (parts.Length < 3) return null;
        if (!int.TryParse(parts[1], out var day) || !int.TryParse(parts[2], out var year))
            return null;

        var seasonIdx = -1;
        // 新格式：数字索引（0=Spring, 1=Summer, 2=Fall, 3=Winter）
        if (int.TryParse(parts[0], out var idx)) {
            seasonIdx = idx;
        } else {
            // 旧格式兼容：英文赛季名
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
