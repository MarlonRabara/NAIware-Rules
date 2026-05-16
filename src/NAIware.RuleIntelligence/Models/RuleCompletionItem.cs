namespace NAIware.RuleIntelligence;

/// <summary>
/// One suggestion shown by a UI completion dropdown.
/// </summary>
public sealed record RuleCompletionItem
{
    public required string Label { get; init; }
    public required string InsertText { get; init; }
    public required RuleCompletionItemKind Kind { get; init; }
    public string? Detail { get; init; }
    public string? Documentation { get; init; }
    public string? Path { get; init; }
    public string? TypeName { get; init; }
    public int Score { get; init; }
    public IReadOnlyDictionary<string, string> Tags { get; init; } = ReadOnlyDictionary<string, string>.Empty;

    public static RuleCompletionItem Create(
        string label,
        string insertText,
        RuleCompletionItemKind kind,
        string? detail = null,
        string? documentation = null,
        string? path = null,
        Type? type = null,
        int score = 0)
    {
        return new RuleCompletionItem
        {
            Label = label,
            InsertText = insertText,
            Kind = kind,
            Detail = detail,
            Documentation = documentation,
            Path = path,
            TypeName = type is null ? null : TypeNameFormatter.Format(type),
            Score = score
        };
    }
}

public enum RuleCompletionItemKind
{
    RootObject,
    Property,
    Collection,
    CollectionItem,
    Operator,
    LogicalOperator,
    Literal,
    Keyword,
    Function,
    Snippet
}
