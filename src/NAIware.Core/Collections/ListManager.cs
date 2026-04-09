using System.Reflection;

namespace NAIware.Core.Collections;

/// <summary>
/// A class that manages and sorts a <see cref="List{T}"/> by specified property names using reflection.
/// </summary>
/// <typeparam name="T">The type of items in the managed list.</typeparam>
[Serializable]
public class ListManager<T>
{
    private readonly List<T> _managedList;
    private readonly Dictionary<string, PropertyInfo> _propertyLookup = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ListManager{T}"/> class.
    /// </summary>
    /// <param name="listToManage">The list to manage.</param>
    public ListManager(List<T> listToManage)
    {
        ArgumentNullException.ThrowIfNull(listToManage);
        _managedList = listToManage;

        foreach (var p in typeof(T).GetProperties())
        {
            _propertyLookup.Add(p.Name, p);
        }
    }

    /// <summary>
    /// Sorts the managed list by the specified property names in ascending order.
    /// </summary>
    /// <param name="propertiesToSortBy">The property names to sort by.</param>
    public void Sort(params string[] propertiesToSortBy) =>
        Sort(true, propertiesToSortBy);

    /// <summary>
    /// Sorts the managed list by the specified property names.
    /// </summary>
    /// <param name="ascending"><c>true</c> for ascending sort; <c>false</c> for descending.</param>
    /// <param name="propertiesToSortBy">The property names to sort by.</param>
    public void Sort(bool ascending, params string[] propertiesToSortBy)
    {
        if (_managedList.Count < 2) return;

        var genericComparer = new GenericListComparer<T>(_propertyLookup, ascending, propertiesToSortBy);
        _managedList.Sort(genericComparer);
    }
}
