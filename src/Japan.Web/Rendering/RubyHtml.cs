using Japan.Core;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Japan.Web.Rendering;

public static class RubyHtml
{
    public static IHtmlContent Ruby(this IHtmlHelper _, Example example) => Render(example.Ruby ?? example.Japanese);

    public static IHtmlContent Render(string ruby)
    {
        var builder = new HtmlContentBuilder();
        foreach (var segment in RubyText.Parse(ruby))
        {
            if (segment.Reading is null)
            {
                builder.Append(segment.Text);
                continue;
            }

            builder.AppendHtml("<ruby>").Append(segment.Text)
                   .AppendHtml("<rt>").Append(segment.Reading).AppendHtml("</rt></ruby>");
        }

        return builder;
    }
}
