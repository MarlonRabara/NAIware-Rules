namespace NAIware.RuleIntelligence;

/// <summary>
/// Provides type-aware comparison, string, collection, null, and logical operators.
/// </summary>
public interface IRuleOperatorProvider
{
    IReadOnlyList<RuleOperatorDescriptor> GetComparisonOperators(Type? leftType);
    IReadOnlyList<RuleOperatorDescriptor> GetLogicalOperators();
    IReadOnlyList<RuleOperatorDescriptor> GetUnaryOperators(Type? operandType);
}
