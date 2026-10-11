using System.Globalization;
using System.Text;

namespace Japan.Core;

public static class TextNormalizer
{
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var sb = new StringBuilder();
        foreach (var c in Fold(text)) AppendCollapsingSpaces(sb, c);
        return sb.ToString().Trim();
    }

    /// <summary>
    /// Finds <paramref name="normalizedQuery"/> (already passed through <see cref="Normalize"/>) inside
    /// <paramref name="raw"/> and returns the matching range of the original, un-normalized text.
    /// </summary>
    public static (int Start, int Length)? FindIn(string raw, string normalizedQuery)
    {
        if (normalizedQuery.Length == 0 || raw.Length == 0) return null;

        var sb = new StringBuilder(raw.Length);
        var starts = new List<int>(raw.Length);
        var ends = new List<int>(raw.Length);
        var elements = StringInfo.GetTextElementEnumerator(raw);
        while (elements.MoveNext())
        {
            var element = elements.GetTextElement();
            foreach (var c in Fold(element))
            {
                if (!AppendCollapsingSpaces(sb, c)) continue;
                starts.Add(elements.ElementIndex);
                ends.Add(elements.ElementIndex + element.Length);
            }
        }

        var at = sb.ToString().IndexOf(normalizedQuery, StringComparison.Ordinal);
        if (at < 0) return null;
        var start = starts[at];
        return (start, ends[at + normalizedQuery.Length - 1] - start);
    }

    private static string Fold(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormKC).ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        var previousBase = '\0';

        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                // Chỉ bỏ dấu đứng sau chữ Latin; dấu (゛゜) của kana phải giữ.
                if (previousBase is >= 'a' and <= 'z') continue;
                sb.Append(ch);
                continue;
            }

            previousBase = ch;
            if (ch is '~' or '〜') continue;

            var c = ch == 'đ' ? 'd' : Kana.ToHiragana(ch);
            sb.Append(char.IsWhiteSpace(c) ? ' ' : c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool AppendCollapsingSpaces(StringBuilder sb, char c)
    {
        if (c == ' ' && (sb.Length == 0 || sb[^1] == ' ')) return false;
        sb.Append(c);
        return true;
    }
}
