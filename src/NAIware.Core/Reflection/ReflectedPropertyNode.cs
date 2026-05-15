using System.Reflection;

namespace NAIware.Core.Reflection;

/// <summary>
/// Represents one node in a reflected object/property tree.
/// The root node represents the named object instance. Child nodes represent properties
/// or synthetic collection items.
/// </summary>
public sealed class ReflectedPropertyNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReflectedPropertyNode"/> class.
    /// Prefer the static factory methods <see cref="ForRoot"/>, <see cref="ForProperty"/>,
    /// and <see cref="ForCollectionItem"/> for readability.
    /// </summary>
    /// <param name="name">The display name for the node.</param>
    /// <param name="path">The full dotted property path represented by the node.</param>
    /// <param name="type">The reflected type represented by the node.</param>
    /// <param name="propertyInfo">The reflected property information, or <see langword="null"/> for synthetic nodes.</param>
    /// <param name="isRoot">A value indicating whether this node is the root node.</param>
    /// <param name="isCollection">A value indicating whether this node represents a collection property.</param>
    /// <param name="isCollectionItem">A value indicating whether this node represents a synthetic collection item.</param>
    public ReflectedPropertyNode(
        string name,
        string path,
        Type type,
        PropertyInfo? propertyInfo,
        bool isRoot,
        bool isCollection,
        bool isCollectionItem)
    {
        Name = name;
        Path = path;
        Type = type;
        PropertyInfo = propertyInfo;
        IsRoot = isRoot;
        IsCollection = isCollection;
        IsCollectionItem = isCollectionItem;
    }

    /// <summary>Gets the display name for the node.</summary>
    public string Name { get; }

    /// <summary>Gets the full dotted property path represented by the node.</summary>
    public string Path { get; }

    /// <summary>Gets the reflected type represented by the node.</summary>
    public Type Type { get; }

    /// <summary>
    /// Gets the reflected property information, or <see langword="null"/> for the root
    /// node and synthetic collection-item nodes.
    /// </summary>
    public PropertyInfo? PropertyInfo { get; }

    /// <summary>Gets a value indicating whether this node is the root node.</summary>
    public bool IsRoot { get; }

    /// <summary>Gets a value indicating whether this node represents a collection property.</summary>
    public bool IsCollection { get; }

    /// <summary>Gets a value indicating whether this node represents a synthetic collection item.</summary>
    public bool IsCollectionItem { get; }

    /// <summary>Creates a node representing the root of a hydrated tree.</summary>
    public static ReflectedPropertyNode ForRoot(string name, Type type) =>
        new(name, name, type, propertyInfo: null, isRoot: true, isCollection: false, isCollectionItem: false);

    /// <summary>Creates a node representing a reflected property.</summary>
    public static ReflectedPropertyNode ForProperty(string name, string path, Type type, PropertyInfo propertyInfo, bool isCollection) =>
        new(name, path, type, propertyInfo, isRoot: false, isCollection: isCollection, isCollectionItem: false);

    /// <summary>Creates a synthetic node representing a collection item.</summary>
    public static ReflectedPropertyNode ForCollectionItem(string name, string path, Type type) =>
        new(name, path, type, propertyInfo: null, isRoot: false, isCollection: false, isCollectionItem: true);

    /// <summary>Returns a string representation of the node.</summary>
    /// <returns>The node path and reflected type name.</returns>
    public override string ToString() => $"{Path} : {Type.Name}";
}
