namespace NAIware.Core.Text;

/// <summary>
/// Defines character classification categories for string analysis.
/// </summary>
public enum CharacterClass
{
    /// <summary>Lowercase letter characters.</summary>
    LowerCase,
    /// <summary>Uppercase letter characters.</summary>
    UpperCase,
    /// <summary>Any letter character.</summary>
    Letter,
    /// <summary>Numeric digit characters.</summary>
    Numeric,
    /// <summary>Non-alphanumeric symbol characters.</summary>
    Symbol
}
