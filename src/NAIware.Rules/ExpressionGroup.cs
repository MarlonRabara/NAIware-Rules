namespace NAIware.Rules;

/// <summary>
/// A class that represents grouping of expressions.
/// </summary>
public class ExpressionGroup
{
    private readonly string? _name;
    private readonly ExpressionGroup? _parent;
    private readonly IEngine? _containingengine;

    /// <summary>Protected default constructor.</summary>
    protected ExpressionGroup() { }

    /// <summary>Creates a new expression group with the specified name and engine.</summary>
    public ExpressionGroup(string name, IEngine containingEngine)
    {
        _name = name;
        _parent = null;
        _containingengine = containingEngine;
    }

    /// <summary>Creates a new expression group with a parent referenced by name.</summary>
    public ExpressionGroup(string name, string parentName, IEngine containingEngine)
        : this(name, containingEngine.ExpressionGroups[parentName])
    {
    }

    /// <summary>Creates a new expression group with the specified parent group.</summary>
    public ExpressionGroup(string name, ExpressionGroup parentGroup)
    {
        _name = name;
        _parent = parentGroup;
        _containingengine = _parent.Container;
    }

    /// <summary>Gets the name of the expression group.</summary>
    public string? Name => _name;

    /// <summary>Gets the parent of the current expression group instance.</summary>
    public ExpressionGroup? Parent => _parent;

    /// <summary>Gets the container engine for the expression group.</summary>
    public IEngine? Container => _containingengine;
}
