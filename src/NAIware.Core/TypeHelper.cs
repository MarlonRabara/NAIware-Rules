using System.ComponentModel;
using System.Text;

namespace NAIware.Core;

/// <summary>
/// Provides static utility methods for type conversion, null checking, and value analysis.
/// </summary>
public static class TypeHelper
{
    /// <summary>
    /// Determines whether the specified type is considered primitive (including <see cref="string"/>).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><c>true</c> if the type is primitive or <see cref="string"/>; otherwise, <c>false</c>.</returns>
    public static bool IsPrimitive(this Type type)
    {
        if (type == typeof(string)) return true;
        return type.IsValueType & type.IsPrimitive;
    }

    /// <summary>
    /// Determines whether the specified type is a simple type (primitives, string, DateTime, Guid, etc.).
    /// </summary>
    /// <param name="anytype">The type to verify.</param>
    /// <returns><c>true</c> if it is a simple type; otherwise, <c>false</c>.</returns>
    public static bool IsSimpleType(Type anytype)
    {
        if (anytype.IsPrimitive) return true;

        return anytype.FullName switch
        {
            "System.String" or "System.DateTime" or "System.Guid" or "System.Boolean" or
            "System.Byte" or "System.SByte" or "System.Single" or
            "System.UInt16" or "System.UInt32" or "System.UInt64" or
            "System.Int16" or "System.Int32" or "System.Int64" or
            "System.Double" or "System.Decimal" or "System.Char" => true,
            _ => false
        };
    }

    /// <summary>
    /// Converts a whole percent number to its fractional representation (e.g., 4.5 → 0.045).
    /// </summary>
    public static decimal ConvertPercentToFraction(decimal wholePercentNumber) =>
        wholePercentNumber / 100m;

    /// <summary>
    /// Removes all non-numeric characters from a string, preserving digits, negative sign, and decimal point.
    /// </summary>
    public static string? RemoveNonNumeric(string? any)
    {
        if (string.IsNullOrEmpty(any)) return any;

        bool isNegative = any[0] == '(' && any[^1] == ')';

        var sb = new StringBuilder();
        for (int i = 0; i < any.Length; i++)
        {
            if (char.IsDigit(any[i]) || any[i] == '-' || any[i] == '.')
                sb.Append(any[i]);
        }

        if (sb.Length > 0 && sb[0] != '-' && isNegative)
            sb.Insert(0, '-');

        return sb.ToString();
    }

    /// <summary>
    /// Converts one enumeration to another if their text representations match.
    /// </summary>
    public static D ConvertEnum<S, D>(S sourceEnum) where S : notnull =>
        (D)Enum.Parse(typeof(D), sourceEnum.ToString()!);

    /// <summary>
    /// Returns the first non-null value from the parameter set, converted to <typeparamref name="T"/>.
    /// </summary>
    public static T? Coalesce<T>(params object?[] valueArray) =>
        Coalesce<T>(false, valueArray);

    /// <summary>
    /// Returns the first non-null (or non-empty) value from the parameter set.
    /// </summary>
    /// <param name="useEmptyAlgorithm">If <c>true</c>, considers empty strings as null.</param>
    /// <param name="valueArray">The values to coalesce.</param>
    public static T? Coalesce<T>(bool useEmptyAlgorithm, params object?[] valueArray)
    {
        ArgumentNullException.ThrowIfNull(valueArray);
        if (valueArray.Length < 2)
            throw new ArgumentException("There must be a valid reference to a set of variables where the variable count is greater than one.");

        for (int i = 0; i < valueArray.Length; i++)
            if (useEmptyAlgorithm ? !IsEmpty(valueArray[i]) : !IsNull(valueArray[i]))
                return Convert<T>(valueArray[i]);

        return default;
    }

    /// <summary>
    /// Coalesces two decimal values, returning the first non-null value.
    /// </summary>
    public static decimal Coalesce(decimal? v1, decimal v2) =>
        Coalesce<decimal?>(v1, new decimal?(v2))!.Value;

    /// <summary>
    /// Determines whether the specified type is a <see cref="Nullable{T}"/>.
    /// </summary>
    public static bool IsNullable(Type anyType) =>
        anyType.IsGenericType && anyType.GetGenericTypeDefinition() == typeof(Nullable<>);

    /// <summary>
    /// Gets the underlying type from a <see cref="Nullable{T}"/> type.
    /// </summary>
    public static Type GetTypeFromNullableType(Type nullableType)
    {
        if (!IsNullable(nullableType))
            throw new ArgumentException($"The type '{nullableType.Name}' specified is not a nullable type.", nameof(nullableType));

        return nullableType.GetGenericArguments()[0];
    }

