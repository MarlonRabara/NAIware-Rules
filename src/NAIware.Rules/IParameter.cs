namespace NAIware.Rules;

/// <summary>
/// A typed parameter interface with enumeration support.
/// </summary>
/// <typeparam name="V">The value type of the parameter.</typeparam>
public interface IParameter<V> : IValue<V>, IParameter
{
    /// <summary>Gets or sets the enumeration associated with the parameter.</summary>
    IEnumeration Enumeration { get; set; }
}

/// <summary>
/// An untyped parameter interface providing name, description, and enumeration support.
/// </summary>
public interface IParameter : IValue
{
    /// <summary>Gets the name of the parameter.</summary>
    string Name { get; }

    /// <summary>Gets the description of the parameter.</summary>
    string Description { get; }

    /// <summary>Gets a value indicating whether the parameter is enumerated.</summary>
    bool IsEnumerated { get; }

    /// <summary>Gets the enumerated value by name.</summary>
    IValue GetEnumeratedValue(string enumName);
}
