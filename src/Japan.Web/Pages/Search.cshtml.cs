using Japan.Core;
using Japan.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Japan.Web.Pages;

public sealed record SearchResultsViewModel(string? Query, IReadOnlyList<SearchHit> Hits, IReadOnlyList<ConnectorHit> ConnectorHits);

public sealed class SearchModel(GrammarRepository repo, ConnectorRepository connectors) : PageModel
{
    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Query { get; set; }

    public SearchResultsViewModel Results { get; private set; } = new(null, [], []);

    public void OnGet() => Results = new SearchResultsViewModel(Query, repo.Search(Query), connectors.Search(Query));

    public PartialViewResult OnGetPartial()
    {
        OnGet();
        return Partial("_SearchResults", Results);
    }
}
