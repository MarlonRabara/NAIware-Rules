namespace NAIware.Rules.Models;

/// <summary>
/// Canonical lifecycle state for a rules library version.
/// </summary>
public enum RulesLibraryState
{
    /// <summary>Editable working version.</summary>
    Draft,

    /// <summary>Immutable version available for runtime execution.</summary>
    Published,

    /// <summary>Still executable but discouraged for new runtime usage.</summary>
    Deprecated,

    /// <summary>Retained for audit or replay but not selected for normal runtime execution.</summary>
    Archived
}
