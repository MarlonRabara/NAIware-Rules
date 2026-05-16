namespace NAIware.RuleIntelligence;

/// <summary>
/// Default standalone completion service.
/// </summary>
public sealed class RuleIntelliSenseService : IRuleIntelliSenseService
{
    private readonly IRuleOperatorProvider _operatorProvider;
    private readonly IRuleValueSuggestionProvider _valueSuggestionProvider;
    private readonly RuleCompletionContextParser _parser;

    public RuleIntelliSenseService(
        IRuleOperatorProvider? operatorProvider = null,
        IRuleValueSuggestionProvider? valueSuggestionProvider = null)
    {
        _operatorProvider = operatorProvider ?? new DefaultRuleOperatorProvider();
        _valueSuggestionProvider = valueSuggestionProvider ?? new DefaultRuleValueSuggestionProvider();
        _parser = new RuleCompletionContextParser(_operatorProvider);
    }

    public RuleCompletionResponse GetCompletions(RuleCompletionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Schema);
        ArgumentNullException.ThrowIfNull(request.Expression);

        var resolver = new RulePathResolver(request.Schema);
        var context = _parser.Parse(request, resolver);
        var candidates = GetCandidateItems(request, context);

        var filtered = FilterAndRank(candidates, context.TypedPrefix)
            .Take(Math.Max(0, request.MaxItems))
            .ToList();

        return new RuleCompletionResponse
        {
            Context = context,
            Items = filtered,
            ReplacementPrefix = context.TypedPrefix,
            ReplacementStart = context.ReplacementStart,
            ReplacementLength = context.ReplacementLength
        };
    }

    public RuleCompletionContext Analyze(RuleCompletionRequest request)
    {
        var resolver = new RulePathResolver(request.Schema);
        return _parser.Parse(request, resolver);
    }

    public RuleCompletionNode? ResolvePath(RuleSchema schema, string path)
    {
        var resolver = new RulePathResolver(schema);
        return resolver.Resolve(path);
    }

    public void Invalidate()
    {
        // Stateless by default. Schema providers may maintain their own cache.
    }

    private IReadOnlyList<RuleCompletionItem> GetCandidateItems(RuleCompletionRequest request, RuleCompletionContext context)
    {
        return context.Kind switch
        {
            RuleCompletionContextKind.RootSymbol =>
                GetRootItems(request.Schema, request.IncludeSnippets),

            RuleCompletionContextKind.MemberAccess =>
                GetMemberItems(context.TargetNode, request.IncludeSnippets),

            RuleCompletionContextKind.Operator =>
                context.LeftNode is null
                    ? []
                    : _operatorProvider.GetComparisonOperators(context.LeftNode.Type)
                        .Select(x => x.ToCompletionItem(context.LeftNode.Type))
                        .ToList(),

            RuleCompletionContextKind.Value =>
                _valueSuggestionProvider.GetValueSuggestions(new RuleValueSuggestionContext
                {
                    Schema = request.Schema,
                    CompletionContext = context
                }),

            RuleCompletionContextKind.LogicalConnector =>
                _operatorProvider.GetLogicalOperators()
                    .Select(x => x.ToCompletionItem())
                    .ToList(),

            _ => []
        };
    }

    private static IReadOnlyList<RuleCompletionItem> GetRootItems(RuleSchema schema, bool includeSnippets)
    {
        var items = new List<RuleCompletionItem>();

        // If the root is a real named context, suggest it as well as its children.
        items.Add(schema.Root.ToCompletionItem());
        items.AddRange(schema.Root.Children.Select(c => c.ToCompletionItem()));

        if (includeSnippets)
        {
            items.Add(RuleCompletionItem.Create("true", "true", RuleCompletionItemKind.Literal, "bool"));
            items.Add(RuleCompletionItem.Create("false", "false", RuleCompletionItemKind.Literal, "bool"));
        }

        return items;
    }

    private static IReadOnlyList<RuleCompletionItem> GetMemberItems(RuleCompletionNode? node, bool includeSnippets)
    {
        if (node is null)
            return [];

        var items = node.Children.Select(c => c.ToCompletionItem()).ToList();

        if (includeSnippets && node.IsCollection)
        {
            items.Add(RuleCompletionItem.Create(
                label: "[0]",
                insertText: "[0]",
                kind: RuleCompletionItemKind.Snippet,
                detail: "Collection item",
                documentation: "Access the first collection item. Replace 0 with the desired index.",
                path: $"{node.Path}[0]"));
        }

        return items;
    }

    private static IEnumerable<RuleCompletionItem> FilterAndRank(IEnumerable<RuleCompletionItem> candidates, string prefix)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            if (!seen.Add($"{candidate.Kind}|{candidate.Label}|{candidate.InsertText}"))
                continue;

            var score = Score(candidate, prefix);
            if (score <= 0)
                continue;

            yield return candidate with { Score = score };
        }
    }

    private static int Score(RuleCompletionItem item, string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return item.Kind switch
            {
                RuleCompletionItemKind.Property => 80,
                RuleCompletionItemKind.Collection => 78,
                RuleCompletionItemKind.RootObject => 75,
                RuleCompletionItemKind.Operator => 70,
                RuleCompletionItemKind.LogicalOperator => 70,
                RuleCompletionItemKind.Literal => 60,
                _ => 50
            };

        if (item.Label.Equals(prefix, StringComparison.OrdinalIgnoreCase))
            return 1000;

        if (item.Label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return 900;

        if (item.Label.Contains(prefix, StringComparison.OrdinalIgnoreCase))
            return 500;

        return 0;
    }
}
