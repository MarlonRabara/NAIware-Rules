namespace NAIware.RuleIntelligence;

/// <summary>
/// Builds a rule-completion schema from a root model type or object.
/// </summary>
public interface IRuleSchemaProvider
{
    RuleSchema Build(Type rootType, string rootName, RuleSchemaBuildOptions? options = null);

    RuleSchema Build(object rootInstance, string rootName, RuleSchemaBuildOptions? options = null);
}
