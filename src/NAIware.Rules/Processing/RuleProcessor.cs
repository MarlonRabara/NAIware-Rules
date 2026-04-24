using NAIware.Rules.Models;
using NAIware.Rules.Runtime;

namespace NAIware.Rules.Processing;

/// <summary>
/// A high-level rule processor that evaluates catalog-defined rules against input objects.
/// <para>
/// The processor automatically resolves the <see cref="RuleContext"/> from the input object's type,
/// extracts parameters via <see cref="ParameterFactory"/>, loads expressions from the catalog,
/// evaluates them using the existing <see cref="Rules.Engine"/>, and maps the results to a
/// structured <see cref="RuleEvaluationResult"/>.
/// </para>
/// </summary>
/// <remarks>
/// This processor sits above the existing engine layer and does not modify the engine's behavior.
/// The existing <c>Rules.Engine.AddRule()</c> / <c>Execute()</c> pattern continues to work unchanged.
/// </remarks>
public class RuleProcessor : IRuleProcessor
{
    private readonly IRuleContextResolver _resolver;

    /// <summary>
    /// Creates a rule processor with the specified context resolver.
    /// </summary>
    /// <param name="resolver">The resolver used to match input objects to rule contexts.</param>
    public RuleProcessor(IRuleContextResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolver = resolver;
    }

    /// <summary>
    /// Creates a rule processor backed by a <see cref="ReflectionRuleContextResolver"/>
    /// for the specified rules library.
    /// </summary>
    /// <param name="library">The rules library containing context definitions.</param>
    public RuleProcessor(RulesLibrary library)
        : this(new ReflectionRuleContextResolver(library))
    {
    }

    /// <inheritdoc/>
    public RuleEvaluationResult Evaluate(RuleEvaluationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Resolve context from input object
        RuleContext context = _resolver.Resolve(request.InputObject)
            ?? throw new InvalidOperationException(
                $"No rule context found for type '{request.InputObject.GetType().FullName}'. " +
                "Ensure a RuleContext with a matching QualifiedTypeName is registered in the library.");

        // 2. Determine which expressions to evaluate
        IEnumerable<RuleExpression> expressions = ResolveExpressions(context, request.CategoryName);

        // 3. Extract parameters from the input object
        var factory = new ParameterFactory();
        Parameters? extractedParams = factory.CreateParameters(request.InputObject);
        Parameters runtimeParams = extractedParams ?? new Parameters();

        // 4. Evaluate each expression individually and collect results
        var result = new RuleEvaluationResult(context.Name, request.CategoryName);

        foreach (RuleExpression ruleExpression in expressions)
        {
            var expressionResult = EvaluateExpression(
                ruleExpression,
                runtimeParams,
                request.IncludeDiagnostics);

            if (expressionResult.Matched)
                result.Matches.Add(expressionResult);
            else
                result.Mismatches.Add(expressionResult);
        }

        return result;
    }

    private static IEnumerable<RuleExpression> ResolveExpressions(RuleContext context, string? categoryName)
    {
        if (string.IsNullOrEmpty(categoryName))
        {
            // No category specified — evaluate all active expressions in the context
            return context.Expressions.Where(e => e.IsActive);
        }

        RuleCategory? category = context.FindCategoryByName(categoryName);
        if (category is null)
        {
            throw new InvalidOperationException(
                $"Rule category '{categoryName}' not found in context '{context.Name}'.");
        }

        // Include expressions from nested subcategories so a parent category
        // selection evaluates the entire subtree.
        return category.GetAllActiveExpressions();
    }

    private static RuleExpressionResult EvaluateExpression(
        RuleExpression ruleExpression,
        Parameters runtimeParams,
        bool includeDiagnostics)
    {
        // Create a fresh engine for this expression
        var engine = new Rules.Engine();
        engine.Parameters.Add(runtimeParams);
        engine.AddRule(ruleExpression.Expression, ruleExpression.Name);

        List<Identification> engineResults = engine.Execute();
        bool matched = engineResults.Exists(r => r.Name == ruleExpression.Name);

        if (matched)
        {
            return new RuleExpressionResult(
                ruleExpression.Identity,
                ruleExpression.Name,
                ruleExpression.Version,
                matched: true,
                result: ruleExpression.ResultDefinition);
        }

        // Not matched — optionally produce diagnostics
        RuleMismatchDiagnostic? diagnostic = null;

        if (includeDiagnostics)
        {
            diagnostic = new RuleMismatchDiagnostic(ruleExpression.Expression);

            foreach (var kvp in runtimeParams)
            {
                diagnostic.EvaluatedParameters[kvp.Key] = kvp.Value.Value?.ToString();
            }

            diagnostic.Explanation =
                $"Expression '{ruleExpression.Expression}' evaluated to false against the provided parameters.";
        }

        return new RuleExpressionResult(
            ruleExpression.Identity,
            ruleExpression.Name,
            ruleExpression.Version,
            matched: false,
            diagnostic: diagnostic);
    }
}
