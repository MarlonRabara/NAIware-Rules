namespace NAIware.Rules.Catalog;

/// <summary>
/// The top-level catalog container for rule contexts and their associated definitions.
/// </summary>
public class RulesLibrary
{
    private readonly Guid _identity;
    private readonly string _name;
    private readonly List<RuleContext> _contexts = [];

    /// <summary>Creates a new rules library with the specified name.</summary>
    public RulesLibrary(string name)
        : this(Guid.NewGuid(), name, string.Empty)
    {
    }

    /// <summary>Creates a new rules library with the specified name and description.</summary>
    public RulesLibrary(string name, string description)
        : this(Guid.NewGuid(), name, description)
    {
    }

    /// <summary>Creates a new rules library with the specified identity, name, and description.</summary>
    public RulesLibrary(Guid identity, string name, string description)
    {
        _identity = identity;
        _name = name;
        Description = description;
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets the unique identity of the library.</summary>
    public Guid Identity => _identity;

    /// <summary>Gets the name of the library.</summary>
    public string Name => _name;

    /// <summary>Gets or sets the description of the library.</summary>
    public string Description { get; set; }

    /// <summary>Gets the UTC timestamp when the library was created.</summary>
    public DateTimeOffset CreatedUtc { get; }

    /// <summary>Gets the rule contexts in this library.</summary>
    public List<RuleContext> Contexts => _contexts;

    /// <summary>Adds a context to the library.</summary>
    public RuleContext AddContext(string name, string qualifiedTypeName, string description = "")
    {
        var context = new RuleContext(name, qualifiedTypeName, description);
        _contexts.Add(context);
        return context;
    }

    /// <summary>Finds a context by its qualified type name.</summary>
    public RuleContext? FindContextByTypeName(string qualifiedTypeName) =>
        _contexts.Find(c => string.Equals(c.QualifiedTypeName, qualifiedTypeName, StringComparison.Ordinal));

    /// <summary>Finds a context by name.</summary>
    public RuleContext? FindContextByName(string name) =>
        _contexts.Find(c => string.Equals(c.Name, name, StringComparison.Ordinal));
}
