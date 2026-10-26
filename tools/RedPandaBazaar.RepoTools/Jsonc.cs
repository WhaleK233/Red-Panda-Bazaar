using System.Globalization;
using System.Text;

namespace Red_Panda_Bazaar_RepoTools;

/// <summary>JSON 节点：校验只关心结构与字符串值，数值保留原文。</summary>
public abstract record JNode;

public sealed record JString(string Value) : JNode;

public sealed record JNumber(string Raw) : JNode;

public sealed record JBool(bool Value) : JNode;

public sealed record JNull : JNode;

public sealed record JArray(List<JNode> Items) : JNode;

/// <summary>对象成员按出现顺序保留（i18n 键序校验依赖这一点），并记录键所在行号。</summary>
public sealed record JObject(List<JMember> Members) : JNode
{
    public JMember? Find(string key) => Members.FirstOrDefault(m => m.Key == key);

    public JNode? Get(string key) => Find(key)?.Value;
}

public sealed record JMember(string Key, JNode Value, int Line);

/// <summary>JSON 解析错误，Line 为 1 起始行号。</summary>
public sealed class JsoncException(string message, int line)
    : Exception($"{message}（第 {line} 行）")
{
    public int Line { get; } = line;
}

/// <summary>
/// SMAPI 风格 JSONC 的解析：先经 <see cref="BlankComments"/> 与 <see cref="BlankTrailingCommas"/>
/// 把注释和尾逗号替换为等长空白（保持行号），再走严格 JSON 语法。拒绝重复键。
/// </summary>
public static class Jsonc
{
    /// <summary>把 // 与 /* */ 注释替换为等长空白，保持行列号不变。</summary>
    public static string BlankComments(string text)
    {
        var chars = text.ToCharArray();
        var (i, n) = (0, text.Length);
        var inString = false;
        while (i < n)
        {
            var ch = text[i];
            if (inString)
            {
                if (ch == '\\')
                { i += 2; continue; }
                if (ch == '"')
                    inString = false;
                i++;
                continue;
            }
            if (ch == '"')
            { inString = true; i++; continue; }
            if (ch == '/' && i + 1 < n && text[i + 1] == '*')
            {
                var end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                end = end < 0 ? n : end + 2;
                BlankRange(chars, i, end);
                i = end;
                continue;
            }
            if (ch == '/' && i + 1 < n && text[i + 1] == '/')
            {
                var end = text.IndexOf('\n', i);
                end = end < 0 ? n : end;
                BlankRange(chars, i, end);
                i = end;
                continue;
            }
            i++;
        }
        return new string(chars);
    }

    /// <summary>把对象 / 数组末尾的多余逗号替换为空格，保持行列号不变。</summary>
    public static string BlankTrailingCommas(string text)
    {
        var chars = text.ToCharArray();
        var (i, n) = (0, text.Length);
        var inString = false;
        while (i < n)
        {
            var ch = text[i];
            if (inString)
            {
                if (ch == '\\')
                { i += 2; continue; }
                if (ch == '"')
                    inString = false;
                i++;
                continue;
            }
            if (ch == '"')
            { inString = true; i++; continue; }
            if (ch == ',')
            {
                var j = i + 1;
                while (j < n && text[j] is ' ' or '\t' or '\r' or '\n')
                    j++;
                if (j < n && text[j] is '}' or ']')
                    chars[i] = ' ';
            }
            i++;
        }
        return new string(chars);
    }

    private static void BlankRange(char[] chars, int start, int end)
    {
        for (var k = start; k < end; k++)
        {
            if (chars[k] != '\n')
                chars[k] = ' ';
        }
    }

    /// <summary>解析（应先做过注释与尾逗号空白化）；失败抛 <see cref="JsoncException"/>。</summary>
    public static JNode Parse(string text) => new Parser(text).ParseRoot();

    /// <summary>读文件（自动去 BOM）→ 空白化 → 解析，一步到位。</summary>
    public static JNode LoadFile(string path)
    {
        var text = File.ReadAllText(path);
        return Parse(BlankTrailingCommas(BlankComments(text)));
    }

    private sealed class Parser
    {
        private readonly string s;

        internal Parser(string s)
        {
            this.s = s;
        }

        internal JNode ParseRoot()
        {
            var value = ParseValue();
            SkipWhitespace();
            if (pos < s.Length)
                throw Err("多余的字符");
            return value;
        }

        private int pos;

        private char Peek => pos < s.Length ? s[pos] : throw Err("意外的输入结尾");

