namespace NAIware.Rules;

/// <summary>
/// A typed simple expression interface where operands are values.
/// </summary>
/// <typeparam name="V">The value type of the operands.</typeparam>
/// <typeparam name="R">The result type of the expression.</typeparam>
public interface ISimpleExpression<V, R> : ISimpleExpression<R>
{
    /// <summary>Gets or sets the left operand.</summary>
    new IValue<V> LeftOperand { get; set; }

    /// <summary>Gets or sets the right operand.</summary>
    new IValue<V> RightOperand { get; set; }

    /// <summary>Gets or sets the operator.</summary>
    new IOperator Operator { get; set; }
}

/// <summary>
/// A simple expression interface with a known operand type.
/// </summary>
/// <typeparam name="R">The result type of the expression.</typeparam>
public interface ISimpleExpression<R> : IExpression<R>
{
    /// <summary>Gets the type of the operands.</summary>
    Type Type { get; }
}
