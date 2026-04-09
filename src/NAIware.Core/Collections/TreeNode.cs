namespace NAIware.Core.Collections;

/// <summary>
/// A class that represents a tree node.
/// </summary>
public class TreeNode : ITreeNode<object>
{
    protected object? _value;
    protected TreeNodes _nodes;
    protected ITreeNode<object>? _parent;

    /// <summary>
    /// Initializes a new instance of the <see cref="TreeNode"/> class.
    /// </summary>
    public TreeNode() : this(null, null, null) { }

    /// <summary>
    /// Initializes a new instance with a parent and value.
    /// </summary>
    public TreeNode(ITreeNode<object>? parent, object? value) : this(parent, value, null) { }

    /// <summary>
    /// Initializes a new instance with a parent.
    /// </summary>
    public TreeNode(ITreeNode<object>? parent) : this(parent, null, null) { }

    /// <summary>
    /// Initializes a new instance with a value.
    /// </summary>
    public TreeNode(object? value) : this(null, value, null) { }

    /// <summary>
    /// Initializes a new instance with a parent, value, and children.
    /// </summary>
    public TreeNode(ITreeNode<object>? parent, object? value, TreeNodes? children)
    {
        _parent = parent;
        _value = value;

        if (children is not null)
        {
            _nodes = children;
            _nodes.Parent = this;
        }
        else
        {
            _nodes = new TreeNodes(this);
        }
    }

    /// <summary>
    /// Gets or sets the value stored in the node.
    /// </summary>
    public object? Value
    {
        get => _value;
        set => _value = value;
    }

    /// <summary>
    /// Gets the collection of child nodes.
    /// </summary>
    public TreeNodes Nodes => _nodes;

    /// <summary>
    /// Gets or sets the parent of the current node.
    /// </summary>
    public ITreeNode<object>? Parent
    {
        get => _parent;
        set => _parent = value;
    }

    ITreeNode<object>? ITreeNode<object>.Parent
    {
        get => _parent;
        set => _parent = value;
    }

    bool ITreeNode<object>.HasParent => _parent is not null;

    /// <summary>
    /// Gets whether this node has children.
    /// </summary>
    public bool HasChildren => _nodes is not null && _nodes.Count > 0;

    List<ITreeNode<object>> ITreeNode<object>.Children
    {
        get
        {
            var children = new List<ITreeNode<object>>();
            foreach (TreeNode node in _nodes)
                children.Add(node);
            return children;
        }
    }

    int ITreeNode<object>.Depth
    {
        get
        {
            if (!HasChildren) return 0;

            int largestDepth = -1;
            foreach (ITreeNode<object> node in _nodes)
            {
                int currentDepth = node.Depth;
                if (currentDepth > largestDepth) largestDepth = currentDepth;
            }

            return largestDepth;
        }
    }

    /// <summary>
    /// Gets the total number of nodes in this subtree (including self).
    /// </summary>
    public long Size
    {
        get
        {
            long totalSize = 1;
            foreach (TreeNode node in _nodes)
            {
                totalSize += node.Size;
            }
            return totalSize;
        }
    }
}
