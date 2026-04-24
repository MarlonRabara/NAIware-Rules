using NAIware.Rules.Catalog;

namespace NAIware.RuleEditor;

/// <summary>
/// Maps between the UI <see cref="RuleLibraryDocument"/> DTO and the engine domain model
/// in <see cref="NAIware.Rules.Catalog"/>. The UI DTO is the persistence shape; the domain
/// model is what the engine and rule processor consume at runtime.
/// </summary>
public static class CatalogMapper
{
    /// <summary>
    /// Produces a <see cref="RulesLibrary"/> suitable for evaluation from a UI document.
    /// Only rules that have a non-empty expression are attached.
    /// </summary>
    /// <param name="document">The UI document model.</param>
    /// <returns>A populated <see cref="RulesLibrary"/>.</returns>
    public static RulesLibrary ToDomain(RuleLibraryDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var library = new RulesLibrary(document.Name, document.Description);

        foreach (RuleContextDocument contextDoc in document.Contexts)
        {
            RuleContext context = library.AddContext(
                string.IsNullOrWhiteSpace(contextDoc.Name) ? contextDoc.QualifiedTypeName : contextDoc.Name,
                contextDoc.QualifiedTypeName,
                contextDoc.Description);

            var expressionById = new Dictionary<Guid, RuleExpression>();

            foreach (RuleExpressionDocument ruleDoc in contextDoc.Expressions)
            {
                if (string.IsNullOrWhiteSpace(ruleDoc.Expression)) continue;

                RuleExpression ruleExpression = context.AddExpression(
                    ruleDoc.Name,
                    ruleDoc.Expression,
                    ruleDoc.Description);

                ruleExpression.IsActive = ruleDoc.IsActive;

                if (!string.IsNullOrWhiteSpace(ruleDoc.ResultCode) || !string.IsNullOrWhiteSpace(ruleDoc.ResultMessage))
                {
                    ruleExpression.WithResult(
                        ruleDoc.ResultCode ?? string.Empty,
                        ruleDoc.ResultMessage ?? string.Empty,
                        ruleDoc.Severity);
                }

                expressionById[ruleDoc.Id] = ruleExpression;
            }

            foreach (RuleCategoryDocument categoryDoc in contextDoc.Categories)
            {
                AppendCategoryRecursive(context, parent: null, categoryDoc, expressionById);
            }
        }

        return library;
    }

    private static void AppendCategoryRecursive(
        RuleContext context,
        RuleCategory? parent,
        RuleCategoryDocument categoryDoc,
        IReadOnlyDictionary<Guid, RuleExpression> expressionById)
    {
        // The current domain model does not expose parent/child categories;
        // nested subcategories are flattened with a dotted name for traceability.
        string effectiveName = parent is null ? categoryDoc.Name : $"{parent.Name}.{categoryDoc.Name}";

        RuleCategory category = context.AddCategory(effectiveName, categoryDoc.Description);

        foreach (Guid expressionId in categoryDoc.ExpressionIds)
        {
            if (expressionById.TryGetValue(expressionId, out RuleExpression? ruleExpression))
                category.AddExpression(ruleExpression);
        }

        foreach (RuleCategoryDocument child in categoryDoc.Categories)
        {
            AppendCategoryRecursive(context, category, child, expressionById);
        }
    }
}
