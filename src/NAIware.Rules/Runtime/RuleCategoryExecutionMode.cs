namespace NAIware.Rules.Runtime;

/// <summary>
/// Controls whether a category request must target a leaf category or may expand to descendant leaf categories.
/// </summary>
public enum RuleCategoryExecutionMode
{
    /// <summary>The requested category must be a leaf category.</summary>
    LeafOnly,

    /// <summary>A non-leaf category expands to all descendant leaf categories in deterministic order.</summary>
    IncludeDescendantLeaves
}
