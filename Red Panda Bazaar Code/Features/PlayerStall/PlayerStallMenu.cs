using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.DeBug;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Features.PlayerStall;

/// <summary>玩家商店界面。每个 ActionId 对应一个独立摊位。</summary>
public class PlayerStallMenu : MenuWithInventory {
    private readonly string _actionId;
    private readonly ClickableComponent _stallSlot;
    private readonly ClickableComponent _priceButton;
    private readonly string _priceButtonLabel;
    private readonly Vector2 _priceButtonLabelSize;
    private Item? _stallItem;

    private const int PriceButtonPaddingX = 12;
    private const int PriceButtonPaddingY = 6;

    private List<StallItem> GetUnsoldItems() =>
        PlayerStall.GetItems(_actionId);

    public PlayerStallMenu(string actionId)
        : base(okButton: true, trashCan: true) {
        _actionId = actionId;

        var slotSize = 64;
        var slotX = xPositionOnScreen + (width - slotSize) / 2;
        var slotY = yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + 32;
        _stallSlot = new ClickableComponent(new Rectangle(slotX, slotY, slotSize, slotSize), "stall");

        _priceButtonLabel = Tools.GetI18n(I18nKeys.PlayerStall_PriceEditTitle).ToString();
        _priceButtonLabelSize = Game1.smallFont.MeasureString(_priceButtonLabel);
        var priceButtonWidth = (int)_priceButtonLabelSize.X + PriceButtonPaddingX * 2;
        var priceButtonHeight = (int)_priceButtonLabelSize.Y + PriceButtonPaddingY * 2;
        var priceButtonX = _stallSlot.bounds.X + (_stallSlot.bounds.Width - priceButtonWidth) / 2;
        var priceButtonY = _stallSlot.bounds.Y - 48 - PriceButtonPaddingY;
        _priceButton = new ClickableComponent(
            new Rectangle(priceButtonX, priceButtonY, priceButtonWidth, priceButtonHeight), "price");

        // 重新连接 inventory 到玩家物品栏
        var invY = yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth + 192 - 16;
        inventory = new InventoryMenu(
            xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + IClickableMenu.borderWidth / 2,
            invY, playerInventory: true, Game1.player.Items,
            highlightMethod: item => item is not Tool);

        // 加载未售物品
        var si = GetUnsoldItems().FirstOrDefault();
        if (si != null)
            _stallItem = ItemRegistry.Create(si.ItemId, si.Amount);

        // 控制器/键盘焦点导航
        _stallSlot.myID = 101;
        _priceButton.myID = 102;
        var firstInv = inventory?.inventory?.FirstOrDefault();
        _stallSlot.downNeighborID = firstInv?.myID ?? 101;
        _stallSlot.upNeighborID = _priceButton.myID;
        _priceButton.upNeighborID = upperRightCloseButton?.myID ?? 102;
        _priceButton.downNeighborID = _stallSlot.myID;
        if (firstInv != null) firstInv.upNeighborID = 101;

        allClickableComponents = new();
        allClickableComponents.Add(_stallSlot);
        allClickableComponents.Add(_priceButton);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true) {
        if (_priceButton.bounds.Contains(x, y)) {
            var items = GetUnsoldItems();
            if (_stallItem != null && items.Count > 0)
                Game1.activeClickableMenu = new PriceEditMenu(this, items[0]);
            return;
        }

        // 点击库存物品 → 放入摊位或与现有物品叠加
        for (var i = 0; i < inventory.inventory.Count; i++) {
            if (!inventory.inventory[i].bounds.Contains(x, y)) continue;
            var item = Game1.player.Items[i];
            if (item is null or Tool) return;

            var unsoldItems = GetUnsoldItems();
            if (unsoldItems.Count > 0) {
                var existing = unsoldItems[0];
                if (item.ItemId != existing.ItemId) return; // 摊位有不同物品，不能放入
                // 叠加到已有物品
                var newAmt = existing.Amount + item.Stack;
                PlayerStall.UpdateItem(existing.Id, newAmt, existing.Price);
                Game1.player.Items[i] = null;
                _stallItem = ItemRegistry.Create(item.ItemId, newAmt);
                Game1.playSound("coin");
                return;
            }

            // 摊位空 → 放入
            var price = Math.Max(1, item.sellToStorePrice());
            Game1.player.Items[i] = null;
            PlayerStall.AddItem(_actionId, item.ItemId, item.Stack, price);
            _stallItem = ItemRegistry.Create(item.ItemId, item.Stack);
            Game1.playSound("coin");
            return;
        }

        // 摊位格子
        if (_stallSlot.bounds.Contains(x, y)) {
            var items = GetUnsoldItems();

            // 手上有物品
            if (heldItem != null) {
                if (heldItem is Tool) return;

                // 摊位有同类型物品 → 叠加
                if (items.Count > 0 && heldItem.ItemId == items[0].ItemId) {
                    var newAmt = items[0].Amount + heldItem.Stack;
                    PlayerStall.UpdateItem(items[0].Id, newAmt, items[0].Price);
                    heldItem = null;
                    _stallItem = ItemRegistry.Create(items[0].ItemId, newAmt);
                    Game1.playSound("coin");
                    return;
                }

                // 格子空 → 放入
                if (_stallItem == null) {
                    var price = Math.Max(1, heldItem.sellToStorePrice());
                    PlayerStall.AddItem(_actionId, heldItem.ItemId, heldItem.Stack, price);
                    _stallItem = ItemRegistry.Create(heldItem.ItemId, heldItem.Stack);
                    heldItem = null;
                    Game1.playSound("coin");
                    return;
                }
                return;
            }

            // 没拿物品，格子有物品 → 取回背包
            if (_stallItem != null && items.Count > 0) {
                var si = items[0];
                var item = ItemRegistry.Create(si.ItemId, si.Amount);
                var leftover = Game1.player.addItemToInventory(item);
                if (leftover != null) return; // 背包满
                PlayerStall.RemoveItem(si);
                _stallItem = null;
                Game1.playSound("pickUpItem");
                return;
            }
            return;
        }

        base.receiveLeftClick(x, y, playSound);
    }

