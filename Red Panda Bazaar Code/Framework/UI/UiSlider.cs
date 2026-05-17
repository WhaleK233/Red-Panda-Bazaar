using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Framework.UI;

/// <summary>数值滑块组件。</summary>
public class UiSlider : UiElement
{
    private const int SliderHeight = 20;
    private const int ThumbWidth = 12;
    private const int MinSliderWidth = 120;

    /// <summary>当前值。</summary>
    public float Value { get; set; }

    /// <summary>最小值。</summary>
    public float Min { get; set; }

    /// <summary>最大值。</summary>
    public float Max { get; set; } = 100;

    /// <summary>步进值。</summary>
    public float Step { get; set; } = 1;

    /// <summary>滑块轨道宽度，0 表示自动。</summary>
    public int SliderTrackWidth { get; set; }

    /// <summary>值变更回调。</summary>
    public Action<float>? OnValueChanged { get; set; }

    /// <summary>值格式化函数，null 显示原始数值。</summary>
    public Func<float, string>? FormatValue { get; set; }

    /// <summary>是否正在拖拽。</summary>
    private bool _dragging;

    public UiSlider(float value = 0, float min = 0, float max = 100, float step = 1)
    {
        Value = value;
        Min = min;
        Max = max;
        Step = step;
    }

    public override bool IsFocusable => Visible;

    public override void Update(int mouseX, int mouseY)
    {
        IsHovered = Bounds.Contains(mouseX, mouseY);

        if (_dragging)
        {
            var trackLeft = X;
            var trackRight = X + GetTrackWidth();
            var ratio = Math.Clamp((mouseX - trackLeft) / (float)(trackRight - trackLeft), 0f, 1f);
            var raw = Min + ratio * (Max - Min);
            var stepped = (float)(Math.Round(raw / Step) * Step);
            Value = Math.Clamp(stepped, Min, Max);
            OnValueChanged?.Invoke(Value);
        }
    }

    public override void Arrange()
    {
        var trackW = Math.Max(MinSliderWidth, SliderTrackWidth);
        var labelW = FormatValue != null
            ? (int)Game1.smallFont.MeasureString(FormatValue(Max)).X
            : 0;
        Width = trackW + (labelW > 0 ? labelW + 10 : 0);
        Height = SliderHeight + 4;
    }

    public override void Draw(SpriteBatch b)
    {
        if (!Visible) return;

        var trackLeft = X;
        var trackRight = X + GetTrackWidth();
        var trackY = Y + Height / 2 - 4;

        // 轨道背景
        var trackRect = new Rectangle(trackLeft, trackY, trackRight - trackLeft, 8);
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(64, 256, 60, 12),
            trackRect.X, trackRect.Y, trackRect.Width, trackRect.Height,
            IsHovered || Focused ? Color.White : Color.LightGray, 2f);

        // 滑块拇指
        var ratio = Max > Min ? (Value - Min) / (Max - Min) : 0f;
        var thumbX = trackLeft + (int)(ratio * (trackRight - trackLeft)) - ThumbWidth / 2;
        var thumbY = Y + Height / 2 - SliderHeight / 2;
        var thumbRect = new Rectangle(thumbX, thumbY, ThumbWidth, SliderHeight);

        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9),
            thumbRect.X, thumbRect.Y, thumbRect.Width, thumbRect.Height,
            _dragging ? Color.Wheat : Color.White, 3f);

        // 值标签
        if (FormatValue != null)
        {
            var label = FormatValue(Value);
            var labelPos = new Vector2(trackRight + 10, Y + (Height - Game1.smallFont.MeasureString(label).Y) / 2);
            Utility.drawTextWithShadow(b, label, Game1.smallFont, labelPos, Game1.textColor);
        }
    }

    public override bool HandleClick(int x, int y)
    {
        if (!Visible || !Bounds.Contains(x, y)) return false;

        _dragging = true;
        Game1.playSound("smallSelect");
        Update(x, y); // 立即计算值
        return true;
    }

    public override bool HandleScroll(int direction)
    {
        if (!Visible || !Bounds.Contains(Game1.getMouseX(), Game1.getMouseY())) return false;

        var oldValue = Value;
        Value = Math.Clamp(Value + (direction > 0 ? Step : -Step), Min, Max);
        if (Math.Abs(Value - oldValue) > Step * 0.01f)
            OnValueChanged?.Invoke(Value);
        return true;
    }

    public void Release()
    {
        if (_dragging)
        {
            _dragging = false;
            Game1.playSound("bigDeSelect");
        }
    }

    private int GetTrackWidth()
    {
        return Math.Max(MinSliderWidth, SliderTrackWidth > 0 ? SliderTrackWidth : Width - 0);
    }
}
