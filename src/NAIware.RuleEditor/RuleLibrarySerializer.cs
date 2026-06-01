using System.Text.Json;
using System.Text.Json.Serialization;

namespace NAIware.RuleEditor;

/// <summary>
/// JSON serializer for <see cref="RulesLibrary"/>. Uses indented output for
/// human-readable files and case-insensitive property matching on read.
/// </summary>
public static class RuleLibrarySerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Loads a rule library from the specified JSON file path.</summary>
    public static RulesLibrary Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string json = File.ReadAllText(path);
        RulesLibrary? library = JsonSerializer.Deserialize<RulesLibrary>(json, Options);
        if (library is null) throw new InvalidOperationException("Unable to deserialize rule library — file is empty or invalid.");

        ResolveContextPaths(library, Path.GetDirectoryName(Path.GetFullPath(path)));
        RebuildCategoryExpressionLinks(library);
        return library;
    }

    /// <summary>
    /// Resolves any relative assembly or sample-data paths on each context against the
    /// directory that contains the library file. This keeps a saved library portable:
    /// relative paths (for example "Mortgage.Model.dll") resolve next to the JSON file
    /// regardless of which machine opens it, while absolute paths are left untouched.
    /// </summary>
    private static void ResolveContextPaths(RulesLibrary library, string? libraryDirectory)
    {
        if (string.IsNullOrWhiteSpace(libraryDirectory)) return;

        foreach (RuleContext context in library.Contexts)
        {
            context.SourceAssemblyPath = ResolveAgainst(libraryDirectory, context.SourceAssemblyPath);
            context.SerializerAssemblyPath = ResolveAgainst(libraryDirectory, context.SerializerAssemblyPath);
            context.SerializedDataPath = ResolveAgainst(libraryDirectory, context.SerializedDataPath);
        }
    }

    private static string? ResolveAgainst(string baseDirectory, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate)) return candidate;
        if (Path.IsPathRooted(candidate)) return candidate;

        return Path.GetFullPath(Path.Combine(baseDirectory, candidate));
    }

    /// <summary>Saves the supplied rule library to the specified JSON file path.</summary>
    public static void Save(string path, RulesLibrary library)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(library);

        library.SavedUtc = DateTimeOffset.UtcNow;
        RebuildExpressionIds(library);

        string? libraryDirectory = Path.GetDirectoryName(Path.GetFullPath(path));
        RelativizeContextPaths(library, libraryDirectory, out List<(RuleContext Context, string? Source, string? Serializer, string? Data)> originals);
        try
        {
            string json = JsonSerializer.Serialize(library, Options);
            File.WriteAllText(path, json);
        }
        finally
        {
            // Restore the in-memory absolute paths so the live editor session keeps working
            // after a save; only the persisted file carries the portable relative form.
            foreach ((RuleContext context, string? source, string? serializer, string? data) in originals)
            {
                context.SourceAssemblyPath = source;
                context.SerializerAssemblyPath = serializer;
                context.SerializedDataPath = data;
            }
        }
    }

    /// <summary>
    /// Rewrites context paths that live under the library directory to a relative form so the
    /// saved file is portable. Absolute paths outside the directory are preserved as-is.
    /// The original values are captured so the caller can restore the in-memory state.
    /// </summary>
    private static void RelativizeContextPaths(
        RulesLibrary library,
        string? libraryDirectory,
        out List<(RuleContext Context, string? Source, string? Serializer, string? Data)> originals)
    {
        originals = new List<(RuleContext, string?, string?, string?)>(library.Contexts.Count);
        if (string.IsNullOrWhiteSpace(libraryDirectory)) return;

        foreach (RuleContext context in library.Contexts)
        {
            originals.Add((context, context.SourceAssemblyPath, context.SerializerAssemblyPath, context.SerializedDataPath));
            context.SourceAssemblyPath = RelativizeAgainst(libraryDirectory, context.SourceAssemblyPath);
            context.SerializerAssemblyPath = RelativizeAgainst(libraryDirectory, context.SerializerAssemblyPath);
            context.SerializedDataPath = RelativizeAgainst(libraryDirectory, context.SerializedDataPath);
        }
    }

    private static string? RelativizeAgainst(string baseDirectory, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate) || !Path.IsPathRooted(candidate)) return candidate;

        string relative = Path.GetRelativePath(baseDirectory, candidate);

        // Only store the relative form when it stays within the library directory tree;
        // a path that escapes via "..\" or onto another drive is clearer left absolute.
        bool escapesTree = relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative);
        return escapesTree ? candidate : relative;
    }

    /// <summary>Rebuilds category-expression joins from persisted expression ids.</summary>
    public static void RebuildCategoryExpressionLinks(RulesLibrary library)
    {
        foreach (RuleContext context in library.Contexts)
        {
            var expressions = context.Expressions.ToDictionary(e => e.Identity);
            foreach (RuleCategory category in context.Categories)
            {
                RebuildCategory(category, expressions);
            }
        }
    }

    private static void RebuildCategory(RuleCategory category, IReadOnlyDictionary<Guid, RuleExpression> expressions)
    {
        category.CategoryExpressions.Clear();
        foreach (Guid id in category.ExpressionIds.Distinct())
        {
            expressions.TryGetValue(id, out RuleExpression? expression);
            category.CategoryExpressions.Add(new RuleCategoryExpression(category.Identity, id, category.CategoryExpressions.Count, expression));
        }

        foreach (RuleCategory child in category.Categories)
        {
            category.AttachSubcategory(child);
            RebuildCategory(child, expressions);
        }
    }

    private static void RebuildExpressionIds(RulesLibrary library)
    {
        foreach (RuleContext context in library.Contexts)
        {
            foreach (RuleCategory category in context.Categories)
            {
                RebuildExpressionIds(category);
            }
        }
    }

    private static void RebuildExpressionIds(RuleCategory category)
    {
        foreach (RuleCategoryExpression link in category.CategoryExpressions)
        {
            if (!category.ExpressionIds.Contains(link.ExpressionIdentity))
                category.ExpressionIds.Add(link.ExpressionIdentity);
        }

        foreach (RuleCategory child in category.Categories)
        {
            RebuildExpressionIds(child);
        }
    }
}
