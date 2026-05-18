using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Framework.UI.Components;

/// <summary>复选框组件，带文字标签。</summary>
public class UiCheckbox : UiElement
{
    private const int CheckboxSize = 32;
    private const int Gap = 8;

    public string Text { get; set; }
    public bool Checked { get; set; }
    public bool Enabled { get; set; } = true;
    public Action<bool>? OnToggle { get; set; }

    /// <summary>点击播放的音效名，null 表示不播放。</summary>
    public string? ClickSound { get; set; } = "drumkit6";

    public UiCheckbox(string text, bool isChecked = false, Action<bool>? onToggle = null)
    {
        Text = text;
        Checked = isChecked;
        OnToggle = onToggle;
    }

    public override bool IsFocusable => Enabled && Visible;

    public override void Update(int mouseX, int mouseY)
    {
        IsHovered = Bounds.Contains(mouseX, mouseY);
    }

    public override void Arrange()
    {
        var textSize = Game1.smallFont.MeasureString(Text);
        Width = CheckboxSize + Gap + (int)textSize.X;
        Height = Math.Max(CheckboxSize, (int)textSize.Y);
    }

    public override void Draw(SpriteBatch b)
    {
        if (!Visible) return;

        var alpha = Enabled ? 1f : 0.5f;
        var baseColor = Color.White * alpha;

        // 复选框背景
        var cbRect = new Rectangle(X, Y + (Height - CheckboxSize) / 2, CheckboxSize, CheckboxSize);
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 396, 15, 15),
            cbRect.X, cbRect.Y, cbRect.Width, cbRect.Height, baseColor, 3f);

        // 勾选标记
        if (Checked)
        {
            b.Draw(Game1.mouseCursors, new Vector2(cbRect.X + 8, cbRect.Y + 8),
                new Rectangle(236, 420, 18, 18), baseColor, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0.5f);
        }

        // 文字
        var textColor = (Enabled ? Game1.textColor : Color.Gray) * alpha;
        var textPos = new Vector2(X + CheckboxSize + Gap, Y + (Height - Game1.smallFont.MeasureString(Text).Y) / 2);
        Utility.drawTextWithShadow(b, Text, Game1.smallFont, textPos, textColor);

        // 悬停 / 焦点高亮边框
        if (IsHovered || Focused)
        {
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(375, 357, 3, 3),
                cbRect.X - 4, cbRect.Y - 4, cbRect.Width + 8, cbRect.Height + 8, Color.Wheat * 0.6f, 3f);
        }
    }

    public override bool HandleClick(int x, int y)
    {
        if (!Visible || !Enabled) return false;

        // 点击复选框区域或文字区域
        if (!Bounds.Contains(x, y)) return false;

        if (ClickSound != null)
            Game1.playSound(ClickSound);

        Checked = !Checked;
        OnToggle?.Invoke(Checked);
        return true;
    }
}
