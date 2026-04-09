namespace NAIware.Rules;

/// <summary>
/// An identification object with a unique GUID and name.
/// </summary>
public class Identification : IIdentification
{
    private readonly Guid _id;
    private readonly string _name;

    /// <summary>Creates a new identification with a new GUID and empty name.</summary>
    public Identification() : this(Guid.NewGuid(), string.Empty) { }

    /// <summary>Creates a new identification with a new GUID and the specified name.</summary>
    public Identification(string name) : this(Guid.NewGuid(), name) { }

    /// <summary>Creates a new identification with the specified GUID and name.</summary>
    public Identification(Guid guid, string name)
    {
        _id = guid;
        _name = name;
    }

    /// <inheritdoc/>
    public Guid Identity => _id;

    /// <inheritdoc/>
    public string Name => _name;

    /// <inheritdoc/>
    object ICloneable.Clone() => Clone();

    /// <summary>Returns a clone of this identification.</summary>
    public Identification Clone() => (Identification)MemberwiseClone();
}
