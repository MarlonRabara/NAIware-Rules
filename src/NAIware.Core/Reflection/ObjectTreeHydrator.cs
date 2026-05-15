using System.Collections;
using System.Reflection;
using NAIware.Core.Collections;

namespace NAIware.Core.Reflection;

/// <summary>
/// Builds a reflected property tree for a root object instance or root type.
/// Intended for IntelliSense, validation, and parameter discovery.
/// </summary>
/// <remarks>
/// <para>
/// The returned tree is strongly typed: every node value is a
/// <see cref="ReflectedPropertyNode"/>. The root node carries <see cref="ReflectedPropertyNode.IsRoot"/>
/// set to <see langword="true"/> and its <see cref="ReflectedPropertyNode.Name"/> / <see cref="ReflectedPropertyNode.Path"/>
/// are taken from the supplied instance name.
/// </para>
/// <para>
/// Cycles are detected on a path-scoped basis: revisiting the same type within an
/// ancestor chain is skipped. Sibling branches may still traverse the same type.
/// </para>
/// </remarks>
public static class ObjectTreeHydrator
{
    /// <summary>
    /// Builds a reflected property tree for the specified object instance.
    /// </summary>
    /// <param name="rootObject">The object instance to inspect.</param>
    /// <param name="instanceName">The display name and root path for the instance.</param>
    /// <param name="maxDepth">The maximum property depth to traverse. Must be at least one.</param>
    /// <returns>A strongly typed reflected property tree rooted at the specified instance.</returns>
    public static Tree<PropertyTreeNode, ReflectedPropertyNode> Create(
        object rootObject,
        string instanceName,
        int maxDepth = ObjectTreeHydratorOptions.DefaultMaxDepth)
    {
        ArgumentNullException.ThrowIfNull(rootObject);
        return Create(rootObject.GetType(), rootObject, instanceName, new ObjectTreeHydratorOptions { MaxDepth = maxDepth });
    }

    /// <summary>
    /// Builds a reflected property tree for the specified root type and optional instance.
    /// </summary>
    /// <param name="rootType">The root type to inspect.</param>
    /// <param name="rootObject">The optional root object instance. When supplied it must be assignable to <paramref name="rootType"/>.</param>
    /// <param name="instanceName">The display name and root path for the type.</param>
    /// <param name="maxDepth">The maximum property depth to traverse. Must be at least one.</param>
    /// <returns>A strongly typed reflected property tree rooted at the specified type.</returns>
    public static Tree<PropertyTreeNode, ReflectedPropertyNode> Create(
        Type rootType,
        object? rootObject,
        string instanceName,
        int maxDepth = ObjectTreeHydratorOptions.DefaultMaxDepth)
        => Create(rootType, rootObject, instanceName, new ObjectTreeHydratorOptions { MaxDepth = maxDepth });

    /// <summary>
    /// Builds a reflected property tree using the supplied options.
    /// </summary>
    /// <param name="rootType">The root type to inspect.</param>
    /// <param name="rootObject">The optional root object instance. When supplied it must be assignable to <paramref name="rootType"/>.</param>
    /// <param name="instanceName">The display name and root path for the type.</param>
    /// <param name="options">The hydrator options.</param>
    /// <returns>A strongly typed reflected property tree rooted at the specified type.</returns>
    public static Tree<PropertyTreeNode, ReflectedPropertyNode> Create(
        Type rootType,
        object? rootObject,
        string instanceName,
        ObjectTreeHydratorOptions options)
    {
        ArgumentNullException.ThrowIfNull(rootType);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(instanceName))
        {
            throw new ArgumentException("Instance name is required.", nameof(instanceName));
        }

