namespace NAIware.Rules.Catalog;

/// <summary>
/// Join entity representing the many-to-many relationship between
/// <see cref="RuleExpression"/> and <see cref="RuleParameterDefinition"/>.
/// </summary>
public class RuleExpressionParameter
{
    /// <summary>Creates a new expression-parameter join.</summary>
    public RuleExpressionParameter(Guid expressionIdentity, Guid parameterIdentity, RuleParameterDefinition parameter)
    {
        ExpressionIdentity = expressionIdentity;
        ParameterIdentity = parameterIdentity;
        Parameter = parameter;
    }

    /// <summary>Gets the identity of the owning expression.</summary>
    public Guid ExpressionIdentity { get; }

    /// <summary>Gets the identity of the linked parameter definition.</summary>
    public Guid ParameterIdentity { get; }

    /// <summary>Gets the linked parameter definition.</summary>
    public RuleParameterDefinition Parameter { get; }
}
