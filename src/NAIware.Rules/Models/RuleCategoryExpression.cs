namespace NAIware.Rules.Models;

/// <summary>
/// Join entity representing the many-to-many relationship between
/// <see cref="RuleCategory"/> and <see cref="RuleExpression"/>.
/// Includes an ordinal for controlling evaluation order within a category.
/// </summary>
public class RuleCategoryExpression
{
    /// <summary>Creates a new category-expression join.</summary>
    public RuleCategoryExpression(Guid categoryIdentity, Guid expressionIdentity, int ordinal, RuleExpression expression)
    {
        CategoryIdentity = categoryIdentity;
        ExpressionIdentity = expressionIdentity;
        Ordinal = ordinal;
        Expression = expression;
    }

    /// <summary>Gets the identity of the owning category.</summary>
    public Guid CategoryIdentity { get; }

    /// <summary>Gets the identity of the linked expression.</summary>
    public Guid ExpressionIdentity { get; }

    /// <summary>Gets or sets the evaluation order within the category.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets the linked expression.</summary>
    public RuleExpression Expression { get; }
}
