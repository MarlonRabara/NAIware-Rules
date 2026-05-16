namespace NAIware.RuleIntelligence;

public sealed class DefaultRuleValueSuggestionProvider : IRuleValueSuggestionProvider
{
    public IReadOnlyList<RuleCompletionItem> GetValueSuggestions(RuleValueSuggestionContext context)
    {
        var expectedType = context.ExpectedType;
        if (expectedType is null)
            return [];

        var type = RuleTypeClassifier.Normalize(expectedType);
        var items = new List<RuleCompletionItem>();

        if (type == typeof(bool))
        {
            items.Add(RuleCompletionItem.Create("true", "true", RuleCompletionItemKind.Literal, "bool"));
            items.Add(RuleCompletionItem.Create("false", "false", RuleCompletionItemKind.Literal, "bool"));
            return items;
        }

        if (type == typeof(string))
        {
            items.Add(RuleCompletionItem.Create("\"text\"", "\"\"", RuleCompletionItemKind.Literal, "string", "String literal."));
            items.Add(RuleCompletionItem.Create("null", "null", RuleCompletionItemKind.Literal, "null"));
            return items;
        }

        if (RuleTypeClassifier.IsNumeric(type))
        {
            items.Add(RuleCompletionItem.Create("0", "0", RuleCompletionItemKind.Literal, TypeNameFormatter.Format(type)));
            return items;
        }

        if (RuleTypeClassifier.IsDateLike(type))
        {
            items.Add(RuleCompletionItem.Create("Today", "Today", RuleCompletionItemKind.Literal, TypeNameFormatter.Format(type)));
            items.Add(RuleCompletionItem.Create("\"yyyy-MM-dd\"", "\"\"", RuleCompletionItemKind.Literal, TypeNameFormatter.Format(type)));
            return items;
        }

        if (type == typeof(Guid))
        {
            items.Add(RuleCompletionItem.Create("Guid.Empty", "Guid.Empty", RuleCompletionItemKind.Literal, "Guid"));
            items.Add(RuleCompletionItem.Create("\"00000000-0000-0000-0000-000000000000\"", "\"\"", RuleCompletionItemKind.Literal, "Guid"));
            return items;
        }

        if (type.IsEnum)
        {
            foreach (var name in Enum.GetNames(type))
                items.Add(RuleCompletionItem.Create(name, name, RuleCompletionItemKind.Literal, type.Name));

            return items;
        }

        items.Add(RuleCompletionItem.Create("null", "null", RuleCompletionItemKind.Literal, "null"));
        return items;
    }
}
