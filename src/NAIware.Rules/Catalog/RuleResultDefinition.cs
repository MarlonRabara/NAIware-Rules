namespace NAIware.Rules.Catalog;

/// <summary>
/// Defines the result payload returned when a <see cref="RuleExpression"/> evaluates to true.
/// The engine is agnostic about what <see cref="Code"/> means — the consuming application
/// decides whether a code is informational, warning, critical, etc.
/// </summary>
public class RuleResultDefinition
{
    /// <summary>Creates a new rule result definition.</summary>
    public RuleResultDefinition(string code, string message, string? severity = null)
    {
        Code = code;
        Message = message;
        Severity = severity;
    }

    /// <summary>Gets or sets the application-defined result code (e.g., "ELIG-001").</summary>
    public string Code { get; set; }

    /// <summary>Gets or sets the human-readable result message.</summary>
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets an optional severity hint. The engine does not interpret this value;
    /// it is passed through for the consuming application to classify.
    /// </summary>
    public string? Severity { get; set; }
}
