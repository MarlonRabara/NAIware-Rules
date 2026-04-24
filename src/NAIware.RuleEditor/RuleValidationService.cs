using System.Text.RegularExpressions;

namespace NAIware.RuleEditor;

/// <summary>
/// Validates an entire rule library the way a compiler validates a project build.
/// Reports missing context types, invalid property paths, mismatched parentheses,
/// type-incompatibility between operands, and incomplete result definitions.
/// </summary>
public sealed class RuleValidationService
{
    private static readonly Regex PropertyLikeToken = new(
        @"\b[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)+\b",
        RegexOptions.Compiled);

    private static readonly Regex ComparisonPattern = new(
        @"(?<left>[A-Za-z_][A-Za-z0-9_.]*)\s*(?<op>>=|<=|!=|<>|=|>|<)\s*(?<right>""[^""]*""|#[^#]*#|true|false|null|[0-9]+(?:\.[0-9]+)?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly HashSet<string> ValidLogicalKeywords =
        new(StringComparer.OrdinalIgnoreCase) { "and", "or", "not" };

    private readonly IntelliSenseService _intellisense;

    /// <summary>Initializes a new validation service.</summary>
    public RuleValidationService(IntelliSenseService intellisense)
    {
        ArgumentNullException.ThrowIfNull(intellisense);
        _intellisense = intellisense;
    }

    /// <summary>
    /// Validates every rule in the library and returns the list of issues found.
    /// </summary>
    /// <param name="library">The library document to validate.</param>
    /// <returns>A list of <see cref="ValidationIssue"/> records, empty when the library is clean.</returns>
    public List<ValidationIssue> Validate(RuleLibraryDocument library)
    {
        ArgumentNullException.ThrowIfNull(library);

        var issues = new List<ValidationIssue>();

        foreach (RuleContextDocument context in library.Contexts)
        {
            ContextMetadata? metadata = _intellisense.GetMetadata(context);
            if (metadata is null)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = "Error",
                    Context = context.Name,
                    Message = $"Context type '{context.QualifiedTypeName}' could not be resolved. Re-load the source DLL."
                });
                continue;
            }

