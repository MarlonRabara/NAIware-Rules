namespace NAIware.Rules;

/// <summary>
/// A logical operator that validates against known logical symbols.
/// </summary>
public class LogicalOperator : Operator, ILogicalOperator
{
    /// <summary>Creates a logical operator with the specified symbol.</summary>
    /// <exception cref="ArgumentException">Thrown when the symbol is not a recognized logical operator.</exception>
    public LogicalOperator(string symbol) : base(symbol)
    {
        switch (symbol.ToLower())
        {
            case "and" or "or" or "&&" or "||":
                break;
            default:
                throw new ArgumentException("Symbol is an unrecognized logical operator.", nameof(symbol));
        }
    }
}
