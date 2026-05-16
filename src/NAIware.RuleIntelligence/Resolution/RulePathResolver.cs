namespace NAIware.RuleIntelligence;

public sealed class RulePathResolver
{
    private readonly RuleSchema _schema;

    public RulePathResolver(RuleSchema schema)
    {
        _schema = schema;
    }

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
