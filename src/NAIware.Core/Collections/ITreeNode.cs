namespace NAIware.Core.Collections;

/// <summary>
/// Defines a contract for a tree node containing a value and child relationships.
/// </summary>
/// <typeparam name="V">The type of value stored in the tree node.</typeparam>
public interface ITreeNode<V>
{
    /// <summary>Gets or sets the value of the tree node.</summary>
    V Value { get; set; }

    /// <summary>Gets or sets the parent node.</summary>
    ITreeNode<V>? Parent { get; set; }

    /// <summary>Gets the child nodes.</summary>
    List<ITreeNode<V>> Children { get; }

    /// <summary>Gets whether this node has a parent.</summary>
    bool HasParent { get; }

    /// <summary>Gets whether this node has children.</summary>
    bool HasChildren { get; }

    /// <summary>Gets the depth of the subtree rooted at this node.</summary>
    int Depth { get; }

    /// <summary>Gets the total size of the subtree rooted at this node.</summary>
    long Size { get; }
}
