namespace NAIware.RuleIntelligence;

internal static class RulePathNormalizer
{
    /// <summary>
    /// Converts collection paths to the style emitted by ObjectTreeHydrator: Borrowers.0.Name -> Borrowers[0].Name.
    /// </summary>
    public static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        return Regex.Replace(path.Trim(), @"\.([0-9]+)(?=\.|$)", "[$1]");
    }

    public static string WithoutRoot(string fullPath, string rootName)
    {
        if (fullPath.Equals(rootName, StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        if (fullPath.StartsWith(rootName + ".", StringComparison.OrdinalIgnoreCase))
            return fullPath[(rootName.Length + 1)..];

        return fullPath;
    }
}
