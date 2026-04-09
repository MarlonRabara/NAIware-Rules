namespace NAIware.Rules;

/// <summary>
/// A complex expression interface where operands are themselves expressions.
/// </summary>
/// <typeparam name="R">The result type of the expression.</typeparam>
public interface IComplexExpression<R> : IExpression<R>
{
    /// <summary>Gets the left expression operand.</summary>
    new IExpression<R> LeftOperand { get; }

    /// <summary>Gets the right expression operand.</summary>
    new IExpression<R> RightOperand { get; }

    /// <summary>Gets the operator.</summary>
    new IOperator Operator { get; }
}
