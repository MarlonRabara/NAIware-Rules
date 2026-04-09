using System.Reflection;

namespace NAIware.Core.Collections;

/// <summary>
/// Determines the type of reflection comparisons executed.
/// </summary>
public enum ReflectionComparisons
{
    /// <summary>Compare both fields and properties.</summary>
    All,
    /// <summary>Compare using fields only.</summary>
    Fields,
    /// <summary>Compare using properties only.</summary>
    Properties
}

/// <summary>
/// A deep content comparer using reflection for extracting and comparing object internals.
/// </summary>
public class Comparer : System.Collections.IComparer
{
    private readonly FieldInfo[] _farray;
    private readonly PropertyInfo[] _parray;
    private readonly ReflectionComparisons _comparemethods;
    private readonly string[]? _excMembers;

    /// <summary>Creates a comparer for the specified type, comparing all members.</summary>
    public Comparer(Type objectType) : this(objectType, ReflectionComparisons.All) { }

    /// <summary>Creates a comparer for the specified type with the given comparison method.</summary>
    public Comparer(Type objectType, ReflectionComparisons comparisonMethods)
    {
        _farray = objectType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        _parray = objectType.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        _comparemethods = comparisonMethods;
    }

    /// <summary>Creates a comparer with excluded members.</summary>
    public Comparer(Type objectType, ReflectionComparisons comparisonMethods, params string[]? excludedMembers)
    {
        _comparemethods = comparisonMethods;
        _excMembers = excludedMembers;

        var farray = new List<FieldInfo>(objectType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public));
        var parray = new List<PropertyInfo>(objectType.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public));

        if (excludedMembers is { Length: > 0 })
        {
            var excSet = new HashSet<string>(excludedMembers);
            farray.RemoveAll(fi => excSet.Contains(fi.Name) || excSet.Contains($"{objectType.FullName}.{fi.Name}"));
            parray.RemoveAll(pi => excSet.Contains(pi.Name) || excSet.Contains($"{objectType.FullName}.{pi.Name}"));
        }

        _farray = [.. farray];
        _parray = [.. parray];
    }

    /// <inheritdoc/>
    public int Compare(object? x, object? y)
    {
        var alreadyCompared = new System.Collections.Hashtable();
        return InternalCompare(x, y, alreadyCompared);
    }

    private int InternalCompare(object? x, object? y, System.Collections.Hashtable alreadyCompared)
    {
        if (x is null && y is null) return 0;
        if (x is null) return -1;
        if (y is null) return 1;
        if (y.Equals(x)) return 0;
        if (x.GetType() != y.GetType()) return string.Compare(x.ToString(), y.ToString(), StringComparison.Ordinal);

        if (_comparemethods is ReflectionComparisons.All or ReflectionComparisons.Fields)
        {
            for (int index = 0; index < _farray.Length; index++)
            {
                object? xobj = _farray[index].GetValue(x);
                object? yobj = _farray[index].GetValue(y);

                if (xobj is IComparable cx)
                {
                    int comparetest = cx.CompareTo(yobj);
                    if (comparetest != 0) return comparetest;
                    continue;
                }

                if (xobj is not null && yobj is not null)
                {
                    if (alreadyCompared[xobj] == yobj || alreadyCompared[yobj] == xobj) continue;
                    alreadyCompared.Add(xobj, yobj);
                }

                var newcomp = new Comparer(_farray[index].FieldType, _comparemethods, _excMembers);
                int result = newcomp.InternalCompare(xobj, yobj, alreadyCompared);
                if (result != 0) return result;
            }
        }

        if (_comparemethods is ReflectionComparisons.All or ReflectionComparisons.Properties)
        {
            for (int index = 0; index < _parray.Length; index++)
            {
                if (_parray[index].GetIndexParameters().Length > 0) continue;
                if (!_parray[index].CanRead) continue;

                object? xobj = _parray[index].GetValue(x, null);
                object? yobj = _parray[index].GetValue(y, null);

                if (xobj is IComparable cx)
                {
                    int comparetest = cx.CompareTo(yobj);
                    if (comparetest != 0) return comparetest;
                    continue;
                }

                if (xobj is not null && yobj is not null)
                {
                    if (alreadyCompared[xobj] == yobj || alreadyCompared[yobj] == xobj) continue;
                    alreadyCompared.Add(xobj, yobj);
                }

                var newcomp = new Comparer(_parray[index].PropertyType, _comparemethods, _excMembers);
                int result = newcomp.InternalCompare(xobj, yobj, alreadyCompared);
                if (result != 0) return result;
            }
        }

        return 0;
    }
}
