namespace NAIware.Rules.Catalog;

/// <summary>
/// A grouping mechanism under a <see cref="RuleContext"/> that organizes
/// <see cref="RuleExpression"/> instances into named execution sets.
/// A category has a many-to-many relationship with expressions.
/// </summary>
public class RuleCategory
{
    private readonly Guid _identity;
    private readonly string _name;
    private readonly List<RuleCategoryExpression> _categoryExpressions = [];

    /// <summary>Creates a new rule category.</summary>
    public RuleCategory(string name, string description = "")
        : this(Guid.NewGuid(), name, description)
    {
    }

    /// <summary>Creates a new rule category with an explicit identity.</summary>
    public RuleCategory(Guid identity, string name, string description = "")
    {
        _identity = identity;
        _name = name;
        Description = description;
    }

    /// <summary>Gets the unique identity of the category.</summary>
    public Guid Identity => _identity;

    /// <summary>Gets the name of the category.</summary>
    public string Name => _name;

    /// <summary>Gets or sets the description of the category.</summary>
    public string Description { get; set; }

    /// <summary>Gets the join entities linking this category to its expressions.</summary>
    public List<RuleCategoryExpression> CategoryExpressions => _categoryExpressions;

    /// <summary>
    /// Adds a rule expression to this category with the specified ordinal.
    /// If no ordinal is provided, it is appended at the end.
    /// </summary>
    public RuleCategoryExpression AddExpression(RuleExpression expression, int? ordinal = null)
    {
        var join = new RuleCategoryExpression(
            _identity,
            expression.Identity,
            ordinal ?? _categoryExpressions.Count,
            expression);

        _categoryExpressions.Add(join);
        return join;
    }

    /// <summary>Gets the ordered rule expressions in this category.</summary>
    public IEnumerable<RuleExpression> GetExpressions() =>
        _categoryExpressions
            .OrderBy(ce => ce.Ordinal)
            .Select(ce => ce.Expression);

    /// <summary>Gets only the active rule expressions in this category.</summary>
    public IEnumerable<RuleExpression> GetActiveExpressions() =>
        GetExpressions().Where(e => e.IsActive);
}
