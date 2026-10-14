using Japan.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Japan.Web.Pages;

public sealed class ConnectorModel(ConnectorRepository repo) : PageModel
{
    public ConnectorLocation Location { get; private set; } = null!;

    public IActionResult OnGet(string id)
    {
        if (repo.Locate(id) is not { } location) return NotFound();
        Location = location;
        return Page();
    }
}
