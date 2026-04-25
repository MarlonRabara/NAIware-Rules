namespace NAIware.Rules.Models;

/// <summary>
/// The top-level rule library container for contexts, categories, rule expressions, and library-level version metadata.
/// </summary>
/// <remarks>
/// Versioning is intentionally scoped to the library. Individual rule expressions are not independently versioned;
/// changing any rule, category, context, parameter, or result definition produces a new library version or snapshot.
/// </remarks>
public class RulesLibrary
{
    /// <summary>Creates a new rules library.</summary>
    public RulesLibrary()
    {
        Identity = Guid.NewGuid();
        Name = "New Rule Library";
        Description = string.Empty;
        Version = 1;
        CreatedUtc = DateTimeOffset.UtcNow;
        SavedUtc = CreatedUtc;
    }

    /// <summary>Creates a new rules library with the specified name.</summary>
    public RulesLibrary(string name)
        : this(name, string.Empty)
    {
    }

    /// <summary>Creates a new rules library with the specified name and description.</summary>
    public RulesLibrary(string name, string description)
        : this()
    {
        Name = name;
        Description = description;
    }

    /// <summary>Creates a new rules library with the specified identity, name, and description.</summary>
    public RulesLibrary(Guid identity, string name, string description)
        : this(name, description)
    {
        Identity = identity;
    }

    /// <summary>Gets or sets the stable logical identity of the library.</summary>
    public Guid Identity { get; set; }

    /// <summary>Gets or sets the library name.</summary>
    public string Name { get; set; }

    /// <summary>Gets or sets the library description.</summary>
    public string Description { get; set; }

    /// <summary>Gets or sets the library-level version number.</summary>
    public int Version { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the library was created.</summary>
    public DateTimeOffset CreatedUtc { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the library was last saved.</summary>
    public DateTimeOffset SavedUtc { get; set; }

    /// <summary>Gets or sets the rule contexts in this library.</summary>
    public List<RuleContext> Contexts { get; set; } = [];

    /// <summary>Adds a context to the library.</summary>
    public RuleContext AddContext(string name, string qualifiedTypeName, string description = "")
    {
        var context = new RuleContext(name, qualifiedTypeName, description);
        Contexts.Add(context);
        return context;
    }

    /// <summary>Finds a context by its qualified type name.</summary>
    public RuleContext? FindContextByTypeName(string qualifiedTypeName) =>
        Contexts.Find(c => string.Equals(c.QualifiedTypeName, qualifiedTypeName, StringComparison.Ordinal));

    /// <summary>Finds a context by name.</summary>
    public RuleContext? FindContextByName(string name) =>
        Contexts.Find(c => string.Equals(c.Name, name, StringComparison.Ordinal));
}
