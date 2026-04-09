using NAIware.Core.Collections;

namespace NAIware.Rules;

/// <summary>
/// A complex expression where operands are themselves expressions.
/// </summary>
/// <typeparam name="OP">The operator type.</typeparam>
/// <typeparam name="R">The return type.</typeparam>
public class ComplexExpression<OP, R> : ExpressionNode<R>, IComplexExpression<R>
    where OP : IOperator
{
    /// <summary>Creates a complex expression from two expressions and an operator.</summary>
    public ComplexExpression(IExpression<R> lhs, OP op, IExpression<R> rhs) : base(op)
    {
        if (lhs is ISimpleExpression<R> simpleLhs && rhs is ComplexExpression<OP, R> complexRhs)
            Initialize(simpleLhs, complexRhs);
        else if (lhs is ISimpleExpression<R> sl && rhs is ISimpleExpression<R> sr)
            Initialize(sl, sr);
        else if (lhs is ComplexExpression<OP, R> complexLhs2 && rhs is ISimpleExpression<R> simpleRhs2)
            Initialize(complexLhs2, simpleRhs2);
        else if (lhs is ComplexExpression<OP, R> cl && rhs is ComplexExpression<OP, R> cr)
            Initialize(cl, cr);
    }

    /// <summary>Creates a complex expression from a complex left and simple right.</summary>
    public ComplexExpression(ComplexExpression<OP, R> lhs, OP op, ISimpleExpression<R> rhs) : base(op)
    {
        Initialize(lhs, rhs);
    }

    /// <summary>Creates a complex expression from two complex expressions.</summary>
    public ComplexExpression(ComplexExpression<OP, R> lhs, OP op, ComplexExpression<OP, R> rhs) : base(op)
    {
        Initialize(lhs, rhs);
    }

    /// <summary>Creates a complex expression from two simple expressions.</summary>
    public ComplexExpression(ISimpleExpression<R> lhs, OP op, ISimpleExpression<R> rhs) : base(op)
    {
        Initialize(lhs, rhs);
    }

    /// <summary>Creates a complex expression from a simple left and complex right.</summary>
    public ComplexExpression(ISimpleExpression<R> lhs, OP op, ComplexExpression<OP, R> rhs) : base(op)
    {
        Initialize(lhs, rhs);
    }

    #region IComplexExpression implementation

    /// <inheritdoc/>
    public IExpression<R> LeftOperand => (LeftChild as IExpression<R>)!;

    /// <inheritdoc/>
    public IExpression<R> RightOperand
    {
        get
        {
            IExpression<R> thisexp = this;
            return (thisexp.RightOperand as IExpression<R>)!;
        }
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

    IOperator IComplexExpression<R>.Operator
    {
        get
        {
            IExpression<R> thisexp = this;
            return thisexp.Operator;
        }
    }

    /// <inheritdoc/>
    public override string Text => throw new NotImplementedException();

    #endregion

    #region Evaluation

    /// <inheritdoc/>
    public override R Evaluate(ref Parameters parameters)
    {
        if (typeof(R) == typeof(bool))
            return (R)Convert.ChangeType(BoolEvaluator(ref parameters), typeof(R));
        else
            return (R)Convert.ChangeType(MathEvaluator(ref parameters)!, typeof(R));
    }

    private bool BoolEvaluator(ref Parameters parameters)
    {
        int leftdepth = LeftChild is null ? 0 : LeftChild.Depth;
        int rightdepth = RightChild is null ? 0 : RightChild.Depth;

        if (leftdepth <= rightdepth && ((LogicalOperator)Value).Text == "and")
            return ((IExpression<bool>)LeftChild!).Evaluate(ref parameters) && ((IExpression<bool>)RightChild!).Evaluate(ref parameters);
        else if (leftdepth >= rightdepth && ((LogicalOperator)Value).Text == "and")
            return ((IExpression<bool>)RightChild!).Evaluate(ref parameters) && ((IExpression<bool>)LeftChild!).Evaluate(ref parameters);
        else if (leftdepth <= rightdepth && ((LogicalOperator)Value).Text == "or")
            return ((IExpression<bool>)LeftChild!).Evaluate(ref parameters) || ((IExpression<bool>)RightChild!).Evaluate(ref parameters);
        else
            return ((IExpression<bool>)RightChild!).Evaluate(ref parameters) || ((IExpression<bool>)LeftChild!).Evaluate(ref parameters);
    }

    private decimal? MathEvaluator(ref Parameters parameters)
    {
        return Helper.SimpleMath(
            ((MathOperator)Value).Text[0],
            ((IExpression<decimal>)RightChild!).Evaluate(ref parameters),
            ((IExpression<decimal>)LeftChild!).Evaluate(ref parameters));
    }

    #endregion

    #region Initialize helpers

    private void Initialize(ComplexExpression<OP, R> lhs, ISimpleExpression<R> rhs)
    {
        LeftChild = lhs;
        SetRightChild(rhs);
    }

    private void Initialize(ComplexExpression<OP, R> lhs, ComplexExpression<OP, R> rhs)
    {
        LeftChild = lhs;
        RightChild = rhs;
    }

    private void Initialize(ISimpleExpression<R> lhs, ISimpleExpression<R> rhs)
    {
        SetLeftChild(lhs);
        SetRightChild(rhs);
    }

    private void Initialize(ISimpleExpression<R> lhs, ComplexExpression<OP, R> rhs)
    {
        SetLeftChild(lhs);
        RightChild = rhs;
    }

    private void SetLeftChild(ISimpleExpression<R> simpleExpression)
    {
        LeftChild = simpleExpression as BinaryTreeNode<IExpressionComponent>;
    }

    private void SetRightChild(ISimpleExpression<R> simpleExpression)
    {
        RightChild = simpleExpression as BinaryTreeNode<IExpressionComponent>;
    }

    #endregion
}
