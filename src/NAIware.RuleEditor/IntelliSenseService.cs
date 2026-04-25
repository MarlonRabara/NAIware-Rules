using System.Collections;
using System.Reflection;

namespace NAIware.RuleEditor;

/// <summary>
/// Provides lightweight IntelliSense-style metadata and suggestions for a context type.
/// Reflects property paths once and caches them; the same cached metadata is reused
/// by <see cref="RuleValidationService"/> to validate property references.
/// </summary>
public sealed class IntelliSenseService
{
    private static readonly string[] Keywords = ["and", "or", "true", "false", "null"];
    private static readonly string[] Operators = ["=", "!=", "<>", ">", "<", ">=", "<=", "+", "-", "*", "/"];

    private readonly AssemblyTypeDiscoveryService _typeDiscovery;
    private readonly Dictionary<string, ContextMetadata> _cache = new(StringComparer.Ordinal);

    /// <summary>Initializes a new IntelliSense service.</summary>
    public IntelliSenseService(AssemblyTypeDiscoveryService typeDiscovery)
    {
        ArgumentNullException.ThrowIfNull(typeDiscovery);
        _typeDiscovery = typeDiscovery;
    }

    /// <summary>
    /// Builds (or retrieves from cache) the reflected metadata for the specified context.
    /// </summary>
    /// <param name="context">The UI context document.</param>
    /// <returns>The reflected metadata, or null when the context type cannot be resolved.</returns>
    public ContextMetadata? GetMetadata(RuleContext context)
    {
        if (string.IsNullOrWhiteSpace(context.QualifiedTypeName)) return null;

        string cacheKey = $"{context.AssemblyPath}|{context.QualifiedTypeName}";
        if (_cache.TryGetValue(cacheKey, out ContextMetadata? cached)) return cached;

        Type? type = _typeDiscovery.ResolveContextType(context);
        if (type is null) return null;

        var metadata = new ContextMetadata(type, BuildPaths(type, maxDepth: 4));
        _cache[cacheKey] = metadata;
        return metadata;
    }

    /// <summary>
    /// Clears cached reflection data. Call after a referenced assembly is replaced on disk.
    /// </summary>
    public void Invalidate() => _cache.Clear();

    /// <summary>
    /// Returns candidate completions for the given prefix. Matches dot-notation paths,
    /// operators, and keywords. Matching is case-insensitive and prefix-based.
    /// </summary>
    /// <param name="context">The active context document.</param>
    /// <param name="prefix">The partial token the user has typed.</param>
    /// <returns>An ordered list of completion suggestions.</returns>
    public IReadOnlyList<string> GetSuggestions(RuleContext context, string prefix)
    {
        if (string.IsNullOrEmpty(prefix)) return [];

        ContextMetadata? metadata = GetMetadata(context);
        List<string> candidates = [];

        if (metadata is not null)
        {
            candidates.AddRange(metadata.PropertyPaths);
            candidates.Add(metadata.Type.Name);
        }

        candidates.AddRange(Keywords);
        candidates.AddRange(Operators);

        return [.. candidates
            .Where(c => c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .Take(25)];
    }

    private static IReadOnlyList<string> BuildPaths(Type root, int maxDepth)
    {
        var results = new List<string>();
        var visited = new HashSet<Type>();
        AppendPaths(root, string.Empty, results, visited, maxDepth, currentDepth: 0);
        return results;
    }

    private static void AppendPaths(Type type, string prefix, List<string> results, HashSet<Type> visited, int maxDepth, int currentDepth)
    {
        if (currentDepth >= maxDepth || !visited.Add(type)) return;

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            string path = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
            results.Add(path);

            Type propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (IsSimple(propertyType)) continue;

            if (propertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType.IsGenericType)
            {
                Type elementType = propertyType.GetGenericArguments()[0];
                results.Add($"{path}.Count");
                results.Add($"{path}.0");
                if (!IsSimple(elementType))
                    AppendPaths(elementType, $"{path}.0", results, visited, maxDepth, currentDepth + 1);
                continue;
            }

            AppendPaths(propertyType, path, results, visited, maxDepth, currentDepth + 1);
        }

        visited.Remove(type);
    }

    private static bool IsSimple(Type type) =>
        type.IsPrimitive
        || type.IsEnum
        || type == typeof(string)
        || type == typeof(decimal)
        || type == typeof(DateTime)
        || type == typeof(DateTimeOffset)
        || type == typeof(Guid)
        || type == typeof(TimeSpan);
}

/// <summary>
/// Cached reflection data for a context type. Both IntelliSense and validation consume this.
/// </summary>
/// <param name="Type">The resolved .NET type.</param>
/// <param name="PropertyPaths">All reachable property paths up to a configured depth.</param>
public sealed record ContextMetadata(Type Type, IReadOnlyList<string> PropertyPaths);
