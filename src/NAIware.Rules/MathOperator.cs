namespace NAIware.Rules;

/// <summary>
/// A math operator that validates against known arithmetic symbols.
/// </summary>
public class MathOperator : Operator, IMathOperator
{
    /// <summary>Creates a math operator with the specified symbol.</summary>
    /// <exception cref="ArgumentException">Thrown when the symbol is not a recognized math operator.</exception>
    public MathOperator(string symbol) : base(symbol)
    {
        switch (symbol)
        {
            case "*" or "/" or "+" or "-":
                break;
            default:
                throw new ArgumentException("Symbol is an unrecognized math operator.", nameof(symbol));
        }
    }
}
