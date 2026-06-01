using System.ComponentModel.DataAnnotations;

namespace NAIware.RuleService.Contracts;

/// <summary>
/// A request to validate a single draft rule expression against a model type before it is saved.
/// </summary>
/// <remarks>
/// This supports authoring scenarios: a formula is drafted in a client and posted here for a fast
/// compiler-style check (property paths, parentheses, operand type compatibility) without requiring
/// a full rules library. The model type is loaded from <see cref="ModelAssemblyPath"/> and resolved
/// by <see cref="ModelQualifiedTypeName"/>, mirroring the deserialization contract.
/// </remarks>
public sealed class ValidateExpressionRequest
{
    /// <summary>Gets or sets the absolute path to the model assembly that defines the context type.</summary>
    [Required]
    public string ModelAssemblyPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the qualified (assembly-qualified or full) type name of the context model.</summary>
    [Required]
    public string ModelQualifiedTypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional display name used to label the context in returned issues.</summary>
    public string? ContextName { get; set; }

    /// <summary>Gets or sets the draft expression text to validate.</summary>
    [Required]
    public string Expression { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional draft result code; supply with a message to suppress the incomplete-result warning.</summary>
    public string? ResultCode { get; set; }

    /// <summary>Gets or sets an optional draft result message; supply with a code to suppress the incomplete-result warning.</summary>
    public string? ResultMessage { get; set; }

    /// <summary>Gets or sets an optional display name for the draft rule used to label returned issues.</summary>
    public string? RuleName { get; set; }
}
