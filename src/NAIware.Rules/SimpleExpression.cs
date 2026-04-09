namespace NAIware.Rules;

/// <summary>
/// A simple expression where the operands are values, supporting rule and math evaluation.
/// </summary>
/// <typeparam name="V">The value type.</typeparam>
/// <typeparam name="OP">The operator type.</typeparam>
/// <typeparam name="R">The expression result type.</typeparam>
public class SimpleExpression<V, OP, R> : ExpressionNode<R>, ISimpleExpression<V, R>
    where OP : IOperator
{
    /// <summary>Creates a simple expression with the specified left value, operator, and right value.</summary>
    public SimpleExpression(IValue lhs, OP op, IValue rhs) : base(op)
    {
        LeftChild = new ValueNode<R>(lhs);
        RightChild = new ValueNode<R>(rhs);
    }

    #region ISimpleExpression<V,R> implementation

    /// <inheritdoc/>
    public IValue<V> LeftOperand
    {
        get
        {
            IValue? valObj = LeftChild?.Value as IValue;
            if (valObj is not null && valObj.Value is not null && valObj.Type != typeof(V))
                return new GenericValue<V>((V)valObj.Value);

            return (LeftChild?.Value as IValue<V>)!;
        }
        set => LeftChild!.Value = (value as IValue<V>)!;
    }

    /// <inheritdoc/>
    public IValue<V> RightOperand
    {
        get
        {
            IValue? valObj = RightChild?.Value as IValue;
            if (valObj is not null && valObj.Value is not null && valObj.Type != typeof(V))
                return new GenericValue<V>((V)valObj.Value);

            return (RightChild?.Value as IValue<V>)!;
        }
        set => RightChild!.Value = (value as IValue<V>)!;
    }

    /// <summary>Gets or sets the typed operator.</summary>
    public OP Operator
    {
        get
        {
            IExpression<R> thisexp = this;
            return (OP)thisexp.Operator;
        }
        set
        {
            IExpression<R> thisexp = this;
            thisexp.Operator = value;
        }
    }

    IOperator ISimpleExpression<V, R>.Operator
    {
        get => Operator;
        set => Operator = (OP)value;
    }

    /// <inheritdoc/>
    public override string Text => throw new NotImplementedException();

    #endregion

    #region ISimpleExpression<R> implementation

    Type ISimpleExpression<R>.Type => typeof(V);

    #endregion

    #region Evaluation

    /// <inheritdoc/>
    public override R Evaluate(ref Parameters parameters)
    {
        if (typeof(OP) == typeof(LogicalOperator) || typeof(OP) == typeof(ComparisonOperator))
            return (R)Convert.ChangeType(RuleEvaluator(ref parameters), typeof(R));
        else if (typeof(OP) == typeof(MathOperator))
            return (R)Convert.ChangeType(MathEvaluator(ref parameters)!, typeof(R));
        else
            throw new InvalidOperationException("The current version of the Not-AI Ware Rules Framework only supports rules and math evaluations.");
    }

    private decimal? MathEvaluator(ref Parameters parameters)
    {
        object? leftval;
        object? rightval;

        if (LeftOperand is IParameter lp && parameters is not null)
            leftval = parameters[lp.Name].Value;
        else
            leftval = LeftOperand is null ? default(R) : (object?)LeftOperand.Value;

        if (RightOperand is IParameter rp && parameters is not null)
            rightval = parameters[rp.Name].Value;
        else
            rightval = RightOperand is not null ? (object?)RightOperand.Value : null;

        return Helper.SimpleMath(
            ((MathOperator)Value).Text[0],
            rightval is not null ? (decimal?)Convert.ToDecimal(rightval) : null,
            leftval is not null ? (decimal?)Convert.ToDecimal(leftval) : null);
    }

    private bool RuleEvaluator(ref Parameters parameters)
    {
        IComparable? leftcomparable;
        IValue<V>? rightval;

        if (LeftOperand is IParameter lp && parameters is not null)
            leftcomparable = parameters[lp.Name].Value as IComparable;
        else if (LeftOperand is not null && LeftOperand.Value is not null)
            leftcomparable = LeftOperand.Value as IComparable;
        else
            leftcomparable = null;

        if (RightOperand is IParameter rp && parameters is not null)
            rightval = parameters[rp.Name] as IValue<V>;
        else
            rightval = RightOperand;

        return ((ComparisonOperator)Value).Text switch
        {
            "<=" => leftcomparable is not null && leftcomparable.CompareTo(rightval!.Value) <= 0,
            "<" => leftcomparable is not null && leftcomparable.CompareTo(rightval!.Value) < 0,
            ">=" => leftcomparable is not null && leftcomparable.CompareTo(rightval!.Value) >= 0,
            ">" => leftcomparable is not null && leftcomparable.CompareTo(rightval!.Value) > 0,
            "=" => leftcomparable is null && (rightval is null || rightval.Value is null)
                   || (leftcomparable is not null && leftcomparable.CompareTo(rightval!.Value) == 0),
            "!=" or "<>" => leftcomparable is not null && leftcomparable.CompareTo(rightval!.Value) != 0,
            _ => false
        };
    }

    #endregion
}
