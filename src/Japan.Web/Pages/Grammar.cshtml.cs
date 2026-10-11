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
        var point = repo.Find(id);
        if (point is null) return NotFound();
        Point = point;
        (Previous, Next) = repo.Neighbors(id);
        return Page();
    }
}
