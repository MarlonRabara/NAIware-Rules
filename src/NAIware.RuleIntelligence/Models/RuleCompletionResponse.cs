namespace NAIware.RuleIntelligence;

public sealed record RuleCompletionResponse
{
    public required RuleCompletionContext Context { get; init; }
    public required IReadOnlyList<RuleCompletionItem> Items { get; init; }
    public string? ReplacementPrefix { get; init; }
    public int ReplacementStart { get; init; }
    public int ReplacementLength { get; init; }
}
