using NAIware.Core.Collections;

namespace NAIware.Rules.Formulae;

/// <summary>
/// A tree structure for formula expressions that evaluates to a decimal result.
/// </summary>
public class FormulaTree : Tree<ExpressionNode<decimal>, IExpressionComponent>
{
    private Identification _id;

    /// <summary>Creates a new formula tree with a new GUID.</summary>
    public FormulaTree() : this(Guid.NewGuid(), string.Empty) { }

    /// <summary>Creates a new formula tree with the specified description.</summary>
    public FormulaTree(string description) : this(Guid.NewGuid(), description) { }

    /// <summary>Creates a new formula tree with the specified GUID.</summary>
    public FormulaTree(Guid guid) : this(guid, string.Empty) { }

    /// <summary>Creates a new formula tree with the specified GUID and description.</summary>
    public FormulaTree(Guid guid, string description)
    {
        _id = new Identification(guid, description);
    }

    /// <summary>Gets or sets the identification of the formula tree.</summary>
    public Identification Identification
    {
        get => _id;
        set => _id = value;
    }

    /// <summary>Evaluates the formula tree.</summary>
    public decimal? Evaluate()
    {
        if (_root is null) return 0m;
        return _root.Evaluate();
    }

    /// <summary>Evaluates the formula tree with the given parameters.</summary>
    public decimal Evaluate(ref Parameters parameters)
    {
        if (_root is null) return 0m;
        return _root.Evaluate(ref parameters);
    }

    /// <summary>Renders the expression tree as a string.</summary>
    public string RenderExpression() => RenderExpression(_root);

    private string RenderExpression(ExpressionNode<decimal>? node)
    {
        if (node is null) return string.Empty;
        IExpression<decimal>? formula = node as IExpression<decimal>;
        return string.Format("{0}{1}{2}{3}{4}",
            formula is not null && formula.HasLeftParenthesis ? "(" : string.Empty,
            RenderExpression(node.LeftChild as ExpressionNode<decimal>),
            node.Value is IOperator ? $" {node.Value.Text} " : node.Value.Text,
            RenderExpression(node.RightChild as ExpressionNode<decimal>),
            formula is not null && formula.HasRightParenthesis ? ")" : string.Empty);
    }
}
