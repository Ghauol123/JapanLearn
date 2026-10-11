using Japan.Core;

namespace Japan.Importer.Furigana;

public static class FuriganaApplier
{
    public static GrammarBook Apply(GrammarBook book, IFuriganaGenerator generator)
    {
        Example Fill(Example e) => e with { Ruby = generator.ToRuby(e.Japanese) };

        return book with
        {
            Lessons = book.Lessons.Select(l => l with
            {
                Points = l.Points.Select(p => p with
                {
                    Examples = p.Examples.Select(Fill).ToList(),
                    Enrichment = p.Enrichment is null
                        ? null
                        : p.Enrichment with { Examples = p.Enrichment.Examples.Select(Fill).ToList() },
                }).ToList(),
            }).ToList(),
        };
    }
}
