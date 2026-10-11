using Japan.Core;
using Japan.Importer.Furigana;

namespace Japan.Tests.Importer;

public sealed class FuriganaTests : IDisposable
{
    private readonly NMeCabFuriganaGenerator _generator = new();

    public void Dispose() => _generator.Dispose();

    [Theory]
    [InlineData("行か", "イカ", "[行|い]か")]
    [InlineData("お茶", "オチャ", "お[茶|ちゃ]")]
    [InlineData("会社", "カイシャ", "[会社|かいしゃ]")]
    [InlineData("一ヶ月", "イッカゲツ", "[一ヶ月|いっかげつ]")]
    [InlineData("召し上がり", "メシアガリ", "[召|め]し[上|あ]がり")]
    [InlineData("申し込み", "モウシコミ", "[申|もう]し[込|こ]み")]
    [InlineData("パソコン", "パソコン", "パソコン")]
    [InlineData("東京", null, "東京")]
    public void Split_PutsReadingOnKanjiOnly(string surface, string? reading, string expected) =>
        Assert.Equal(expected, RubyText.Format(NMeCabFuriganaGenerator.Split(surface, reading)));

    [Fact]
    public void ToRuby_AnnotatesSentence() =>
        Assert.Equal("[会社|かいしゃ]へ[行|い]かずに", _generator.ToRuby("会社へ行かずに"));

    [Theory]
    [InlineData("Ａ：いいえ、そのつもりはありません。")]
    [InlineData("日本語 と 英語")]
    [InlineData("最近、結婚したがらない若者が増えています。")]
    [InlineData("１週間ぐらい休みを取りますか。")]
    public void ToRuby_PreservesOriginalText(string sentence) =>
        Assert.Equal(sentence, RubyText.PlainText(_generator.ToRuby(sentence)));

    [Fact]
    public void ToRuby_AppliesOverrides()
    {
        using var generator = new NMeCabFuriganaGenerator(new Dictionary<string, string> { ["金閣寺"] = "きんかくじ", ["一度"] = "いちど" });

        var ruby = generator.ToRuby("京都にいるうちに一度金閣寺に行ってみたい。");

        Assert.Contains("[金閣寺|きんかくじ]", ruby);
        Assert.Contains("[一度|いちど]", ruby);
        Assert.Equal("京都にいるうちに一度金閣寺に行ってみたい。", RubyText.PlainText(ruby));
    }

    [Fact]
    public void Apply_FillsOriginalAndEnrichmentExamples()
    {
        var book = FuriganaApplier.Apply(TestBooks.Sample(), new FakeGenerator());

        var point = book.Lessons[1].Points[0];
        Assert.Equal("R:熱いうちに食べてください。", point.Examples[0].Ruby);
        Assert.Equal("R:若いうちに旅行したい。", point.Enrichment!.Examples[0].Ruby);
        Assert.Equal("R:夏休みの間に、富士山に登ります。", book.Lessons[1].Points[1].Examples[0].Ruby);
    }

    [Fact]
    public void Overrides_Load_MissingFileIsEmpty_BadJsonNamesFile()
    {
        var dir = Directory.CreateTempSubdirectory("furi-").FullName;
        try
        {
            Assert.Empty(FuriganaOverrides.Load(Path.Combine(dir, "furigana-overrides.json")));

            var bad = Path.Combine(dir, "furigana-overrides.json");
            File.WriteAllText(bad, "{ \"金閣寺\": ");
            var ex = Assert.Throws<InvalidDataException>(() => FuriganaOverrides.Load(bad));
            Assert.Contains("furigana-overrides.json", ex.Message);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private sealed class FakeGenerator : IFuriganaGenerator
    {
        public string ToRuby(string japanese) => "R:" + japanese;
    }
}