    public override void receiveRightClick(int x, int y, bool playSound = true) {
        // 仅允许右键摊位格子修改价格，其余右键操作全部忽略
        if (_stallSlot.bounds.Contains(x, y) && _stallItem != null) {
            var items = GetUnsoldItems();
            if (items.Count > 0)
                Game1.activeClickableMenu = new PriceEditMenu(this, items[0]);
        }
    }

    public override void draw(SpriteBatch b) {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);
        base.draw(b, drawUpperPortion: false, drawDescriptionArea: false);

        // 调试模式：顶部显示当前菜单的 Action ID
        if (DebugOverlay.IsEnabled)
        {
            var debugText = $"Stall: {_actionId}";
            var debugSize = Game1.smallFont.MeasureString(debugText);
            var debugPos = new Vector2(
                (Game1.graphics.GraphicsDevice.Viewport.Width - debugSize.X) / 2,
                16f);
            b.DrawString(Game1.smallFont, debugText, debugPos + Vector2.One, Color.Black);
            b.DrawString(Game1.smallFont, debugText, debugPos, Color.Yellow);
        }

        // 摊位背景框
        var padding = IClickableMenu.borderWidth * 2;
        var boxX = _stallSlot.bounds.X - padding;
        var boxY = _stallSlot.bounds.Y - padding;
        var w = _stallSlot.bounds.Width + padding * 2;
        var h = _stallSlot.bounds.Height + padding * 2;
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
            boxX, boxY, w, h, Color.White, 4f);

        // 摊位格子
        b.Draw(Game1.menuTexture, _stallSlot.bounds,
            Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), Color.White);

        var unsoldItems = GetUnsoldItems();
        var canEditPrice = _stallItem != null && unsoldItems.Count > 0;
        var buttonColor = canEditPrice ? Color.White : Color.White * 0.5f;
        var buttonTextColor = canEditPrice ? Game1.textColor : Color.Gray;
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9),
            _priceButton.bounds.X, _priceButton.bounds.Y, _priceButton.bounds.Width, _priceButton.bounds.Height,
            buttonColor, 4f);
        Utility.drawTextWithShadow(b, _priceButtonLabel, Game1.smallFont,
            new Vector2(
                _priceButton.bounds.X + (_priceButton.bounds.Width - _priceButtonLabelSize.X) / 2,
                _priceButton.bounds.Y + (_priceButton.bounds.Height - _priceButtonLabelSize.Y) / 2),
            buttonTextColor);

        if (_stallItem != null) {
            if (unsoldItems.Count > 0) {
                var si = unsoldItems[0];
                _stallItem.drawInMenu(b, new Vector2(_stallSlot.bounds.X + 4, _stallSlot.bounds.Y + 4), 0.75f);

                var priceText = $"{si.Price}{Tools.GetI18n(I18nKeys.Text_Gold)}";
                Utility.drawTextWithShadow(b, priceText, Game1.smallFont,
                    new Vector2(
                        _stallSlot.bounds.X +
                        (_stallSlot.bounds.Width - Game1.smallFont.MeasureString(priceText).X) / 2,
                        _stallSlot.bounds.Y + 80), Color.Black);
            }
        }

        heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 16, Game1.getOldMouseY() + 16), 1f);
        drawMouse(b);
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) {
        Game1.activeClickableMenu = new PlayerStallMenu(_actionId);
    }

    private class PriceEditMenu : NumberSelectionMenu {
        public PriceEditMenu(PlayerStallMenu parent, StallItem stallItem)
            : base(
                Tools.GetI18n(I18nKeys.PlayerStall_PriceEditTitle).ToString(),
                (number, price, who) => {
                    PlayerStall.UpdateItem(stallItem.Id, stallItem.Amount, number);
                    Game1.playSound("coin");
                    parent._stallItem = ItemRegistry.Create(stallItem.ItemId, stallItem.Amount);
                    Game1.activeClickableMenu = new PlayerStallMenu(parent._actionId);
                },
                price: -1,
                minValue: Math.Max(1, (int)Math.Ceiling(ItemRegistry.Create(stallItem.ItemId).sellToStorePrice() * 0.01)),
                maxValue: Math.Max(1, ItemRegistry.Create(stallItem.ItemId).sellToStorePrice() * 100),
                defaultNumber: Math.Max(1, stallItem.Price)) { }
    }
}
