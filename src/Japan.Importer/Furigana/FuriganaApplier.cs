using Japan.Core;

namespace Japan.Importer.Furigana;

public static class FuriganaApplier
{
    public static GrammarBook Apply(GrammarBook book, IFuriganaGenerator generator) => book with
    {
        Lessons = book.Lessons.Select(l => l with
        {
            Points = l.Points.Select(p => p with
            {
                Examples = Fill(p.Examples, generator),
                Enrichment = p.Enrichment is null
                    ? null
                    : p.Enrichment with { Examples = Fill(p.Enrichment.Examples, generator) },
            }).ToList(),
        }).ToList(),
    };

    public static ConnectorBook Apply(ConnectorBook book, IFuriganaGenerator generator) => book with
    {
        Groups = book.Groups.Select(g => g with
        {
            Connectors = g.Connectors.Select(c => c with { Examples = Fill(c.Examples, generator) }).ToList(),
        }).ToList(),
    };

    private static List<Example> Fill(IEnumerable<Example> examples, IFuriganaGenerator generator) =>
        examples.Select(e => e with { Ruby = generator.ToRuby(e.Japanese) }).ToList();
}
