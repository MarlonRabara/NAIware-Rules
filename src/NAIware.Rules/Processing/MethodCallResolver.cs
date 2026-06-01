using System.Globalization;
using NAIware.Rules.MethodWrappers;

namespace NAIware.Rules.Processing;

/// <summary>
/// Result of resolving formula method calls within a rule expression: the rewritten, method-free
/// expression and the parameter set augmented with one synthetic, strongly-typed parameter per
/// resolved method call.
/// </summary>
/// <param name="Expression">The expression with every method call replaced by a synthetic parameter token.</param>
/// <param name="Parameters">The original parameters plus the synthetic parameters created for method results.</param>
public readonly record struct MethodResolutionResult(string Expression, Parameters Parameters);

/// <summary>
/// Pre-evaluates formula method-wrapper calls (for example <c>LEFT(PrimaryBorrower.FirstName, 4)</c>) inside a
/// rule expression and rewrites them into strongly-typed synthetic parameters.
/// </summary>
/// <remarks>
/// <para>
/// The boolean <see cref="Rules.Engine"/> used by <see cref="RuleProcessor"/> has no knowledge of method
/// wrappers, and the numeric <see cref="Formulae.Engine"/> coerces every operand to <see cref="decimal"/>,
/// so an expression such as <c>LEFT(PrimaryBorrower.FirstName, 4) = "Marl"</c> cannot be evaluated by either
/// engine directly. This resolver bridges that gap: it locates method calls, evaluates them with the supplied
/// <see cref="MethodMap"/> using the real argument values (resolving property paths from the parameter set),
/// and substitutes each result as a synthetic parameter whose CLR type matches the method result. The
/// remaining expression contains only parameters, literals, and operators — exactly what
/// <see cref="Rules.Engine"/> already evaluates correctly for strings, numbers, dates, and booleans.
/// </para>
/// <para>
/// Nested calls are evaluated innermost-first, so by the time an outer call is processed its inner calls have
/// already collapsed to single synthetic-parameter tokens.
/// </para>
/// </remarks>
public sealed class MethodCallResolver
{
    private const string SyntheticParameterPrefix = "mwr";

    private readonly MethodMap _methodMap;

    /// <summary>Creates a resolver backed by the default formula method wrappers.</summary>
    public MethodCallResolver()
        : this(DefaultMethodWrapperRegistration.CreateDefaultMethodMap())
    {
    }

    /// <summary>Creates a resolver backed by the supplied method map.</summary>
    /// <param name="methodMap">The method wrappers available for resolution. Must not be <see langword="null"/>.</param>
    public MethodCallResolver(MethodMap methodMap)
    {
        ArgumentNullException.ThrowIfNull(methodMap);
        _methodMap = methodMap;
    }

    /// <summary>
    /// Resolves every method call in <paramref name="expression"/> and returns the rewritten expression
    /// together with an augmented parameter set. When the expression contains no method calls the original
    /// expression and a copy of the parameters are returned unchanged.
    /// </summary>
    /// <param name="expression">The rule expression to process.</param>
    /// <param name="parameters">The parameters extracted from the input model. May be <see langword="null"/>.</param>
    public MethodResolutionResult Resolve(string expression, Parameters? parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);

        var effectiveParameters = new Parameters();
        effectiveParameters.Add(parameters);

        if (_methodMap.Count == 0)
            return new MethodResolutionResult(expression, effectiveParameters);

        List<string> tokens = Helper.GetTokens(Helper.ValidateExpression(expression));

        if (!ContainsMethodToken(tokens))
            return new MethodResolutionResult(expression, effectiveParameters);

        int syntheticCounter = 0;

        // Repeatedly collapse the innermost method call until none remain.
        while (TryFindInnermostMethod(tokens, out int methodIndex, out int closeIndex))
        {
            // methodIndex -> function name, methodIndex + 1 -> '(', so arguments start at methodIndex + 2.
            List<List<string>> argumentTokenLists = ExtractArguments(tokens, methodIndex + 2, closeIndex);

            object[] argumentValues = argumentTokenLists
                .Select(argTokens => EvaluateArgument(argTokens, effectiveParameters))
                .ToArray()!;

            IMethodWrapper wrapper = _methodMap[tokens[methodIndex]];
            object? result = wrapper.ExecuteMethod(argumentValues);

            string syntheticName = CreateUniqueParameterName(ref syntheticCounter, tokens, effectiveParameters);
            effectiveParameters[syntheticName] = CreateSyntheticParameter(syntheticName, result);

            // Replace [methodIndex .. closeIndex] with the synthetic parameter token.
            tokens.RemoveRange(methodIndex, closeIndex - methodIndex + 1);
            tokens.Insert(methodIndex, syntheticName);
        }

