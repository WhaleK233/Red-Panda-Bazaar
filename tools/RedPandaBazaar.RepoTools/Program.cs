using Red_Panda_Bazaar_RepoTools;

var command = args.FirstOrDefault() ?? "validate";
switch (command)
{
    case "validate":
        return RepoValidator.Run(new ValidatorOptions
        {
            RepoRoot = RepoLayout.FindRepoRoot(AppContext.BaseDirectory),
            GitHubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") is not null,
            BaseSha = Environment.GetEnvironmentVariable("BASE_SHA"),
            AllowCpChange = Environment.GetEnvironmentVariable("ALLOW_CP_CHANGE") == "1",
            StepSummaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY"),
            Out = Console.Out,
        });

    case "rc-version":
        return RunRcVersion(args.Skip(1).ToList());

    default:
        Console.Error.WriteLine(
            $"未知命令 '{command}'。用法：\n" +
            "  RedPandaBazaar.RepoTools validate            仓库一致性校验\n" +
            "  RedPandaBazaar.RepoTools rc-version [--csproj <路径>] [--run <运行号>] [--date <YYYYMMDD>]");
        return 2;
}

static int RunRcVersion(List<string> args)
{
    var csproj = Path.Combine(RepoLayout.FindRepoRoot(AppContext.BaseDirectory),
        "src", "RedPandaBazaar", "RedPandaBazaar.csproj");
    var run = Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER") ?? "0";
    var date = DateTime.UtcNow.ToString("yyyyMMdd");
    for (var i = 0; i + 1 < args.Count; i += 2)
    {
        switch (args[i])
        {
            case "--csproj": csproj = args[i + 1]; break;
            case "--run": run = args[i + 1]; break;
            case "--date": date = args[i + 1]; break;
            default:
                Console.Error.WriteLine($"未知参数 '{args[i]}'");
                return 2;
        }
    }

    try
    {
        // 版本号要被 $(...) 捕获后原样注入 MSBuild，只能有单独一行输出
        Console.Write(RcVersion.Derive(RcVersion.ReadBaseVersion(csproj), date, run) + "\n");
        return 0;
    }
    catch (Exception error) when (error is IOException or InvalidDataException)
    {
        Console.Error.WriteLine($"Error! {error.Message}");
        return 1;
    }
}
