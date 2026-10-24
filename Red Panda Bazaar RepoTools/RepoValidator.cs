using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Red_Panda_Bazaar_RepoTools;

public sealed record ValidationFailure(string? File, int? Line, string Message);

/// <summary>校验的运行环境；CI 由 Program 从环境变量组装，测试直接构造。</summary>
public sealed class ValidatorOptions
{
    public string RepoRoot { get; init; } = "";

    /// <summary>GITHUB_ACTIONS：存在时按 GitHub Actions 注解格式输出失败项。</summary>
    public bool GitHubActions { get; init; }

    /// <summary>BASE_SHA：PR 基线提交，驱动 CP 只读检查。</summary>
    public string? BaseSha { get; init; }

    /// <summary>ALLOW_CP_CHANGE：为真时豁免 CP 只读检查（CP 上游同步）。</summary>
    public bool AllowCpChange { get; init; }

    /// <summary>GITHUB_STEP_SUMMARY：存在时向该文件追加 markdown 摘要。</summary>
    public string? StepSummaryPath { get; init; }

    /// <summary>进度输出；测试传 null 静默运行。</summary>
    public TextWriter? Out { get; init; }
}

/// <summary>
/// Red Panda Bazaar 仓库一致性校验（ValidateRepo.py 的 C# 移植）：
/// JSONC 可解析且无重复键、i18n 双文件键集合 / 顺序 / 占位符一致、
/// I18nKeys.cs 与 default.json 双向对应、manifest 必填字段与 CP 必需依赖、
/// csproj 的 TFM 与版本号、PR 不改动只读的 [CP]Red Panda Bazaar/。
/// </summary>
public static class RepoValidator
{
    private static readonly Regex PlaceholderRe = new(@"\{\{([^{}]+)\}\}", RegexOptions.Compiled);
    private static readonly Regex ConstRe = new(
        @"public\s+const\s+string\s+(\w+)\s*=\s*""((?:[^""\\]|\\.)*)""\s*;", RegexOptions.Compiled);

    public static int Run(ValidatorOptions options)
    {
        var ctx = new Context(options);
        var parsed = CheckJsonFiles(ctx);
        CheckI18n(ctx, parsed);
        CheckI18nKeys(ctx, parsed.GetValueOrDefault(ctx.Paths.I18nDefault));
        CheckManifest(ctx, parsed);
        CheckCsproj(ctx);
        CheckCpReadonly(ctx);
        ctx.WriteLine();
        if (ctx.Failures.Count > 0)
        {
            ctx.WriteLine($"校验失败：{ctx.Failures.Count} 项未通过。");
            WriteSummary(ctx);
            return 1;
        }
        ctx.WriteLine("校验通过：全部检查项均满足。");
        WriteSummary(ctx);
        return 0;
    }

    // ------------------------------------------------------------------ #
    // 路径与输出
    // ------------------------------------------------------------------ #

    private sealed class Paths(string repoRoot)
    {
        // 属性初始化器不能引用同类其他实例属性，统一从 repoRoot 直接拼
        public string CodeDir { get; } = Path.Combine(repoRoot, "Red Panda Bazaar Code");
        public string CpDir { get; } = Path.Combine(repoRoot, "[CP]Red Panda Bazaar");

        public string I18nDefault { get; } = Path.Combine(repoRoot, "Red Panda Bazaar Code", "i18n", "default.json");
        public string I18nZh { get; } = Path.Combine(repoRoot, "Red Panda Bazaar Code", "i18n", "zh.json");
        public string I18nKeysCs { get; } = Path.Combine(repoRoot, "Red Panda Bazaar Code", "Constant", "I18nKeys.cs");
        public string ModManifest { get; } = Path.Combine(repoRoot, "Red Panda Bazaar Code", "manifest.json");
        public string Csproj { get; } = Path.Combine(repoRoot, "Red Panda Bazaar Code", "Red Panda Bazaar Code.csproj");
        public string CpManifest { get; } = Path.Combine(repoRoot, "[CP]Red Panda Bazaar", "manifest.json");
        public string CpContent { get; } = Path.Combine(repoRoot, "[CP]Red Panda Bazaar", "content.json");
        public string CpConfig { get; } = Path.Combine(repoRoot, "[CP]Red Panda Bazaar", "config.json");

