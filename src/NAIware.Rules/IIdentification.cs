namespace NAIware.Rules;

/// <summary>
/// An interface for identification with a GUID and name.
/// </summary>
public interface IIdentification : ICloneable
{
    /// <summary>Gets the unique identity.</summary>
    Guid Identity { get; }

    /// <summary>Gets the name.</summary>
    string Name { get; }
}
