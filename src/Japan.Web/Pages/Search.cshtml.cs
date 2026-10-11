using Japan.Core;
using Japan.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Japan.Web.Pages;

public sealed record SearchResultsViewModel(string? Query, IReadOnlyList<SearchHit> Hits);

public sealed class SearchModel(GrammarRepository repo) : PageModel
{
    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Query { get; set; }

    public SearchResultsViewModel Results { get; private set; } = new(null, []);

    public void OnGet() => Results = new SearchResultsViewModel(Query, repo.Search(Query));

    public PartialViewResult OnGetPartial()
    {
        OnGet();
        return Partial("_SearchResults", Results);
    }
}
