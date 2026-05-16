namespace NAIware.RuleIntelligence;

public sealed class DefaultRuleOperatorProvider : IRuleOperatorProvider
{
    public IReadOnlyList<RuleOperatorDescriptor> GetComparisonOperators(Type? leftType)
    {
        if (leftType is null)
            return RuleOperatorCatalog.CoreComparisons;

        var categories = RuleTypeClassifier.GetCategories(leftType);

        var results = new List<RuleOperatorDescriptor>
        {
            RuleOperatorCatalog.Equal,
            RuleOperatorCatalog.NotEqual,
            RuleOperatorCatalog.NotEqualAlternate
        };

        if (categories.Contains(TypeCategory.Numeric)
            || categories.Contains(TypeCategory.Date)
            || categories.Contains(TypeCategory.Time))
        {
            results.Add(RuleOperatorCatalog.GreaterThan);
            results.Add(RuleOperatorCatalog.LessThan);
            results.Add(RuleOperatorCatalog.GreaterOrEqual);
            results.Add(RuleOperatorCatalog.LessOrEqual);
        }

        if (categories.Contains(TypeCategory.Nullable)
            || categories.Contains(TypeCategory.Object)
            || categories.Contains(TypeCategory.String))
        {
            results.Add(RuleOperatorCatalog.IsNull);
            results.Add(RuleOperatorCatalog.IsNotNull);
        }

        return results;
    }

    public IReadOnlyList<RuleOperatorDescriptor> GetLogicalOperators() => RuleOperatorCatalog.Logical;

    public IReadOnlyList<RuleOperatorDescriptor> GetUnaryOperators(Type? operandType)
    {
        if (operandType is null)
            return [];

        if (RuleTypeClassifier.IsBoolean(operandType))
        {
            return
            [
                new RuleOperatorDescriptor
                {
                    Symbol = "!",
                    Kind = RuleOperatorKind.Unary,
                    Description = "Boolean negation."
                }
            ];
        }

        return [];
    }
}