    /// <summary>
    /// Determines whether an object is <c>null</c> or <see cref="DBNull.Value"/>.
    /// </summary>
    public static bool IsNull(object? anyObject) =>
        anyObject is null || anyObject == DBNull.Value;

    /// <summary>
    /// Determines whether all specified objects have values (not null or empty).
    /// </summary>
    public static bool HasValues(object? anyObject, params object?[] otherObjects)
    {
        if (IsEmpty(anyObject)) return false;
        if (otherObjects is null || otherObjects.Length == 0) return true;

        foreach (object? obj in otherObjects)
            if (IsEmpty(obj)) return false;

        return true;
    }

    /// <summary>
    /// Determines whether any of the specified objects are empty.
    /// </summary>
    public static bool IsAnyEmpty(object? anyObject, params object?[] otherObjects)
    {
        if (IsEmpty(anyObject)) return true;
        if (otherObjects is null || otherObjects.Length == 0) return false;

        foreach (object? obj in otherObjects)
            if (IsEmpty(obj)) return true;

        return false;
    }

    /// <summary>
    /// Determines whether all of the specified objects are empty.
    /// </summary>
    public static bool IsEmpty(object? anyObject, params object?[] otherObjects)
    {
        if (!IsEmpty(anyObject)) return false;
        if (otherObjects is null || otherObjects.Length == 0) return true;

        foreach (object? obj in otherObjects)
            if (!IsEmpty(obj)) return false;

        return true;
    }

    /// <summary>
    /// Determines whether an object is null or an empty string.
    /// </summary>
    public static bool IsEmpty(object? anyObject) =>
        IsNull(anyObject) || (anyObject is string s && s.Trim() == string.Empty);

    /// <summary>
    /// Converts an empty value to the default of <typeparamref name="T"/>.
    /// </summary>
    public static T? ConvertEmptyToDefault<T>(object? any) =>
        IsEmpty(any) ? default : Convert<T>(any);

    /// <summary>
    /// Converts a null value to the default of <typeparamref name="T"/>.
    /// </summary>
    public static T? ConvertNullToDefault<T>(object? any) =>
        IsNull(any) ? default : Convert<T>(any);

    /// <summary>
    /// Sets the target value from the source, using default if empty.
    /// </summary>
    public static void SetValue<DestT>(ref DestT? targetValue, object? sourceValue)
    {
        if (IsEmpty(sourceValue))
            targetValue = default;
        else
            targetValue = Convert<DestT>(sourceValue);
    }

    /// <summary>
    /// Determines whether an object instance is nullable.
    /// </summary>
    public static bool IsNullable(object? anyValue)
    {
        if (anyValue is null) return true;
        return IsNullable(anyValue.GetType());
    }

    /// <summary>
    /// Determines whether an object has a meaningful value.
    /// </summary>
    /// <param name="anyValue">The value to test.</param>
    /// <param name="considerZerosAsValue">If <c>false</c>, numeric zero is treated as having no value.</param>
    public static bool HasValue(object? anyValue, bool considerZerosAsValue) =>
        !(anyValue is null ||
          (anyValue is string s && s == string.Empty) ||
          (IsNumeric(anyValue) && !considerZerosAsValue && Convert<decimal>(anyValue) == 0m));

    /// <summary>
    /// Determines whether the specified value can be converted to a number.
    /// </summary>
    public static bool IsNumeric(object? anyValue)
    {
        try
        {
            Convert<decimal>(anyValue);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Converts any value to the specified destination type, with nullable type support.
    /// </summary>
    /// <typeparam name="DestT">The destination type.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted value.</returns>
    public static DestT? Convert<DestT>(object? value)
    {
        if (value is null) return default;

        Type conversionType = typeof(DestT);
        Type valType = value.GetType();

        if (conversionType == valType ||
            valType.IsSubclassOf(conversionType) ||
            (conversionType.FullName is not null && valType.GetInterface(conversionType.FullName) is not null))
            return (DestT)value;

        if (value is string s && s.Trim() == string.Empty) return default;

        if (value is string && typeof(DestT) != typeof(string))
            value = ((string)value).Trim();

        if (IsNullable(conversionType))
        {
            var nullableConverter = new NullableConverter(conversionType);
            conversionType = nullableConverter.UnderlyingType;
        }

        if (conversionType == typeof(bool))
        {
            if (value is string boolstring && !string.IsNullOrEmpty(boolstring))
            {
                boolstring = boolstring.ToLower().Trim();
                value = boolstring is "true" or "1" or "t" or "y";
            }
        }

        if (conversionType == typeof(Guid))
        {
            try { value = new Guid(value.ToString()!); } catch { /* intentional */ }
        }

        return (DestT)System.Convert.ChangeType(value, conversionType);
    }
}
