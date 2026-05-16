namespace NAIware.RuleIntelligence;

/// <summary>
/// UI/editor-friendly schema node mapped from NAIware.Core.Reflection.ReflectedPropertyNode.
/// </summary>
public sealed record RuleCompletionNode
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required Type Type { get; init; }
    public PropertyInfo? PropertyInfo { get; init; }
    public bool IsRoot { get; init; }
    public bool IsCollection { get; init; }
    public bool IsCollectionItem { get; init; }
    public bool IsLeaf { get; init; }
    public int Depth { get; init; }
    public RuleCompletionNode? Parent { get; init; }
    public IReadOnlyList<RuleCompletionNode> Children { get; init; } = [];

    public string DisplayType => TypeNameFormatter.Format(Type);

    public bool HasChildren => Children.Count > 0;

    public RuleCompletionItem ToCompletionItem(string? replacementPrefix = null)
    {
        var kind = IsRoot
            ? RuleCompletionItemKind.RootObject
            : IsCollection
                ? RuleCompletionItemKind.Collection
                : IsCollectionItem
                    ? RuleCompletionItemKind.CollectionItem
                    : RuleCompletionItemKind.Property;

        return RuleCompletionItem.Create(
            label: Name,
            insertText: Name,
            kind: kind,
            detail: DisplayType,
            documentation: Path,
            path: Path,
            type: Type);
    }
}
