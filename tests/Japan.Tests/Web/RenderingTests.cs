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
}
