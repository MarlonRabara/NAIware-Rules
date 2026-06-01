namespace NAIware.RuleIntelligence;

/// <summary>
/// Lightweight scanner for completion context detection. It is intentionally forgiving of incomplete expressions.
/// </summary>
public static partial class RuleExpressionScanner
{
    /// <summary>Returns the substring of <paramref name="expression"/> to the left of the (clamped) caret.</summary>
    public static string SafeLeftText(string expression, int cursor)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return expression[..Math.Clamp(cursor, 0, expression.Length)];
    }

    /// <summary>Returns the identifier currently being typed at the end of the text, or empty if none.</summary>
    public static string GetTypedPrefix(string leftText)
    {
        if (string.IsNullOrEmpty(leftText))
            return string.Empty;

        var match = TokenAtEndRegex().Match(leftText);
        return match.Success ? match.Value : string.Empty;
    }

    /// <summary>
    /// Computes the offset at which a replacement should begin: the caret position minus the length of the
    /// identifier currently being typed, so an accepted suggestion overwrites that partial token.
    /// </summary>
    public static int GetReplacementStart(string expression, int cursor)
    {
        var left = SafeLeftText(expression, cursor);
        var prefix = GetTypedPrefix(left);
        return Math.Max(0, cursor - prefix.Length);
    }

    /// <summary>Returns whether the left text ends with a member-access dot.</summary>
    public static bool EndsWithDot(string leftText) => leftText.EndsWith(".", StringComparison.Ordinal);

    /// <summary>
    /// Returns whether the caret follows a logical connector (<c>and</c>/<c>or</c>/<c>&amp;&amp;</c>/<c>||</c>) or an
    /// open parenthesis, indicating the start of a new sub-expression where a root symbol is expected.
    /// </summary>
    public static bool IsAfterLogicalConnector(string leftText)
    {
        var text = leftText.TrimEnd();
        return text.EndsWith(" and", StringComparison.OrdinalIgnoreCase)
            || text.EndsWith(" or", StringComparison.OrdinalIgnoreCase)
            || text.EndsWith("&&", StringComparison.Ordinal)
            || text.EndsWith("||", StringComparison.Ordinal)
            || text.EndsWith("(", StringComparison.Ordinal);
    }

    /// <summary>
    /// Attempts to split a trailing member-access expression (e.g. <c>A.B.Cit</c>) into its resolvable
    /// target path (<c>A.B</c>) and the partially typed member name (<c>Cit</c>).
    /// </summary>
    /// <returns><see langword="true"/> when a member-access pattern is found at the end of the text.</returns>
    public static bool TryGetMemberAccessTarget(string leftText, out string targetPath, out string typedPrefix)
    {
        targetPath = string.Empty;
        typedPrefix = string.Empty;

        var match = MemberAccessRegex().Match(leftText);
        if (!match.Success)
            return false;

        targetPath = match.Groups["target"].Value;
        typedPrefix = match.Groups["prefix"].Value;
        return true;
    }

    /// <summary>Attempts to extract a complete dotted/indexed path located at the end of the text.</summary>
    /// <returns><see langword="true"/> when a path is found.</returns>
    public static bool TryGetPathAtEnd(string leftText, out string path)
    {
        path = string.Empty;

        var match = PathAtEndRegex().Match(leftText);
        if (!match.Success)
            return false;

        path = match.Groups["path"].Value;
        return true;
    }

    /// <summary>
    /// Attempts to split a trailing comparison (e.g. <c>Amount &gt;= 10</c>) into its left-hand path, operator
    /// symbol, and the partially typed value, so the service can offer type-aware value suggestions.
    /// </summary>
    /// <returns><see langword="true"/> when a comparison pattern is found at the end of the text.</returns>
    public static bool TryGetComparisonAtEnd(string leftText, out string leftPath, out string operatorSymbol, out string valuePrefix)
    {
        leftPath = string.Empty;
        operatorSymbol = string.Empty;
        valuePrefix = string.Empty;

        var match = ComparisonAtEndRegex().Match(leftText);
        if (!match.Success)
            return false;

        leftPath = match.Groups["left"].Value;
        operatorSymbol = match.Groups["op"].Value;
        valuePrefix = match.Groups["value"].Value;
        return true;
    }

    /// <summary>
    /// Heuristically determines whether the text ends in a completed value — a boolean/null literal, a
    /// balanced quoted string, or a digit — which signals that a logical connector should be suggested next.
    /// </summary>
    public static bool LooksLikeCompletedValue(string leftText)
    {
        var text = leftText.TrimEnd();
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (text.EndsWith("true", StringComparison.OrdinalIgnoreCase)
            || text.EndsWith("false", StringComparison.OrdinalIgnoreCase)
            || text.EndsWith("null", StringComparison.OrdinalIgnoreCase))
            return true;

        if (text.EndsWith('"'))
            return Count(text, '"') >= 2;

        return char.IsDigit(text[^1]);
    }

    private static int Count(string text, char value)
    {
        var count = 0;
        foreach (var ch in text)
        {
            if (ch == value) count++;
        }
        return count;
    }

    [GeneratedRegex(@"[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex TokenAtEndRegex();

    [GeneratedRegex(@"(?<target>[A-Za-z_][A-Za-z0-9_]*(?:\[[0-9]+\])?(?:\.[A-Za-z_][A-Za-z0-9_]*(?:\[[0-9]+\])?)*)\.(?<prefix>[A-Za-z_][A-Za-z0-9_]*)$")]
    private static partial Regex MemberAccessRegex();

    [GeneratedRegex(@"(?<path>[A-Za-z_][A-Za-z0-9_]*(?:\[[0-9]+\])?(?:\.[A-Za-z_][A-Za-z0-9_]*(?:\[[0-9]+\])?)*)\s*$")]
    private static partial Regex PathAtEndRegex();

    [GeneratedRegex(@"(?<left>[A-Za-z_][A-Za-z0-9_]*(?:\[[0-9]+\])?(?:\.[A-Za-z_][A-Za-z0-9_]*(?:\[[0-9]+\])?)*)\s*(?<op>!=|<>|>=|<=|=|>|<)\s*(?<value>[^\s]*)$", RegexOptions.IgnoreCase)]
    private static partial Regex ComparisonAtEndRegex();
}
