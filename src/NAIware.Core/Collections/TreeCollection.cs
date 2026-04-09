namespace NAIware.Core.Collections;

/// <summary>
/// A class that represents a collection data structure that is tree based.
/// </summary>
public class TreeCollection : TreeNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TreeCollection"/> class.
    /// </summary>
    public TreeCollection() : base() { }

    /// <summary>
    /// Gets a flat list of objects from the tree collection.
    /// </summary>
    /// <returns>A list of items.</returns>
    public List<object?> GetList()
    {
        if (_nodes is not null)
            return _nodes.GetList();
        else
            return [];
    }
}
