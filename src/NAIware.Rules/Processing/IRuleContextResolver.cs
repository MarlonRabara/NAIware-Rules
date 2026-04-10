using NAIware.Rules.Catalog;

namespace NAIware.Rules.Processing;

/// <summary>
/// Defines the contract for resolving a <see cref="RuleContext"/> from an input object.
/// </summary>
public interface IRuleContextResolver
{
    /// <summary>
    /// Resolves the rule context that applies to the given input object.
    /// </summary>
    /// <param name="inputObject">The object being evaluated.</param>
    /// <returns>The matching rule context, or null if no context matches.</returns>
    RuleContext? Resolve(object inputObject);
}
