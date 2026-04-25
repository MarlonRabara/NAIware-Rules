using System.Text.Json.Serialization;

namespace NAIware.Rules.Models;

/// <summary>
/// Join entity representing the relationship between <see cref="RuleCategory"/> and <see cref="RuleExpression"/>.
/// Includes an ordinal for controlling evaluation order within a category.
/// </summary>
public class RuleCategoryExpression
{
    /// <summary>Creates an empty category-expression join.</summary>
    public RuleCategoryExpression()
    {
    }

    /// <summary>Creates a new category-expression join.</summary>
    public RuleCategoryExpression(Guid categoryIdentity, Guid expressionIdentity, int ordinal, RuleExpression? expression)
    {
        CategoryIdentity = categoryIdentity;
        ExpressionIdentity = expressionIdentity;
        Ordinal = ordinal;
        Expression = expression;
    }

    /// <summary>Gets or sets the identity of the owning category.</summary>
    public Guid CategoryIdentity { get; set; }

    /// <summary>Gets or sets the identity of the linked expression.</summary>
    public Guid ExpressionIdentity { get; set; }

    /// <summary>Gets or sets the evaluation order within the category.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the linked expression. This is rebuilt after JSON load from the expression id.</summary>
    [JsonIgnore]
    public RuleExpression? Expression { get; set; }
}
