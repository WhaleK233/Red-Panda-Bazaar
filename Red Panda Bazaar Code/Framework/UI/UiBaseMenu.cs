using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Framework.UI;

public abstract class UiBaseMenu : IClickableMenu
{
    protected const int ContentPadding = 24;
    protected const int TopPadding = 24;

    private const int ChromeWidth = 24;
    private const int ChromeHeight = 40;
    private const int FocusRingPadding = 4;

    private UiElement? _focusedElement;

    protected UiColumn Root { get; } = new();
    protected abstract void BuildUi();

    /// <summary>子类在此返回内容区域的期望宽高（不含边框）。</summary>
    protected abstract Point CalcContentSize();

    public UiBaseMenu()
    {
        Game1.player.isCharging = false;
        Game1.player.canMove = true;

        Rebuild();
    }

    protected void Rebuild()
    {
        _focusedElement = null;
        Root.Children.Clear();
        BuildUi();
        Root.Arrange();

        var content = CalcContentSize();
        var w = Math.Max(content.X, Root.Width) + ContentPadding * 2;
        var h = Math.Max(content.Y, Root.Height) + ContentPadding + TopPadding;

        width = Math.Clamp(w + ChromeWidth, 300, Game1.uiViewport.Width - 40);
        height = Math.Clamp(h + ChromeHeight, 120, Game1.uiViewport.Height - 40);

        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;

        initializeUpperRightCloseButton();

        Root.X = xPositionOnScreen + ContentPadding;
        Root.Y = yPositionOnScreen + TopPadding;
        Root.Arrange();

        if (Game1.options.gamepadControls)
            SnapToFirstFocusable();
    }

    protected override void cleanupBeforeExit()
    {
        Game1.player.isCharging = false;
        Game1.player.canMove = true;

        // 关闭所有打开的下拉框
        UiDropdown.ActiveDropdown = null;

        base.cleanupBeforeExit();
    }

    public override bool areGamePadControlsImplemented() => true;

    public override void setUpForGamePadMode()
    {
        base.setUpForGamePadMode();
        SnapToFirstFocusable();
    }

    private void SnapToFirstFocusable()
    {
        var focusables = GetAllFocusableElements();
        _focusedElement = focusables.FirstOrDefault();
        if (_focusedElement == null) return;

        var c = _focusedElement.Bounds.Center;
        Game1.setMousePosition(c.X, c.Y);
        EnsureFocusedElementVisible();
    }

    public override void applyMovementKey(int direction)
    {
        MoveFocus(direction);
    }

    public override void receiveGamePadButton(Buttons b)
    {
        switch (b)
        {
            case Buttons.A:
                ActivateFocusedElement();
                return;
            case Buttons.B:
                exitThisMenuNoSound();
                return;
            case Buttons.LeftTrigger:
            case Buttons.LeftShoulder:
                ScrollFocusedContainer(-1);
                return;
            case Buttons.RightTrigger:
            case Buttons.RightShoulder:
                ScrollFocusedContainer(1);
                return;
        }
        base.receiveGamePadButton(b);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (Game1.activeClickableMenu != this) return;

        // 如果下拉框打开且点击不在其内部，先交给下拉处理（可能关闭它）
        if (UiDropdown.ActiveDropdown != null)
        {
            UiDropdown.ActiveDropdown.HandleClick(x, y);
            return;
        }

        UpdateFocusFromClick(x, y);
        Root.HandleClick(x, y);
    }

    public override void releaseLeftClick(int x, int y)
    {
        base.releaseLeftClick(x, y);
        ReleaseDragging(Root);
    }

    public override void leftClickHeld(int x, int y)
    {
        base.leftClickHeld(x, y);
        // 持续拖拽由每帧 Update 处理（滑块等）
    }

