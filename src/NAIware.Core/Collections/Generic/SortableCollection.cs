using System.Collections;

namespace NAIware.Core.Collections.Generic;

/// <summary>
/// A generic class that represents a sortable collection with reflection-based sorting and binary search.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public class SortableCollection<T> : List<T>, IConvertible
{
    /// <summary>
    /// Stores the sorted state of the collection instance.
    /// </summary>
    protected bool _sorted = false;

    /// <summary>
    /// An array of properties that we have sorted on.
    /// </summary>
    private string[]? _sortedProperties;

    /// <summary>
    /// Returns <c>true</c> if the collection is sorted; otherwise <c>false</c>.
    /// </summary>
    public bool Sorted => _sorted;

    /// <summary>
    /// Sorts the collection by the specified properties.
    /// </summary>
    /// <param name="propertyNames">The properties to sort by.</param>
    public void Sort(params string[] propertyNames)
    {
        if (propertyNames is null || propertyNames.Length < 1)
            throw new InvalidOperationException("To sort a sortable collection, you must specify at least one property.");

        if (Count > 0)
            Sort(new ReflectionComparer<T>(propertyNames));

        _sortedProperties = propertyNames;
        _sorted = true;
    }

    /// <summary>
    /// Performs a binary search on the collection.
    /// </summary>
    /// <param name="match">The match object to search for.</param>
    /// <returns>The match index if found; otherwise the bitwise complement of the nearest match.</returns>
    public new int BinarySearch(T match)
    {
        if (!Sorted)
            throw new InvalidOperationException("The collection must be sorted before a binary search is called!");

        if (Count == 0) return -1;

        return base.BinarySearch(match, new ReflectionComparer<T>(_sortedProperties!));
    }

    #region IConvertible Members

    TypeCode IConvertible.GetTypeCode() => throw new NotImplementedException();
    bool IConvertible.ToBoolean(IFormatProvider? provider) => throw new NotImplementedException();
    byte IConvertible.ToByte(IFormatProvider? provider) => throw new NotImplementedException();
    char IConvertible.ToChar(IFormatProvider? provider) => throw new NotImplementedException();
    DateTime IConvertible.ToDateTime(IFormatProvider? provider) => throw new NotImplementedException();
    decimal IConvertible.ToDecimal(IFormatProvider? provider) => throw new NotImplementedException();
    double IConvertible.ToDouble(IFormatProvider? provider) => throw new NotImplementedException();
    short IConvertible.ToInt16(IFormatProvider? provider) => throw new NotImplementedException();
    int IConvertible.ToInt32(IFormatProvider? provider) => throw new NotImplementedException();
    long IConvertible.ToInt64(IFormatProvider? provider) => throw new NotImplementedException();
    sbyte IConvertible.ToSByte(IFormatProvider? provider) => throw new NotImplementedException();
    float IConvertible.ToSingle(IFormatProvider? provider) => throw new NotImplementedException();
    string IConvertible.ToString(IFormatProvider? provider) => throw new NotImplementedException();
    ushort IConvertible.ToUInt16(IFormatProvider? provider) => throw new NotImplementedException();
    uint IConvertible.ToUInt32(IFormatProvider? provider) => throw new NotImplementedException();
    ulong IConvertible.ToUInt64(IFormatProvider? provider) => throw new NotImplementedException();

    /// <summary>
    /// Converts the collection to the specified type that supports <see cref="IList"/>.
    /// </summary>
    public object ToType(Type conversionType, IFormatProvider? provider)
    {
        if (conversionType.GetInterface("IList") is null)
            throw new NotImplementedException("The object to convert to does not support IList.");

        if (Activator.CreateInstance(conversionType) is not IList o)
            throw new InvalidOperationException($"Unable to create instance of type {conversionType.FullName}.");

        for (int i = 0; i < Count; i++)
        {
            o.Add(this[i]);
        }

        return o;
    }

    #endregion
}
