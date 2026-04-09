namespace NAIware.Rules;

/// <summary>
/// An interface for enumeration value lookup.
/// </summary>
public interface IEnumeration
{
    /// <summary>Gets a value from the enumeration by name.</summary>
    IValue GetValue(string enumeratedName);
}
