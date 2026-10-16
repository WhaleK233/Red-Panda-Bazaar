using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewUI;
using StardewUI.Graphics;
using StardewUI.Layout;
using StardewUI.Widgets;

namespace Red_Panda_Bazaar_Code.Utils;

/// <summary>
/// StardewUI.Core 构件工厂：统一本 mod 各菜单的窗口外观、行布局与常用控件。
/// </summary>
internal static class MenuUi
{
    /// <summary>内容区宽度上限；窗口最终宽度 = 该值 + 边框(76) + 内边距(96)。</summary>
    public const float ContentWidth = 560;

    /// <summary>内容区高度上限：内容更矮时窗口按内容收缩，超出才截断（避免菜单高过屏幕）。</summary>
    public const float ContentHeight = 700;

    /// <summary>列内行距（对齐原框架 <c>UiColumn</c> 的默认 Spacing）。</summary>
    public const int RowGap = 6;

    /// <summary>菜单窗口外壳：菜单底色 + 边框，尺寸随内容自适应（宽度受内容区上限约束）。</summary>
    public static Frame Window(IView content)
    {
        return new Frame
        {
            Background = UiSprites.MenuBackground,
            Border = UiSprites.MenuBorder,
            BorderThickness = UiSprites.MenuBorderThickness,
            Padding = new(48, 32),
            Layout = LayoutParameters.FitContent(),
            Content = content,
        };
    }

    /// <summary>垂直内容列：占满窗口内容区宽度，高度自适应。</summary>
    public static Lane Column(float maxWidth = ContentWidth, float maxHeight = ContentHeight)
    {
        return new Lane
        {
            Orientation = Orientation.Vertical,
            Layout = new()
            {
                Width = Length.Stretch(),
                Height = Length.Content(),
                MaxWidth = maxWidth,
                MaxHeight = maxHeight,
            },
        };
    }

    /// <summary>水平内容行：占满列宽，行内内容按 <paramref name="align"/> 对齐。</summary>
    public static Lane Row(Alignment align = Alignment.Start, int topMargin = 0)
    {
        return new Lane
        {
            Orientation = Orientation.Horizontal,
            HorizontalContentAlignment = align,
            VerticalContentAlignment = Alignment.Middle,
            Layout = LayoutParameters.AutoRow(),
            Margin = new(Top: topMargin),
        };
    }

    /// <summary>单行文本行：占满列宽，文本按 <paramref name="align"/> 对齐。</summary>
    public static Lane TextRow(
        string text,
        SpriteFont? font = null,
        Color? color = null,
        Alignment align = Alignment.Start,
        int topMargin = 0
    )
    {
        var row = Row(align, topMargin);
        row.Children.Add(Text(text, font, color));
        return row;
    }

    /// <summary>两端对齐行：左侧文本占满剩余宽度，右侧控件贴右端（对齐原框架的 SpaceBetween 行）。</summary>
    public static Lane RowBetween(
        string text,
        IView right,
        SpriteFont? font = null,
        Color? color = null,
        int topMargin = RowGap
    )
    {
        var row = Row(topMargin: topMargin);
        var label = Text(text, font, color);
        label.Layout = new() { Width = Length.Stretch(), Height = Length.Content() };
        label.Margin = new(Right: 16);
        row.Children.Add(label);
        row.Children.Add(right);
        return row;
    }

    /// <summary>单行文本控件。</summary>
    public static Label Text(string text, SpriteFont? font = null, Color? color = null)
    {
        var label = new Label { Text = text };
        if (font is not null)
        {
            label.Font = font;
        }
        if (color is not null)
        {
            label.Color = color.Value;
        }
        return label;
    }

    /// <summary>带点击回调的按钮。</summary>
    public static Button Button(string text, Action onClick, Edges? margin = null)
    {
        var button = new Button { Text = text, Margin = margin ?? Edges.NONE };
        button.Click += (_, _) => onClick();
        return button;
    }

    /// <summary>占满列宽的水平分隔线（上间距由自身给出，下间距由下一行的 topMargin 给出）。</summary>
    public static Image Divider(int topMargin = RowGap)
    {
        return new Image
        {
            Sprite = UiSprites.ThinHorizontalDivider,
            Layout = new() { Width = Length.Stretch(), Height = Length.Px(8) },
            Margin = new(Top: topMargin),
        };
    }
}
