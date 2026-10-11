using System.Text;
using System.Text.RegularExpressions;

namespace Japan.Core;

public readonly record struct RubySegment(string Text, string? Reading);

public static partial class RubyText
{
    [GeneratedRegex(@"\[([^\[\]|]+)\|([^\[\]|]+)\]")]
    private static partial Regex TokenRegex();

    public static IReadOnlyList<RubySegment> Parse(string? ruby)
    {
        var result = new List<RubySegment>();
        if (string.IsNullOrEmpty(ruby)) return result;

        var pos = 0;
        foreach (Match m in TokenRegex().Matches(ruby))
        {
            if (m.Index > pos) result.Add(new RubySegment(ruby[pos..m.Index], null));
            result.Add(new RubySegment(m.Groups[1].Value, m.Groups[2].Value));
            pos = m.Index + m.Length;
        }

        if (pos < ruby.Length) result.Add(new RubySegment(ruby[pos..], null));
        return result;
    }

    public static string Format(IEnumerable<RubySegment> segments)
    {
        var sb = new StringBuilder();
        foreach (var s in segments)
        {
            if (s.Reading is null) sb.Append(s.Text);
            else sb.Append('[').Append(s.Text).Append('|').Append(s.Reading).Append(']');
        }

        return sb.ToString();
    }

    public static string PlainText(string? ruby) => string.Concat(Parse(ruby).Select(s => s.Text));
}
