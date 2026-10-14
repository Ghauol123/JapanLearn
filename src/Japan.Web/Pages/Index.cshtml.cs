using Japan.Core;
using Japan.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Japan.Web.Pages;

public sealed class IndexModel(GrammarRepository repo, ConnectorRepository connectors) : PageModel
{
    public IReadOnlyList<Lesson> Lessons => repo.Lessons;

    public int ConnectorCount => connectors.IsLoaded ? connectors.Count : 0;
}
