namespace NAIware.Core.Collections.Generic;

/// <summary>
/// A dictionary that also maintains insertion order via an internal list,
/// allowing access by both key and integer index.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
public class OrderedDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly List<TValue> _innerList = [];

    /// <summary>
    /// Adds a new item to the ordered dictionary by key-value pair.
    /// </summary>
    public new virtual void Add(TKey key, TValue value)
    {
        base.Add(key, value);
        _innerList.Add(value);
    }

    /// <summary>
    /// Removes the item from the collection by key.
    /// </summary>
    /// <returns><c>true</c> if successful; otherwise <c>false</c>.</returns>
    public new virtual bool Remove(TKey key)
    {
        if (ContainsKey(key))
        {
            _innerList.Remove(this[key]);
        }

        return base.Remove(key);
    }

    /// <summary>
    /// Gets the value at the specified index position.
    /// </summary>
    /// <param name="index">The position to get the value for.</param>
    /// <returns>The value at the specified index position.</returns>
    public TValue GetByIndex(int index) => _innerList[index];

    /// <summary>
    /// Gets the index of the specified value.
    /// </summary>
    /// <param name="item">The item to get an index for.</param>
    /// <returns>The index position of the value, or -1 if not found.</returns>
    public int IndexOf(TValue item) => _innerList.IndexOf(item);

    /// <summary>
    /// Gets the number of items in the ordered list.
    /// </summary>
    public int OrderedCount => _innerList.Count;
}
