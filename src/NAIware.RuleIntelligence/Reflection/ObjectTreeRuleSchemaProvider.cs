using NAIware.Core.Collections;
using NAIware.Core.Reflection;

namespace NAIware.RuleIntelligence;

/// <summary>
/// Builds a RuleSchema from NAIware.Core.Reflection.ObjectTreeHydrator.
/// This keeps reflection-tree logic consolidated in NAIware.Core while this library focuses on editor intelligence.
/// </summary>
public sealed class ObjectTreeRuleSchemaProvider : IRuleSchemaProvider
{
    private readonly ConcurrentDictionary<string, RuleSchema> _cache = new(StringComparer.Ordinal);

    public RuleSchema Build(Type rootType, string rootName, RuleSchemaBuildOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(rootType);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootName);

        options ??= new RuleSchemaBuildOptions();
        var key = $"{rootType.AssemblyQualifiedName}|{rootName}|{options.MaxDepth}|{options.IncludeCollectionCountSyntheticNode}";

        if (options.CacheSchemas && _cache.TryGetValue(key, out var cached))
            return cached;

        var tree = ObjectTreeHydrator.Create(rootType, rootObject: null, rootName, options.MaxDepth);
        var schema = RuleSchemaMapper.Map(tree, rootName, rootType, options);

        if (options.CacheSchemas)
            _cache[key] = schema;

        return schema;
    }

    public RuleSchema Build(object rootInstance, string rootName, RuleSchemaBuildOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(rootInstance);
        return Build(rootInstance.GetType(), rootName, options);
    }

    public void Invalidate() => _cache.Clear();
}

internal static class RuleSchemaMapper
{
    public static RuleSchema Map(Tree<PropertyTreeNode, ReflectedPropertyNode> tree, string rootName, Type rootType, RuleSchemaBuildOptions options)
    {
        if (tree.Root is null)
            throw new InvalidOperationException("ObjectTreeHydrator returned a tree without a root node.");

        var nodes = new Dictionary<string, RuleCompletionNode>(StringComparer.OrdinalIgnoreCase);
        var root = MapNode(tree.Root, parent: null, depth: 0, nodes, options);

        return new RuleSchema
        {
            RootName = rootName,
            RootType = rootType,
            Root = root,
            NodesByPath = new ReadOnlyDictionary<string, RuleCompletionNode>(nodes)
        };
    }

    private static RuleCompletionNode MapNode(
        PropertyTreeNode source,
        RuleCompletionNode? parent,
        int depth,
        Dictionary<string, RuleCompletionNode> index,
        RuleSchemaBuildOptions options)
    {
        ReflectedPropertyNode reflected = source.Value;

        var children = new List<RuleCompletionNode>();
        var node = new RuleCompletionNode
        {
            Name = reflected.Name,
            Path = reflected.Path,
            Type = reflected.Type,
            PropertyInfo = reflected.PropertyInfo,
            IsRoot = reflected.IsRoot,
            IsCollection = reflected.IsCollection,
            IsCollectionItem = reflected.IsCollectionItem,
            IsLeaf = RuleTypeClassifier.IsSimple(reflected.Type) || (!reflected.IsCollection && source.Children.Count == 0),
            Depth = depth,
            Parent = parent,
            Children = children
        };

        foreach (PropertyTreeNode child in source.Children)
        {
            var mappedChild = MapNode(child, node, depth + 1, index, options);
            children.Add(mappedChild);
        }

        if (options.IncludeCollectionCountSyntheticNode && reflected.IsCollection)
        {
            var countNode = new RuleCompletionNode
            {
                Name = "Count",
                Path = $"{reflected.Path}.Count",
                Type = typeof(int),
                PropertyInfo = null,
                IsRoot = false,
                IsCollection = false,
                IsCollectionItem = false,
                IsLeaf = true,
                Depth = depth + 1,
                Parent = node,
                Children = []
            };

            children.Add(countNode);
            index[countNode.Path] = countNode;
        }

        index[node.Path] = node;

        // Also allow rootless paths for editor expressions that omit the root object.
        var rootName = GetRootName(node);
        var rootless = RulePathNormalizer.WithoutRoot(node.Path, rootName);
        if (!string.IsNullOrWhiteSpace(rootless))
            index.TryAdd(rootless, node);

        return node;
    }

    private static string GetRootName(RuleCompletionNode node)
    {
        var current = node;
        while (current.Parent is not null)
            current = current.Parent;

        return current.Name;
    }
}
