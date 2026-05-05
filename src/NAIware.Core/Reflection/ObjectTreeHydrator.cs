using System.Collections;
using System.Reflection;
using NAIware.Core.Collections;

namespace NAIware.Core.Reflection;

/// <summary>
/// Builds a reflected property tree for a root object instance or root type.
/// Intended for IntelliSense, validation, and parameter discovery.
/// </summary>
public static class ObjectTreeHydrator
{
    private const int DefaultMaxDepth = 8;

    /// <summary>
    /// Builds a reflected property tree for the specified object instance.
    /// </summary>
    /// <param name='instance'>The object instance to inspect.</param>
    /// <param name='instanceName'>The display name and root path for the instance.</param>
    /// <param name='maxDepth'>The maximum property depth to traverse.</param>
    /// <returns>A reflected property tree rooted at the specified instance.</returns>
    public static Tree<TreeNode, object> Create(
        object instance,
        string instanceName,
        int maxDepth = DefaultMaxDepth)
    {
        ArgumentNullException.ThrowIfNull(instance);

        return Create(instance.GetType(), instanceName, maxDepth);
    }

    /// <summary>
    /// Builds a reflected property tree for the specified root type.
    /// </summary>
    /// <param name='rootType'>The root type to inspect.</param>
    /// <param name='instanceName'>The display name and root path for the type.</param>
    /// <param name='maxDepth'>The maximum property depth to traverse.</param>
    /// <returns>A reflected property tree rooted at the specified type.</returns>
    public static Tree<TreeNode, object> Create(
        Type rootType,
        string instanceName,
        int maxDepth = DefaultMaxDepth)
    {
        ArgumentNullException.ThrowIfNull(rootType);

        if (string.IsNullOrWhiteSpace(instanceName))
        {
            throw new ArgumentException("Instance name is required.", nameof(instanceName));
        }

        var rootValue = new ReflectedPropertyNode(
            name: instanceName,
            path: instanceName,
            type: rootType,
            propertyInfo: null,
            isRoot: true,
            isCollection: false,
            isCollectionItem: false);

        var rootNode = new TreeNode(rootValue);
        var tree = new Tree<TreeNode, object>
        {
            Root = rootNode
        };

        var context = new TreeHydrationContext(maxDepth);
        context.Push(rootType);

        AppendProperties(rootNode, rootType, instanceName, context, depth: 0);

        context.Pop();

        return tree;
    }

    private static void AppendProperties(
        TreeNode parentNode,
        Type parentType,
        string parentPath,
        TreeHydrationContext context,
        int depth)
    {
        if (depth >= context.MaxDepth)
        {
            return;
        }

        foreach (var property in GetReadableProperties(parentType))
        {
            Type propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            bool isCollection = TryGetCollectionItemType(propertyType, out var itemType);

            string propertyPath = $"{parentPath}.{property.Name}";

            var propertyValue = new ReflectedPropertyNode(
                name: property.Name,
                path: propertyPath,
                type: propertyType,
                propertyInfo: property,
                isRoot: false,
                isCollection: isCollection,
                isCollectionItem: false);

            var propertyNode = new TreeNode(parentNode, propertyValue);
            parentNode.Nodes.Add(propertyNode);

            if (IsLeafType(propertyType))
            {
                continue;
            }

            if (isCollection && itemType is not null)
            {
                AppendCollectionItemNode(
                    propertyNode,
                    itemType,
                    $"{propertyPath}[0]",
                    context,
                    depth + 1);

                continue;
            }

            if (propertyType.IsAbstract || propertyType.IsInterface)
            {
                continue;
            }

            if (context.IsInCurrentPath(propertyType))
            {
                continue;
            }

            context.Push(propertyType);
            AppendProperties(propertyNode, propertyType, propertyPath, context, depth + 1);
            context.Pop();
        }
    }

