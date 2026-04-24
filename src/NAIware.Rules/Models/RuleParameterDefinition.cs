namespace NAIware.Rules.Models;

/// <summary>
/// Defines a parameter that can be resolved from an input object within a <see cref="RuleContext"/>.
/// Maps to the runtime <see cref="IParameter"/> that the existing engine uses during evaluation.
/// </summary>
public class RuleParameterDefinition
{
    private readonly Guid _identity;

    /// <summary>Creates a new rule parameter definition.</summary>
    public RuleParameterDefinition(string name, string qualifiedTypeName, string? propertyPath = null, string description = "")
        : this(Guid.NewGuid(), name, qualifiedTypeName, propertyPath, description)
    {
    }

    /// <summary>Creates a new rule parameter definition with an explicit identity.</summary>
    public RuleParameterDefinition(Guid identity, string name, string qualifiedTypeName, string? propertyPath = null, string description = "")
    {
        _identity = identity;
        Name = name;
        QualifiedTypeName = qualifiedTypeName;
        PropertyPath = propertyPath;
        Description = description;
    }

    /// <summary>Gets the unique identity of this parameter definition.</summary>
    public Guid Identity => _identity;

    /// <summary>Gets or sets the parameter name (matches the token used in expressions).</summary>
    public string Name { get; set; }

    /// <summary>Gets or sets the description of the parameter.</summary>
    public string Description { get; set; }

    /// <summary>Gets or sets the fully qualified type name (e.g., "System.Int32").</summary>
    public string QualifiedTypeName { get; set; }

    /// <summary>
    /// Gets or sets the optional dot-notation property path for extraction
    /// via <see cref="ParameterFactory"/> (e.g., "Borrowers.0.Age").
    /// When null, the <see cref="Name"/> is used as the path.
    /// </summary>
    public string? PropertyPath { get; set; }
}
