namespace NAIware.Rules.Validation;

/// <summary>
/// A single validation issue produced by <see cref="RuleValidationService"/>.
/// </summary>
/// <remarks>
/// This is a transport- and host-neutral diagnostic record. The Rule Editor renders it in its
/// Visual Studio-style error list; the Rule Service projects it onto an HTTP response. Severity is
/// a free-form label (<c>Error</c> / <c>Warning</c> / <c>Information</c>) so consumers can apply
/// their own presentation without depending on an enum.
/// </remarks>
public sealed class ValidationIssue
{
    /// <summary>Gets or sets the severity label (Error / Warning / Information).</summary>
    public string Severity { get; set; } = "Error";

    /// <summary>Gets or sets the validation message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the context display name.</summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>Gets or sets the category display name.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the rule display name.</summary>
    public string Rule { get; set; } = string.Empty;

    /// <summary>Gets or sets the rule identity for navigation.</summary>
    public Guid? RuleId { get; set; }

    /// <summary>Returns a short formatted rule identifier string for display.</summary>
    public string ExpressionId => RuleId?.ToString("N")[..8] ?? string.Empty;
}
