using Microsoft.AspNetCore.Html;

namespace Japan.Web.Rendering;

public static class Highlight
{
    public static IHtmlContent Mark(string text, string? query)
    {
        var builder = new HtmlContentBuilder();
        var q = query?.Trim().TrimStart('～', '~', '〜').Trim();
        if (string.IsNullOrEmpty(q)) return builder.Append(text);

        var pos = 0;
        while (true)
        {
            var at = text.IndexOf(q, pos, StringComparison.OrdinalIgnoreCase);
            if (at < 0) break;
            builder.Append(text[pos..at]).AppendHtml("<mark>").Append(text.Substring(at, q.Length)).AppendHtml("</mark>");
            pos = at + q.Length;
        }

        return builder.Append(text[pos..]);
    }
}
