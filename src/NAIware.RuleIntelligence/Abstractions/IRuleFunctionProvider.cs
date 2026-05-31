namespace NAIware.RuleIntelligence;

/// <summary>
/// Provides the catalog of formula functions (method wrappers) that the editor can suggest.
/// </summary>
/// <remarks>
/// Implementations should stay aligned with the runtime method-wrapper registration so IntelliSense
/// advertises exactly the functions the engine can evaluate.
/// </remarks>
public interface IRuleFunctionProvider
{
    /// <summary>Returns the functions available for completion suggestions.</summary>
    IReadOnlyList<RuleFunctionDescriptor> GetFunctions();
}
