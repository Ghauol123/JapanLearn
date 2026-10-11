using Japan.Core;
using Japan.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Japan.Web.Pages;

public sealed class GrammarModel(GrammarRepository repo) : PageModel
{
    public GrammarPoint Point { get; private set; } = null!;
    public GrammarPoint? Previous { get; private set; }
    public GrammarPoint? Next { get; private set; }

    public IActionResult OnGet(string id)
    {
        var (point, previous, next) = repo.Locate(id);
        if (point is null) return NotFound();
        (Point, Previous, Next) = (point, previous, next);
        return Page();
    }
}
