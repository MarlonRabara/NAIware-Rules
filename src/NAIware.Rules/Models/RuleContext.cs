namespace NAIware.Rules.Models;

/// <summary>
/// Represents the domain classifier against which rules are evaluated.
/// The <see cref="QualifiedTypeName"/> enables the rule processor to automatically
/// resolve the context from the input object's type.
/// </summary>
public class RuleContext
{
    private readonly Guid _identity;
    private readonly string _name;
    private readonly List<RuleCategory> _categories = [];
    private readonly List<RuleExpression> _expressions = [];
    private readonly List<RuleParameterDefinition> _parameterDefinitions = [];

    /// <summary>Creates a new rule context.</summary>
    public RuleContext(string name, string qualifiedTypeName, string description = "")
        : this(Guid.NewGuid(), name, qualifiedTypeName, description)
    {
    }

    /// <summary>Creates a new rule context with an explicit identity.</summary>
    public RuleContext(Guid identity, string name, string qualifiedTypeName, string description = "")
    {
        _identity = identity;
        _name = name;
        QualifiedTypeName = qualifiedTypeName;
        Description = description;
    }

    /// <summary>Gets the unique identity of the context.</summary>
    public Guid Identity => _identity;

    /// <summary>Gets the name of the context.</summary>
    public string Name => _name;

    /// <summary>Gets or sets the description of the context.</summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified type name of the input object this context targets.
    /// Used by the rule processor to auto-resolve the context from an input object.
    /// </summary>
    public string QualifiedTypeName { get; set; }

    /// <summary>Gets the categories in this context.</summary>
    public List<RuleCategory> Categories => _categories;

    /// <summary>Gets the rule expressions in this context.</summary>
    public List<RuleExpression> Expressions => _expressions;

    /// <summary>Gets the parameter definitions in this context.</summary>
    public List<RuleParameterDefinition> ParameterDefinitions => _parameterDefinitions;

    /// <summary>Adds a category to this context.</summary>
    public RuleCategory AddCategory(string name, string description = "")
    {
        var category = new RuleCategory(name, description);
        _categories.Add(category);
        return category;
    }

    /// <summary>Adds a rule expression to this context.</summary>
    public RuleExpression AddExpression(string name, string expression, string description = "")
    {
        var ruleExpression = new RuleExpression(name, expression, description);
        _expressions.Add(ruleExpression);
        return ruleExpression;
    }

    /// <summary>Adds a parameter definition to this context.</summary>
    public RuleParameterDefinition AddParameterDefinition(string name, string qualifiedTypeName, string? propertyPath = null, string description = "")
    {
        var paramDef = new RuleParameterDefinition(name, qualifiedTypeName, propertyPath, description);
        _parameterDefinitions.Add(paramDef);
        return paramDef;
    }

    /// <summary>
    /// Finds a category by name or dotted path.
    /// <para>
    /// First searches top-level categories by exact name match. If no top-level
    /// category matches, the name is interpreted as a dotted path
    /// (e.g., <c>"Eligibility.Age"</c>) and resolved through the hierarchy.
    /// As a final fallback, descendant categories are matched by name.
    /// </para>
    /// </summary>
    public RuleCategory? FindCategoryByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        RuleCategory? topLevel = _categories.Find(c => string.Equals(c.Name, name, StringComparison.Ordinal));
        if (topLevel is not null) return topLevel;

        if (name.Contains('.'))
        {
            string[] segments = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0)
            {
                RuleCategory? root = _categories.Find(c => string.Equals(c.Name, segments[0], StringComparison.Ordinal));
                if (root is not null)
                {
                    string remainder = string.Join('.', segments, 1, segments.Length - 1);
                    return string.IsNullOrEmpty(remainder) ? root : root.FindByPath(remainder);
                }
            }
        }

        foreach (RuleCategory top in _categories)
            foreach (RuleCategory descendant in top.EnumerateDescendants())
                if (string.Equals(descendant.Name, name, StringComparison.Ordinal))
                    return descendant;

        return null;
    }
}
