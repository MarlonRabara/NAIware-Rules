namespace NAIware.Rules;

/// <summary>
/// A comparison operator that validates against known comparison symbols.
/// </summary>
public class ComparisonOperator : Operator, IComparisonOperator
{
    /// <summary>Creates a comparison operator with the specified symbol.</summary>
    /// <exception cref="ArgumentException">Thrown when the symbol is not a recognized comparison operator.</exception>
    public ComparisonOperator(string symbol) : base(symbol)
    {
        switch (symbol)
        {
            case "!=" or "<>" or ">" or "<" or ">=" or "<=" or "=":
                break;
            default:
                throw new ArgumentException("Symbol is an unrecognized comparison operator.", nameof(symbol));
        }
    }
}
