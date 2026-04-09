namespace NAIware.Core.Collections;

/// <summary>
/// Defines a contract for a tree structure containing nodes.
/// </summary>
/// <typeparam name="N">The node type.</typeparam>
/// <typeparam name="V">The value type stored in nodes.</typeparam>
public interface ITree<N, V> where N : ITreeNode<V>
{
    /// <summary>Gets or sets the root node.</summary>
    N Root { get; set; }

    /// <summary>Gets the total size of the tree.</summary>
    long Size { get; }

    /// <summary>Gets whether the tree is empty.</summary>
    bool IsEmpty { get; }

    /// <summary>Gets the depth of the tree.</summary>
    int Depth { get; }
}
