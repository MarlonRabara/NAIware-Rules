namespace NAIware.RuleIntelligence;

/// <summary>
/// Default, stateless completion service that turns a <see cref="RuleCompletionRequest"/> into a ranked
/// list of <see cref="RuleCompletionItem"/> suggestions for a rule-expression editor.
/// </summary>
/// <remarks>
/// The service orchestrates three collaborators: a <see cref="RuleCompletionContextParser"/> classifies
/// the caret position, an <see cref="IRuleOperatorProvider"/> supplies comparison/logical operators, and
/// an <see cref="IRuleValueSuggestionProvider"/> supplies literal value suggestions. The resulting
/// candidates are de-duplicated, scored against the typed prefix, and truncated to the requested maximum.
/// Because it holds no per-request state, a single instance is safe to share.
/// </remarks>
public sealed class RuleIntelliSenseService : IRuleIntelliSenseService
{
    private readonly IRuleOperatorProvider _operatorProvider;
    private readonly IRuleValueSuggestionProvider _valueSuggestionProvider;
    private readonly RuleCompletionContextParser _parser;

    /// <summary>
    /// Creates the service, falling back to the default operator and value-suggestion providers when none
    /// are supplied.
    /// </summary>
    public RuleIntelliSenseService(
        IRuleOperatorProvider? operatorProvider = null,
        IRuleValueSuggestionProvider? valueSuggestionProvider = null)
    {
        _operatorProvider = operatorProvider ?? new DefaultRuleOperatorProvider();
        _valueSuggestionProvider = valueSuggestionProvider ?? new DefaultRuleValueSuggestionProvider();
        _parser = new RuleCompletionContextParser(_operatorProvider);
    }

    /// <summary>
    /// Computes ranked completion suggestions for the supplied request.
    /// </summary>
    /// <param name="request">The completion request (expression, caret, schema, and limits).</param>
    /// <returns>
    /// A response carrying the analyzed context, the filtered/ranked items, and the replacement span the
    /// editor should overwrite when a suggestion is accepted.
    /// </returns>
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

    /// <summary>
    /// Analyzes the caret position without producing suggestions, returning the classified completion context.
    /// Useful for editors that want to drive UI affordances from the context kind alone.
    /// </summary>
    public RuleCompletionContext Analyze(RuleCompletionRequest request)
    {
        var resolver = new RulePathResolver(request.Schema);
        return _parser.Parse(request, resolver);
    }

    /// <summary>Resolves a path against a schema to its node, or <see langword="null"/> when not found.</summary>
    public RuleCompletionNode? ResolvePath(RuleSchema schema, string path)
    {
        var resolver = new RulePathResolver(schema);
        return resolver.Resolve(path);
    }

    /// <summary>
    /// No-op for this stateless service. Schema providers that cache may expose their own invalidation.
    /// </summary>
    public void Invalidate()
    {
        // Stateless by default. Schema providers may maintain their own cache.
    }

    /// <summary>
    /// Produces the raw, unranked candidate items appropriate for the classified context kind: root symbols,
    /// member access children, type-aware comparison operators, value suggestions, or logical connectors.
    /// </summary>
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

    /// <summary>
    /// De-duplicates candidates (by kind/label/insert-text), scores each against the typed prefix, drops
    /// non-matching items (score &lt;= 0), and yields the survivors with their score attached for ordering.
    /// </summary>
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

    /// <summary>
    /// Scores a candidate against the typed prefix. With no prefix, items are ranked by kind (properties and
    /// collections first). With a prefix, an exact match scores highest, then prefix match, then a substring
    /// match; a non-match scores zero so it is filtered out.
    /// </summary>
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
