using NAIware.Core.Collections;
using System.Reflection;

namespace NAIware.Core.Reflection;

/// <summary>
/// A class that represents a collection of <see cref="ReflectedPropertyTreeNode"/> instances.
/// </summary>
public class ReflectedPropertyTreeNodes : List<ReflectedPropertyTreeNode>
{
    /// <summary>The parent tree node.</summary>
    protected ITreeNode<PropertyInfo>? _parent;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReflectedPropertyTreeNodes"/> class.
    /// </summary>
    public ReflectedPropertyTreeNodes() : this(null, -1) { }

    /// <summary>
    /// Initializes a new instance with a specified maximum capacity.
    /// </summary>
    public ReflectedPropertyTreeNodes(int maxCapacity) : this(null, maxCapacity) { }

    /// <summary>
    /// Initializes a new instance for a specified parent.
    /// </summary>
    public ReflectedPropertyTreeNodes(ITreeNode<PropertyInfo>? parent) : this(parent, -1) { }

    /// <summary>
    /// Initializes a new instance for a specified parent with a maximum capacity.
    /// </summary>
    public ReflectedPropertyTreeNodes(ITreeNode<PropertyInfo>? parent, int maxCapacity)
    {
        if (maxCapacity != -1)
            Capacity = maxCapacity;
        _parent = parent;
    }

    /// <summary>
    /// Gets or sets the parent of the current tree node instance.
    /// </summary>
    public ITreeNode<PropertyInfo>? Parent
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
    /// Adds the elements of a <see cref="ReflectedPropertyTreeNodes"/> collection to the end of this collection.
    /// </summary>
    /// <param name="treeNodes">A collection of tree nodes to add.</param>
    public void AddRange(ReflectedPropertyTreeNodes treeNodes)
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
