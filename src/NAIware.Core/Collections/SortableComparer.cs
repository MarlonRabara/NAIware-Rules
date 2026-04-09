using System.Reflection;

namespace NAIware.Core.Collections;

/// <summary>
/// A comparer that uses reflection on member info (properties and fields) to compare objects.
/// </summary>
public class SortableComparer : System.Collections.IComparer
{
    private readonly Type _sortObjType;
    private readonly string[] _properties;

    /// <summary>
    /// Creates a new comparer instance from an object instance and property names.
    /// </summary>
    /// <param name="sortCollectionObject">An object from the sortable collection instance.</param>
    /// <param name="propertyNames">An array of property names.</param>
    public SortableComparer(object sortCollectionObject, params string[] propertyNames)
        : this(sortCollectionObject.GetType(), propertyNames) { }

    /// <summary>
    /// Creates a new comparer instance from a type and property names.
    /// </summary>
    /// <param name="sortObjectType">The type from the sortable collection instance.</param>
    /// <param name="propertyNames">An array of property names.</param>
    public SortableComparer(Type sortObjectType, params string[] propertyNames)
    {
        _sortObjType = sortObjectType;
        _properties = propertyNames;
    }

    /// <summary>
    /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
    /// </summary>
    public int Compare(object? obj1, object? obj2)
    {
        if (obj1 is null && obj2 is null) return 0;
        if (obj1 is null) return -1;
        if (obj2 is null) return 1;
        if (ReferenceEquals(obj1, obj2)) return 0;

        if (_sortObjType.IsInstanceOfType(obj1) && _sortObjType.IsInstanceOfType(obj2))
            return CompareSortableObjects(obj1, obj2);

        if (_sortObjType.IsInstanceOfType(obj1) && !_sortObjType.IsInstanceOfType(obj2))
            return CompareToSortableObject(obj2 as IComparable, obj1);

        if (!_sortObjType.IsInstanceOfType(obj1) && _sortObjType.IsInstanceOfType(obj2))
            return CompareToSortableObject(obj1 as IComparable, obj2);

        if (obj1 is IComparable compare1 && obj2 is IComparable)
            return compare1.CompareTo(obj2);

        if (obj1 is not IComparable && obj2 is not IComparable) return 0;
        if (obj1 is not IComparable) return -1;
        return 1;
    }

    private int CompareSortableObjects(object obj1, object obj2)
    {
        int compareResult = 0;
        IComparable? val1 = null, val2 = null;

        for (int i = 0; i < _properties.Length; i++)
        {
            var mi = _sortObjType.GetMember(_properties[i],
                MemberTypes.Field | MemberTypes.Property,
                BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance);

            if (mi.Length > 0)
            {
                if (mi[0] is PropertyInfo pi)
                {
                    val1 = pi.GetValue(obj1, null) as IComparable;
                    val2 = pi.GetValue(obj2, null) as IComparable;
                }
                else if (mi[0] is FieldInfo fi)
                {
                    val1 = fi.GetValue(obj1) as IComparable;
                    val2 = fi.GetValue(obj2) as IComparable;
                }
            }

            if (val1 is null || val2 is null)
                throw new InvalidOperationException("A specified member could not be compared during searching/sorting. Make sure that their values are primitives or that they implement IComparable!");

            compareResult = val1.CompareTo(val2);
            if (compareResult != 0) break;
        }

        return compareResult;
    }

    private int CompareToSortableObject(IComparable? someObject, object sortableObject)
    {
        int compareResult = 0;

        for (int i = 0; i < _properties.Length; i++)
        {
            var sortableValue = _sortObjType.GetProperty(_properties[i])?.GetValue(sortableObject, null) as IComparable;

            if (sortableValue is null || someObject is null)
                throw new InvalidOperationException("A comparing item could not be compared during searching/sorting. Make sure that their values are primitives or that they implement IComparable!");

            compareResult = sortableValue.CompareTo(someObject);
            if (compareResult != 0) break;
        }

        return compareResult;
    }
}
