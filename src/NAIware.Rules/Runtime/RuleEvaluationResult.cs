namespace NAIware.Rules.Runtime;

/// <summary>
/// The result of evaluating a set of rules against an input object.
/// </summary>
public class RuleEvaluationResult
{
    /// <summary>Creates a new evaluation result.</summary>
    public RuleEvaluationResult(string contextName, string? categoryName)
    {
        ContextName = contextName;
        CategoryName = categoryName;
        Matches = [];
        Mismatches = [];
        EvaluatedUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets the name of the rule context that was evaluated.</summary>
    public string ContextName { get; }

    /// <summary>Gets the category name that was evaluated, or null if all expressions were evaluated.</summary>
    public string? CategoryName { get; }

    /// <summary>Gets the expressions that matched (evaluated to true).</summary>
    public List<RuleExpressionResult> Matches { get; }

    /// <summary>Gets the expressions that did not match (evaluated to false).</summary>
    public List<RuleExpressionResult> Mismatches { get; }

    /// <summary>Gets the UTC timestamp when evaluation completed.</summary>
    public DateTimeOffset EvaluatedUtc { get; }

    /// <summary>Gets whether any expressions matched.</summary>
    public bool HasMatches => Matches.Count > 0;

    /// <summary>Gets the total number of expressions evaluated.</summary>
    public int TotalEvaluated => Matches.Count + Mismatches.Count;
}
