namespace NAIware.RuleIntelligence;

/// <summary>
/// Provides editor-grade IntelliSense for NAIware rule expressions.
/// The UI should call this service instead of performing reflection, path resolution, or operator logic itself.
/// </summary>
public interface IRuleIntelliSenseService
{
    /// <summary>
    /// Returns context-aware completion suggestions for the supplied expression and cursor position.
    /// </summary>
    RuleCompletionResponse GetCompletions(RuleCompletionRequest request);

    /// <summary>
    /// Analyzes the cursor position without returning suggestions.
    /// Useful for diagnostics, telemetry, debugging, or editor status bars.
    /// </summary>
    RuleCompletionContext Analyze(RuleCompletionRequest request);

    /// <summary>
    /// Resolves a dot/bracket path to a reflected schema node.
    /// </summary>
    RuleCompletionNode? ResolvePath(RuleSchema schema, string path);

    /// <summary>
    /// Clears all internal schema and completion caches.
    /// </summary>
    void Invalidate();
}
