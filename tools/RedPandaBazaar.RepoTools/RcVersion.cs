using System.Text.RegularExpressions;

namespace Red_Panda_Bazaar_RepoTools;

/// <summary>
/// RC 版本号派生（MakeRcVersion.py 的 C# 移植）：
/// 取 csproj &lt;Version&gt; 的 x.y.z 语义化部分，拼成 x.y.z-rc.&lt;UTC 日期&gt;.&lt;运行号&gt;。
/// 只输出到 stdout 的单独一行，不改写任何文件。
/// </summary>
public static class RcVersion
{
    private static readonly Regex VersionRe = new(@"<Version>\s*([^<\s]+)\s*</Version>");
    private static readonly Regex BaseRe = new(@"^(\d+\.\d+\.\d+)");

    /// <summary>从 csproj 读 &lt;Version&gt; 并取出 x.y.z 部分；找不到或格式不对时抛 <see cref="InvalidDataException"/>。</summary>
    public static string ReadBaseVersion(string csprojPath)
    {
        var text = File.ReadAllText(csprojPath);
        var match = VersionRe.Match(text);
        if (!match.Success)
            throw new InvalidDataException($"{Path.GetFileName(csprojPath)} 中没有找到 <Version> 属性");
        var baseMatch = BaseRe.Match(match.Groups[1].Value);
        if (!baseMatch.Success)
            throw new InvalidDataException($"<Version> '{match.Groups[1].Value}' 不以 x.y.z 开头，无法生成 RC 版本号");
        return baseMatch.Groups[1].Value;
    }

    public static string Derive(string baseVersion, string date, string run) =>
        $"{baseVersion}-rc.{date}.{run}";
}
