namespace NAIware.RuleIntelligence;

public sealed record RuleValueSuggestionContext
{
    public required RuleSchema Schema { get; init; }
    public required RuleCompletionContext CompletionContext { get; init; }
    public Type? ExpectedType => CompletionContext.ExpectedValueType;
    public RuleCompletionNode? LeftNode => CompletionContext.LeftNode;
}