        private JNode ParseValue()
        {
            SkipWhitespace();
            return Peek switch
            {
                '{' => ParseObject(),
                '[' => ParseArray(),
                '"' => new JString(ParseString()),
                't' => Literal("true", new JBool(true)),
                'f' => Literal("false", new JBool(false)),
                'n' => Literal("null", new JNull()),
                _ => ParseNumber(),
            };
        }

        private JNode Literal(string word, JNode value)
        {
            if (string.CompareOrdinal(s, pos, word, 0, word.Length) != 0)
                throw Err($"非法字面量（应为 {word}）");
            pos += word.Length;
            return value;
        }

        private JObject ParseObject()
        {
            pos++; // '{'
            var members = new List<JMember>();
            SkipWhitespace();
            if (Peek == '}')
            { pos++; return new JObject(members); }
            while (true)
            {
                SkipWhitespace();
                if (Peek != '"')
                    throw Err("期望对象键字符串");
                var keyStart = pos;
                var key = ParseString();
                var keyLine = LineOf(keyStart);
                SkipWhitespace();
                if (Peek != ':')
                    throw Err("期望 ':'");
                pos++;
                var value = ParseValue();
                if (members.Any(m => m.Key == key))
                    throw Err($"重复的键 '{key}'");
                members.Add(new JMember(key, value, keyLine));
                SkipWhitespace();
                var ch = Peek;
                if (ch == ',')
                { pos++; continue; }
                if (ch == '}')
                { pos++; return new JObject(members); }
                throw Err("期望 ',' 或 '}'");
            }
        }

        private JArray ParseArray()
        {
            pos++; // '['
            var items = new List<JNode>();
            SkipWhitespace();
            if (Peek == ']')
            { pos++; return new JArray(items); }
            while (true)
            {
                items.Add(ParseValue());
                SkipWhitespace();
                var ch = Peek;
                if (ch == ',')
                { pos++; continue; }
                if (ch == ']')
                { pos++; return new JArray(items); }
                throw Err("期望 ',' 或 ']'");
            }
        }

        private string ParseString()
        {
            pos++; // '"'
            var sb = new StringBuilder();
            while (true)
            {
                if (pos >= s.Length)
                    throw Err("字符串未闭合");
                var c = s[pos];
                if (c == '"')
                { pos++; return sb.ToString(); }
                if (c == '\n')
                    throw Err("字符串内出现换行");
                if (c == '\\')
                {
                    if (pos + 1 >= s.Length)
                        throw Err("字符串未闭合");
                    var escape = s[pos + 1];
                    pos += 2;
                    switch (escape)
                    {
                        case '"':
                            sb.Append('"');
                            break;
                        case '\\':
                            sb.Append('\\');
                            break;
                        case '/':
                            sb.Append('/');
                            break;
                        case 'b':
                            sb.Append('\b');
                            break;
                        case 'f':
                            sb.Append('\f');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'u':
                            if (pos + 4 > s.Length ||
                                !ushort.TryParse(s.Substring(pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                                throw Err("非法的 \\u 转义");
                            sb.Append((char)code);
                            pos += 4;
                            break;
                        default:
                            throw Err($"非法转义 \\{escape}");
                    }
                    continue;
                }
                sb.Append(c);
                pos++;
            }
        }

        private JNode ParseNumber()
        {
            var start = pos;
            if (Peek == '-')
                pos++;
            if (Peek == '0')
                pos++;
            else if (char.IsDigit(Peek))
            { while (pos < s.Length && char.IsDigit(s[pos])) pos++; }
            else
                throw Err("非法数字");
            if (pos < s.Length && s[pos] == '.')
            {
                pos++;
                if (pos >= s.Length || !char.IsDigit(s[pos]))
                    throw Err("数字缺少小数部分");
                while (pos < s.Length && char.IsDigit(s[pos]))
                    pos++;
            }
            if (pos < s.Length && s[pos] is 'e' or 'E')
            {
                pos++;
                if (pos < s.Length && s[pos] is '+' or '-')
                    pos++;
                if (pos >= s.Length || !char.IsDigit(s[pos]))
                    throw Err("指数缺少数字");
                while (pos < s.Length && char.IsDigit(s[pos]))
                    pos++;
            }
            return new JNumber(s[start..pos]);
        }

        private void SkipWhitespace()
        {
            while (pos < s.Length && s[pos] is ' ' or '\t' or '\r' or '\n')
                pos++;
        }

        private JsoncException Err(string message) => new(message, LineOf(pos));

        private int LineOf(int offset)
        {
            var line = 1;
            for (var k = 0; k < offset && k < s.Length; k++)
            {
                if (s[k] == '\n')
                    line++;
            }
            return line;
        }
    }
}
