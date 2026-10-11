using System.Text.RegularExpressions;
using Japan.Core;
using NMeCab.Specialized;

namespace Japan.Importer.Furigana;

public sealed class NMeCabFuriganaGenerator : IFuriganaGenerator, IDisposable
{
    private readonly MeCabIpaDicTagger _tagger = MeCabIpaDicTagger.Create();
    private readonly IReadOnlyList<KeyValuePair<string, string>> _overrides;

    public NMeCabFuriganaGenerator(IReadOnlyDictionary<string, string>? overrides = null)
    {
        _overrides = (overrides ?? new Dictionary<string, string>())
            .OrderByDescending(kv => kv.Key.Length)
            .ToList();
    }

    public string ToRuby(string japanese)
    {
        var segments = new List<RubySegment>();
        AppendWithOverrides(japanese, segments);
        return RubyText.Format(segments);
    }

    public static IReadOnlyList<RubySegment> Split(string surface, string? reading)
    {
        if (!Kana.ContainsKanji(surface) || string.IsNullOrEmpty(reading) || reading == "*")
            return [new RubySegment(surface, null)];

        var kana = Kana.ToHiragana(reading);
        var folded = Kana.ToHiragana(surface);

        if (AlignRuns(surface, folded, kana) is { } aligned) return aligned;

        var prefix = 0;
        while (prefix < folded.Length && prefix < kana.Length && folded[prefix] == kana[prefix] && !Kana.IsKanji(surface[prefix]))
            prefix++;

        var suffix = 0;
        while (suffix < folded.Length - prefix && suffix < kana.Length - prefix
               && folded[^(suffix + 1)] == kana[^(suffix + 1)] && !Kana.IsKanji(surface[^(suffix + 1)]))
            suffix++;

        if (kana.Length - prefix - suffix <= 0) return [new RubySegment(surface, null)];

        var result = new List<RubySegment>(3);
        if (prefix > 0) result.Add(new RubySegment(surface[..prefix], null));
        result.Add(new RubySegment(surface[prefix..^suffix], kana[prefix..^suffix]));
        if (suffix > 0) result.Add(new RubySegment(surface[^suffix..], null));
        return result;
    }

    public void Dispose() => _tagger.Dispose();

    private static List<RubySegment>? AlignRuns(string surface, string folded, string kana)
    {
        var runs = new List<(int Start, int Length, bool Kanji)>();
        for (var i = 0; i < surface.Length; i++)
        {
            var kanji = Kana.IsKanji(surface[i]);
            if (runs.Count > 0 && runs[^1].Kanji == kanji) runs[^1] = runs[^1] with { Length = runs[^1].Length + 1 };
            else runs.Add((i, 1, kanji));
        }

        var pattern = "^" + string.Concat(runs.Select(r =>
            r.Kanji ? "(.+?)" : Regex.Escape(folded.Substring(r.Start, r.Length)))) + "$";
        var match = Regex.Match(kana, pattern);
        if (!match.Success) return null;

        var group = 1;
        return runs.Select(r => r.Kanji
            ? new RubySegment(surface.Substring(r.Start, r.Length), match.Groups[group++].Value)
            : new RubySegment(surface.Substring(r.Start, r.Length), null)).ToList();
    }

    private void AppendWithOverrides(string text, List<RubySegment> segments)
    {
        if (text.Length == 0) return;

        foreach (var (word, reading) in _overrides)
        {
            var at = text.IndexOf(word, StringComparison.Ordinal);
            if (at < 0) continue;
            AppendWithOverrides(text[..at], segments);
            segments.AddRange(Split(word, reading));
            AppendWithOverrides(text[(at + word.Length)..], segments);
            return;
        }

        // MeCab bỏ qua khoảng trắng, nên dò lại vị trí từng token để không làm mất ký tự gốc.
        var pos = 0;
        foreach (var node in _tagger.Parse(text))
        {
            var at = text.IndexOf(node.Surface, pos, StringComparison.Ordinal);
            if (at < 0) continue;
            if (at > pos) segments.Add(new RubySegment(text[pos..at], null));
            segments.AddRange(Split(node.Surface, node.Reading));
            pos = at + node.Surface.Length;
        }

        if (pos < text.Length) segments.Add(new RubySegment(text[pos..], null));
    }
}
