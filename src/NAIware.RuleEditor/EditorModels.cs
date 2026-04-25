using System.Text.Json.Serialization;

namespace NAIware.RuleEditor;

/// <summary>
/// A single validation issue produced by <see cref="RuleValidationService"/>.
/// Displayed in the Visual Studio-style error list panel.
/// </summary>
public sealed class ValidationIssue
{
    /// <summary>Gets or sets the severity label (Error / Warning / Info).</summary>
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

/// <summary>
/// Describes a public concrete class discovered inside an assembly.
/// Used by <see cref="ContextTypePickerDialog"/> to present selectable types.
/// </summary>
public sealed class ReflectedTypeInfo
{
    /// <summary>Gets the display name (namespace + type name).</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets the fully qualified type name.</summary>
    public required string FullName { get; init; }

    /// <summary>Gets the originating assembly path.</summary>
    public required string AssemblyPath { get; init; }

    /// <summary>Gets the reflected <see cref="Type"/> instance (not serialized).</summary>
    [JsonIgnore] public Type? Type { get; init; }
}
