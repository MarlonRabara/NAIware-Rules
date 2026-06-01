namespace NAIware.RuleIntelligence;

public sealed record RuleCompletionContext
{
    public required RuleCompletionContextKind Kind { get; init; }
    public required string ExpressionBeforeCursor { get; init; }
    public required string TypedPrefix { get; init; }
    public string? TargetPath { get; init; }
    public RuleCompletionNode? TargetNode { get; init; }
    public RuleCompletionNode? LeftNode { get; init; }
    public RuleOperatorDescriptor? Operator { get; init; }
    public Type? ExpectedValueType { get; init; }
    public int ReplacementStart { get; init; }
    public int ReplacementLength { get; init; }
    public bool IsCompleteComparison { get; init; }
}

public enum RuleCompletionContextKind
{
    Unknown,
    RootSymbol,
    MemberAccess,
    Operator,
    Value,
    LogicalConnector
}