    private static void AppendCollectionItemNode(
        TreeNode collectionNode,
        Type itemType,
        string itemPath,
        TreeHydrationContext context,
        int depth)
    {
        itemType = Nullable.GetUnderlyingType(itemType) ?? itemType;

        var itemValue = new ReflectedPropertyNode(
            name: "[0]",
            path: itemPath,
            type: itemType,
            propertyInfo: null,
            isRoot: false,
            isCollection: false,
            isCollectionItem: true);

        var itemNode = new TreeNode(collectionNode, itemValue);
        collectionNode.Nodes.Add(itemNode);

        if (IsLeafType(itemType) ||
            itemType.IsAbstract ||
            itemType.IsInterface ||
            context.IsInCurrentPath(itemType))
        {
            return;
        }

        context.Push(itemType);
        AppendProperties(itemNode, itemType, itemPath, context, depth + 1);
        context.Pop();
    }

    private static IEnumerable<PropertyInfo> GetReadableProperties(Type type)
    {
        return type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p =>
                p.CanRead &&
                p.GetIndexParameters().Length == 0);
    }

    private static bool TryGetCollectionItemType(Type type, out Type? itemType)
    {
        itemType = null;

        if (type == typeof(string))
        {
            return false;
        }

        if (type.IsArray)
        {
            itemType = type.GetElementType();
            return itemType is not null;
        }

        var enumerableType = type
            .GetInterfaces()
            .Concat([type])
            .FirstOrDefault(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableType is null)
        {
            return false;
        }

        itemType = enumerableType.GetGenericArguments()[0];
        return true;
    }

    private static bool IsLeafType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(Guid) ||
               type == typeof(TimeSpan);
    }

    private sealed class TreeHydrationContext
    {
        private readonly Stack<Type> _path = new();

        public TreeHydrationContext(int maxDepth)
        {
            MaxDepth = maxDepth;
        }

        public int MaxDepth { get; }

        public bool IsInCurrentPath(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return _path.Contains(type);
        }

        public void Push(Type type)
        {
            _path.Push(Nullable.GetUnderlyingType(type) ?? type);
        }

        public void Pop()
        {
            if (_path.Count > 0)
            {
                _path.Pop();
            }
        }
    }
}

/// <summary>
/// Represents one node in a reflected object/property tree.
/// The root node represents the named object instance. Child nodes represent properties.
/// </summary>
public sealed class ReflectedPropertyNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref='ReflectedPropertyNode'/> class.
    /// </summary>
    /// <param name='name'>The display name for the node.</param>
    /// <param name='path'>The full property path represented by the node.</param>
    /// <param name='type'>The reflected type represented by the node.</param>
    /// <param name='propertyInfo'>The reflected property information, or <see langword='null'/> for synthetic nodes.</param>
    /// <param name='isRoot'>A value indicating whether this node is the root node.</param>
    /// <param name='isCollection'>A value indicating whether this node represents a collection property.</param>
    /// <param name='isCollectionItem'>A value indicating whether this node represents a synthetic collection item.</param>
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

    /// <summary>
    /// Gets the display name for the node.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the full property path represented by the node.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the reflected type represented by the node.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Gets the reflected property information, or <see langword='null'/> for synthetic nodes.
    /// </summary>
    public PropertyInfo? PropertyInfo { get; }

    /// <summary>
    /// Gets a value indicating whether this node is the root node.
    /// </summary>
    public bool IsRoot { get; }

    /// <summary>
    /// Gets a value indicating whether this node represents a collection property.
    /// </summary>
    public bool IsCollection { get; }

    /// <summary>
    /// Gets a value indicating whether this node represents a synthetic collection item.
    /// </summary>
    public bool IsCollectionItem { get; }

    /// <summary>
    /// Returns a string representation of the reflected property node.
    /// </summary>
    /// <returns>The node path and reflected type name.</returns>
    public override string ToString()
    {
        return $"{Path} : {Type.Name}";
    }
}