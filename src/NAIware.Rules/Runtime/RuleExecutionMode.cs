namespace NAIware.Rules.Runtime;

/// <summary>
/// Controls how runtime evaluation errors are handled.
/// </summary>
public enum RuleExecutionMode
{
    /// <summary>Fails the request if a required artifact cannot be resolved or evaluated.</summary>
    Strict,

    /// <summary>Evaluates valid expressions and records errors for invalid expressions.</summary>
    Lenient,

    /// <summary>Runs validation or evaluation for troubleshooting without authoritative business outcomes.</summary>
    DiagnosticOnly
}
