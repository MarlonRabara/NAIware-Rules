namespace NAIware.Rules.Models;

/// <summary>
/// Defines the result payload returned when a rule expression matches.
/// </summary>
public class RuleResultDefinition
{
    /// <summary>Creates an empty result definition.</summary>
    public RuleResultDefinition()
    {
        Code = string.Empty;
        Message = string.Empty;
    }

    /// <summary>Creates a configured result definition.</summary>
    public RuleResultDefinition(string code, string message, string? severity = null, string? value = null)
    {
        Code = code;
        Message = message;
        Severity = severity;
        Value = value;
    }

    /// <summary>Gets or sets the application-defined result code.</summary>
    public string Code { get; set; }

    /// <summary>Gets or sets the human-readable result message.</summary>
    public string Message { get; set; }

    /// <summary>Gets or sets an optional severity hint such as Info, Warning, or Error.</summary>
    public string? Severity { get; set; }

    /// <summary>Gets or sets an optional result value payload.</summary>
    public string? Value { get; set; }
}
