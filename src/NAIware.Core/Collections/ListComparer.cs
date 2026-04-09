using System.Reflection;

namespace NAIware.Core.Collections;

/// <summary>
/// A generic list comparer that compares objects by specified properties using reflection.
/// </summary>
/// <typeparam name="T">The type of objects to compare.</typeparam>
internal sealed class GenericListComparer<T>(
    Dictionary<string, PropertyInfo> propertyLookup,
    bool ascending,
    params string[] propertiesToCompare)
    : Comparer<T>, System.Collections.IComparer
{
    private int LTReturn => ascending ? -1 : 1;
    private int GTReturn => ascending ? 1 : -1;

    int System.Collections.IComparer.Compare(object? x, object? y)
    {
        if (x is null && y is null) throw new NullReferenceException("Null objects cannot be compared in a generic list.");
        if (x is null) return LTReturn;
        if (y is null) return GTReturn;
        if (ReferenceEquals(x, y)) return 0;

        return Compare((T)x, (T)y);
    }

    public override int Compare(T? x, T? y)
    {
        if (x is null && y is null) return 0;
        if (x is null) return LTReturn;
        if (y is null) return GTReturn;

        int compareResult = 0;

        for (int i = 0; i < propertiesToCompare.Length; i++)
        {
            var propertyInfo = propertyLookup[propertiesToCompare[i]];
            var xVal = propertyInfo.GetValue(x, null);
            var yVal = propertyInfo.GetValue(y, null);

            if (xVal is null && yVal is null)
            {
                compareResult = 0;
            }
            else if (xVal is null)
            {
                compareResult = LTReturn;
            }
            else if (yVal is null)
            {
                compareResult = GTReturn;
            }
            else if (xVal is IComparable xComparable)
            {
                compareResult = xComparable.CompareTo(yVal);
                if (compareResult != 0 && !ascending) compareResult *= -1;
            }

            if (compareResult != 0) return compareResult;
        }

        return compareResult;
    }
}
