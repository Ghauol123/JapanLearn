using Japan.Core;
using Japan.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Japan.Web.Pages;

public sealed class LessonModel(GrammarRepository repo) : PageModel
{
    public Lesson Lesson { get; private set; } = null!;

    public IActionResult OnGet(int n)
    {
        var lesson = repo.FindLesson(n);
        if (lesson is null) return NotFound();
        Lesson = lesson;
        return Page();
    }
}
