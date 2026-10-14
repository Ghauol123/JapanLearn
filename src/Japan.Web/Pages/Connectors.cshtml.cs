using Japan.Core;
using Japan.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Japan.Web.Pages;

public sealed class ConnectorsModel(ConnectorRepository repo) : PageModel
{
    [BindProperty(SupportsGet = true, Name = "cap")]
    public string? Cap { get; set; }

    public JlptLevel? Level { get; private set; }

    public IReadOnlyList<ConnectorGroup> Groups { get; private set; } = [];

    public bool IsLoaded { get; private set; }

    public string? LoadError => repo.LoadError;

    public void OnGet()
    {
        IsLoaded = repo.IsLoaded;
        if (!IsLoaded) return;
        Level = JlptLevels.TryParse(Cap, out var level) ? level : null;
        Groups = repo.Filter(Level);
    }
}
