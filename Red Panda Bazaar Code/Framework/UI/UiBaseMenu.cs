using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Red_Panda_Bazaar_Code.Framework.UI.Components;
using StardewValley;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Framework.UI;

public abstract class UiBaseMenu : IClickableMenu
{
    protected const int ContentPadding = 24;
    protected const int TopPadding = 24;

    private const int ChromeWidth = 24;
    private const int ChromeHeight = 40;
    private UiElement? _focusedElement;
    private List<UiElement>? _focusableCache;
    private bool _focusableCacheDirty = true;

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
        _focusableCacheDirty = true;
        Root.Children.Clear();
        BuildUi();

        // 测量阶段：计算所有元素的自然尺寸（不依赖最终坐标）
        Root.Measure();

        var content = CalcContentSize();
        var w = Math.Max(content.X, Root.Width) + ContentPadding * 2;
        var h = Math.Max(content.Y, Root.Height) + ContentPadding + TopPadding;

        width = Math.Clamp(w + ChromeWidth, 300, Game1.uiViewport.Width - 40);
        height = Math.Clamp(h + ChromeHeight, 120, Game1.uiViewport.Height - 40);

        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;

        initializeUpperRightCloseButton();

        // 布局阶段：一次性定位所有元素
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
                if (_focusedElement is UiDropdown { IsOpen: true } dd)
                {
                    dd.Close();
                    return;
                }
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

        for (var i = 0; i < el.ChildCount; i++)
            ReleaseDragging(el.GetChild(i)!);
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
        for (var i = 0; i < el.ChildCount; i++)
            ClearFocusedFlag(el.GetChild(i)!);
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

        // 优先检查子元素中是否还有悬停的（倒序遍历，上层覆盖优先）
        for (var i = el.ChildCount - 1; i >= 0; i--)
        {
            var found = FindHoveredElement(el.GetChild(i)!);
            if (found != null) return found;
        }

        // 子元素中没有悬停的，且当前元素有 tooltip 则返回自身
        return el.Tooltip != null ? el : null;
    }

    // ---- 焦点导航辅助方法 ----

    private void MoveFocus(int direction)
    {
        // 下拉框打开时，上下键切换选项
        if (_focusedElement is UiDropdown { IsOpen: true } dd)
        {
            if (direction is 0 or 2) // 上/下
            {
                dd.NavigateSelection(direction == 0 ? -1 : 1);
                return;
            }
            // 左/右关闭下拉框并移动焦点
            dd.Close();
        }

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
        else if (direction is 0 or 2) // 无法移动焦点时（已到边缘），滚动容器
        {
            ScrollFocusedContainer(direction == 0 ? 1 : -1);
        }
    }

    private void ActivateFocusedElement()
    {
        if (_focusedElement == null || !_focusedElement.Visible) return;

        // 下拉框打开时，A 键确认当前选项
        if (_focusedElement is UiDropdown { IsOpen: true } dd)
        {
            dd.ConfirmSelection();
            return;
        }

        var c = _focusedElement.Bounds.Center;
        _focusedElement.HandleClick(c.X, c.Y);
    }

    private void ScrollFocusedContainer(int direction)
    {
        var sc = FindAncestorScrollContainer(_focusedElement) ?? FindFirstScrollContainer(Root);
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
        if (_focusableCache != null && !_focusableCacheDirty)
            return _focusableCache;

        _focusableCache = new List<UiElement>();
        CollectFocusable(Root, _focusableCache);
        _focusableCacheDirty = false;
        return _focusableCache;
    }

    private static void CollectFocusable(UiElement element, List<UiElement> result)
    {
        if (!element.Visible) return;
        if (element.IsFocusable) result.Add(element);

        for (var i = 0; i < element.ChildCount; i++)
            CollectFocusable(element.GetChild(i)!, result);
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

    private static UiScrollContainer? FindFirstScrollContainer(UiElement element)
    {
        if (element is UiScrollContainer sc) return sc;
        for (var i = 0; i < element.ChildCount; i++)
        {
            var found = FindFirstScrollContainer(element.GetChild(i)!);
            if (found != null) return found;
        }
        return null;
    }
}
