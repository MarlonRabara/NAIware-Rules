namespace NAIware.RuleIntelligence;

/// <summary>
/// Classifies the text immediately to the left of the editor caret into a
/// <see cref="RuleCompletionContext"/>, which tells the completion service what kind of suggestions
/// to produce (a root symbol, a member access, an operator, a value, or a logical connector).
/// </summary>
/// <remarks>
/// The parser is deliberately ordered from most-specific to least-specific match so that, for example,
/// a partially typed comparison value is detected before a bare path, and an explicit member access
/// (trailing dot) is detected before a generic "looks like a completed value" case. Path fragments are
/// resolved against the schema via the supplied <see cref="RulePathResolver"/> so the resulting context
/// carries the resolved node and its type, enabling type-aware operator and value suggestions.
/// </remarks>
public sealed class RuleCompletionContextParser
{
    private readonly IRuleOperatorProvider _operatorProvider;

    /// <summary>Creates a parser that uses the supplied operator provider for comparison-operator lookup.</summary>
    public RuleCompletionContextParser(IRuleOperatorProvider operatorProvider)
    {
        _operatorProvider = operatorProvider;
    }

    /// <summary>
    /// Analyzes the request expression up to the caret and produces the completion context.
    /// </summary>
    /// <param name="request">The completion request carrying the expression, caret position, and schema.</param>
    /// <param name="resolver">The path resolver used to map textual paths onto schema nodes.</param>
    /// <returns>
    /// A populated <see cref="RuleCompletionContext"/> describing the caret situation and the
    /// replacement span the editor should overwrite when inserting a suggestion.
    /// </returns>
    /// <remarks>
    /// The detection order is: completed comparison value → member access after a dot → member access
    /// with a partial member name → completed value (suggest logical connector) → after a logical
    /// connector (suggest a root symbol) → a resolvable leaf path (suggest an operator). When nothing
    /// matches, a root-symbol context with the typed prefix is returned as the fallback.
    /// </remarks>
    public RuleCompletionContext Parse(RuleCompletionRequest request, RulePathResolver resolver)
    {
        var expression = request.Expression ?? string.Empty;
        var cursor = Math.Clamp(request.CursorPosition, 0, expression.Length);
        var leftText = RuleExpressionScanner.SafeLeftText(expression, cursor);
        var typedPrefix = RuleExpressionScanner.GetTypedPrefix(leftText);
        var replacementStart = RuleExpressionScanner.GetReplacementStart(expression, cursor);

        if (RuleExpressionScanner.TryGetComparisonAtEnd(leftText, out var leftPath, out var opSymbol, out var valuePrefix))
        {
            var leftNode = resolver.Resolve(leftPath);
            var op = _operatorProvider
                .GetComparisonOperators(leftNode?.Type)
                .FirstOrDefault(x => string.Equals(x.Symbol, opSymbol, StringComparison.OrdinalIgnoreCase));

            return new RuleCompletionContext
            {
                Kind = RuleCompletionContextKind.Value,
                ExpressionBeforeCursor = leftText,
                TypedPrefix = valuePrefix,
                TargetPath = leftPath,
                LeftNode = leftNode,
                Operator = op,
                ExpectedValueType = leftNode?.Type,
                ReplacementStart = cursor - valuePrefix.Length,
                ReplacementLength = valuePrefix.Length,
                IsCompleteComparison = RuleExpressionScanner.LooksLikeCompletedValue(leftText)
            };
        }

        if (RuleExpressionScanner.EndsWithDot(leftText))
        {
            var target = leftText.TrimEnd('.');
            target = ExtractTrailingPath(target);

            var targetNode = resolver.Resolve(target);
            return new RuleCompletionContext
            {
                Kind = RuleCompletionContextKind.MemberAccess,
                ExpressionBeforeCursor = leftText,
                TypedPrefix = string.Empty,
                TargetPath = target,
                TargetNode = targetNode,
                LeftNode = targetNode,
                ReplacementStart = cursor,
                ReplacementLength = 0
            };
        }

        if (RuleExpressionScanner.TryGetMemberAccessTarget(leftText, out var memberTarget, out var memberPrefix))
        {
            var targetNode = resolver.Resolve(memberTarget);
            return new RuleCompletionContext
            {
                Kind = RuleCompletionContextKind.MemberAccess,
                ExpressionBeforeCursor = leftText,
                TypedPrefix = memberPrefix,
                TargetPath = memberTarget,
                TargetNode = targetNode,
                LeftNode = targetNode,
                ReplacementStart = cursor - memberPrefix.Length,
                ReplacementLength = memberPrefix.Length
            };
        }

        if (RuleExpressionScanner.LooksLikeCompletedValue(leftText))
        {
            return new RuleCompletionContext
            {
                Kind = RuleCompletionContextKind.LogicalConnector,
                ExpressionBeforeCursor = leftText,
                TypedPrefix = typedPrefix,
                ReplacementStart = replacementStart,
                ReplacementLength = typedPrefix.Length,
                IsCompleteComparison = true
            };
        }

        if (RuleExpressionScanner.IsAfterLogicalConnector(leftText))
        {
            return new RuleCompletionContext
            {
                Kind = RuleCompletionContextKind.RootSymbol,
                ExpressionBeforeCursor = leftText,
                TypedPrefix = string.Empty,
                ReplacementStart = cursor,
                ReplacementLength = 0
            };
        }

        if (RuleExpressionScanner.TryGetPathAtEnd(leftText, out var possiblePath))
        {
            var node = resolver.Resolve(possiblePath);
            if (node is not null && node != request.Schema.Root && !node.HasChildren)
            {
                return new RuleCompletionContext
                {
                    Kind = RuleCompletionContextKind.Operator,
                    ExpressionBeforeCursor = leftText,
                    TypedPrefix = string.Empty,
                    TargetPath = possiblePath,
                    TargetNode = node,
                    LeftNode = node,
                    ExpectedValueType = node.Type,
                    ReplacementStart = cursor,
                    ReplacementLength = 0
                };
            }
        }

        return new RuleCompletionContext
        {
            Kind = RuleCompletionContextKind.RootSymbol,
            ExpressionBeforeCursor = leftText,
            TypedPrefix = typedPrefix,
            ReplacementStart = replacementStart,
            ReplacementLength = typedPrefix.Length
        };
    }

    private static string ExtractTrailingPath(string text)
    {
        var parts = text.Split([' ', '\t', '\r', '\n', '(', ')'], StringSplitOptions.RemoveEmptyEntries);
        return parts.LastOrDefault() ?? string.Empty;
    }
}
