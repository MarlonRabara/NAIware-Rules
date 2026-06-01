namespace NAIware.RuleIntelligence;

/// <summary>
/// Cached rule authoring schema for one root context.
/// </summary>
public sealed record RuleSchema
{
    public required string RootName { get; init; }
    public required Type RootType { get; init; }
    public required RuleCompletionNode Root { get; init; }
    public required IReadOnlyDictionary<string, RuleCompletionNode> NodesByPath { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public RuleCompletionNode? Find(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Root;

        var normalized = RulePathNormalizer.Normalize(path);

        if (NodesByPath.TryGetValue(normalized, out var node))
            return node;

        return NodesByPath.TryGetValue(path, out node) ? node : null;
    }
}