    private static void ReleaseDragging(UiElement el)
    {
        if (el is UiSlider slider)
            slider.Release();

        switch (el)
        {
            case UiRow row:
                foreach (var child in row.Children)
                    ReleaseDragging(child);
                break;
            case UiColumn col:
                foreach (var child in col.Children)
                    ReleaseDragging(child);
                break;
            case UiScrollContainer sc when sc.Child != null:
                ReleaseDragging(sc.Child);
                break;
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);

        // 下拉框打开时拦截滚轮，防止背景滚动
        if (UiDropdown.ActiveDropdown != null)
            return;

        Root.HandleScroll(direction);
    }

    public override void receiveRightClick(int x, int y, bool playSound = true) { }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        Rebuild();
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
            xPositionOnScreen, yPositionOnScreen, width, height, Color.White, 4f);

        // 更新元素状态（悬停、焦点）
        UpdateElementStates();

        Root.Draw(b);

        // 绘制焦点指示器
        DrawFocusIndicator(b);

        // 绘制 Tooltip
        DrawTooltips(b);

        // 绘制下拉覆盖层（在顶层）
        DrawDropdownOverlay(b);

        base.draw(b);
        drawMouse(b);
    }

    // ---- 元素状态更新 ----

    private void UpdateElementStates()
    {
        var mx = Game1.getOldMouseX();
        var my = Game1.getOldMouseY();

        // 更新所有元素的悬停状态
        Root.Update(mx, my);

        // 清除旧焦点标记
        ClearFocusedFlag(Root);

        // 标记当前焦点元素
        if (_focusedElement != null)
            _focusedElement.Focused = true;
    }

    private static void ClearFocusedFlag(UiElement el)
    {
        el.Focused = false;
        switch (el)
        {
            case UiRow row:
                foreach (var child in row.Children)
                    ClearFocusedFlag(child);
                break;
            case UiColumn col:
                foreach (var child in col.Children)
                    ClearFocusedFlag(child);
                break;
            case UiScrollContainer sc when sc.Child != null:
                ClearFocusedFlag(sc.Child);
                break;
        }
    }

    // ---- 焦点指示器 ----

    private void DrawFocusIndicator(SpriteBatch b)
    {
        if (_focusedElement == null) return;
        if (!Game1.options.gamepadControls && !_focusedElement.IsHovered) return;

        var bounds = _focusedElement.Bounds;
        var r = new Rectangle(bounds.X - FocusRingPadding, bounds.Y - FocusRingPadding,
            bounds.Width + FocusRingPadding * 2, bounds.Height + FocusRingPadding * 2);

        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(375, 357, 3, 3),
            r.X, r.Y, r.Width, r.Height, Color.Wheat * 0.8f, 3f);
    }

    // ---- Tooltip ----

    private void DrawTooltips(SpriteBatch b)
    {
        var hoveredEl = FindHoveredElement(Root);
        if (hoveredEl?.Tooltip == null) return;

        var text = hoveredEl.Tooltip();
        if (string.IsNullOrEmpty(text)) return;

        IClickableMenu.drawHoverText(b, text, Game1.smallFont);
    }

    private static UiElement? FindHoveredElement(UiElement el)
    {
        if (!el.Visible || !el.IsHovered) return null;

        // 优先检查子元素中是否还有悬停的
        switch (el)
        {
            case UiRow row:
                for (var i = row.Children.Count - 1; i >= 0; i--)
                {
                    var found = FindHoveredElement(row.Children[i]);
                    if (found != null) return found;
                }
                break;
            case UiColumn col:
                for (var i = col.Children.Count - 1; i >= 0; i--)
                {
                    var found = FindHoveredElement(col.Children[i]);
                    if (found != null) return found;
                }
                break;
            case UiScrollContainer sc when sc.Child != null:
                return FindHoveredElement(sc.Child);
        }

        // 子元素中没有悬停的，且当前元素有 tooltip 则返回自身
        return el.Tooltip != null ? el : null;
    }

    // ---- 焦点导航辅助方法 ----

    private void MoveFocus(int direction)
    {
        var focusables = GetAllFocusableElements();
        if (focusables.Count == 0) return;

        if (_focusedElement == null)
        {
            _focusedElement = focusables[0];
            EnsureFocusedElementVisible();
            return;
        }

        var currentBounds = _focusedElement.Bounds;
        var currentCenter = new Point(currentBounds.Center.X, currentBounds.Center.Y);

        UiElement? best = null;
        var bestScore = double.MaxValue;

        foreach (var candidate in focusables)
        {
            if (candidate == _focusedElement) continue;

            var cb = candidate.Bounds;
            var candidateCenter = new Point(cb.Center.X, cb.Center.Y);
            var dx = candidateCenter.X - currentCenter.X;
            var dy = candidateCenter.Y - currentCenter.Y;

            bool inDirection = direction switch
            {
                0 => dy < 0,  // 上
                1 => dx > 0,  // 右
                2 => dy > 0,  // 下
                3 => dx < 0,  // 左
                _ => false
            };
            if (!inDirection) continue;

            var distSq = dx * (double)dx + dy * (double)dy;
            var axisDev = direction switch
            {
                0 or 2 => Math.Abs(dx) / Math.Max(1.0, Math.Abs(dy)),
                1 or 3 => Math.Abs(dy) / Math.Max(1.0, Math.Abs(dx)),
                _ => 0
            };

            var score = distSq * (1.0 + axisDev);
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        if (best != null)
        {
            _focusedElement = best;
            Game1.playSound("shiny4");
            Game1.setMousePosition(best.Bounds.Center.X, best.Bounds.Center.Y);
            EnsureFocusedElementVisible();
        }
    }

    private void ActivateFocusedElement()
    {
        if (_focusedElement == null || !_focusedElement.Visible) return;
        var c = _focusedElement.Bounds.Center;
        _focusedElement.HandleClick(c.X, c.Y);
    }

    private void ScrollFocusedContainer(int direction)
    {
        var sc = FindAncestorScrollContainer(_focusedElement);
        sc?.Scroll(direction);
    }

    private void EnsureFocusedElementVisible()
    {
        if (_focusedElement == null) return;
        var sc = FindAncestorScrollContainer(_focusedElement);
        sc?.EnsureChildVisible(_focusedElement);
    }

    private void UpdateFocusFromClick(int x, int y)
    {
        foreach (var f in GetAllFocusableElements())
        {
            if (f.Bounds.Contains(x, y))
            {
                _focusedElement = f;
                return;
            }
        }
    }

    private List<UiElement> GetAllFocusableElements()
    {
        var result = new List<UiElement>();
        CollectFocusable(Root, result);
        return result;
    }

    private static void CollectFocusable(UiElement element, List<UiElement> result)
    {
        if (!element.Visible) return;
        if (element.IsFocusable) result.Add(element);

        switch (element)
        {
            case UiRow row:
                foreach (var child in row.Children)
                    CollectFocusable(child, result);
                break;
            case UiColumn col:
                foreach (var child in col.Children)
                    CollectFocusable(child, result);
                break;
            case UiScrollContainer sc when sc.Child != null:
                CollectFocusable(sc.Child, result);
                break;
        }
    }

    // ---- 下拉覆盖层 ----

    private void DrawDropdownOverlay(SpriteBatch b)
    {
        if (UiDropdown.ActiveDropdown == null) return;

        // 在 scissor 区域外绘制
        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
            SamplerState.PointClamp, null, null);

        UiDropdown.ActiveDropdown.DrawOverlay(b);

        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
            SamplerState.PointClamp, null, null);
    }

    private static UiScrollContainer? FindAncestorScrollContainer(UiElement? element)
    {
        while (element != null)
        {
            if (element is UiScrollContainer sc) return sc;
            element = element.Parent;
        }
        return null;
    }
}
