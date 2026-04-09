namespace NAIware.Core.Collections;

/// <summary>
/// Defines a contract for a hierarchy node containing a value and optional children.
/// </summary>
/// <typeparam name="T">The type of value stored in the hierarchy node.</typeparam>
public interface IHierarchyNode<T>
{
    /// <summary>Gets or sets the value of the node.</summary>
    T Value { get; set; }

    /// <summary>Gets whether the node has child levels.</summary>
    bool HasChildren { get; }

    /// <summary>Gets the child level of this node.</summary>
    IHierarchyLevel<T> Children { get; }

    /// <summary>Gets the level that contains this node.</summary>
    IHierarchyLevel<T> Container { get; }
}

/// <summary>
/// Defines a contract for a hierarchy level containing nodes.
/// </summary>
/// <typeparam name="T">The type of value stored in the hierarchy nodes.</typeparam>
public interface IHierarchyLevel<T>
{
    /// <summary>Gets the collection of nodes at this level.</summary>
    List<IHierarchyNode<T>> Nodes { get; }

    /// <summary>Gets the parent node of this level.</summary>
    IHierarchyNode<T>? Parent { get; }
}
