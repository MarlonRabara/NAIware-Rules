namespace NAIware.RuleIntelligence;

public sealed class RuleCompletionContextParser
{
    private readonly IRuleOperatorProvider _operatorProvider;

    public RuleCompletionContextParser(IRuleOperatorProvider operatorProvider)
    {
        _operatorProvider = operatorProvider;
    }

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
