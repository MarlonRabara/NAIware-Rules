namespace NAIware.RuleIntelligence;

/// <summary>
/// Describes a formula function (method wrapper) the editor can suggest as an IntelliSense completion.
/// </summary>
/// <remarks>
/// Each descriptor mirrors a method wrapper registered through
/// <c>NAIware.Rules.MethodWrappers.DefaultMethodWrapperRegistration</c>. Keeping the display name, signature,
/// and documentation here lets the completion UI present rich, human-friendly suggestions while the runtime
/// continues to resolve the function by its case-insensitive name.
/// </remarks>
public sealed record RuleFunctionDescriptor
{
    /// <summary>The canonical function name as registered in the runtime method map (for example, <c>ROUNDUP</c>).</summary>
    public required string Name { get; init; }

    /// <summary>A short signature shown alongside the suggestion, such as <c>IF(condition, ifTrue, ifFalse)</c>.</summary>
    public required string Signature { get; init; }

    /// <summary>A one-line description of what the function does.</summary>
    public required string Description { get; init; }

    /// <summary>The category used for grouping and ranking (Logical, Numeric, Text, or DateTime).</summary>
    public RuleFunctionCategory Category { get; init; } = RuleFunctionCategory.General;

    /// <summary>
    /// Converts the descriptor into a completion item. The inserted text opens the argument list with a
    /// trailing parenthesis so the caret lands inside the call.
    /// </summary>
    public RuleCompletionItem ToCompletionItem()
    {
        return RuleCompletionItem.Create(
            label: Name,
            insertText: $"{Name}(",
            kind: RuleCompletionItemKind.Function,
            detail: Signature,
            documentation: Description);
    }
}

/// <summary>Logical grouping for formula functions used to drive ordering in completion lists.</summary>
public enum RuleFunctionCategory
{
    General,
    Logical,
    Numeric,
    Text,
    DateTime
}
