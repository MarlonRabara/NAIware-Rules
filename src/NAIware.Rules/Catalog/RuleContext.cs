namespace NAIware.Rules.Catalog;

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

    /// <summary>Finds a category by name.</summary>
    public RuleCategory? FindCategoryByName(string name) =>
        _categories.Find(c => string.Equals(c.Name, name, StringComparison.Ordinal));
}
