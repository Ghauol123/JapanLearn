using System.Globalization;
using System.Text;

namespace Japan.Core;

public static class TextNormalizer
{
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var decomposed = text.Normalize(NormalizationForm.FormKC).ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        var previousBase = '\0';
        var lastWasSpace = true;

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
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace) sb.Append(' ');
                lastWasSpace = true;
                continue;
            }

            sb.Append(c);
            lastWasSpace = false;
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).Trim();
    }
}
