namespace NAIware.Rules;

/// <summary>
/// A typed value interface for expression components.
/// </summary>
/// <typeparam name="V">The value type.</typeparam>
public interface IValue<V> : IValue
{
    /// <summary>Gets or sets the typed value.</summary>
    new V Value { get; set; }
}

/// <summary>
/// An untyped value interface for expression components.
/// </summary>
public interface IValue : IExpressionComponent
{
    /// <summary>Gets or sets the value as an object.</summary>
    object Value { get; set; }

    /// <summary>Gets the type of the value.</summary>
    Type Type { get; }
}
