namespace NAIware.Core.Collections;

/// <summary>
/// A class that represents a collection of <see cref="TreeNode"/> instances.
/// </summary>
public class TreeNodes : List<TreeNode>
{
    /// <summary>The parent tree node.</summary>
    protected ITreeNode<object>? _parent;

    /// <summary>
    /// Initializes a new instance of the <see cref="TreeNodes"/> class.
    /// </summary>
    public TreeNodes() : this(null, -1) { }

    /// <summary>
    /// Initializes a new instance with a specified maximum capacity.
    /// </summary>
    public TreeNodes(int maxCapacity) : this(null, maxCapacity) { }

    /// <summary>
    /// Initializes a new instance for a specified parent.
    /// </summary>
    public TreeNodes(ITreeNode<object>? parent) : this(parent, -1) { }

    /// <summary>
    /// Initializes a new instance for a specified parent with a maximum capacity.
    /// </summary>
    public TreeNodes(ITreeNode<object>? parent, int maxCapacity)
    {
        if (maxCapacity != -1)
            Capacity = maxCapacity;
        _parent = parent;
    }

    /// <summary>
    /// Gets or sets the parent of the current tree node instance.
    /// </summary>
    public ITreeNode<object>? Parent
    {
        get => _parent;
        set
        {
            _parent = value;

            if (Count > 0)
            {
                for (int i = 0; i < Count; i++)
                {
                    this[i].Parent = value;
                }
            }
        }
    }

    /// <summary>
    /// Adds the elements of a <see cref="TreeNodes"/> collection to the end of this collection.
    /// </summary>
    /// <param name="treeNodes">A collection of tree nodes to add.</param>
    public void AddRange(TreeNodes treeNodes)
    {
        if (treeNodes is null || treeNodes.Count == 0) return;
        treeNodes.Parent = _parent;
        base.AddRange(treeNodes);
    }

    /// <summary>
    /// Gets a flat list of the objects in the current nodes collection and its children.
    /// </summary>
    /// <returns>A list of objects.</returns>
    public List<object?> GetList()
    {
        if (Count == 0) return [];

        var newList = new List<object?>();

        for (int i = 0; i < Count; i++)
        {
            newList.Add(this[i].Value);
            var childList = this[i].Nodes.GetList();
            if (childList.Count > 0)
            {
                newList.AddRange(childList);
            }
        }

        return newList;
    }
}
