namespace Japan.Core;

public static class Kana
{
    public static char ToHiragana(char ch) => ch is >= 'ァ' and <= 'ヶ' ? (char)(ch - 0x60) : ch;

    public static string ToHiragana(string text) => string.Create(text.Length, text, static (span, source) =>
    {
        for (var i = 0; i < source.Length; i++) span[i] = ToHiragana(source[i]);
    });

    public static bool IsKanji(char ch) =>
        ch is >= '\u4E00' and <= '\u9FFF' or >= '\u3400' and <= '\u4DBF' or '々' or '〆' or 'ヶ';

    public static bool ContainsKanji(string text) => text.Any(IsKanji);
}