        string rewritten = Reconstruct(tokens);
        return new MethodResolutionResult(rewritten, effectiveParameters);
    }

    private bool ContainsMethodToken(List<string> tokens) =>
        tokens.Any(token => _methodMap.ContainsKey(token));

    /// <summary>
    /// Finds the first method call whose argument list contains no further method calls (the innermost call).
    /// </summary>
    private bool TryFindInnermostMethod(List<string> tokens, out int methodIndex, out int closeIndex)
    {
        for (int i = 0; i < tokens.Count; i++)
        {
            if (!_methodMap.ContainsKey(tokens[i]))
                continue;

            if (i + 1 >= tokens.Count || tokens[i + 1] != "(")
                continue;

            int matchingClose = FindMatchingParenthesis(tokens, i + 1);
            if (matchingClose < 0)
                continue;

            bool hasNestedMethod = false;
            for (int j = i + 2; j < matchingClose; j++)
            {
                if (_methodMap.ContainsKey(tokens[j]))
                {
                    hasNestedMethod = true;
                    break;
                }
            }

            if (!hasNestedMethod)
            {
                methodIndex = i;
                closeIndex = matchingClose;
                return true;
            }
        }

        methodIndex = -1;
        closeIndex = -1;
        return false;
    }

    private static int FindMatchingParenthesis(List<string> tokens, int openIndex)
    {
        int depth = 0;
        for (int i = openIndex; i < tokens.Count; i++)
        {
            if (tokens[i] == "(") depth++;
            else if (tokens[i] == ")")
            {
                depth--;
                if (depth == 0) return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Splits the token range between an opening and closing parenthesis into top-level argument token lists,
    /// honoring nested parentheses so that commas inside a sub-group do not split arguments.
    /// </summary>
    private static List<List<string>> ExtractArguments(List<string> tokens, int firstArgIndex, int closeIndex)
    {
        var arguments = new List<List<string>>();

        if (firstArgIndex >= closeIndex)
            return arguments;

        var current = new List<string>();
        int depth = 0;

        for (int i = firstArgIndex; i < closeIndex; i++)
        {
            string token = tokens[i];

            if (token == "(") depth++;
            else if (token == ")") depth--;

            if (token == "," && depth == 0)
            {
                arguments.Add(current);
                current = [];
                continue;
            }

            current.Add(token);
        }

        arguments.Add(current);
        return arguments;
    }

    /// <summary>
    /// Evaluates a single argument token list to a CLR value. Single tokens resolve to a parameter value,
    /// string literal, date literal, or number; multi-token arguments are treated as arithmetic and evaluated
    /// through the formula engine.
    /// </summary>
    private object? EvaluateArgument(List<string> argumentTokens, Parameters parameters)
    {
        if (argumentTokens.Count == 0)
            return string.Empty;

        if (argumentTokens.Count == 1)
            return ResolveAtom(argumentTokens[0], parameters);

        // Multi-token argument (for example, MIN(a + b, c)). Delegate arithmetic to the formula engine.
        var formulaEngine = new Formulae.Engine();
        formulaEngine.Parameters.Add(parameters);
        Formulae.FormulaTree? tree = formulaEngine.Parse([.. argumentTokens]);
        return tree?.Evaluate();
    }

    private static object? ResolveAtom(string token, Parameters parameters)
    {
        if (parameters.ContainsKey(token))
            return parameters[token].Value;

        if (token.Length >= 2 && token[0] == '"' && token[^1] == '"')
            return token.Length == 2 ? string.Empty : token[1..^1];

        if (token.Length >= 2 && token[0] == '#' && token[^1] == '#'
            && DateTime.TryParse(token[1..^1], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            return date;

        if (decimal.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal number))
            return number;

        // Bare word that is neither a parameter nor a recognized literal — treat as a string literal.
        return token;
    }

    private static string CreateUniqueParameterName(ref int counter, List<string> tokens, Parameters parameters)
    {
        string name;
        do
        {
            name = $"{SyntheticParameterPrefix}{counter++}";
        }
        while (parameters.ContainsKey(name) || tokens.Contains(name));

        return name;
    }

    /// <summary>
    /// Wraps a method result in a strongly-typed <see cref="GenericParameter{V}"/> so that
    /// <see cref="Rules.Engine"/> infers the correct comparison type (string, number, date, or boolean).
    /// </summary>
    private static IParameter CreateSyntheticParameter(string name, object? value)
    {
        Type valueType = value?.GetType() ?? typeof(string);
        Type parameterType = typeof(GenericParameter<>).MakeGenericType(valueType);

        var parameter = (IParameter)Activator.CreateInstance(parameterType, name, name)!;
        if (value is not null)
            ((IValue)parameter).Value = value;

        return parameter;
    }

    private static string Reconstruct(List<string> tokens) => string.Join(' ', tokens);
}
