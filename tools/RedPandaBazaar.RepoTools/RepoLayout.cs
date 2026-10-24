namespace Red_Panda_Bazaar_RepoTools;

/// <summary>仓库布局：定位仓库根（工具与测试共用）。</summary>
public static class RepoLayout
{
    /// <summary>
    /// 找仓库根：同时含 .git 与解决方案文件的目录。
    /// 判据不绑具体工程目录名，工程目录重组后依然成立。
    /// 先从程序集位置向上找（源内运行），失败再从当前工作目录向上找
    /// （CI 把工具 build -o 到仓库外时，靠调用处的 CWD 定位）。
    /// </summary>
    public static string FindRepoRoot(string start)
    {
        var fromAssembly = Find(start);
        return fromAssembly ?? Find(Environment.CurrentDirectory)
            ?? throw new InvalidOperationException(
                $"从 {start} 与 {Environment.CurrentDirectory} 向上均未找到仓库根（需含 .git 与 .sln 文件）");
    }

    private static string? Find(string start)
    {
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            var isRepoRoot =
                (Directory.Exists(Path.Combine(dir.FullName, ".git")) ||
                 File.Exists(Path.Combine(dir.FullName, ".git"))) &&
                Directory.EnumerateFiles(dir.FullName, "*.sln").Any();
            if (isRepoRoot)
                return dir.FullName;
        }
        return null;
    }
}
