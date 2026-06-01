namespace NAIware.RuleIntelligence;

/// <summary>
/// Provides literal/value suggestions once the editor is positioned after a comparison operator.
/// </summary>
public interface IRuleValueSuggestionProvider
{
    IReadOnlyList<RuleCompletionItem> GetValueSuggestions(RuleValueSuggestionContext context);
}
