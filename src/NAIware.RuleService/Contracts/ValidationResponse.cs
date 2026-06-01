namespace NAIware.RuleService.Contracts;

/// <summary>
/// The structured result of validating a draft expression or a rules library.
/// </summary>
/// <remarks>
/// This is a transport-friendly projection of <c>NAIware.Rules.Validation.ValidationIssue</c>.
/// <see cref="IsValid"/> is a convenience derived from the absence of <c>Error</c>-severity issues;
/// warnings (for example, a missing result definition) do not make a draft invalid.
/// </remarks>
public sealed class ValidationResponse
{
    /// <summary>Gets or sets whether the validated subject contains no error-severity issues.</summary>
    public bool IsValid { get; set; }

    /// <summary>Gets or sets the number of error-severity issues.</summary>
    public int ErrorCount { get; set; }

    /// <summary>Gets or sets the number of warning-severity issues.</summary>
    public int WarningCount { get; set; }

    /// <summary>Gets the individual validation issues found.</summary>
    public List<ValidationIssueResult> Issues { get; init; } = [];
}

/// <summary>A single validation issue projected for transport.</summary>
public sealed class ValidationIssueResult
{
    /// <summary>Gets or sets the severity label (Error / Warning / Information).</summary>
    public string Severity { get; set; } = "Error";

    /// <summary>Gets or sets the validation message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the context display name.</summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>Gets or sets the category display name, when applicable.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the rule display name, when applicable.</summary>
    public string Rule { get; set; } = string.Empty;

    /// <summary>Gets or sets the rule identity, when the issue targets a persisted rule.</summary>
    public Guid? RuleId { get; set; }
}
