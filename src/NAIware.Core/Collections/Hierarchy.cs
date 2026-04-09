namespace NAIware.Core.Collections;

/// <summary>
/// Represents a hierarchy level containing nodes, each of which can have their own child levels.
/// </summary>
/// <typeparam name="T">The type of value stored in the hierarchy.</typeparam>
public class Hierarchy<T> : IHierarchyLevel<T>
{
    private readonly List<IHierarchyNode<T>> _nodes = [];

    /// <inheritdoc/>
    public List<IHierarchyNode<T>> Nodes => _nodes;

    /// <inheritdoc/>
    public IHierarchyNode<T>? Parent => null;

    /// <summary>Adds a node with the specified value to this level.</summary>
    public HierarchyNode<T> Add(T value)
    {
        var node = new HierarchyNode<T>(value, this);
        _nodes.Add(node);
        return node;
    }
}

/// <summary>
/// Represents a node in a hierarchy containing a value and optional child levels.
/// </summary>
/// <typeparam name="T">The type of value stored in the hierarchy node.</typeparam>
public class HierarchyNode<T> : IHierarchyNode<T>
{
    private HierarchyLevel<T>? _children;

    /// <summary>Creates a hierarchy node with the given value in the specified container.</summary>
    public HierarchyNode(T value, IHierarchyLevel<T> container)
    {
        Value = value;
        Container = container;
    }

    /// <inheritdoc/>
    public T Value { get; set; }

    /// <inheritdoc/>
    public bool HasChildren => _children is not null && _children.Nodes.Count > 0;

    /// <inheritdoc/>
    public IHierarchyLevel<T> Children
    {
        get
        {
            _children ??= new HierarchyLevel<T>(this);
            return _children;
        }
    }

    /// <inheritdoc/>
    public IHierarchyLevel<T> Container { get; }
}

/// <summary>
/// Represents a child level in a hierarchy, belonging to a parent node.
/// </summary>
/// <typeparam name="T">The type of value stored in the hierarchy.</typeparam>
public class HierarchyLevel<T> : IHierarchyLevel<T>
{
    private readonly List<IHierarchyNode<T>> _nodes = [];

    /// <summary>Creates a hierarchy level belonging to the given parent node.</summary>
    public HierarchyLevel(IHierarchyNode<T> parent)
    {
        Parent = parent;
    }

    /// <inheritdoc/>
    public List<IHierarchyNode<T>> Nodes => _nodes;

    /// <inheritdoc/>
    public IHierarchyNode<T>? Parent { get; }

    /// <summary>Adds a node with the specified value to this level.</summary>
    public HierarchyNode<T> Add(T value)
    {
        var node = new HierarchyNode<T>(value, this);
        _nodes.Add(node);
        return node;
    }
}
