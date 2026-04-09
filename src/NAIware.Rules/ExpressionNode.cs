using NAIware.Core.Collections;

namespace NAIware.Rules;

/// <summary>
/// An expression node backed by a binary tree structure.
/// </summary>
/// <typeparam name="R">The resulting type of the expression node.</typeparam>
public class ExpressionNode<R> : BinaryTreeNode<IExpressionComponent>, IExpression<R>
{
    private bool _hasleftparen;
    private bool _hasrightparen;

    /// <summary>Creates an expression node with the specified component value.</summary>
    public ExpressionNode(IExpressionComponent nodeValue) : base()
    {
        Value = nodeValue;
    }

    #region IExpression implementation

    IExpressionComponent IExpression<R>.LeftOperand
    {
        get
        {
            if (LeftChild is null) return null!;

            if (this is ISimpleExpression<R>)
                return LeftChild.Value;
            else
                return (LeftChild as IExpression<R>)!;
        }
    }

    IExpressionComponent IExpression<R>.RightOperand
    {
        get
        {
            if (RightChild is null) return null!;
            if (this is ISimpleExpression<R>)
                return RightChild.Value;
            else
                return (RightChild as IExpression<R>)!;
        }
    }

    IOperator IExpression<R>.Operator
    {
        get => (Value as IOperator)!;
        set => Value = value;
    }

    /// <inheritdoc/>
    public virtual string Text => throw new NotImplementedException();

    /// <summary>Evaluates the expression using internal parameters.</summary>
    public R Evaluate()
    {
        Parameters? parameters = null;
        return Evaluate(ref parameters!);
    }

    /// <summary>Evaluates the expression with the given parameters.</summary>
    public virtual R Evaluate(ref Parameters parameters) => default!;

    /// <inheritdoc/>
    public bool HasLeftParenthesis
    {
        get => _hasleftparen;
        set => _hasleftparen = value;
    }

    /// <inheritdoc/>
    public bool HasRightParenthesis
    {
        get => _hasrightparen;
        set => _hasrightparen = value;
    }

    #endregion

    #region ICloneable implementation

    object ICloneable.Clone() => InnerClone();

    /// <summary>Inherited inner clone for deep cloning.</summary>
    protected virtual ExpressionNode<R> InnerClone()
    {
        var clone = (ExpressionNode<R>)MemberwiseClone();
        clone.Value = ((IExpressionComponent)Value).Clone() as IExpressionComponent;
        if (clone.LeftChild is not null)
            clone.LeftChild = ((ExpressionNode<R>)LeftChild!).Clone();
        if (clone.RightChild is not null)
            clone.RightChild = ((ExpressionNode<R>)RightChild!).Clone();
        return clone;
    }

    /// <summary>Returns a clone of this expression node.</summary>
    public ExpressionNode<R> Clone() => InnerClone();

    #endregion

    #region IValue explicit implementation

    R IValue<R>.Value
    {
        get => Evaluate();
        set => throw new NotImplementedException();
    }

    object IValue.Value
    {
        get => ((IValue<R>)this).Value!;
        set => throw new NotImplementedException();
    }

    /// <summary>Gets the result type of the expression.</summary>
    public Type Type => typeof(bool);

    #endregion
}
