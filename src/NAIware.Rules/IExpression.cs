namespace NAIware.Rules;

/// <summary>
/// An interface for an expression that evaluates to a result of type <typeparamref name="R"/>.
/// </summary>
/// <typeparam name="R">The result type of the expression.</typeparam>
public interface IExpression<R> : IExpressionComponent, IValue<R>
{
    /// <summary>Gets the left operand of the expression.</summary>
    IExpressionComponent LeftOperand { get; }

    /// <summary>Gets the right operand of the expression.</summary>
    IExpressionComponent RightOperand { get; }

    /// <summary>Gets or sets the operator of the expression.</summary>
    IOperator Operator { get; set; }

    /// <summary>Evaluates the expression.</summary>
    R Evaluate();

    /// <summary>Evaluates the expression with the given parameters.</summary>
    R Evaluate(ref Parameters parameters);

    /// <summary>Gets or sets whether there is a left parenthesis.</summary>
    bool HasLeftParenthesis { get; set; }

    /// <summary>Gets or sets whether there is a right parenthesis.</summary>
    bool HasRightParenthesis { get; set; }
}
