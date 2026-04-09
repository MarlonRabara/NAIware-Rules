namespace NAIware.Core.Collections;

/// <summary>
/// A binary tree node supporting left and right children with depth and size computation.
/// </summary>
/// <typeparam name="V">The type of value stored in the node.</typeparam>
public class BinaryTreeNode<V> : ITreeNode<V>
{
    /// <summary>The children of the binary tree node.</summary>
    protected List<ITreeNode<V>> _children = [];

    /// <summary>The index of the left child.</summary>
    protected int _leftpos = -1;

    /// <summary>The index of the right child.</summary>
    protected int _rightpos = -1;

    /// <summary>The value stored in this node.</summary>
    protected V _val;

    private BinaryTreeNode<V>? _parent;

    /// <summary>Creates a binary tree node with the specified value.</summary>
    public BinaryTreeNode(V value) { _val = value; }

    /// <summary>Creates a binary tree node with default value.</summary>
    public BinaryTreeNode() : this(default!) { }

    /// <inheritdoc/>
    public bool HasChildren => _children.Count > 0;

    /// <inheritdoc/>
    public bool HasParent => _parent is not null;

    /// <summary>Gets or sets the left child node.</summary>
    public virtual BinaryTreeNode<V>? LeftChild
    {
        get => _leftpos == -1 || _children.Count == 0 ? null : _children[_leftpos] as BinaryTreeNode<V>;
        set
        {
            if (value is null)
            {
                if (_leftpos != -1)
                {
                    _children.RemoveAt(_leftpos);
                    _leftpos = -1;
                    if (_rightpos != -1) _rightpos = 0;
                }
                return;
            }

            if (_leftpos == -1)
            {
                _children.Add(value);
                _leftpos = _children.IndexOf(value);
                _children[_leftpos].Parent = this;
            }
            else
            {
                _children[_leftpos] = value;
            }
        }
    }

    /// <summary>Gets or sets the right child node.</summary>
    public virtual BinaryTreeNode<V>? RightChild
    {
        get => _rightpos == -1 || _children.Count == 0 ? null : _children[_rightpos] as BinaryTreeNode<V>;
        set
        {
            if (value is null)
            {
                if (_rightpos != -1)
                {
                    _children.RemoveAt(_rightpos);
                    _rightpos = -1;
                    if (_leftpos != -1) _leftpos = 0;
                }
                return;
            }

            if (_rightpos == -1)
            {
                _children.Add(value);
                _rightpos = _children.IndexOf(value);
                _children[_rightpos].Parent = this;
            }
            else
            {
                _children[_rightpos] = value;
            }
        }
    }

    /// <summary>Gets or sets the parent node.</summary>
    public BinaryTreeNode<V>? Parent
    {
        get => _parent;
        set => _parent = value;
    }

    ITreeNode<V>? ITreeNode<V>.Parent
    {
        get => Parent;
        set => Parent = value as BinaryTreeNode<V>;
    }

    /// <inheritdoc/>
    public V Value
    {
        get => _val;
        set => _val = value;
    }

    List<ITreeNode<V>> ITreeNode<V>.Children => _children;

    /// <inheritdoc/>
    public int Depth
    {
        get
        {
            int maxchilddepth = 0;
            for (int i = 0, j = _children.Count; i < j; i++)
            {
                int currentdepth = _children[i].Depth;
                if (currentdepth > maxchilddepth)
                    maxchilddepth = currentdepth;
            }
            return 1 + maxchilddepth;
        }
    }

    /// <inheritdoc/>
    public long Size
    {
        get
        {
            long currentsize = 1;
            for (int i = 0, j = _children.Count; i < j; i++)
                currentsize += _children[i].Size;
            return currentsize;
        }
    }
}
