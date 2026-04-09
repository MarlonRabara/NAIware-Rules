namespace NAIware.Core.Collections;

/// <summary>
/// An abstract class that represents a sortable collection backed by an inner array list.
/// </summary>
/// <remarks>
/// Intended to be abstract so that consuming developers implement their own
/// update and indexer methods for specific collections.
/// </remarks>
public abstract class SortableCollection : CollectionBase
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
    /// Reverses the order of items in the collection.
    /// </summary>
    public void Reverse()
    {
        InnerList.Reverse();
    }

    /// <summary>
    /// Sorts a sortable collection by the specified properties.
    /// </summary>
    /// <param name="propertyNames">The properties to sort by.</param>
    public void Sort(params string[] propertyNames)
    {
        if (propertyNames is null || propertyNames.Length < 1)
            throw new InvalidOperationException("To sort a sortable collection, you must specify at least one property.");

        if (InnerList.Count > 0)
            InnerList.Sort(new ReflectionComparer(InnerList[0]!.GetType(), propertyNames));

        _sortedProperties = propertyNames;
        _sorted = true;
    }

    /// <summary>
    /// Performs a binary search on the collection.
    /// </summary>
    /// <param name="match">The match object to search for.</param>
    /// <returns>The match index if found; otherwise the bitwise complement of the nearest match.</returns>
    public int BinarySearch(object match)
    {
        if (!Sorted)
            throw new InvalidOperationException("The collection must be sorted before a binary search is called!");

        if (InnerList is null || InnerList.Count == 0) return -1;

        return InnerList.BinarySearch(match, new ReflectionComparer(InnerList[0]!.GetType(), _sortedProperties!));
    }
}