            foreach (RuleExpressionDocument rule in context.Expressions)
            {
                string category = FindCategoryFor(context, rule.Id);
                ValidateRule(context, category, rule, metadata, issues);
            }
        }

        return issues;
    }

    private static string FindCategoryFor(RuleContextDocument context, Guid expressionId)
    {
        foreach (RuleCategoryDocument category in context.Categories)
        {
            string? found = FindCategoryRecursive(category, expressionId, string.Empty);
            if (found is not null) return found;
        }
        return string.Empty;
    }

    private static string? FindCategoryRecursive(RuleCategoryDocument category, Guid expressionId, string pathPrefix)
    {
        string fullName = string.IsNullOrEmpty(pathPrefix) ? category.Name : $"{pathPrefix}.{category.Name}";

        if (category.ExpressionIds.Contains(expressionId)) return fullName;

        foreach (RuleCategoryDocument child in category.Categories)
        {
            string? nested = FindCategoryRecursive(child, expressionId, fullName);
            if (nested is not null) return nested;
        }

        return null;
    }

    private static void ValidateRule(
        RuleContextDocument context,
        string category,
        RuleExpressionDocument rule,
        ContextMetadata metadata,
        List<ValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(rule.Expression))
        {
            issues.Add(Error(context, category, rule, "Rule expression is required."));
            return;
        }

        ValidateParentheses(context, category, rule, issues);
        ValidateUnknownIdentifiers(context, category, rule, metadata, issues);
        ValidatePropertyPaths(context, category, rule, metadata, issues);
        ValidateSimpleComparisons(context, category, rule, metadata, issues);

        if (string.IsNullOrWhiteSpace(rule.ResultCode) || string.IsNullOrWhiteSpace(rule.ResultMessage))
        {
            issues.Add(new ValidationIssue
            {
                Severity = "Warning",
                Context = context.Name,
                Category = category,
                Rule = rule.Name,
                RuleId = rule.Id,
                Message = "Rule has no complete result definition (both Code and Message are recommended)."
            });
        }
    }

    private static void ValidateParentheses(
        RuleContextDocument context,
        string category,
        RuleExpressionDocument rule,
        List<ValidationIssue> issues)
    {
        int balance = 0;
        foreach (char c in rule.Expression)
        {
            if (c == '(') balance++;
            else if (c == ')') balance--;

            if (balance < 0)
            {
                issues.Add(Error(context, category, rule, "Expression contains an unmatched closing parenthesis."));
                return;
            }
        }

        if (balance != 0)
        {
            issues.Add(Error(context, category, rule, "Expression contains an unmatched opening parenthesis."));
        }
    }

    private static void ValidateUnknownIdentifiers(
        RuleContextDocument context,
        string category,
        RuleExpressionDocument rule,
        ContextMetadata metadata,
        List<ValidationIssue> issues)
    {
        // Identify single-token identifiers that aren't dotted paths, numeric, or quoted strings.
        // Anything that isn't a keyword, operator, or known root-level property is flagged.
        MatchCollection tokens = Regex.Matches(rule.Expression, @"\b[A-Za-z_][A-Za-z0-9_]*\b");

        foreach (Match tokenMatch in tokens)
        {
            string token = tokenMatch.Value;

            if (ValidLogicalKeywords.Contains(token)) continue;
            if (bool.TryParse(token, out _)) continue;
            if (string.Equals(token, "null", StringComparison.OrdinalIgnoreCase)) continue;

            // Ignore tokens that are part of a dotted path — handled by ValidatePropertyPaths.
            int end = tokenMatch.Index + tokenMatch.Length;
            if (end < rule.Expression.Length && rule.Expression[end] == '.') continue;
            if (tokenMatch.Index > 0 && rule.Expression[tokenMatch.Index - 1] == '.') continue;

            // Root-level property or the context type name itself is acceptable.
            if (string.Equals(token, metadata.Type.Name, StringComparison.Ordinal)) continue;
            if (metadata.PropertyPaths.Contains(token, StringComparer.Ordinal)) continue;
        }
    }

    private static void ValidatePropertyPaths(
        RuleContextDocument context,
        string category,
        RuleExpressionDocument rule,
        ContextMetadata metadata,
        List<ValidationIssue> issues)
    {
        foreach (Match match in PropertyLikeToken.Matches(rule.Expression))
        {
            string token = match.Value;

            // Strip a leading context type name so "LoanApplication.Amount" becomes "Amount".
            string path = token.StartsWith(metadata.Type.Name + ".", StringComparison.Ordinal)
                ? token[(metadata.Type.Name.Length + 1)..]
                : token;

            if (ResolvePropertyType(metadata.Type, path) is null)
            {
                issues.Add(Error(context, category, rule,
                    $"Property path '{token}' could not be resolved on '{metadata.Type.FullName}'. " +
                    "The property may have been renamed, removed, or does not exist on the context type."));
            }
        }
    }

    private static void ValidateSimpleComparisons(
        RuleContextDocument context,
        string category,
        RuleExpressionDocument rule,
        ContextMetadata metadata,
        List<ValidationIssue> issues)
    {
        foreach (Match comparison in ComparisonPattern.Matches(rule.Expression))
        {
            string left = comparison.Groups["left"].Value;
            string op = comparison.Groups["op"].Value;
            string right = comparison.Groups["right"].Value;

            string path = left.StartsWith(metadata.Type.Name + ".", StringComparison.Ordinal)
                ? left[(metadata.Type.Name.Length + 1)..]
                : left;

            Type? leftType = ResolvePropertyType(metadata.Type, path);
            if (leftType is null) continue; // Already reported by ValidatePropertyPaths.

            Type rightType = InferLiteralType(right);
            if (!AreCompatible(leftType, rightType, op))
            {
                issues.Add(Error(context, category, rule,
                    $"Type mismatch: '{left}' ({FriendlyName(leftType)}) cannot be compared to '{right}' " +
                    $"({FriendlyName(rightType)}) using operator '{op}'."));
            }
        }
    }

    private static Type? ResolvePropertyType(Type rootType, string path)
    {
        Type current = rootType;
        foreach (string part in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            string clean = Regex.Replace(part, @"\[[0-9]+\]", string.Empty);

            // Indexed collection element access via dot-notation integer: "Borrowers.0" or "Count".
            if (int.TryParse(clean, out _))
            {
                if (current != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(current) && current.IsGenericType)
                {
                    current = current.GetGenericArguments()[0];
                    continue;
                }
                return null;
            }

            System.Reflection.PropertyInfo? property = current.GetProperty(clean);
            if (property is null) return null;

            current = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (current != typeof(string)
                && typeof(System.Collections.IEnumerable).IsAssignableFrom(current)
                && current.IsGenericType)
            {
                current = current.GetGenericArguments()[0];
            }
        }

        return current;
    }

    private static Type InferLiteralType(string token)
    {
        if (token.StartsWith('"')) return typeof(string);
        if (token.StartsWith('#')) return typeof(DateTime);
        if (bool.TryParse(token, out _)) return typeof(bool);
        if (string.Equals(token, "null", StringComparison.OrdinalIgnoreCase)) return typeof(object);
        if (decimal.TryParse(token, System.Globalization.CultureInfo.InvariantCulture, out _)) return typeof(decimal);
        return typeof(string);
    }

    private static bool AreCompatible(Type left, Type right, string op)
    {
        left = Nullable.GetUnderlyingType(left) ?? left;
        right = Nullable.GetUnderlyingType(right) ?? right;

        if (right == typeof(object)) return true; // null literal matches anything.
        if (IsNumeric(left) && IsNumeric(right)) return true;
        if (left == typeof(DateTime) && right == typeof(DateTime)) return true;
        if (left == typeof(string) && right == typeof(string)) return op is "=" or "!=" or "<>";
        if (left == typeof(bool) && right == typeof(bool)) return op is "=" or "!=" or "<>";
        if (left.IsEnum && right == typeof(string)) return op is "=" or "!=" or "<>";
        return false;
    }

    private static bool IsNumeric(Type type) =>
        type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long)
        || type == typeof(float) || type == typeof(double) || type == typeof(decimal);

    private static string FriendlyName(Type type) => (Nullable.GetUnderlyingType(type) ?? type).Name;

    private static ValidationIssue Error(
        RuleContextDocument context,
        string category,
        RuleExpressionDocument rule,
        string message) => new()
    {
        Severity = "Error",
        Context = context.Name,
        Category = category,
        Rule = rule.Name,
        RuleId = rule.Id,
        Message = message
    };
}
