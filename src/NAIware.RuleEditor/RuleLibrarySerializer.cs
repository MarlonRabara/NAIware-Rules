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

        RebuildCategoryExpressionLinks(library);
        return library;
    }

    /// <summary>Saves the supplied rule library to the specified JSON file path.</summary>
    public static void Save(string path, RulesLibrary library)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(library);

        library.SavedUtc = DateTimeOffset.UtcNow;
        RebuildExpressionIds(library);
        string json = JsonSerializer.Serialize(library, Options);
        File.WriteAllText(path, json);
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
