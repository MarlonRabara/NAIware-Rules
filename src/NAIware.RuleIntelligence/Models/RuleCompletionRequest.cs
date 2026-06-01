namespace NAIware.RuleIntelligence;

/// <summary>
/// Completion request passed by the editor UI.
/// </summary>
public sealed record RuleCompletionRequest
{
    public required RuleSchema Schema { get; init; }
    public required string Expression { get; init; }
    public required int CursorPosition { get; init; }

    /// <summary>
    /// Optional limit applied after filtering and ranking.
    /// </summary>
    public int MaxItems { get; init; } = 50;

    /// <summary>
    /// When true, snippets such as collection-index templates are included.
    /// </summary>
    public bool IncludeSnippets { get; init; } = true;
}
