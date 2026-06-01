namespace NAIware.RuleIntelligence;

/// <summary>
/// Describes an operator the editor can suggest.
/// </summary>
public sealed record RuleOperatorDescriptor
{
    public required string Symbol { get; init; }
    public required RuleOperatorKind Kind { get; init; }
    public string? DisplayText { get; init; }
    public string? Description { get; init; }
    public IReadOnlySet<TypeCategory> SupportedTypeCategories { get; init; } = ReadOnlySet<TypeCategory>.Empty;

    public RuleCompletionItem ToCompletionItem(Type? leftType = null)
    {
        return RuleCompletionItem.Create(
            label: DisplayText ?? Symbol,
            insertText: Symbol,
            kind: Kind == RuleOperatorKind.Logical ? RuleCompletionItemKind.LogicalOperator : RuleCompletionItemKind.Operator,
            detail: leftType is null ? null : TypeNameFormatter.Format(leftType),
            documentation: Description);
    }
}

public enum RuleOperatorKind
{
    Comparison,
    Logical,
    StringFunction,
    CollectionFunction,
    NullCheck,
    Unary
}

public enum TypeCategory
{
    Any,
    Boolean,
    String,
    Numeric,
    Date,
    Enum,
    Guid,
    Time,
    Collection,
    Object,
    Nullable
}
