namespace NAIware.RuleIntelligence;

internal static class ReadOnlySet<T>
{
    public static readonly IReadOnlySet<T> Empty = new HashSet<T>();
}