        public string[] JsonFiles =>
        [
            ModManifest, I18nDefault, I18nZh, CpManifest, CpContent, CpConfig
        ];
    }

    private sealed class Context(ValidatorOptions options)
    {
        public readonly ValidatorOptions Options = options;
        public readonly Paths Paths = new(options.RepoRoot);
        public readonly List<ValidationFailure> Failures = [];

        public void WriteLine(string? line = null) => Options.Out?.WriteLine(line);

        public void Ok(string message) => Options.Out?.WriteLine($"[ OK ] {message}");

        public void Skip(string message) => Options.Out?.WriteLine($"[SKIP] {message}");

        public void Fail(string? path, string message, int? line = null)
        {
            var location = path is null ? null : Path.GetRelativePath(Options.RepoRoot, path).Replace('\\', '/');
            Failures.Add(new ValidationFailure(location, line, message));
            if (Options.Out is null)
                return;
            if (Options.GitHubActions)
            {
                var where = "";
                if (location is not null)
                    where += $" file={location}";
                if (line is not null)
                    where += $",line={line}";
                Options.Out.WriteLine($"::error{where}::{message}");
            }
            else
            {
                var prefix = location is not null && line is not null
                    ? $"{location}:{line}: "
                    : location is not null ? $"{location}: " : "";
                Options.Out.WriteLine($"[FAIL] {prefix}{message}");
            }
        }

        /// <summary>相对仓库根的路径（用于测试断言）。</summary>
        public string Rel(string path) =>
            Path.GetRelativePath(Options.RepoRoot, path).Replace('\\', '/');
    }

    private static string ReadText(string path) => File.ReadAllText(path);

    private static int? LineOfMatch(string text, Regex pattern)
    {
        var match = pattern.Match(text);
        if (!match.Success)
            return null;
        var line = 1;
        for (var k = 0; k < match.Index; k++)
        {
            if (text[k] == '\n')
                line++;
        }
        return line;
    }