        if (options.MaxDepth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.MaxDepth, "MaxDepth must be at least 1.");
        }

        if (options.MaxNodes < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.MaxNodes, "MaxNodes must be at least 1.");
        }

        if (rootObject is not null && !rootType.IsInstanceOfType(rootObject))
        {
            throw new ArgumentException(
                $"rootObject of type '{rootObject.GetType().FullName}' is not assignable to rootType '{rootType.FullName}'.",
                nameof(rootObject));
        }

        var rootValue = ReflectedPropertyNode.ForRoot(instanceName, rootType);
        var rootNode = new PropertyTreeNode(rootValue);

        var tree = new Tree<PropertyTreeNode, ReflectedPropertyNode>
        {
            Root = rootNode
        };

        var context = new TreeHydrationContext(options);
        context.IncrementNodeCount();

        using (context.Enter(rootType))
        {
            AppendProperties(rootNode, rootType, instanceName, context, depth: 0);
        }

        return tree;
    }

    private static void AppendProperties(
        PropertyTreeNode parentNode,
        Type parentType,
        string parentPath,
        TreeHydrationContext context,
        int depth)
    {
        if (depth >= context.Options.MaxDepth || context.IsBudgetExhausted)
        {
            return;
        }

        foreach (var property in GetReadableProperties(parentType, context.Options))
        {
            if (context.IsBudgetExhausted)
            {
                return;
            }

            Type propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            bool isCollection = TryGetCollectionItemType(propertyType, out Type? itemType);

            string propertyPath = $"{parentPath}.{property.Name}";

            var propertyValue = ReflectedPropertyNode.ForProperty(
                name: property.Name,
                path: propertyPath,
                type: propertyType,
                propertyInfo: property,
                isCollection: isCollection);

            var propertyNode = new PropertyTreeNode(propertyValue);
            parentNode.AddChild(propertyNode);
            context.IncrementNodeCount();

            if (IsLeafType(propertyType, context.Options))
            {
                continue;
            }

            if (isCollection && itemType is not null)
            {
                Type normalizedItemType = Nullable.GetUnderlyingType(itemType) ?? itemType;

                // Skip synthetic child for collections of leaf types — they add noise
                // without surfacing additional structure.
                if (IsLeafType(normalizedItemType, context.Options))
                {
                    continue;
                }

                AppendCollectionItemNode(
                    propertyNode,
                    normalizedItemType,
                    propertyPath,
                    context,
                    depth + 1);

                continue;
            }

            if (propertyType.IsAbstract || propertyType.IsInterface)
            {
                // Interface/abstract property types are recorded but not recursed:
                // we cannot know which concrete implementation a consumer will supply.
                continue;
            }

            if (context.IsInCurrentPath(propertyType))
            {
                continue;
            }

            using (context.Enter(propertyType))
            {
                AppendProperties(propertyNode, propertyType, propertyPath, context, depth + 1);
            }
        }
    }

    private static void AppendCollectionItemNode(
        PropertyTreeNode collectionNode,
        Type itemType,
        string collectionPath,
        TreeHydrationContext context,
        int depth)
    {
        string itemSegment = context.Options.CollectionItemSegment;
        string itemPath = $"{collectionPath}{itemSegment}";

        var itemValue = ReflectedPropertyNode.ForCollectionItem(itemSegment, itemPath, itemType);
        var itemNode = new PropertyTreeNode(itemValue);
        collectionNode.AddChild(itemNode);
        context.IncrementNodeCount();

        if (itemType.IsAbstract ||
            itemType.IsInterface ||
            context.IsInCurrentPath(itemType))
        {
            return;
        }

        using (context.Enter(itemType))
        {
            AppendProperties(itemNode, itemType, itemPath, context, depth);
        }
    }

    private static IEnumerable<PropertyInfo> GetReadableProperties(Type type, ObjectTreeHydratorOptions options)
    {
        // Walk the type hierarchy explicitly so that we can de-duplicate properties
        // hidden via the `new` keyword on a derived type (most-derived wins).
        var seen = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

        for (Type? current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            foreach (var property in current.GetProperties(flags))
            {
                if (!property.CanRead || property.GetIndexParameters().Length != 0)
                {
                    continue;
                }

                if (options.PropertyFilter is not null && !options.PropertyFilter(property))
                {
                    continue;
                }

                // Most-derived declaration is encountered first; ignore shadowed base members.
                if (!seen.ContainsKey(property.Name))
                {
                    seen[property.Name] = property;
                }
            }
        }

        // Surface interface-declared properties when the root itself is an interface.
        if (type.IsInterface)
        {
            foreach (var iface in new[] { type }.Concat(type.GetInterfaces()))
            {
                foreach (var property in iface.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (!property.CanRead || property.GetIndexParameters().Length != 0)
                    {
                        continue;
                    }

                    if (options.PropertyFilter is not null && !options.PropertyFilter(property))
                    {
                        continue;
                    }

                    if (!seen.ContainsKey(property.Name))
                    {
                        seen[property.Name] = property;
                    }
                }
            }
        }

        return seen.Values;
    }

    private static bool TryGetCollectionItemType(Type type, out Type? itemType)
    {
        itemType = null;

        // String is enumerable but should be treated as a scalar.
        if (type == typeof(string))
        {
            return false;
        }

        // byte[] is conventionally a binary blob; surface as a collection but the caller
        // will skip the synthetic item child because byte is a leaf type.
        if (type.IsArray)
        {
            itemType = type.GetElementType();
            return itemType is not null;
        }

        // Dictionaries: surface the value type as the "item" so consumers see the
        // meaningful payload instead of KeyValuePair<,>.
        Type? dictionaryValueType = TryGetDictionaryValueType(type);
        if (dictionaryValueType is not null)
        {
            itemType = dictionaryValueType;
            return true;
        }

        // Generic IEnumerable<T>: pick the most specific T when multiple are implemented.
        Type? bestEnumerable = null;
        foreach (var iface in new[] { type }.Concat(type.GetInterfaces()))
        {
            if (!iface.IsGenericType || iface.GetGenericTypeDefinition() != typeof(IEnumerable<>))
            {
                continue;
            }

            if (bestEnumerable is null)
            {
                bestEnumerable = iface;
                continue;
            }

            Type currentArg = iface.GetGenericArguments()[0];
            Type bestArg = bestEnumerable.GetGenericArguments()[0];
            if (bestArg.IsAssignableFrom(currentArg) && bestArg != currentArg)
            {
                bestEnumerable = iface;
            }
        }

        if (bestEnumerable is not null)
        {
            itemType = bestEnumerable.GetGenericArguments()[0];
            return true;
        }

        // Non-generic IEnumerable: mark as collection but treat items as object.
        if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            itemType = typeof(object);
            return true;
        }

        return false;
    }

    private static Type? TryGetDictionaryValueType(Type type)
    {
        foreach (var iface in new[] { type }.Concat(type.GetInterfaces()))
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                return iface.GetGenericArguments()[1];
            }
        }

        return null;
    }

    private static bool IsLeafType(Type type, ObjectTreeHydratorOptions options)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (options.LeafTypeOverride is not null && options.LeafTypeOverride(type))
        {
            return true;
        }

        if (type.IsPrimitive || type.IsEnum)
        {
            return true;
        }

        return type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(DateOnly)
            || type == typeof(TimeOnly)
            || type == typeof(TimeSpan)
            || type == typeof(Guid)
            || type == typeof(Uri)
            || type == typeof(Version);
    }

    private sealed class TreeHydrationContext
    {
        private readonly Stack<Type> _pathStack = new();
        private readonly HashSet<Type> _pathSet = new();
        private int _nodeCount;

        public TreeHydrationContext(ObjectTreeHydratorOptions options)
        {
            Options = options;
        }

        public ObjectTreeHydratorOptions Options { get; }

        public bool IsBudgetExhausted => _nodeCount >= Options.MaxNodes;

        public void IncrementNodeCount() => _nodeCount++;

        public bool IsInCurrentPath(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return _pathSet.Contains(type);
        }

        public PathScope Enter(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            bool added = _pathSet.Add(type);
            _pathStack.Push(type);
            return new PathScope(this, type, added);
        }

        private void Exit(Type type, bool removeFromSet)
        {
            if (_pathStack.Count > 0)
            {
                _pathStack.Pop();
            }

            if (removeFromSet)
            {
                _pathSet.Remove(type);
            }
        }

        public readonly struct PathScope : IDisposable
        {
            private readonly TreeHydrationContext _context;
            private readonly Type _type;
            private readonly bool _removeFromSet;

            internal PathScope(TreeHydrationContext context, Type type, bool removeFromSet)
            {
                _context = context;
                _type = type;
                _removeFromSet = removeFromSet;
            }

            public void Dispose() => _context.Exit(_type, _removeFromSet);
        }
    }
}
