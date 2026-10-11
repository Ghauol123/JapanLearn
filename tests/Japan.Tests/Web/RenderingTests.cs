using System.Text.Encodings.Web;
using System.Text.Unicode;
using Japan.Web.Rendering;
using Microsoft.AspNetCore.Html;

namespace Japan.Tests.Web;

public class RenderingTests
{
    internal static string Html(IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, HtmlEncoder.Create(UnicodeRanges.All));
        return writer.ToString();
    }

    [Fact]
    public void Render_WrapsReadingsInRubyAndEncodesText() =>
        Assert.Equal("<ruby>漢字<rt>かんじ</rt></ruby>&lt;b&gt;", Html(RubyHtml.Render("[漢字|かんじ]<b>")));

    [Fact]
    public void Render_PlainText_HasNoRuby() =>
        Assert.Equal("いいえ。", Html(RubyHtml.Render("いいえ。")));

    [Fact]
    public void Mark_HighlightsAllLiteralMatchesIgnoringCaseAndWaveDash() =>
        Assert.Equal("<mark>Tranh</mark> thủ, <mark>tranh</mark> &lt;x&gt;",
            Html(Highlight.Mark("Tranh thủ, tranh <x>", "～tranh")));

    [Theory]
    [InlineData("Tranh thủ làm", "tranh thu", "<mark>Tranh thủ</mark> làm")]
    [InlineData("熱いうちに", "ウチニ", "熱い<mark>うちに</mark>")]
    public void Mark_UsesSearchNormalization(string text, string query, string expected) =>
        Assert.Equal(expected, Html(Highlight.Mark(text, query)));

    [Fact]
    public void Mark_NoQuery_ReturnsEncodedText() =>
        Assert.Equal("&lt;x&gt;", Html(Highlight.Mark("<x>", "  ")));
}