    private static string KeyLinePattern(string key) =>
        @"^\s*""" + Regex.Escape(key) + @"""\s*:";

    // ------------------------------------------------------------------ #
    // 1. JSON 文件可解析性
    // ------------------------------------------------------------------ #

    private static Dictionary<string, JNode> CheckJsonFiles(Context ctx)
    {
        ctx.WriteLine("-- JSON 文件可解析性 --");
        var parsed = new Dictionary<string, JNode>();
        foreach (var path in ctx.Paths.JsonFiles)
        {
            if (!File.Exists(path))
            {
                ctx.Fail(path, "文件不存在");
                continue;
            }
            try
            {
                parsed[path] = Jsonc.LoadFile(path);
            }
            catch (JsoncException e)
            {
                ctx.Fail(path, $"JSON 解析失败：{e.Message}", e.Line);
            }
        }
        if (parsed.Count == ctx.Paths.JsonFiles.Length)
            ctx.Ok($"{ctx.Paths.JsonFiles.Length} 个 JSON 文件解析通过，且无重复键");
        return parsed;
    }

    // ------------------------------------------------------------------ #
    // 2. i18n 双文件一致性
    // ------------------------------------------------------------------ #

    private static void CheckI18n(Context ctx, Dictionary<string, JNode> parsed)
    {
        ctx.WriteLine("-- i18n 双文件一致性（键集合 / 键顺序 / 占位符） --");
        if (parsed.GetValueOrDefault(ctx.Paths.I18nDefault) is not JObject defaultObj ||
            parsed.GetValueOrDefault(ctx.Paths.I18nZh) is not JObject zhObj)
        {
            ctx.Skip("default.json 或 zh.json 无法解析，跳过");
            return;
        }

        var defaultKeys = defaultObj.Members.Select(m => m.Key).ToList();
        var zhKeys = zhObj.Members.Select(m => m.Key).ToList();
        var sameSet = defaultKeys.ToHashSet().SetEquals(zhKeys);
        var zhText = ReadText(ctx.Paths.I18nZh);

        if (!sameSet)
        {
            foreach (var key in defaultKeys.Except(zhKeys).OrderBy(s => s, StringComparer.Ordinal))
                ctx.Fail(ctx.Paths.I18nZh, $"缺少键 '{key}'（default.json 中存在）");
            foreach (var key in zhKeys.Except(defaultKeys).OrderBy(s => s, StringComparer.Ordinal))
                ctx.Fail(ctx.Paths.I18nDefault, $"缺少键 '{key}'（zh.json 中存在）");
        }
        else if (!defaultKeys.SequenceEqual(zhKeys))
        {
            for (var index = 0; index < defaultKeys.Count; index++)
            {
                var (expected, actual) = (defaultKeys[index], zhKeys[index]);
                if (expected == actual)
                    continue;
                ctx.Fail(
                    ctx.Paths.I18nZh,
                    $"键顺序与 default.json 不一致：第 {index + 1} 个键应为 '{expected}'，实际为 '{actual}'",
                    LineOfMatch(zhText, new Regex(KeyLinePattern(actual), RegexOptions.Multiline)));
                break;
            }
        }

        // 顺序不一致时只对交集做占位符比对，避免大量无关失败
        IEnumerable<string> keysToCheck = defaultKeys.SequenceEqual(zhKeys)
            ? defaultKeys
            : defaultKeys.Intersect(zhKeys).OrderBy(s => s, StringComparer.Ordinal);
        var mismatch = 0;
        foreach (var key in keysToCheck)
        {
            // i18n 值应为字符串；null 值按空串比对，让占位符检查报出差异
            var defaultTokens = Tokens(Str(defaultObj.Find(key)!.Value) ?? "");
            var zhTokens = Tokens(Str(zhObj.Find(key)!.Value) ?? "");
            if (defaultTokens.SetEquals(zhTokens))
                continue;
            mismatch++;
            ctx.Fail(
                ctx.Paths.I18nZh,
                $"{key} 的占位符与 default.json 不一致：" +
                $"default.json [{string.Join(", ", defaultTokens.OrderBy(s => s, StringComparer.Ordinal))}] / zh.json [{string.Join(", ", zhTokens.OrderBy(s => s, StringComparer.Ordinal))}]",
                LineOfMatch(zhText, new Regex(KeyLinePattern(key), RegexOptions.Multiline)));
        }

        if (sameSet && defaultKeys.SequenceEqual(zhKeys) && mismatch == 0)
            ctx.Ok($"两份文件键集合与顺序一致，共 {defaultKeys.Count} 个键，占位符全部匹配");
    }

    private static HashSet<string> Tokens(string value) =>
        PlaceholderRe.Matches(value).Select(m => m.Groups[1].Value).ToHashSet();

    // ------------------------------------------------------------------ #
    // 3. I18nKeys.cs 与 default.json 双向对应
    // ------------------------------------------------------------------ #

    private static void CheckI18nKeys(Context ctx, JNode? defaultNode)
    {
        ctx.WriteLine("-- I18nKeys.cs 与 default.json 双向对应 --");
        if (defaultNode is not JObject defaultObj)
        {
            ctx.Skip("default.json 无法解析，跳过");
            return;
        }
        if (!File.Exists(ctx.Paths.I18nKeysCs))
        {
            ctx.Fail(ctx.Paths.I18nKeysCs, "文件不存在");
            return;
        }

        var text = ReadText(ctx.Paths.I18nKeysCs);
        var constants = ConstRe.Matches(text)
            .Select(m => (Name: m.Groups[1].Value, Value: m.Groups[2].Value))
            .ToList();
        if (constants.Count == 0)
        {
            ctx.Fail(ctx.Paths.I18nKeysCs, "未解析到任何 public const string 常量");
            return;
        }

        foreach (var name in constants.GroupBy(c => c.Name).Where(g => g.Count() > 1).Select(g => g.Key).OrderBy(s => s, StringComparer.Ordinal))
            ctx.Fail(ctx.Paths.I18nKeysCs, $"常量 {name} 重复定义");

        var constantValues = constants.Select(c => c.Value).ToHashSet();
        var fileKeys = defaultObj.Members.Select(m => m.Key).ToHashSet();
        foreach (var value in constantValues.Except(fileKeys).OrderBy(s => s, StringComparer.Ordinal))
            ctx.Fail(ctx.Paths.I18nKeysCs, $"常量值 '{value}' 在 default.json 中不存在");
        foreach (var key in fileKeys.Except(constantValues).OrderBy(s => s, StringComparer.Ordinal))
            ctx.Fail(ctx.Paths.I18nDefault, $"i18n 键 '{key}' 在 I18nKeys.cs 中没有对应常量");

        if (constantValues.SetEquals(fileKeys))
            ctx.Ok($"{constants.Count} 个常量与 {fileKeys.Count} 个 i18n 键双向对应");
    }

    // ------------------------------------------------------------------ #
    // 4. manifest.json
    // ------------------------------------------------------------------ #

    private static void CheckManifest(Context ctx, Dictionary<string, JNode> parsed)
    {
        ctx.WriteLine("-- manifest.json --");
        if (parsed.GetValueOrDefault(ctx.Paths.ModManifest) is not JObject manifest)
        {
            ctx.Skip("manifest.json 无法解析，跳过");
            return;
        }
        var before = ctx.Failures.Count;

        foreach (var field in new[] { "Name", "Author", "Version", "Description", "UniqueID", "EntryDll", "MinimumApiVersion" })
        {
            if (string.IsNullOrWhiteSpace(Str(manifest.Get(field))))
                ctx.Fail(ctx.Paths.ModManifest, $"字段 {field} 缺失或为空");
        }

        var version = Str(manifest.Get("Version"));
        if (version is not null &&
            version != "%ProjectVersion%" &&
            !Regex.IsMatch(version, @"^\d+\.\d+\.\d+([.\-][0-9A-Za-z.\-]+)?$"))
        {
            // 正常情况是 %ProjectVersion% 占位符，实际值取自 csproj <Version>
            ctx.Fail(
                ctx.Paths.ModManifest,
                $"版本号 '{version}' 应为 %ProjectVersion% 占位符，或符合 x.y.z / x.y.z-beta / x.y.z-rc.<日期>.<run> 格式");
        }

        var uniqueId = Str(manifest.Get("UniqueID"));
        if (uniqueId is not null && !Regex.IsMatch(uniqueId, @"^[A-Za-z0-9_.]+$"))
            ctx.Fail(ctx.Paths.ModManifest, $"UniqueID '{uniqueId}' 含非法字符");

        var minApi = Str(manifest.Get("MinimumApiVersion"));
        if (minApi is not null && !minApi.StartsWith("4."))
            ctx.Fail(ctx.Paths.ModManifest, $"MinimumApiVersion 应为 SMAPI 4.x，实际 '{minApi}'");

        var expectedDll = Path.GetFileNameWithoutExtension(ctx.Paths.Csproj) + ".dll";
        var entryDll = Str(manifest.Get("EntryDll"));
        if (entryDll != expectedDll)
            ctx.Fail(ctx.Paths.ModManifest, $"EntryDll 应为 '{expectedDll}'（csproj 项目名），实际 '{entryDll}'");

        if (manifest.Find("Dependencies")?.Value is not JArray dependencies || dependencies.Items.Count == 0)
        {
            ctx.Fail(ctx.Paths.ModManifest, "Dependencies 缺失或为空");
            return;
        }

        var cpId = parsed.GetValueOrDefault(ctx.Paths.CpManifest) is JObject cpManifest
            ? Str(cpManifest.Get("UniqueID"))
            : "Lilaoliu.RedPandaBazaar";
        var requiredIds = dependencies.Items
            .OfType<JObject>()
            .Where(dep => dep.Find("IsRequired")?.Value is JBool { Value: true })
            .Select(dep => Str(dep.Get("UniqueID")))
            .ToHashSet();
        if (!requiredIds.Contains(cpId))
            ctx.Fail(ctx.Paths.ModManifest, $"必须以 IsRequired: true 声明对 CP 包 '{cpId}' 的依赖（缺失会导致游戏启动硬报错）");

        if (ctx.Failures.Count == before)
            ctx.Ok($"清单字段、版本号 {version} 与对 CP 包 '{cpId}' 的必需依赖均正常");
    }

    // ------------------------------------------------------------------ #
    // 5. csproj
    // ------------------------------------------------------------------ #

    private static void CheckCsproj(Context ctx)
    {
        ctx.WriteLine("-- csproj --");
        if (!File.Exists(ctx.Paths.Csproj))
        {
            ctx.Fail(ctx.Paths.Csproj, "文件不存在");
            return;
        }
        var text = ReadText(ctx.Paths.Csproj);

        var tfmMatch = Regex.Match(text, @"<TargetFramework>\s*([^<\s]+)\s*</TargetFramework>");
        if (!tfmMatch.Success)
            ctx.Fail(ctx.Paths.Csproj, "未找到 TargetFramework");
        else if (tfmMatch.Groups[1].Value != "net6.0")
            ctx.Fail(ctx.Paths.Csproj,
                $"TargetFramework 必须为 net6.0（SMAPI 4.x），实际 '{tfmMatch.Groups[1].Value}'",
                LineOfMatch(text, new Regex(@"<TargetFramework>")));
        else
            ctx.Ok("TargetFramework = net6.0");

        // 版本号唯一来源：csproj <Version>，manifest 用 %ProjectVersion% 引用
        var versionMatch = Regex.Match(text, @"<Version>\s*([^<\s]+)\s*</Version>");
        if (!versionMatch.Success)
            ctx.Fail(ctx.Paths.Csproj, "未找到 <Version>（版本号的唯一来源，manifest 依赖它）");
        else if (!Regex.IsMatch(versionMatch.Groups[1].Value, @"^\d+\.\d+\.\d+(-[0-9A-Za-z.\-]+)?$"))
            ctx.Fail(ctx.Paths.Csproj,
                $"<Version> '{versionMatch.Groups[1].Value}' 不符合 x.y.z / x.y.z-beta 格式",
                LineOfMatch(text, new Regex(@"<Version>")));
        else
            ctx.Ok($"Version = {versionMatch.Groups[1].Value}");
    }

    // ------------------------------------------------------------------ #
    // 6. CP 内容包只读
    // ------------------------------------------------------------------ #

    private static void CheckCpReadonly(Context ctx)
    {
        ctx.WriteLine("-- [CP]Red Panda Bazaar/ 只读约束 --");
        if (ctx.Options.AllowCpChange)
        {
            ctx.Skip("已通过 ALLOW_CP_CHANGE=1 豁免（CP 上游同步）");
            return;
        }
        var isCi = string.IsNullOrWhiteSpace(ctx.Options.BaseSha) == false;
        if (!isCi)
        {
            ctx.Skip("未提供 BASE_SHA（本地运行或非 PR 事件）");
            return;
        }
        var baseSha = ctx.Options.BaseSha!.Trim();

        // 路径中的方括号会被 git 当作通配符，须显式声明为字面量
        var psi = new ProcessStartInfo("git",
            $"-C \"{ctx.Options.RepoRoot}\" diff --name-only {baseSha} HEAD -- :(literal)[CP]Red Panda Bazaar")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            ctx.Fail(null, $"无法与基线 {baseSha[..Math.Min(7, baseSha.Length)]} 比对（需完整 git 历史）：{stderr.Trim()}");
            return;
        }

        var changed = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(name => name.Trim())
            .Where(name => name.Length > 0)
            .ToList();
        if (changed.Count > 0)
        {
            var preview = string.Join("、", changed.Take(5));
            var suffix = changed.Count > 5 ? $" 等 {changed.Count} 个文件" : "";
            ctx.Fail(
                null,
                $"[CP]Red Panda Bazaar/ 为只读内容包，本次改动了 {preview}{suffix}；" +
                "若是同步上游请给 PR 打上 cp-change-approved 标签");
        }
        else
        {
            ctx.Ok("[CP]Red Panda Bazaar/ 未被改动");
        }
    }

    // ------------------------------------------------------------------ #
    // 摘要
    // ------------------------------------------------------------------ #

    private static void WriteSummary(Context ctx)
    {
        var path = ctx.Options.StepSummaryPath;
        if (string.IsNullOrEmpty(path))
            return;
        var lines = new List<string> { "## Red Panda Bazaar 一致性校验", "" };
        if (ctx.Failures.Count > 0)
        {
            lines.Add($"**{ctx.Failures.Count} 项未通过**");
            lines.AddRange(["", "| 位置 | 问题 |", "|---|---|"]);
            foreach (var (file, line, message) in ctx.Failures)
            {
                var where = file ?? "-";
                if (line is not null)
                    where += $":{line}";
                lines.Add($"| `{where}` | {message.Replace("|", "\\|")} |");
            }
        }
        else
        {
            lines.Add("全部检查通过。");
        }
        File.AppendAllText(path, string.Join("\n", lines) + "\n");
    }

    private static string? Str(JNode? node) => node switch
    {
        JString s => s.Value,
        JNumber n => n.Raw,
        JBool b => b.Value ? "true" : "false",
        _ => null,
    };
}
