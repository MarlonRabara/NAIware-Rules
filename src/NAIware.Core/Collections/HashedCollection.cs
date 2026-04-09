using System.Collections;
using System.Runtime.Serialization;

namespace NAIware.Core.Collections;

/// <summary>
/// A class that represents a hashed collection where items are accessible by key or index.
/// </summary>
[Serializable]
public class HashedCollection : CollectionBase, ISerializable
{
    private Hashtable _keyIndexLookup;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashedCollection"/> class with case-insensitive keys.
    /// </summary>
    public HashedCollection() : this(false) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashedCollection"/> class.
    /// </summary>
    /// <param name="isCaseSensitive"><c>true</c> if keys will be case sensitive; otherwise <c>false</c>.</param>
    public HashedCollection(bool isCaseSensitive) : base()
    {
        _keyIndexLookup = isCaseSensitive
            ? new Hashtable()
            : new Hashtable(StringComparer.CurrentCultureIgnoreCase);
    }

#pragma warning disable SYSLIB0051 // Serialization infrastructure is obsolete
    /// <summary>
    /// Deserialization constructor.
    /// </summary>
    /// <param name="info">The serialization info.</param>
    /// <param name="context">The streaming context.</param>
    public HashedCollection(SerializationInfo info, StreamingContext context)
    {
        _keyIndexLookup = new Hashtable();
        if (info.GetValue("items", typeof(object[])) is object[] objs)
        {
            for (int i = 0; i < objs.Length; i++)
            {
                List.Add(objs[i]);
            }
        }
    }
#pragma warning restore SYSLIB0051

    /// <summary>
    /// Gets or sets the object with the specified key.
    /// </summary>
    public object? this[object key]
    {
        get
        {
            var indexval = _keyIndexLookup[key];
            if (indexval is null) return null;
            return List[(int)indexval];
        }
        set
        {
            var indexval = _keyIndexLookup[key];
            if (indexval is null) return;
            List[(int)indexval] = value;
        }
    }

    /// <summary>
    /// Gets or sets the object with the specified index.
    /// </summary>
    public object? this[int index]
    {
        get => List[index];
        set => List[index] = value;
    }

    /// <inheritdoc/>
    protected override void OnClear()
    {
        _keyIndexLookup.Clear();
        base.OnClear();
    }

    /// <summary>
    /// Adds an object to the collection keyed by the specified key.
    /// </summary>
    /// <param name="key">The key of the object to add.</param>
    /// <param name="value">The value to add.</param>
    public void Add(object key, object value)
    {
        int index = List.Add(value);
        _keyIndexLookup[key] = index;
    }

    /// <summary>
    /// Gets the indicator of whether the collection contains the specified key.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns><c>true</c> if the collection contains the key; otherwise <c>false</c>.</returns>
    public bool ContainsKey(object key) => _keyIndexLookup.ContainsKey(key);

    /// <inheritdoc/>
    protected override void OnRemove(int index, object? value)
    {
        base.OnRemove(index, value);

        foreach (object key in _keyIndexLookup.Keys)
        {
            if (((int)_keyIndexLookup[key]!).CompareTo(index) == 0)
            {
                _keyIndexLookup.Remove(key);
                break;
            }
        }
    }

#pragma warning disable SYSLIB0051 // Serialization infrastructure is obsolete
    /// <summary>
    /// Gets the object data and adds it to the serialization info.
    /// </summary>
    /// <param name="info">The serialization info object.</param>
    /// <param name="context">The streaming context.</param>
    public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        object[] objs = InnerList.ToArray();
        info.AddValue("items", objs);
    }
#pragma warning restore SYSLIB0051
}
