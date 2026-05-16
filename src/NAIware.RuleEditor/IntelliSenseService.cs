using NAIware.RuleIntelligence;

namespace NAIware.RuleEditor;

/// <summary>
/// Thin adapter over <see cref="NAIware.RuleIntelligence"/> that resolves the context type from
/// the editor's assembly discovery service, builds a <see cref="RuleSchema"/>, and exposes a
/// rich completion API to the UI.
/// </summary>
/// <remarks>
/// Reflection, path generation, operator selection, and value suggestions are all delegated to
/// <see cref="RuleIntelliSenseService"/>. The legacy <see cref="ContextMetadata"/> surface is
/// preserved so <see cref="RuleValidationService"/> continues to work unchanged; its property
/// path list is derived from the schema rather than rebuilt locally.
/// </remarks>
public sealed class IntelliSenseService
{
    private readonly AssemblyTypeDiscoveryService _typeDiscovery;
    private readonly IRuleSchemaProvider _schemaProvider;
    private readonly IRuleIntelliSenseService _intelliSense;
    private readonly Dictionary<string, CachedContext> _cache = new(StringComparer.Ordinal);

    /// <summary>Initializes a new IntelliSense service.</summary>
    public IntelliSenseService(AssemblyTypeDiscoveryService typeDiscovery)
        : this(typeDiscovery, new ObjectTreeRuleSchemaProvider(), new RuleIntelliSenseService())
    {
    }

    /// <summary>Initializes a new IntelliSense service with explicit dependencies (testing).</summary>
    public IntelliSenseService(
        AssemblyTypeDiscoveryService typeDiscovery,
        IRuleSchemaProvider schemaProvider,
        IRuleIntelliSenseService intelliSense)
    {
        ArgumentNullException.ThrowIfNull(typeDiscovery);
        ArgumentNullException.ThrowIfNull(schemaProvider);
        ArgumentNullException.ThrowIfNull(intelliSense);
        _typeDiscovery = typeDiscovery;
        _schemaProvider = schemaProvider;
        _intelliSense = intelliSense;
    }

    /// <summary>
    /// Builds (or retrieves from cache) the reflected metadata for the specified context.
    /// </summary>
    public ContextMetadata? GetMetadata(RuleContext context)
    {
        CachedContext? cached = ResolveCached(context);
        return cached?.Metadata;
    }

    /// <summary>Returns the rule schema for the supplied context, or null when it cannot be resolved.</summary>
    public RuleSchema? GetSchema(RuleContext context) => ResolveCached(context)?.Schema;

    /// <summary>
    /// Returns context-aware completions for the supplied expression and cursor position.
    /// </summary>
    public RuleCompletionResponse? GetCompletions(RuleContext context, string expression, int cursorPosition, int maxItems = 50)
    {
        if (expression is null) return null;

        RuleSchema? schema = GetSchema(context);
        if (schema is null) return null;

        var request = new RuleCompletionRequest
        {
            Schema = schema,
            Expression = expression,
            CursorPosition = Math.Clamp(cursorPosition, 0, expression.Length),
            MaxItems = maxItems,
            IncludeSnippets = true
        };

        return _intelliSense.GetCompletions(request);
    }

    /// <summary>Clears cached schemas. Call after a referenced assembly is replaced on disk.</summary>
    public void Invalidate()
    {
        _cache.Clear();
        _intelliSense.Invalidate();
        if (_schemaProvider is ObjectTreeRuleSchemaProvider provider) provider.Invalidate();
    }

    private CachedContext? ResolveCached(RuleContext context)
    {
        if (context is null || string.IsNullOrWhiteSpace(context.QualifiedTypeName)) return null;

        string cacheKey = $"{context.AssemblyPath}|{context.QualifiedTypeName}";
        if (_cache.TryGetValue(cacheKey, out CachedContext? cached)) return cached;

        Type? type = _typeDiscovery.ResolveContextType(context);
        if (type is null) return null;

        string rootName = string.IsNullOrWhiteSpace(context.Name) ? type.Name : context.Name;
        RuleSchema schema = _schemaProvider.Build(type, rootName);

        IReadOnlyList<string> paths = ExtractPropertyPaths(schema);
        var metadata = new ContextMetadata(type, paths);
        var entry = new CachedContext(schema, metadata);
        _cache[cacheKey] = entry;
        return entry;
    }

    private static IReadOnlyList<string> ExtractPropertyPaths(RuleSchema schema)
    {
        var results = new List<string>();
        Collect(schema.Root, prefix: string.Empty, results);
        return results;

        static void Collect(RuleCompletionNode node, string prefix, List<string> results)
        {
            foreach (RuleCompletionNode child in node.Children)
            {
                // Collection-item synthetic nodes have segment names like "[0]"; expose
                // them as a dotted "0" segment so the legacy validator regex still matches.
                string segment = child.IsCollectionItem ? "0" : child.Name;
                string path = string.IsNullOrEmpty(prefix) ? segment : $"{prefix}.{segment}";
                results.Add(path);
                Collect(child, path, results);
            }
        }
    }

    private sealed record CachedContext(RuleSchema Schema, ContextMetadata Metadata);
}

/// <summary>
/// Cached reflection data for a context type. Both IntelliSense and validation consume this.
/// </summary>
/// <param name="Type">The resolved .NET type.</param>
/// <param name="PropertyPaths">All reachable property paths up to a configured depth.</param>
public sealed record ContextMetadata(Type Type, IReadOnlyList<string> PropertyPaths);

