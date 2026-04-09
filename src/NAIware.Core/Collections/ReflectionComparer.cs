namespace NAIware.Core.Collections;

/// <summary>
/// A comparer that uses reflection to compare objects by specified member names (properties or fields).
/// </summary>
public class ReflectionComparer : System.Collections.IComparer
{
    private readonly Type _reflectedType;
    private readonly string[] _reflectedItems;
    private readonly bool _isProperties;

    /// <summary>
    /// Creates a new comparer instance from a type and property names.
    /// </summary>
    /// <param name="reflectedType">The type of reflection object being compared.</param>
    /// <param name="reflectedItems">An array of item names being reflected.</param>
    public ReflectionComparer(Type reflectedType, string[] reflectedItems)
        : this(reflectedType, reflectedItems, true) { }

    /// <summary>
    /// Creates a new comparer instance from a type and member names.
    /// </summary>
    /// <param name="reflectedType">The type of reflection object being compared.</param>
    /// <param name="reflectedItems">An array of item names being reflected.</param>
    /// <param name="isProperties"><c>true</c> if the items are properties; <c>false</c> if fields.</param>
    public ReflectionComparer(Type reflectedType, string[] reflectedItems, bool isProperties)
    {
        _reflectedType = reflectedType;
        _reflectedItems = reflectedItems;
        _isProperties = isProperties;
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

        Type type1 = obj1.GetType();
        Type type2 = obj2.GetType();

        if (type1.Name == _reflectedType.Name && type2.Name == _reflectedType.Name)
            return CompareSortableObjects(obj1, obj2);

        if (type1.Name == _reflectedType.Name && type2.Name != _reflectedType.Name)
            return CompareToReflectedObject(obj2 as IComparable, obj1);

        if (type1.Name != _reflectedType.Name && type2.Name == _reflectedType.Name)
            return CompareToReflectedObject(obj1 as IComparable, obj2);

        if (obj1 is IComparable compare1 && obj2 is IComparable)
            return compare1.CompareTo(obj2);

        if (obj1 is not IComparable && obj2 is not IComparable) return 0;
        if (obj1 is not IComparable) return -1;
        return 1;
    }

    private int CompareSortableObjects(object obj1, object obj2)
    {
        int compareResult = 0;

        for (int i = 0; i < _reflectedItems.Length; i++)
        {
            IComparable? val1, val2;

            if (_isProperties)
            {
                val1 = _reflectedType.GetProperty(_reflectedItems[i])?.GetValue(obj1, null) as IComparable;
                val2 = _reflectedType.GetProperty(_reflectedItems[i])?.GetValue(obj2, null) as IComparable;
            }
            else
            {
                val1 = _reflectedType.GetField(_reflectedItems[i])?.GetValue(obj1) as IComparable;
                val2 = _reflectedType.GetField(_reflectedItems[i])?.GetValue(obj2) as IComparable;
            }

            if (val1 is null || val2 is null)
                throw new InvalidOperationException("A specified member could not be compared during searching/sorting. Make sure that their values are primitives or that they implement IComparable!");

            compareResult = val1.CompareTo(val2);
            if (compareResult != 0) break;
        }

        return compareResult;
    }

    private int CompareToReflectedObject(IComparable? someObject, object reflectedObject)
    {
        int compareResult = 0;

        for (int i = 0; i < _reflectedItems.Length; i++)
        {
            IComparable? reflectedVal;

            if (_isProperties)
                reflectedVal = _reflectedType.GetProperty(_reflectedItems[i])?.GetValue(reflectedObject, null) as IComparable;
            else
                reflectedVal = _reflectedType.GetField(_reflectedItems[i])?.GetValue(reflectedObject) as IComparable;

            if (reflectedVal is null || someObject is null)
                throw new InvalidOperationException("A comparing item could not be compared during searching/sorting. Make sure that their values are primitives or that they implement IComparable!");

            compareResult = reflectedVal.CompareTo(someObject);
            if (compareResult != 0) break;
        }

        return compareResult;
    }
}
