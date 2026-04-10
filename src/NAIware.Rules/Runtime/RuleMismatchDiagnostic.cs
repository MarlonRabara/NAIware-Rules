namespace NAIware.Rules.Runtime;

/// <summary>
/// A diagnostic artifact produced when a rule expression does not match.
/// This is not a business result — it is a debugging/trace/explainability artifact
/// that describes why a rule did not fire.
/// </summary>
public class RuleMismatchDiagnostic
{
    /// <summary>Creates a new mismatch diagnostic.</summary>
    public RuleMismatchDiagnostic(string expression)
    {
        Expression = expression;
        EvaluatedParameters = new Dictionary<string, string?>();
    }

    /// <summary>Gets the raw expression text that was evaluated.</summary>
    public string Expression { get; }

    /// <summary>
    /// Gets the parameter values that were used during evaluation,
    /// keyed by parameter name with string-formatted values.
    /// </summary>
    public Dictionary<string, string?> EvaluatedParameters { get; }

    /// <summary>Gets or sets an optional human-readable explanation of why the rule did not match.</summary>
    public string? Explanation { get; set; }
}
