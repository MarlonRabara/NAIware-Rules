using NAIware.Rules.Catalog;

namespace NAIware.Rules.Runtime;

/// <summary>
/// The result of evaluating a single <see cref="RuleExpression"/> against an input object.
/// </summary>
public class RuleExpressionResult
{
    /// <summary>Creates a new expression result.</summary>
    public RuleExpressionResult(
        Guid expressionIdentity,
        string expressionName,
        int expressionVersion,
        bool matched,
        RuleResultDefinition? result = null,
        RuleMismatchDiagnostic? diagnostic = null)
    {
        ExpressionIdentity = expressionIdentity;
        ExpressionName = expressionName;
        ExpressionVersion = expressionVersion;
        Matched = matched;
        Result = result;
        Diagnostic = diagnostic;
    }

    /// <summary>Gets the identity of the evaluated expression.</summary>
    public Guid ExpressionIdentity { get; }

    /// <summary>Gets the name of the evaluated expression.</summary>
    public string ExpressionName { get; }

    /// <summary>Gets the version of the expression that was evaluated.</summary>
    public int ExpressionVersion { get; }

    /// <summary>Gets whether the expression matched (evaluated to true).</summary>
    public bool Matched { get; }

    /// <summary>
    /// Gets the configured result definition, populated when the expression matches
    /// and a <see cref="RuleResultDefinition"/> was configured.
    /// </summary>
    public RuleResultDefinition? Result { get; }

    /// <summary>
    /// Gets the mismatch diagnostic, populated when the expression did not match
    /// and diagnostics were requested.
    /// </summary>
    public RuleMismatchDiagnostic? Diagnostic { get; }
}
