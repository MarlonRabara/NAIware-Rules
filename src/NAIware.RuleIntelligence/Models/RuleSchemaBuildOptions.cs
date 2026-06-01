namespace NAIware.RuleIntelligence;

public sealed record RuleSchemaBuildOptions
{
    public int MaxDepth { get; init; } = 8;
    public bool IncludeCollectionItemNode { get; init; } = true;
    public bool IncludeCollectionCountSyntheticNode { get; init; } = true;
    public bool CacheSchemas { get; init; } = true;
}
