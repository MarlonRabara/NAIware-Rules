namespace NAIware.Rules.Models;

/// <summary>
/// Represents the domain classifier against which rules are evaluated.
/// The <see cref="QualifiedTypeName"/> enables the rule processor to automatically
/// resolve the context from the input object's type.
/// </summary>
public class RuleContext
{
    /// <summary>Creates a new rule context.</summary>
    public RuleContext()
    {
        Identity = Guid.NewGuid();
        Name = string.Empty;
        QualifiedTypeName = string.Empty;
        Description = string.Empty;
    }

    /// <summary>Creates a new rule context.</summary>
    public RuleContext(string name, string qualifiedTypeName, string description = "")
        : this(Guid.NewGuid(), name, qualifiedTypeName, description)
    {
    }

    /// <summary>Creates a new rule context with an explicit identity.</summary>
    public RuleContext(Guid identity, string name, string qualifiedTypeName, string description = "")
    {
        Identity = identity;
        Name = name;
        QualifiedTypeName = qualifiedTypeName;
        Description = description;
    }

    /// <summary>Gets or sets the unique identity of the context.</summary>
    public Guid Identity { get; set; }

    /// <summary>Gets or sets the name of the context.</summary>
    public string Name { get; set; }

    /// <summary>Gets or sets the description of the context.</summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified type name of the input object this context targets.
    /// Used by the rule processor to auto-resolve the context from an input object.
    /// </summary>
    public string QualifiedTypeName { get; set; }

    /// <summary>Gets or sets the optional source assembly path used by the editor to resolve the context type.</summary>
    public string? AssemblyPath { get; set; }

    /// <summary>Gets or sets the optional serialized sample file path used to hydrate and preview context data in the editor.</summary>
    public string? SerializedDataPath { get; set; }

    /// <summary>Gets or sets the optional source assembly path used by the editor to resolve a custom serializer type.</summary>
    public string? SerializerAssemblyPath { get; set; }

    /// <summary>Gets or sets the optional custom serializer type that exposes Deserialize(string filePath).</summary>
    public string? SerializerQualifiedTypeName { get; set; }

    /// <summary>Gets or sets the categories in this context.</summary>
    public List<RuleCategory> Categories { get; set; } = [];

    /// <summary>Gets or sets the rule expressions in this context.</summary>
    public List<RuleExpression> Expressions { get; set; } = [];

    /// <summary>Gets or sets the parameter definitions in this context.</summary>
    public List<RuleParameterDefinition> ParameterDefinitions { get; set; } = [];

    /// <summary>Adds a category to this context.</summary>
    public RuleCategory AddCategory(string name, string description = "")
    {
        var category = new RuleCategory(name, description);
        Categories.Add(category);
        return category;
    }

    /// <summary>Adds a rule expression to this context.</summary>
    public RuleExpression AddExpression(string name, string expression, string description = "")
    {
        var ruleExpression = new RuleExpression(name, expression, description);
        Expressions.Add(ruleExpression);
        return ruleExpression;
    }

    /// <summary>Adds a parameter definition to this context.</summary>
    public RuleParameterDefinition AddParameterDefinition(string name, string qualifiedTypeName, string? propertyPath = null, string description = "")
    {
        var paramDef = new RuleParameterDefinition(name, qualifiedTypeName, propertyPath, description);
        ParameterDefinitions.Add(paramDef);
        return paramDef;
    }

    /// <summary>Finds a category by name or dotted path.</summary>
    public RuleCategory? FindCategoryByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        RuleCategory? topLevel = Categories.Find(c => string.Equals(c.Name, name, StringComparison.Ordinal));
        if (topLevel is not null) return topLevel;

        if (name.Contains('.'))
        {
            string[] segments = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0)
            {
                RuleCategory? root = Categories.Find(c => string.Equals(c.Name, segments[0], StringComparison.Ordinal));
                if (root is not null)
                {
                    string remainder = string.Join('.', segments, 1, segments.Length - 1);
                    return string.IsNullOrEmpty(remainder) ? root : root.FindByPath(remainder);
                }
            }
        }

        foreach (RuleCategory top in Categories)
            foreach (RuleCategory descendant in top.EnumerateDescendants())
                if (string.Equals(descendant.Name, name, StringComparison.Ordinal))
                    return descendant;

        return null;
    }
}
