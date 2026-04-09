namespace NAIware.Rules;

/// <summary>
/// A generic value that wraps a typed value for use in expressions.
/// </summary>
/// <typeparam name="V">The type of value stored.</typeparam>
public class GenericValue<V> : IValue<V>
{
    private V _value;

    /// <summary>Creates a new generic value.</summary>
    public GenericValue(V value)
    {
        _value = value;
    }

    /// <summary>Sets the value, allowing overloads in descending classes.</summary>
    protected virtual void SetValue(object value)
    {
        _value = (V)Convert.ChangeType(value, typeof(V));
    }

    /// <inheritdoc/>
    public V Value
    {
        get => _value;
        set => _value = value;
    }

    /// <inheritdoc/>
    public virtual string Text
    {
        get
        {
            if (!typeof(V).IsSubclassOf(typeof(ValueType)))
            {
                object? any = _value;
                if (any is null) return string.Empty;
            }

            if (typeof(V) == typeof(DateTime))
                return $"#{_value}#";
            else
                return _value?.ToString() ?? string.Empty;
        }
    }

    /// <inheritdoc/>
    public override string ToString() => Text;

    #region IValue explicit implementation

    object IValue.Value
    {
        get => _value!;
        set => SetValue(value);
    }

    Type IValue.Type => typeof(V);

    #endregion

    #region ICloneable implementation

    object ICloneable.Clone() => InnerClone();

    /// <summary>Inherited inner clone for deep cloning.</summary>
    protected virtual GenericValue<V> InnerClone() => (GenericValue<V>)MemberwiseClone();

    /// <summary>Returns a clone of this value.</summary>
    public GenericValue<V> Clone() => InnerClone();

    #endregion
}
