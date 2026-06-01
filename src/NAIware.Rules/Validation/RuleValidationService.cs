using System.Text.RegularExpressions;
using NAIware.Rules.Models;

namespace NAIware.Rules.Validation;

/// <summary>
/// Validates rule libraries and individual draft expressions the way a compiler validates a build.
/// Reports missing context types, invalid property paths, mismatched parentheses,
/// type-incompatibility between operands, and incomplete result definitions.
/// </summary>
/// <remarks>
/// The service is host-neutral: it depends only on <see cref="IContextMetadataProvider"/> to resolve
/// a context's reflected <see cref="ContextMetadata"/>. The Rule Editor and the Rule Service each
/// supply their own provider, so the same validation logic runs design-time in the editor and over
/// HTTP in the service.
/// </remarks>
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

    private readonly IContextMetadataProvider _metadataProvider;

    /// <summary>Initializes a new validation service.</summary>
    /// <param name="metadataProvider">The host-supplied provider that resolves context metadata.</param>
    public RuleValidationService(IContextMetadataProvider metadataProvider)
    {
        ArgumentNullException.ThrowIfNull(metadataProvider);
        _metadataProvider = metadataProvider;
    }

    /// <summary>
    /// Validates every rule in the library and returns the list of issues found.
    /// </summary>
    /// <param name="library">The library document to validate.</param>
    /// <returns>A list of <see cref="ValidationIssue"/> records, empty when the library is clean.</returns>
    public List<ValidationIssue> Validate(RulesLibrary library)
    {
        ArgumentNullException.ThrowIfNull(library);

        var issues = new List<ValidationIssue>();

        foreach (RuleContext context in library.Contexts)
        {
            ContextMetadata? metadata = _metadataProvider.GetMetadata(context);
            if (metadata is null)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = "Error",
                    Context = context.Name,
                    Message = DescribeUnresolvedContext(context)
                });
                continue;
            }

            foreach (RuleExpression rule in context.Expressions)
            {
                string category = FindCategoryFor(context, rule.Id);
                ValidateRule(context, category, rule, metadata, issues);
            }
        }

        return issues;
    }

    /// <summary>
    /// Validates a single draft expression against the supplied context without requiring it to be
    /// attached to a library. This is the entry point for authoring scenarios where a formula is
    /// drafted and checked before it is saved — for example, the Rule Service's validation endpoint.
    /// </summary>
    /// <param name="context">The context the expression is authored against.</param>
    /// <param name="expression">The draft expression text.</param>
    /// <param name="resultCode">Optional result code; when supplied with a message, suppresses the incomplete-result warning.</param>
    /// <param name="resultMessage">Optional result message; when supplied with a code, suppresses the incomplete-result warning.</param>
    /// <param name="ruleName">Optional display name used to label issues.</param>
    /// <returns>A list of <see cref="ValidationIssue"/> records, empty when the draft is clean.</returns>
    public List<ValidationIssue> ValidateExpression(
        RuleContext context,
        string expression,
        string? resultCode = null,
        string? resultMessage = null,
        string? ruleName = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        var issues = new List<ValidationIssue>();

        ContextMetadata? metadata = _metadataProvider.GetMetadata(context);
        if (metadata is null)
        {
            issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Context = context.Name,
                Message = DescribeUnresolvedContext(context)
            });
            return issues;
        }

        var rule = new RuleExpression
        {
            Name = string.IsNullOrWhiteSpace(ruleName) ? "(draft)" : ruleName,
            Expression = expression ?? string.Empty,
            ResultCode = resultCode,
            ResultMessage = resultMessage
        };

        ValidateRule(context, category: string.Empty, rule, metadata, issues);
        return issues;
    }

    /// <summary>
    /// Produces a precise diagnostic explaining why a context type could not be resolved,
    /// distinguishing missing configuration, a missing DLL on disk, and a type that is
    /// absent from an otherwise-loadable assembly. This is far more actionable than a
    /// generic "could not be resolved" message.
    /// </summary>
    private static string DescribeUnresolvedContext(RuleContext context)
    {
        string typeName = string.IsNullOrWhiteSpace(context.QualifiedTypeName)
            ? "(unspecified)"
            : context.QualifiedTypeName;

        string? assemblyPath = context.SourceAssemblyPath;

        if (string.IsNullOrWhiteSpace(assemblyPath))
        {
            return $"Context type '{typeName}' could not be resolved because no source assembly is " +
                   "configured for this context. Open the context and re-select the model DLL.";
        }

        if (!File.Exists(assemblyPath))
        {
            return $"Context type '{typeName}' could not be resolved because the source assembly was " +
                   $"not found at '{assemblyPath}'. The DLL may have moved; re-select the model DLL " +
                   "or restore it to that path.";
        }

        return $"Context type '{typeName}' was not found in assembly '{assemblyPath}'. The type may have " +
               "been renamed, removed, or the qualified type name is incorrect. Re-select the model type.";
    }

    private static string FindCategoryFor(RuleContext context, Guid expressionId)
    {
        foreach (RuleCategory category in context.Categories)
        {
            string? found = FindCategoryRecursive(category, expressionId, string.Empty);
            if (found is not null) return found;
        }
        return string.Empty;
    }

    private static string? FindCategoryRecursive(RuleCategory category, Guid expressionId, string pathPrefix)
    {
        string fullName = string.IsNullOrEmpty(pathPrefix) ? category.Name : $"{pathPrefix}.{category.Name}";

        if (category.ExpressionIds.Contains(expressionId)) return fullName;

        foreach (RuleCategory child in category.Categories)
        {
            string? nested = FindCategoryRecursive(child, expressionId, fullName);
            if (nested is not null) return nested;
        }

        return null;
    }

    private static void ValidateRule(
        RuleContext context,
        string category,
        RuleExpression rule,
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
        RuleContext context,
        string category,
        RuleExpression rule,
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
        RuleContext context,
        string category,
        RuleExpression rule,
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
        RuleContext context,
        string category,
        RuleExpression rule,
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

            // Enum literals (e.g. "LoanPurposeType.Refinance") appear as comparison operands, not
            // property paths. The runtime engine resolves these via GenericEnumeration, so the
            // validator must recognize them too rather than treating them as unresolved properties.
            if (IsEnumLiteral(metadata.Type, token))
                continue;

            if (ResolvePropertyType(metadata.Type, path, context.Name) is null)
            {
                issues.Add(Error(context, category, rule,
                    $"Property path '{token}' could not be resolved on '{metadata.Type.FullName}'. " +
                    "The property may have been renamed, removed, or does not exist on the context type."));
            }
        }
    }

    private static void ValidateSimpleComparisons(
        RuleContext context,
        string category,
        RuleExpression rule,
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

            Type? leftType = ResolvePropertyType(metadata.Type, path, context.Name);
            if (leftType is null) continue; // Already reported by ValidatePropertyPaths.

            // An enum literal right operand (e.g. "LoanPurposeType.Refinance") is type-compatible when
            // the left operand is the matching enum. The ComparisonPattern does not capture dotted
            // right operands, so this is handled here against the qualified-enum form.
            Type? leftEnum = Nullable.GetUnderlyingType(leftType) ?? leftType;
            if (leftEnum.IsEnum
                && right.StartsWith(leftEnum.Name + ".", StringComparison.Ordinal)
                && op is "=" or "!=" or "<>")
            {
                continue;
            }

            Type rightType = InferLiteralType(right);
            if (!AreCompatible(leftType, rightType, op))
            {
                issues.Add(Error(context, category, rule,
                    $"Type mismatch: '{left}' ({FriendlyName(leftType)}) cannot be compared to '{right}' " +
                    $"({FriendlyName(rightType)}) using operator '{op}'."));
            }
        }
    }

    /// <summary>
    /// Determines whether a dotted token is a qualified enum literal (e.g. <c>LoanPurposeType.Refinance</c>)
    /// reachable from the model's property graph. This mirrors how the runtime engine resolves enum
    /// operands via <c>GenericEnumeration</c>, so the validator does not misreport them as property paths.
    /// </summary>
    private static bool IsEnumLiteral(Type rootType, string token)
    {
        int lastDot = token.LastIndexOf('.');
        if (lastDot <= 0) return false;

        string typeName = token[..lastDot];
        string memberName = token[(lastDot + 1)..];

        // The qualifier must be a single, simple type name (no further dots) and the member must be a
        // defined value of an enum type used somewhere in the model graph.
        if (typeName.Contains('.')) return false;

        foreach (Type enumType in EnumerateModelEnumTypes(rootType, new HashSet<Type>()))
        {
            if (string.Equals(enumType.Name, typeName, StringComparison.Ordinal)
                && Enum.IsDefined(enumType, memberName))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Walks the model's property graph (to a bounded depth) and yields every distinct enum type it
    /// references, including enum element types of generic collections.
    /// </summary>
    private static IEnumerable<Type> EnumerateModelEnumTypes(Type rootType, HashSet<Type> visited, int depth = 0)
    {
        if (depth > 8 || !visited.Add(rootType)) yield break;

        foreach (System.Reflection.PropertyInfo property in rootType.GetProperties())
        {
            Type propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            // Unwrap generic collection element types so enums inside lists are discovered.
            if (propertyType != typeof(string)
                && typeof(System.Collections.IEnumerable).IsAssignableFrom(propertyType)
                && propertyType.IsGenericType)
            {
                propertyType = propertyType.GetGenericArguments()[0];
            }

            if (propertyType.IsEnum)
            {
                yield return propertyType;
            }
            else if (!propertyType.IsPrimitive
                     && propertyType != typeof(string)
                     && propertyType != typeof(decimal)
                     && propertyType != typeof(DateTime)
                     && propertyType != typeof(Guid)
                     && !propertyType.IsValueType)
            {
                foreach (Type nested in EnumerateModelEnumTypes(propertyType, visited, depth + 1))
                    yield return nested;
            }
        }
    }

    private static Type? ResolvePropertyType(Type rootType, string path, string instanceName = "")
    {
        Type current = rootType;
        foreach (string part in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            string cleanPropertyPath = Regex.Replace(part, @"\[[0-9]+\]", string.Empty);

            if (cleanPropertyPath == instanceName)
            {
                // this is the root type instance name.
                continue;
            }

            // Determine whether the current type is a generic collection. Unwrapping is deferred to
            // here (rather than eagerly at the end of the previous iteration) so collection-level
            // members such as ".Count" remain resolvable against the collection itself. This mirrors
            // ParameterFactory, which emits a "{collection}.Count" parameter and "{collection}.{i}.{member}"
            // element parameters.
            bool currentIsCollection = current != typeof(string)
                && typeof(System.Collections.IEnumerable).IsAssignableFrom(current)
                && current.IsGenericType;

            // Collection-level "Count" maps to the synthetic int parameter created by ParameterFactory.
            // Resolve it directly to avoid interface-inheritance reflection quirks (Count is declared on
            // IReadOnlyCollection<T>/ICollection<T>, not always discoverable via GetProperty on the
            // declared interface type).
            if (currentIsCollection && string.Equals(cleanPropertyPath, "Count", StringComparison.Ordinal))
            {
                current = typeof(int);
                continue;
            }

            // Indexed collection element access via dot-notation integer: "Borrowers.0".
            if (int.TryParse(cleanPropertyPath, out _))
            {
                if (currentIsCollection)
                {
                    current = current.GetGenericArguments()[0];
                    continue;
                }
                return null;
            }

            // A member name following a collection (without an index) resolves against the element type,
            // e.g. "Borrowers.FirstName".
            if (currentIsCollection)
            {
                current = current.GetGenericArguments()[0];
            }

            System.Reflection.PropertyInfo? property = current.GetProperty(cleanPropertyPath);
            if (property is null) return null;

            current = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
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
        RuleContext context,
        string category,
        RuleExpression rule,
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
