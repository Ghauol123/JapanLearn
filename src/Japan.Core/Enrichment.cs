namespace Japan.Core;

public sealed record GrammarEnrichment(
    string Explanation,
    IReadOnlyList<FormationRule> Formation,
    IReadOnlyList<string> Usage,
    IReadOnlyList<string> Pitfalls,
    IReadOnlyList<Comparison> Comparisons,
    IReadOnlyList<Example> Examples);

public sealed record FormationRule(string WordType, string Rule, IReadOnlyList<string> Samples);

public sealed record Comparison(string Pattern, string? TargetId, string Difference);
