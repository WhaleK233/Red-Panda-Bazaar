using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewUI;
using StardewUI.Graphics;
using StardewUI.Layout;
using StardewUI.Widgets;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Utils;

/// <summary>
/// 本 mod 菜单的 StardewUI.Core 设计系统：窗口外壳（标题条 + 内容区）、分组卡片、主次按钮、
/// 数值与图标展示、布局工具。颜色与间距令牌集中在此，各菜单不再散落硬编码。
/// </summary>
internal static class MenuUi
{
    // ---- 颜色令牌 ----

    /// <summary>标题与表头的强调色。</summary>
    public static readonly Color Accent = new(87, 58, 24);

    /// <summary>次要说明（提示语、单位、辅助信息）。</summary>
    public static readonly Color Muted = new(120, 108, 96);

    /// <summary>正向状态（已解锁、收益）。</summary>
    public static readonly Color Positive = new(40, 110, 40);

    /// <summary>负向状态（亏损、警告）。</summary>
    public static readonly Color Negative = new(170, 48, 40);

    /// <summary>未解锁 / 不可用状态。</summary>
    public static readonly Color Locked = new(128, 118, 108);

    // ---- 尺寸令牌 ----

    /// <summary>常规行距。</summary>
    public const int RowGap = 6;

    /// <summary>分组之间的间距。</summary>
    public const int SectionGap = 16;

    /// <summary>内容区高度上限；内容更矮时窗口按内容收缩，超出才截断（避免菜单高过屏幕）。</summary>
    public const float ContentHeight = 700;

    /// <summary>内容区宽度：跟随视口宽度（带上下限），宽屏下不再挤在固定窄列里。</summary>
    public static float ContentWidth => Math.Clamp(Game1.uiViewport.Width - 320f, 560f, 920f);

    // ---- 外壳 ----

    /// <summary>菜单窗口外壳：菜单底纹 + 边框，尺寸随内容自适应。</summary>
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

    /// <summary>菜单标题条：占满内容宽度、居中白字，一眼看清"这是什么菜单"。</summary>
    public static Banner Header(string title)
    {
        return new Banner
        {
            Background = UiSprites.BannerBackground,
            BackgroundBorderThickness = new(12, 8),
            Text = title,
            Color = Color.White,
            TextShadowAlpha = 0.75f,
            Layout = new() { Width = Length.Stretch(), Height = Length.Content() },
        };
    }

    /// <summary>分组标题行：小三角 + 强调色粗体小标题。</summary>
    public static Lane Section(string title, int topMargin = SectionGap)
    {
        var row = Row(topMargin: topMargin);
        var caret = Icon(UiSprites.CaretRight, 2f);
        caret.Margin = new(Right: 6);
        row.Children.Add(caret);
        row.Children.Add(Text(title, color: Accent, bold: true));
        return row;
    }

    /// <summary>分组卡片：内嵌式底纹 + 边框把一组信息框起来；<paramref name="tint"/> 用于状态着色。</summary>
    public static Frame Card(
        IView content,
        Color? tint = null,
        int topMargin = SectionGap,
        int paddingX = 16,
        int paddingY = 12
    )
    {
        return new Frame
        {
            Background = UiSprites.MenuSlotInset,
            BackgroundTint = tint ?? Color.White,
            Padding = new(paddingX, paddingY),
            Margin = new(Top: topMargin),
            Layout = new() { Width = Length.Stretch(), Height = Length.Content() },
            Content = content,
        };
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

    // ---- 布局 ----

    /// <summary>垂直内容列：占满内容区宽度（默认跟随视口），高度自适应。</summary>
    public static Lane Column(float? maxWidth = null, float maxHeight = ContentHeight)
    {
        return new Lane
        {
            Orientation = Orientation.Vertical,
            Layout = new()
            {
                Width = Length.Stretch(),
                Height = Length.Content(),
                MaxWidth = maxWidth ?? ContentWidth,
                MaxHeight = maxHeight,
            },
        };
    }

    /// <summary>水平内容行：占满列宽，行内内容按 <paramref name="align"/> 对齐、垂直居中。</summary>
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
        int topMargin = 0,
        bool bold = false
    )
    {
        var row = Row(align, topMargin);
        row.Children.Add(Text(text, font, color, bold: bold));
        return row;
    }

    /// <summary>"说明 …… 操作"行：左侧文本占满剩余宽度，右侧控件贴右端。</summary>
    public static Lane RowBetween(
        string text,
        IView right,
        SpriteFont? font = null,
        Color? color = null,
        int topMargin = RowGap,
        bool bold = false
    )
    {
        return RowBetween(Text(text, font, color, bold: bold), right, topMargin);
    }

    /// <summary>"左标签 …… 右控件"行：左侧 Label 吃掉剩余宽度，右侧控件贴右端（标签可留引用动态改文本）。</summary>
    public static Lane RowBetween(Label left, IView right, int topMargin = RowGap)
    {
        left.Layout = new() { Width = Length.Stretch(), Height = Length.Content() };
        left.Margin = new(Right: 16);
        var row = Row(topMargin: topMargin);
        row.Children.Add(left);
        row.Children.Add(right);
        return row;
    }

    /// <summary>"标签 …… 数值"行：数值加粗贴右端，用于金额与统计。</summary>
    public static Lane ValueRow(
        string label,
        string value,
        Color? valueColor = null,
        float valueScale = 1f,
        int topMargin = 0
    )
    {
        var row = Row(topMargin: topMargin);
        var labelView = Text(label);
        labelView.Layout = new() { Width = Length.Stretch(), Height = Length.Content() };
        labelView.MaxLines = 1;
        row.Children.Add(labelView);
        row.Children.Add(Text(value, color: valueColor ?? Color.Black, scale: valueScale, bold: true));
        return row;
    }

    // ---- 控件 ----

    public static Label Text(
        string text,
        SpriteFont? font = null,
        Color? color = null,
        float scale = 1f,
        int maxLines = 0,
        bool bold = false,
        int topMargin = 0
    )
    {
        var label = new Label
        {
            Text = text,
            Scale = scale,
            MaxLines = maxLines,
            Bold = bold,
            Margin = new(Top: topMargin),
        };
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

    /// <summary>原版贴图切片的图标（整数倍放大，保持像素清晰）。</summary>
    public static Image Icon(Sprite sprite, float scale = 3f)
    {
        var size = sprite.Size;
        return new Image
        {
            Sprite = sprite,
            Scale = scale,
            Layout = LayoutParameters.FixedSize(size.X * scale, size.Y * scale),
        };
    }

    /// <summary>主操作按钮（亮底，用于"存入 / 收取 / 申请"这类主要动作）。</summary>
    public static Button PrimaryButton(string text, Action onClick, Edges? margin = null)
    {
        var button = new Button
        {
            Text = text,
            DefaultBackground = UiSprites.ButtonLight,
            HoverBackground = UiSprites.ButtonDark,
            Margin = margin ?? Edges.NONE,
        };
        button.Click += (_, _) => onClick();
        return button;
    }

    /// <summary>次要按钮（暗底，用于"取出 / 退出"这类次要动作）。</summary>
    public static Button SecondaryButton(string text, Action onClick, Edges? margin = null)
    {
        var button = new Button { Text = text, Margin = margin ?? Edges.NONE };
        button.Click += (_, _) => onClick();
        return button;
    }
}
