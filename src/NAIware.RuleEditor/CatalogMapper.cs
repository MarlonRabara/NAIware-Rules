using NAIware.Rules.Models;

namespace NAIware.RuleEditor;

/// <summary>
/// Maps between the UI <see cref="RuleLibraryDocument"/> DTO and the engine domain model
/// in <see cref="NAIware.Rules.Models"/>. The UI DTO is the persistence shape; the domain
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
        // Top-level categories are owned by the context; nested categories are
        // attached to their parent via RuleCategory.AddSubcategory so the
        // engine hierarchy mirrors the UI tree exactly.
        RuleCategory category = parent is null
            ? context.AddCategory(categoryDoc.Name, categoryDoc.Description)
            : parent.AddSubcategory(categoryDoc.Name, categoryDoc.Description);

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
