using NAIware.Core.Collections;

namespace NAIware.Rules.Rules;

/// <summary>
/// A tree structure for rule expressions that evaluates to a boolean result.
/// </summary>
public class RuleTree : Tree<ExpressionNode<bool>, IExpressionComponent>
{
    private Identification _id;

    /// <summary>Creates a new rule tree with a new GUID.</summary>
    public RuleTree() : this(Guid.NewGuid(), string.Empty) { }

    /// <summary>Creates a new rule tree with the specified description.</summary>
    public RuleTree(string description) : this(Guid.NewGuid(), description) { }

    /// <summary>Creates a new rule tree with the specified GUID.</summary>
    public RuleTree(Guid guid) : this(guid, string.Empty) { }

    /// <summary>Creates a new rule tree with the specified GUID and description.</summary>
    public RuleTree(Guid guid, string description)
    {
        _id = new Identification(guid, description);
    }

    /// <summary>Gets or sets the identification of the rule tree.</summary>
    public Identification Identification
    {
        get => _id;
        set => _id = value;
    }

    /// <summary>Evaluates the rule tree.</summary>
    /// <returns>True if the rules evaluate to true; otherwise false.</returns>
    public bool Evaluate()
    {
        if (_root is null) return false;
        return _root.Evaluate();
    }

    /// <summary>Evaluates the rule tree with the given parameters.</summary>
    public bool Evaluate(ref Parameters parameters)
    {
        if (_root is null) return false;
        return _root.Evaluate(ref parameters);
    }

    /// <summary>Renders the expression tree as a string.</summary>
    public string RenderExpression() => RenderExpression(_root);

    private string RenderExpression(ExpressionNode<bool>? node)
    {
        if (node is null) return string.Empty;
        IExpression<bool>? rule = node as IExpression<bool>;
        return string.Format("{0}{1}{2}{3}{4}",
            rule is not null && rule.HasLeftParenthesis ? "(" : string.Empty,
            RenderExpression(node.LeftChild as ExpressionNode<bool>),
            node.Value is IOperator ? $" {node.Value.Text} " : node.Value.Text,
            RenderExpression(node.RightChild as ExpressionNode<bool>),
            rule is not null && rule.HasRightParenthesis ? ")" : string.Empty);
    }
}
