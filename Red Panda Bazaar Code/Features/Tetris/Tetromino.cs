using Microsoft.Xna.Framework;

namespace Red_Panda_Bazaar_Code.Features.Tetris;

/// <summary>
/// 7 种俄罗斯方块的静态定义:形状矩阵与配色。
/// 矩阵行主序(<c>shape[row, col]</c>),方阵边长 I 为 4、O 为 2、其余为 3;
/// 顺时针旋转公式 <c>new[r, c] = old[N - 1 - c, r]</c>。
/// </summary>
public static class Tetromino
{
    /// <summary>方块种类数(I J L O S T Z)。</summary>
    public const int KindCount = 7;

    private static readonly string[] ShapeText =
    {
        "0000/1111/0000/0000", // I
        "100/111/000",         // J
        "001/111/000",         // L
        "11/11",               // O
        "011/110/000",         // S
        "010/111/000",         // T
        "110/011/000",         // Z
    };

    /// <summary>各方块配色(青、蓝、橙、黄、绿、紫、红)。</summary>
    public static readonly Color[] Colors =
    {
        new(0, 188, 212),
        new(48, 92, 222),
        new(255, 148, 28),
        new(255, 205, 0),
        new(76, 175, 80),
        new(156, 80, 222),
        new(229, 57, 53),
    };

    private static readonly List<bool[,]> Shapes = ParseShapes();

    /// <summary>只读访问方块种类 <paramref name="kind"/> 的缓存初始矩阵(调用方不得修改)。</summary>
    public static bool[,] PeekShape(int kind) => Shapes[kind];

    /// <summary>获取方块种类 <paramref name="kind"/> 的初始形状副本(旋转/落子可自由改写)。</summary>
    public static bool[,] GetShape(int kind) => (bool[,])Shapes[kind].Clone();

    /// <summary>顺时针旋转方阵,返回新矩阵。</summary>
    public static bool[,] RotateCW(bool[,] shape)
    {
        var n = shape.GetLength(0);
        var rotated = new bool[n, n];
        for (var r = 0; r < n; r++)
        for (var c = 0; c < n; c++)
            rotated[r, c] = shape[n - 1 - c, r];
        return rotated;
    }

    private static List<bool[,]> ParseShapes()
    {
        var shapes = new List<bool[,]>(KindCount);
        for (var kind = 0; kind < KindCount; kind++)
        {
            var rows = ShapeText[kind].Split('/');
            var n = rows.Length;
            var matrix = new bool[n, n];
            for (var r = 0; r < n; r++)
            for (var c = 0; c < n; c++)
                matrix[r, c] = rows[r][c] == '1';
            shapes.Add(matrix);
        }
        return shapes;
    }
}
