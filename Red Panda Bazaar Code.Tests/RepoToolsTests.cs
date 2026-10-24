using Red_Panda_Bazaar_RepoTools;
using Xunit;

namespace Red_Panda_Bazaar_Code.Tests;

public class JsoncTests
{
    private static JNode Parse(string text) =>
        Jsonc.Parse(Jsonc.BlankTrailingCommas(Jsonc.BlankComments(text)));

    [Fact]
    public void CommentsAndTrailingCommasAreBlanked()
    {
        const string source = @"{
  // 行注释
  ""a"": 1, /* 块
  注释 */
  ""b"": [2, 3,],
}";
        var parsed = (JObject)Parse(source);
        Assert.Equal("1", ((JNumber)parsed.Find("a")!.Value).Raw);
        var array = (JArray)parsed.Find("b")!.Value;
        Assert.Equal(2, array.Items.Count);
        Assert.Equal("2", ((JNumber)array.Items[0]).Raw);
    }

    [Fact]
    public void CommentMarkersInsideStringsArePreserved()
    {
        var parsed = (JObject)Parse("""{"a": "// 不是注释", "b": "/* 也不是注释",}""");
        Assert.Equal("// 不是注释", ((JString)parsed.Find("a")!.Value).Value);
        Assert.Equal("/* 也不是注释", ((JString)parsed.Find("b")!.Value).Value);
    }

    [Fact]
    public void DuplicateKeysAreRejected()
    {
        Assert.Throws<JsoncException>(() => Parse("""{"a": 1, "a": 2}"""));
    }

    [Fact]
    public void CommentBlankingKeepsLineNumbers()
    {
        const string source = "line1\n// comment\nline3";
        var blanked = Jsonc.BlankComments(source);
        Assert.Equal(new[] { "line1", new string(' ', "// comment".Length), "line3" },
            blanked.Split('\n'));
    }

    [Fact]
    public void ObjectMembersKeepOrderAndLine()
    {
        var parsed = (JObject)Parse("{\n  \"first\": 1,\n  \"second\": 2\n}");
        Assert.Equal(["first", "second"], parsed.Members.Select(m => m.Key));
        Assert.Equal(3, parsed.Find("second")!.Line);
    }
}

public class RcVersionTests : IDisposable
{
    // py 版测试为绕过本机临时目录 ACL 用测试目录放临时文件，C# 沿用同一思路
    private readonly string tempFile = Path.Combine(Path.GetTempPath(), $"rpab-csproj-{Guid.NewGuid():N}.tmp");

    private string WriteCsproj(string content)
    {
        File.WriteAllText(tempFile, content);
        return tempFile;
    }

    [Theory]
    [InlineData("<Project><Version>1.2.0-Beta</Version></Project>", "1.2.0")]
    [InlineData("<Version>2.0.1-RC.1</Version>", "2.0.1")]
    [InlineData("<Project>\n  <Version>  3.4.5  </Version>\n</Project>", "3.4.5")]
    public void ReadBaseVersionTakesSemanticPart(string content, string expected)
    {
        Assert.Equal(expected, RcVersion.ReadBaseVersion(WriteCsproj(content)));
    }

    [Theory]
    [InlineData("<Project><Version>beta</Version></Project>")]
    [InlineData("<Project><PropertyGroup /></Project>")]
    public void ReadBaseVersionRejectsInvalidInput(string content)
    {
        Assert.Throws<InvalidDataException>(() => RcVersion.ReadBaseVersion(WriteCsproj(content)));
    }

    [Fact]
    public void DeriveConcatenatesRcVersion()
    {
        Assert.Equal("1.2.0-rc.20261024.7", RcVersion.Derive("1.2.0", "20261024", "7"));
    }

    public void Dispose()
    {
        if (File.Exists(tempFile))
            File.Delete(tempFile);
    }
}

/// <summary>对当前检出副本运行完整仓库校验，作为 CI 的冒烟测试（移植自 test_repo_smoke.py）。</summary>
public class RepoSmokeTests
{
    [Fact]
    public void ValidateRepoPasses()
    {
        var repoRoot = RepoLayout.FindRepoRoot(AppContext.BaseDirectory);
        var exitCode = RepoValidator.Run(new ValidatorOptions
        {
            RepoRoot = repoRoot,
            Out = null, // 静默：失败详情由断言消息给出
        });
        Assert.True(exitCode == 0,
            $"仓库校验未通过（在 {repoRoot} 上运行；本地可用 dotnet run --project \"Red Panda Bazaar RepoTools\" -- validate 查看详情）");
    }
}
