using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Framework.UI.Components;

public class UiTable : UiElement
{
    public List<UiTableColumn> Columns { get; } = new();
    public int Spacing { get; set; } = 8;
    public int CellPadding { get; set; } = 8;
    public bool ShowHeader { get; set; } = true;

    /// <summary>视口最大高度，null 表示不限制。</summary>
    public int? ScrollMaxHeight { get; set; }

    private UiElement? _tree;
    private bool _treeDirty = true;
    private readonly List<string?[]> _rows = new();

    public void AddRow(params string[] cells)
    {
        var row = new string?[Columns.Count];
        for (var i = 0; i < cells.Length && i < Columns.Count; i++)
            row[i] = cells[i];
        _rows.Add(row);
        _treeDirty = true;
    }

    public void ClearRows()
    {
        _rows.Clear();
        _treeDirty = true;
        _tree = null;
    }

    public override int ChildCount => _tree != null ? 1 : 0;
    public override UiElement? GetChild(int index) => index == 0 ? _tree : null;

    public override void Measure()
    {
        if (Columns.Count == 0) return;

        if (_treeDirty)
            RebuildTree();

        _tree!.Measure();
        Width = _tree.Width;
        Height = _tree.Height;
    }

    public override void Arrange()
    {
        if (_tree == null) return;

        _tree.X = X;
        _tree.Y = Y;
        _tree.Arrange();
        Width = _tree.Width;
        Height = _tree.Height;
    }

    private void RebuildTree()
    {
        // 1. 计算列宽
        var widths = new int[Columns.Count];
        for (var i = 0; i < Columns.Count; i++)
        {
            var col = Columns[i];
            if (col.Width > 0)
            {
                widths[i] = col.Width;
            }
            else
            {
                var maxW = (int)Game1.smallFont.MeasureString(col.Header).X + CellPadding;
                foreach (var row in _rows)
                {
                    if (row[i] != null)
                        maxW = Math.Max(maxW, (int)Game1.smallFont.MeasureString(row[i]!).X + CellPadding);
                }
                widths[i] = Math.Max(maxW, col.MinWidth);
            }
        }

        // 2. 构建内部树
        var body = new UiColumn { Spacing = 0 };

        // 表头
        if (ShowHeader)
        {
            var headerRow = new UiRow { Spacing = Spacing };
            for (var i = 0; i < Columns.Count; i++)
            {
                var col = Columns[i];
                headerRow.Add(new UiText(col.Header)
                {
                    MinWidth = widths[i],
                    HorizontalAlignment = col.Align,
                });
            }
            body.Add(headerRow);
        }

        // 数据行
        var dataColumn = new UiColumn();
        foreach (var rowCells in _rows)
        {
            var uiRow = new UiRow { Spacing = Spacing };
            for (var i = 0; i < Columns.Count; i++)
            {
                var col = Columns[i];
                var text = rowCells[i] ?? "";
                uiRow.Add(new UiText(text)
                {
                    MinWidth = widths[i],
                    HorizontalAlignment = col.Align,
                    Color = col.Bold ? Color.Black : Game1.textColor,
                });
            }
            dataColumn.Add(uiRow);
        }

        if (ScrollMaxHeight.HasValue)
            body.Add(new UiScrollContainer { Child = dataColumn, MaxHeight = ScrollMaxHeight.Value });
        else
            body.Add(dataColumn);

        _tree = body;
        _tree.Parent = this;
        _treeDirty = false;
    }

    public override void Update(int mouseX, int mouseY)
    {
        IsHovered = Bounds.Contains(mouseX, mouseY);
        _tree?.Update(mouseX, mouseY);
    }

    public override void Draw(SpriteBatch b)
    {
        _tree?.Draw(b);
    }

    public override bool HandleClick(int x, int y)
    {
        return _tree?.HandleClick(x, y) ?? false;
    }

    public override bool HandleScroll(int direction)
    {
        return _tree?.HandleScroll(direction) ?? false;
    }
}
