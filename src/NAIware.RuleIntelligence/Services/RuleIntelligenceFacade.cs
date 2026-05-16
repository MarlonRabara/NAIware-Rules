namespace NAIware.RuleIntelligence;

/// <summary>
/// Convenience facade that owns both schema creation and completions.
/// </summary>
public sealed class RuleIntelligenceFacade
{
    private readonly IRuleSchemaProvider _schemaProvider;
    private readonly IRuleIntelliSenseService _intelliSenseService;

    public RuleIntelligenceFacade(
        IRuleSchemaProvider? schemaProvider = null,
        IRuleIntelliSenseService? intelliSenseService = null)
    {
        _schemaProvider = schemaProvider ?? new ObjectTreeRuleSchemaProvider();
        _intelliSenseService = intelliSenseService ?? new RuleIntelliSenseService();
    }

    public RuleSchema BuildSchema(Type rootType, string rootName, RuleSchemaBuildOptions? options = null)
    {
        return _schemaProvider.Build(rootType, rootName, options);
    }

    public RuleCompletionResponse Complete(Type rootType, string rootName, string expression, int cursorPosition, RuleSchemaBuildOptions? options = null)
    {
        var schema = _schemaProvider.Build(rootType, rootName, options);
        return _intelliSenseService.GetCompletions(new RuleCompletionRequest
        {
            Schema = schema,
            Expression = expression,
            CursorPosition = cursorPosition
        });
    }
}
