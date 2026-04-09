using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace NAIware.Core.Reflection;

/// <summary>
/// Provides static utility methods for reflection-based property/field access and object fingerprinting.
/// </summary>
public static class ReflectionHelper
{
    private static readonly System.Collections.Hashtable _fieldtable = new();
    private static readonly System.Collections.Hashtable _proptable = new();

    /// <summary>
    /// Gets a property dictionary for quick lookup of properties on a reflected type.
    /// </summary>
    /// <param name="reflectedType">The type to reflect.</param>
    /// <param name="bindingFlags">Optional binding flags for property retrieval.</param>
    /// <param name="excludedInheritedTypes">Types whose derived properties should be excluded.</param>
    /// <param name="isCaseInsensitiveComparison">If <c>true</c>, uses case-insensitive key comparison.</param>
    /// <returns>A dictionary of property name to <see cref="PropertyInfo"/> mappings.</returns>
    public static Dictionary<string, PropertyInfo>? GetPropertyDictionary(
        Type reflectedType,
        BindingFlags? bindingFlags,
        List<Type>? excludedInheritedTypes,
        bool isCaseInsensitiveComparison = false)
    {
        PropertyInfo[] props = bindingFlags is not null
            ? reflectedType.GetProperties(bindingFlags.Value)
            : reflectedType.GetPublicProperties();

        if (excludedInheritedTypes is not null)
        {
            foreach (Type excludedT in excludedInheritedTypes)
            {
                props = props.Where(pi => !pi.PropertyType.IsSubclassOf(excludedT)).ToArray();
            }
        }

        var propertyDictionary = isCaseInsensitiveComparison
            ? new Dictionary<string, PropertyInfo>(StringComparer.InvariantCultureIgnoreCase)
            : new Dictionary<string, PropertyInfo>();

        foreach (PropertyInfo p in props)
        {
            propertyDictionary.TryAdd(p.Name, p);
        }

        return propertyDictionary;
    }

    /// <summary>
    /// Gets a property dictionary for quick lookup of properties on a reflected object.
    /// </summary>
    public static Dictionary<string, PropertyInfo>? GetPropertyDictionary(
        object? reflectedObject,
        BindingFlags? bindingFlags,
        List<Type>? excludedInheritedTypes,
        bool isCaseInsensitiveComparison = false)
    {
        if (reflectedObject is null) return null;
        return GetPropertyDictionary(reflectedObject.GetType(), bindingFlags, excludedInheritedTypes, isCaseInsensitiveComparison);
    }

    /// <summary>
    /// Generates a fingerprint hash for an object based on its property values.
    /// Two objects with identical data produce the same fingerprint.
    /// </summary>
    /// <param name="anyObject">The object to fingerprint.</param>
    /// <returns>A hex string fingerprint, or <c>null</c> if fingerprinting fails.</returns>
    /// <remarks>
    /// Migration note: This method previously used BinaryFormatter (removed in .NET 5+).
    /// It now uses JSON serialization to produce deterministic byte sequences for hashing.
    /// </remarks>
    public static string? GetObjectFingerprint(object? anyObject)
    {
        var visitedObjectHashCodes = new HashSet<int>();
        var values = new List<object>();

        CollectFingerprintValues(anyObject, ref values, ref visitedObjectHashCodes);

        try
        {
            using var memory = new MemoryStream();
            foreach (var val in values)
            {
                var json = JsonSerializer.SerializeToUtf8Bytes(val, val.GetType());
                memory.Write(json, 0, json.Length);
            }

            return SHA384.HashData(memory.ToArray()).ToHexString();
        }
        catch
        {
            return null;
        }
    }

    private static void CollectFingerprintValues(
        object? anyObject,
        ref List<object> values,
        ref HashSet<int> visitedObjectHashCodes)
    {
        if (anyObject is null) return;

        if (anyObject is DateTime dateTimeObject)
        {
            values.Add($"{dateTimeObject.ToLongDateString()} {dateTimeObject.ToLongTimeString()}");
            return;
        }

        Type objectType = anyObject.GetType();

        if (anyObject is string || objectType.IsPrimitive() || anyObject is ValueType)
        {
            values.Add(anyObject);
            return;
        }

        if (visitedObjectHashCodes.Contains(anyObject.GetHashCode()))
            return;

        visitedObjectHashCodes.Add(anyObject.GetHashCode());

        values.Add($"Object:{objectType.Name}");

        var properties = GetPropertyDictionary(anyObject,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy, null);

        if (properties?.Count > 0)
        {
            if (anyObject is System.Collections.IList list)
            {
                values.Add($"ListCount:{properties["Count"].GetValue(anyObject)}");

                for (int indexpos = 0; indexpos < list.Count; indexpos++)
                {
                    CollectFingerprintValues(
                        properties["Item"].GetValue(anyObject, [indexpos]),
                        ref values, ref visitedObjectHashCodes);
                }
            }
            else
            {
                foreach (var propName in properties.Keys.OrderBy(x => x).ToList())
                {
                    values.Add($"Property:{propName}");
                    CollectFingerprintValues(properties[propName].GetValue(anyObject), ref values, ref visitedObjectHashCodes);
                }
            }
        }
    }

    /// <summary>
    /// Gets a cached property table for quick property lookup.
    /// </summary>
    public static PropertyTable GetProperties(object any)
    {
        ArgumentNullException.ThrowIfNull(any);

        lock (_proptable.SyncRoot)
        {
            var match = _proptable[any.GetType()] as PropertyTable;
            if (match is not null) return match;

            match = new PropertyTable(any);
            _proptable[any.GetType()] = match;
            return match;
        }
    }

    /// <summary>
    /// Gets a cached field table for quick field lookup.
    /// </summary>
    public static FieldTable GetFields(object any)
    {
        ArgumentNullException.ThrowIfNull(any);

        lock (_proptable.SyncRoot)
        {
            var match = _fieldtable[any.GetType()] as FieldTable;
            if (match is not null) return match;

            match = new FieldTable(any);
            _fieldtable[any.GetType()] = match;
            return match;
        }
    }

    /// <summary>
    /// Gets a property value from an object instance using reflection.
    /// </summary>
    public static object? GetPropertyValue(object objectInstance, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(objectInstance);

        var proptable = GetProperties(objectInstance);
        var property = proptable[propertyName];

        return property?.GetValue(objectInstance, null);
    }

    /// <summary>
    /// Sets a property value on an object instance using reflection.
    /// </summary>
    public static void SetPropertyValue(object objectInstance, string propertyName, object? value)
    {
        ArgumentNullException.ThrowIfNull(objectInstance);

        var proptable = GetProperties(objectInstance);
        var property = proptable[propertyName];

        property?.SetValue(objectInstance, value, null);
    }
}
