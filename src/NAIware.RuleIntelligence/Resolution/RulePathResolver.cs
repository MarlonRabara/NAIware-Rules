namespace NAIware.RuleIntelligence;

/// <summary>
/// Resolves a textual rule path (such as <c>LoanApplication.Borrowers[0].Age</c>) to the matching
/// <see cref="RuleCompletionNode"/> within a <see cref="RuleSchema"/>.
/// </summary>
/// <remarks>
/// Resolution is attempted in increasing cost order: an exact lookup in the schema's path index, a
/// root-prefixed lookup (to tolerate paths that omit the root object name), and finally a segment-by-segment
/// walk of the node tree that tolerates collection index syntax. This layered approach keeps the common
/// cases O(1) while still resolving rootless or index-bearing paths that are not pre-indexed.
/// </remarks>
public sealed class RulePathResolver
{
    private readonly RuleSchema _schema;

    /// <summary>Creates a resolver bound to the supplied schema.</summary>
    public RulePathResolver(RuleSchema schema)
    {
        _schema = schema;
    }

    /// <summary>
    /// Resolves a path to its schema node.
    /// </summary>
    /// <param name="path">
    /// The dotted/indexed path to resolve. A null or whitespace path resolves to the schema root.
    /// </param>
    /// <returns>The matching node, or <see langword="null"/> when the path cannot be resolved.</returns>
    public RuleCompletionNode? Resolve(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return _schema.Root;

        var normalized = RulePathNormalizer.Normalize(path);

        if (_schema.NodesByPath.TryGetValue(normalized, out var exact))
            return exact;

        if (!normalized.StartsWith(_schema.RootName + ".", StringComparison.OrdinalIgnoreCase)
            && !normalized.Equals(_schema.RootName, StringComparison.OrdinalIgnoreCase))
        {
            var withRoot = $"{_schema.RootName}.{normalized}";
            if (_schema.NodesByPath.TryGetValue(withRoot, out exact))
                return exact;
        }

        return ResolveByWalking(normalized);
    }

    /// <summary>
    /// Resolves a normalized path by walking the node tree one segment at a time, starting from the root.
    /// Each segment is matched against child names, also accepting the bracketed form (<c>[name]</c>) used
    /// for collection-item nodes. Returns <see langword="null"/> as soon as a segment cannot be matched.
    /// </summary>
    private RuleCompletionNode? ResolveByWalking(string path)
    {
        var withoutRoot = RulePathNormalizer.WithoutRoot(path, _schema.RootName);
        if (string.IsNullOrWhiteSpace(withoutRoot))
            return _schema.Root;

        var current = _schema.Root;
        foreach (var part in SplitPath(withoutRoot))
        {
            var next = current.Children.FirstOrDefault(c =>
                string.Equals(c.Name, part, StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.Name, $"[{part}]", StringComparison.OrdinalIgnoreCase));

            if (next is null)
                return null;

            current = next;
        }

        return current;
    }

    /// <summary>
    /// Splits a path into walkable segments, expanding <c>name[index]</c> into two segments
    /// (<c>name</c> followed by <c>[index]</c>) so the collection node and its item node can each be matched.
    /// </summary>
    private static IEnumerable<string> SplitPath(string path)
    {
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var bracket = Regex.Match(segment, @"^(?<name>[^\[]+)(?:\[(?<index>[0-9]+)\])?$");
            if (!bracket.Success)
            {
                yield return segment;
                continue;
            }

            yield return bracket.Groups["name"].Value;
            if (bracket.Groups["index"].Success)
                yield return $"[{bracket.Groups["index"].Value}]";
        }
    }
}
