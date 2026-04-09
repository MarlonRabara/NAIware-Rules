namespace NAIware.Rules;

/// <summary>
/// A generic enumeration that provides lookup for enum values by name.
/// </summary>
/// <typeparam name="E">The enum type.</typeparam>
public class GenericEnumeration<E> : GenericValue<E>, IEnumeration
{
    private readonly Dictionary<string, E> _enum = new();

    /// <summary>Creates a new generic enumeration from the specified enum type.</summary>
    /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="E"/> is not an enum type.</exception>
    public GenericEnumeration() : base(default!)
    {
        Type enumerationType = typeof(E);
        if (!enumerationType.IsEnum)
            throw new InvalidOperationException("Unable to create a generic enumeration object from a type that is not an enumeration.");

        string[] enumNames = Enum.GetNames(enumerationType);
        E[] enumValues = (E[])Enum.GetValues(enumerationType);

        for (int i = 0; i < enumNames.Length; i++)
        {
            _enum.Add($"{enumerationType.Name}.{enumNames[i]}", enumValues[i]);
        }
    }

    /// <summary>Sets the value, handling both string-based and direct enum value assignment.</summary>
    protected override void SetValue(object value)
    {
        if (value is string enumkey)
        {
            Value = _enum[enumkey];
        }
        else
        {
            base.SetValue(value);
            foreach (E val in _enum.Values)
            {
                if (Equals(val, value))
                {
                    Value = val;
                    return;
                }
            }
            throw new OverflowException($"The value ({Value}) is not within the boundaries of the supported enumerations");
        }
    }

    /// <summary>Gets a value from the enumeration by name with case-insensitive fallback.</summary>
    internal GenericValue<E> GetValue(string enumeration)
    {
        E matchedValue = default!;
        bool hasMatch = false;
        string enumTypeName = typeof(E).Name;

        if (_enum.TryGetValue(enumeration, out E? directMatch))
        {
            matchedValue = directMatch;
            hasMatch = true;
        }
        else
        {
            string fullEnumName = $"{enumTypeName}.{enumeration}";
            if (_enum.TryGetValue(fullEnumName, out E? fullMatch))
            {
                matchedValue = fullMatch;
                hasMatch = true;
            }
            else
            {
                foreach (var key in _enum.Keys)
                {
                    if (string.Equals(key, fullEnumName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(key, enumeration, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedValue = _enum[key];
                        hasMatch = true;
                        break;
                    }
                }
            }
        }

        if (!hasMatch)
        {
            throw new InvalidOperationException(
                $"The parameter [{nameof(enumeration)}] with value '{enumeration}' is not a valid value for the enumeration: {enumTypeName}. Allowed values are: {string.Join(",", _enum.Keys)}.");
        }

        return new GenericValue<E>(matchedValue);
    }

    IValue IEnumeration.GetValue(string enumeratedName) => GetValue(enumeratedName);
}
