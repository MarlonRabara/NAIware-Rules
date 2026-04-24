using System.Reflection;
using System.Text.Json.Serialization;

namespace NAIware.RuleEditor;

/// <summary>
/// UI-facing document model that represents a rule library on disk.
/// Maps to and from <see cref="NAIware.Rules.Catalog.RulesLibrary"/> via <see cref="CatalogMapper"/>.
/// </summary>
public sealed class RuleLibraryDocument
{
    /// <summary>Gets or sets the library name.</summary>
    public string Name { get; set; } = "New Rule Library";

    /// <summary>Gets or sets the library description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the library version number.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Gets or sets the UTC timestamp the library was last saved.</summary>
    public DateTimeOffset SavedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets the collection of rule contexts in this library.</summary>
    public List<RuleContextDocument> Contexts { get; set; } = [];
}

/// <summary>
/// UI-facing document model for a rule context.
/// Stores the qualified type name and the originating assembly path so the context
/// can be resolved via reflection during validation and testing.
/// </summary>
public sealed class RuleContextDocument
{
    /// <summary>Gets or sets the display name of the context.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the fully qualified .NET type name (e.g., "Mortgage.Models.LoanApplication").</summary>
    public string QualifiedTypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional source assembly path used for type resolution.</summary>
    public string? AssemblyPath { get; set; }

    /// <summary>Gets or sets the context description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the categories owned by this context.</summary>
    public List<RuleCategoryDocument> Categories { get; set; } = [];

    /// <summary>Gets or sets the rule expressions owned by this context.</summary>
    public List<RuleExpressionDocument> Expressions { get; set; } = [];
}

/// <summary>
/// UI-facing document model for a category (or nested subcategory).
/// Categories reference <see cref="RuleExpressionDocument"/> entries by id.
/// </summary>
public sealed class RuleCategoryDocument
{
    /// <summary>Gets or sets the category name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the category description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets nested subcategories.</summary>
    public List<RuleCategoryDocument> Categories { get; set; } = [];

    /// <summary>Gets or sets the expression ids that belong to this category.</summary>
    public List<Guid> ExpressionIds { get; set; } = [];
}

/// <summary>
/// UI-facing document model for a single rule expression, including metadata and result definition.
/// </summary>
public sealed class RuleExpressionDocument
{
    /// <summary>Gets or sets the stable expression identity.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the rule name.</summary>
    public string Name { get; set; } = "NewRule";

    /// <summary>Gets or sets the rule description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the raw rule expression text.</summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the rule is active (enabled).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the rule priority (higher runs earlier in future sorted execution).</summary>
    public int Priority { get; set; }

    /// <summary>Gets or sets the free-form tags associated with the rule.</summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>Gets or sets the result definition code returned on match.</summary>
    public string? ResultCode { get; set; }

    /// <summary>Gets or sets the result definition message returned on match.</summary>
    public string? ResultMessage { get; set; }

    /// <summary>Gets or sets the result definition severity hint (Info / Warning / Error).</summary>
    public string? Severity { get; set; }

    /// <summary>Gets or sets an optional result value payload returned on match.</summary>
    public string? OptionalValue { get; set; }
}

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
