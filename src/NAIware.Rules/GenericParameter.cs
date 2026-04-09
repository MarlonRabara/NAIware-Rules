using NAIware.Core;

namespace NAIware.Rules;

/// <summary>
/// A generic parameter that wraps a typed value with name, description, and optional enumeration support.
/// </summary>
/// <typeparam name="V">The type of value stored.</typeparam>
public class GenericParameter<V> : GenericValue<V>, IParameter<V>
{
    private readonly string _name;
    private readonly string _description;
    private IEnumeration? _enum;
    private IEngine? _container;

    /// <summary>Creates a new parameter with the specified name and description.</summary>
    public GenericParameter(string parameterName, string description)
        : this(parameterName, description, default!)
    {
    }

    /// <summary>Creates a new parameter with the specified name, description, and value.</summary>
    public GenericParameter(string parameterName, string description, V value)
        : base(value)
    {
        _name = parameterName;
        _description = description;

        Type paramType = typeof(V);

        if (TypeHelper.IsNullable(paramType))
            paramType = TypeHelper.GetTypeFromNullableType(paramType);

        if (paramType.IsEnum)
        {
            Type genericType = typeof(GenericEnumeration<>).MakeGenericType(paramType);
            _enum = Activator.CreateInstance(genericType) as IEnumeration;
        }
    }

    /// <summary>Gets or sets the container engine of the parameter.</summary>
    internal IEngine? Container
    {
        get => _container;
        set => _container = value;
    }

    /// <inheritdoc/>
    public IEnumeration Enumeration
    {
        get => _enum!;
        set => _enum = value;
    }

    /// <inheritdoc/>
    public bool IsEnumerated => _enum is not null;

    /// <inheritdoc/>
    public IValue GetEnumeratedValue(string enumeratedName)
    {
        if (!IsEnumerated) return null!;
        return _enum!.GetValue(enumeratedName);
    }

    /// <inheritdoc/>
    public override string ToString() => _name;

    /// <inheritdoc/>
    protected override GenericValue<V> InnerClone()
    {
        return (GenericParameter<V>)base.InnerClone();
    }

    /// <summary>Returns a clone of this parameter.</summary>
    public new GenericParameter<V> Clone() => (GenericParameter<V>)InnerClone();

    /// <inheritdoc/>
    public string Name => _name;

    /// <inheritdoc/>
    public string Description => _description;

    /// <inheritdoc/>
    public override string Text => $"{_name}";
}
