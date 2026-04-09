namespace NAIware.Rules;

/// <summary>
/// A method mapper for dynamic method invocation with optional case sensitivity.
/// </summary>
public class MethodMap : Dictionary<string, IMethodWrapper>
{
    private readonly bool _isCaseSensitive;

    /// <summary>Creates a case-insensitive method map.</summary>
    public MethodMap() : this(false) { }

    /// <summary>Creates a method map with the specified case sensitivity.</summary>
    public MethodMap(bool isCaseSensitive)
    {
        _isCaseSensitive = isCaseSensitive;
    }

    /// <summary>Adds a method wrapper to the map.</summary>
    public new void Add(string methodName, IMethodWrapper methodWrapper)
    {
        base.Add(GetKey(methodName), methodWrapper);
    }

    private string GetKey(string methodName) =>
        _isCaseSensitive ? methodName : methodName.ToLower();

    /// <summary>Gets or sets a method wrapper by name.</summary>
    public new IMethodWrapper this[string methodName]
    {
        get => base[GetKey(methodName)];
        set
        {
            if (!ContainsKey(methodName))
                Add(methodName, value);
            else
                base[GetKey(methodName)] = value;
        }
    }

    /// <summary>Determines whether the map contains a method with the specified name.</summary>
    public new bool ContainsKey(string methodName) => base.ContainsKey(GetKey(methodName));
}
