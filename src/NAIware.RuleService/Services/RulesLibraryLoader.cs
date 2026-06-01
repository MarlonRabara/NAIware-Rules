using System.Text.Json;
using System.Text.Json.Serialization;
using NAIware.Rules.Models;

namespace NAIware.RuleService.Services;

/// <summary>
/// Loads a <see cref="RulesLibrary"/> from JSON and rebuilds the in-memory category/expression
/// links the rule processor depends on.
/// </summary>
/// <remarks>
/// The on-disk library persists category membership as expression id lists. Before evaluation,
/// those ids must be re-joined to their <see cref="RuleExpression"/> instances so categories can
/// resolve their expressions. This logic intentionally mirrors the Rule Editor's
/// <c>RuleLibrarySerializer</c> so libraries authored in the editor evaluate identically here.
/// </remarks>
public sealed class RulesLibraryLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Loads a rules library from a JSON document.</summary>
    /// <param name="json">The serialized rules library.</param>
    /// <returns>The hydrated, link-rebuilt library.</returns>
    public RulesLibrary LoadFromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        RulesLibrary library = JsonSerializer.Deserialize<RulesLibrary>(json, Options)
            ?? throw new InvalidOperationException("Unable to deserialize rules library — content is empty or invalid.");

        RebuildCategoryExpressionLinks(library);
        return library;
    }

    /// <summary>Loads a rules library from a JSON file path.</summary>
    /// <param name="path">The absolute path to the library JSON file.</param>
    /// <returns>The hydrated, link-rebuilt library.</returns>
    public RulesLibrary LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path)) throw new FileNotFoundException("Rules library file not found.", path);

        return LoadFromJson(File.ReadAllText(path));
    }

    private static void RebuildCategoryExpressionLinks(RulesLibrary library)
    {
        foreach (RuleContext context in library.Contexts)
        {
            Dictionary<Guid, RuleExpression> expressions = context.Expressions.ToDictionary(e => e.Identity);
            foreach (RuleCategory category in context.Categories)
                RebuildCategory(category, expressions);
        }
    }

    private static void RebuildCategory(RuleCategory category, IReadOnlyDictionary<Guid, RuleExpression> expressions)
    {
        category.CategoryExpressions.Clear();
        foreach (Guid id in category.ExpressionIds.Distinct())
        {
            expressions.TryGetValue(id, out RuleExpression? expression);
            if (expression is not null) category.AddExpression(expression);
        }

        foreach (RuleCategory child in category.Categories)
        {
            category.AttachSubcategory(child);
            RebuildCategory(child, expressions);
        }
    }
}
