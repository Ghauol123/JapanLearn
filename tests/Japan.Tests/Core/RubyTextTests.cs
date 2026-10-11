using Japan.Core;

namespace Japan.Tests.Core;

public class RubyTextTests
{
    [Fact]
    public void Parse_SplitsPlainAndRubySegments()
    {
        var segments = RubyText.Parse("[会社|かいしゃ]へ[行|い]かずに");

        Assert.Equal(
            [new RubySegment("会社", "かいしゃ"), new RubySegment("へ", null), new RubySegment("行", "い"), new RubySegment("かずに", null)],
            segments);
    }

    [Fact]
    public void Parse_PlainText_ReturnsSingleSegment() =>
        Assert.Equal([new RubySegment("いいえ、そのつもりはありません。", null)], RubyText.Parse("いいえ、そのつもりはありません。"));

    [Fact]
    public void Parse_NullOrEmpty_ReturnsNoSegments()
    {
        Assert.Empty(RubyText.Parse(null));
        Assert.Empty(RubyText.Parse(""));
    }

    [Fact]
    public void Format_IsInverseOfParse()
    {
        const string ruby = "[熱|あつ]いうちに[食|た]べてください。";
        Assert.Equal(ruby, RubyText.Format(RubyText.Parse(ruby)));
    }

    [Fact]
    public void PlainText_StripsReadings() =>
        Assert.Equal("会社へ行かずに", RubyText.PlainText("[会社|かいしゃ]へ[行|い]かずに"));
}
