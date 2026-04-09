namespace NAIware.Core.Collections;

/// <summary>
/// A generic tree structure containing a root node and computed metrics.
/// </summary>
/// <typeparam name="N">The node type.</typeparam>
/// <typeparam name="V">The value type stored in nodes.</typeparam>
public class Tree<N, V> : ITree<N, V> where N : ITreeNode<V>
{
    /// <summary>The root node of the tree.</summary>
    protected N _root = default!;

    /// <inheritdoc/>
    public N Root
    {
        get => _root;
        set => _root = value;
    }

    /// <inheritdoc/>
    public int Depth => _root is null ? 0 : _root.Depth;

    /// <inheritdoc/>
    public long Size
    {
        get
        {
            if (_root is null) return 0;

            long size = 1;
            ITreeNode<V> thisnode = _root;

            if (thisnode.Children is not null)
            {
                for (int i = 0, j = thisnode.Children.Count; i < j; i++)
                    size += thisnode.Children[i].Size;
            }

            return size;
        }
    }

    /// <inheritdoc/>
    public bool IsEmpty => _root is null;
}
