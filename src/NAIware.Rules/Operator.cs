namespace NAIware.Rules;

/// <summary>
/// An abstract base class for operators in expressions.
/// </summary>
public abstract class Operator : IOperator
{
    private readonly string _symbol;

    /// <summary>Creates an operator with the specified symbol.</summary>
    protected Operator(string symbol)
    {
        _symbol = symbol;
    }

    /// <inheritdoc/>
    public string Text => _symbol;

    /// <inheritdoc/>
    object ICloneable.Clone() => InnerClone();

    /// <summary>Inherited inner clone for deep cloning.</summary>
    protected virtual Operator InnerClone() => (Operator)MemberwiseClone();

    /// <summary>Returns a clone of this operator.</summary>
    public Operator Clone() => InnerClone();
}
