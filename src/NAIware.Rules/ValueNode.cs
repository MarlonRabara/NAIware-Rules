using NAIware.Core.Collections;

namespace NAIware.Rules;

/// <summary>
/// A leaf-level expression node that holds a value and blocks child assignment.
/// </summary>
/// <typeparam name="R">The result type.</typeparam>
public class ValueNode<R> : ExpressionNode<R>
{
    /// <summary>Creates a value node with the specified value.</summary>
    public ValueNode(IValue value) : base(value)
    {
    }

    /// <inheritdoc/>
    public override BinaryTreeNode<IExpressionComponent>? LeftChild
    {
        get => base.LeftChild;
        set => throw new InvalidOperationException("All value nodes do not have any children and are leaf level items.");
    }

    /// <inheritdoc/>
    public override BinaryTreeNode<IExpressionComponent>? RightChild
    {
        get => base.RightChild;
        set => throw new InvalidOperationException("All value nodes do not have any children and are leaf level items.");
    }
}
