using Japan.Core;
using Microsoft.AspNetCore.Html;

namespace Japan.Web.Rendering;

public static class Highlight
{
    public static IHtmlContent Mark(string text, string? query)
    {
        var builder = new HtmlContentBuilder();
        var q = TextNormalizer.Normalize(query);
        if (q.Length == 0) return builder.Append(text);

        var pos = 0;
        while (pos < text.Length && TextNormalizer.FindIn(text[pos..], q) is { } match)
        {
            var at = pos + match.Start;
            builder.Append(text[pos..at]).AppendHtml("<mark>").Append(text.Substring(at, match.Length)).AppendHtml("</mark>");
            pos = at + match.Length;
        }

        return builder.Append(text[pos..]);
    }
}
