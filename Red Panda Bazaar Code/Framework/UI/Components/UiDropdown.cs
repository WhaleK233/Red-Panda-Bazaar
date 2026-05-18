using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Framework.UI.Components;

/// <summary>下拉选择框组件。</summary>
public class UiDropdown : UiElement
{
    private const int PadX = 12;
    private const int PadY = 6;
    private const int MaxVisibleItems = 8;
    private const int ItemHeight = 28;

    /// <summary>当前选中值。</summary>
    public string Value { get; set; }

    /// <summary>所有可选值。</summary>
    public string[] Choices { get; set; }

    /// <summary>值显示文本格式化函数，null 则显示原始值。</summary>
    public Func<string, string>? FormatChoice { get; set; }

    /// <summary>值变更回调。</summary>
    public Action<string>? OnValueChanged { get; set; }

    /// <summary>是否可用。</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>下拉选项列表是否打开。</summary>
    public bool IsOpen { get; private set; }

    /// <summary>当前打开的全局下拉框（由 UiBaseMenu 绘制覆盖层）。</summary>
    internal static UiDropdown? ActiveDropdown { get; set; }

    // 下拉选项的绘制区域
    private int _hoveredIndex = -1;
    private int _selectedIndex = -1;
    private Rectangle _dropdownBounds;

    public UiDropdown(string value, string[] choices, Action<string>? onValueChanged = null)
    {
        Value = value;
        Choices = choices;
        OnValueChanged = onValueChanged;
    }

    public override bool IsFocusable => Enabled && Visible;

    public override void Update(int mouseX, int mouseY)
    {
        IsHovered = Bounds.Contains(mouseX, mouseY);

        if (IsOpen)
        {
            _hoveredIndex = -1;
            for (var i = 0; i < Choices.Length; i++)
            {
                var itemRect = GetItemBounds(i);
                if (itemRect.Contains(mouseX, mouseY))
                {
                    _hoveredIndex = i;
                    _selectedIndex = i;
                    break;
                }
            }
        }
    }

    public override void Arrange()
    {
        var label = GetDisplayText(Value);
        var size = Game1.smallFont.MeasureString(label);
        Width = Math.Max((int)size.X + PadX * 2 + 20, 80); // +20 for arrow
        Height = (int)size.Y + PadY * 2;
    }

    public override void Draw(SpriteBatch b)
    {
        if (!Visible) return;

        var alpha = Enabled ? 1f : 0.5f;
        var bgColor = (IsHovered || Focused) ? Color.Wheat : Color.White;

        // 主按钮区域
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9),
            X, Y, Width, Height, bgColor * alpha, 3f);

        // 当前值文字
        var label = GetDisplayText(Value);
        var textPos = new Vector2(X + PadX, Y + (Height - Game1.smallFont.MeasureString(label).Y) / 2);
        Utility.drawTextWithShadow(b, label, Game1.smallFont, textPos, (Enabled ? Game1.textColor : Color.Gray) * alpha);

        // 下拉箭头
        var arrowX = X + Width - 20;
        var arrowY = Y + Height / 2 - 2;
        b.Draw(Game1.mouseCursors, new Vector2(arrowX, arrowY),
            new Rectangle(349, 346, 7, 7), Color.White * alpha, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0.5f);
    }

    /// <summary>绘制下拉选项覆盖层。由 UiBaseMenu 在最后阶段调用。</summary>
    public void DrawOverlay(SpriteBatch b)
    {
        if (!IsOpen || !Visible) return;

        var totalH = Math.Min(Choices.Length, MaxVisibleItems) * ItemHeight + 4;
        _dropdownBounds = new Rectangle(X, Y + Height, Width, totalH);

        // 背景
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
            _dropdownBounds.X, _dropdownBounds.Y, _dropdownBounds.Width, _dropdownBounds.Height,
            Color.White, 3f);

        // 选项列表
        var startIndex = 0;
        var visibleCount = Math.Min(Choices.Length, MaxVisibleItems);
        for (var i = startIndex; i < startIndex + visibleCount; i++)
        {
            var itemRect = GetItemBounds(i);
            var isHovered = i == _hoveredIndex;

            if (isHovered)
                b.Draw(Game1.staminaRect, itemRect, Color.Wheat * 0.5f);

            var text = GetDisplayText(Choices[i]);
            var textPos = new Vector2(itemRect.X + 8,
                itemRect.Y + (itemRect.Height - Game1.smallFont.MeasureString(text).Y) / 2);

            var color = Choices[i] == Value
                ? Color.DarkGreen
                : Game1.textColor;

            Utility.drawTextWithShadow(b, text, Game1.smallFont, textPos, color);
        }
    }

    public override bool HandleClick(int x, int y)
    {
        if (!Visible || !Enabled) return false;

        // 如果选项已打开，检查是否点击了某个选项
        if (IsOpen)
        {
            if (_dropdownBounds.Contains(x, y))
            {
                var clickedIndex = (y - _dropdownBounds.Y - 2) / ItemHeight;
                if (clickedIndex >= 0 && clickedIndex < Choices.Length)
                {
                    var newValue = Choices[clickedIndex];
                    if (newValue != Value)
                    {
                        Value = newValue;
                        Game1.playSound("smallSelect");
                        OnValueChanged?.Invoke(Value);
                    }
                    Close();
                    return true;
                }
            }
            else
            {
                // 点击外部关闭
                Close();
                return Bounds.Contains(x, y); // 重新点击自身也关闭
            }
        }

        // 点击主按钮打开下拉
        if (Bounds.Contains(x, y))
        {
            Game1.playSound("shiny4");
            Open();
            return true;
        }

        return false;
    }

    public override bool HandleScroll(int direction)
    {
        if (!Visible || !Enabled || !IsOpen) return false;
        return _dropdownBounds.Contains(Game1.getMouseX(), Game1.getMouseY());
    }

    private void Open()
    {
        IsOpen = true;
        ActiveDropdown = this;
        _selectedIndex = Array.IndexOf(Choices, Value);
        if (_selectedIndex < 0) _selectedIndex = 0;
        _hoveredIndex = _selectedIndex;
    }

    public void Close()
    {
        IsOpen = false;
        _hoveredIndex = -1;
        _selectedIndex = -1;
        if (ActiveDropdown == this)
            ActiveDropdown = null;
    }

    /// <summary>键盘方向键选择选项。</summary>
    public void NavigateSelection(int direction)
    {
        if (!IsOpen || Choices.Length == 0) return;
        _selectedIndex = Math.Clamp(_selectedIndex + direction, 0, Choices.Length - 1);
        _hoveredIndex = _selectedIndex;
    }

    /// <summary>确认当前键盘选中的选项。</summary>
    public void ConfirmSelection()
    {
        if (!IsOpen) return;
        if (_selectedIndex < 0 || _selectedIndex >= Choices.Length) return;

        var newValue = Choices[_selectedIndex];
        if (newValue != Value)
        {
            Value = newValue;
            Game1.playSound("smallSelect");
            OnValueChanged?.Invoke(Value);
        }
        Close();
    }

    private string GetDisplayText(string value)
    {
        return FormatChoice?.Invoke(value) ?? value;
    }

    private Rectangle GetItemBounds(int index)
    {
        return new Rectangle(
            _dropdownBounds.X + 2,
            _dropdownBounds.Y + 2 + index * ItemHeight,
            _dropdownBounds.Width - 4,
            ItemHeight);
    }
}
