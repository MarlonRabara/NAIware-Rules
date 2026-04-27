using NAIware.Rules.Models;
using NAIware.Rules.Runtime;

namespace NAIware.Rules.Processing;

/// <summary>
/// A high-level rule processor that evaluates library-defined rules against input objects.
/// <para>
/// The processor automatically resolves the <see cref="RuleContext"/> from the input object's type,
/// extracts parameters via <see cref="ParameterFactory"/>, loads expressions from the library,
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
    private readonly RulesLibrary? _library;

    /// <summary>
    /// Creates a rule processor with the specified context resolver.
    /// </summary>
    /// <param name="resolver">The resolver used to match input objects to rule contexts.</param>
    public RuleProcessor(IRuleContextResolver resolver)
        : this(resolver, library: null)
    {
    }

    private RuleProcessor(IRuleContextResolver resolver, RulesLibrary? library)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolver = resolver;
        _library = library;
    }

    /// <summary>
    /// Creates a rule processor backed by a <see cref="ReflectionRuleContextResolver"/>
    /// for the specified rules library.
    /// </summary>
    /// <param name="library">The rules library containing context definitions.</param>
    public RuleProcessor(RulesLibrary library)
        : this(new ReflectionRuleContextResolver(library), library)
    {
    }

    /// <inheritdoc/>
    public RuleEvaluationResult Evaluate(RuleEvaluationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        RuleContext? context = _resolver.Resolve(request.InputObject);
        if (context is null)
        {
            var result = CreateResult(string.Empty, request);
            AddError(result, new RuleEvaluationError(
                "RULE_CONTEXT_NOT_FOUND",
                $"No rule context found for type '{request.InputObject.GetType().FullName}'. Ensure a RuleContext with a matching QualifiedTypeName is registered in the library."));
            return result;
        }

        // 2. Determine which expressions to evaluate
        RuleEvaluationResult evaluationResult = CreateResult(context.Name, request);
        IEnumerable<RuleExpression> expressions = ResolveExpressions(context, request, evaluationResult);
        if (evaluationResult.Status == RuleEvaluationStatus.Failed) return evaluationResult;

        // 3. Extract parameters from the input object
        var factory = new ParameterFactory();
        Parameters? extractedParams = factory.CreateParameters(request.InputObject);
        Parameters runtimeParams = extractedParams ?? new Parameters();

        // 4. Evaluate each expression individually and collect results
        foreach (RuleExpression ruleExpression in expressions)
        {
            try
            {
                var expressionResult = EvaluateExpression(
                    ruleExpression,
                    runtimeParams,
                    request.IncludeDiagnostics);

                if (expressionResult.Matched)
                    evaluationResult.Matches.Add(expressionResult);
                else
                    evaluationResult.Mismatches.Add(expressionResult);
            }
            catch (Exception ex) when (request.ExecutionMode != RuleExecutionMode.Strict)
            {
                AddError(evaluationResult, new RuleEvaluationError(
                    "RULE_EXPRESSION_EVALUATION_FAILED",
                    ex.Message,
                    context.Name,
                    request.CategoryName,
                    ruleExpression.Identity));
            }
        }

        return evaluationResult;
    }

    private RuleEvaluationResult CreateResult(string contextName, RuleEvaluationRequest request)
    {
        return new RuleEvaluationResult(contextName, request.CategoryName, _library?.Name, _library?.Version ?? 0, _library?.SnapshotIdentity ?? Guid.Empty)
        {
            ExecutionMode = request.ExecutionMode,
            CategoryExecutionMode = request.CategoryExecutionMode
        };
    }

    private static IEnumerable<RuleExpression> ResolveExpressions(RuleContext context, RuleEvaluationRequest request, RuleEvaluationResult result)
    {
        string? categoryName = request.CategoryName;
        if (string.IsNullOrEmpty(categoryName))
        {
            // No category specified — evaluate expressions in the context.
            return request.IncludeInactiveRules ? context.Expressions : context.Expressions.Where(e => e.IsActive);
        }

        RuleCategory? category = context.FindCategoryByName(categoryName);
        if (category is null)
        {
            AddError(result, new RuleEvaluationError(
                "RULE_CATEGORY_NOT_FOUND",
                $"Rule category '{categoryName}' not found in context '{context.Name}'.",
                context.Name,
                categoryName));
            return [];
        }

        if (category.IsLeaf)
            return request.IncludeInactiveRules ? category.GetExpressions() : category.GetActiveExpressions();

        if (request.CategoryExecutionMode == RuleCategoryExecutionMode.LeafOnly)
        {
            AddError(result, new RuleEvaluationError(
                "RULE_CATEGORY_NOT_EXECUTABLE",
                $"Rule category '{categoryName}' is not a leaf category and cannot be executed unless descendant leaf execution is enabled.",
                context.Name,
                categoryName));
            return [];
        }

        IEnumerable<RuleCategory> leaves = category.EnumerateDescendants()
            .Where(c => c.IsLeaf)
            .OrderBy(c => c.Path, StringComparer.Ordinal)
            .ThenBy(c => c.Identity);

        return leaves.SelectMany(c => request.IncludeInactiveRules ? c.GetExpressions() : c.GetActiveExpressions());
    }

    private static void AddError(RuleEvaluationResult result, RuleEvaluationError error)
    {
        result.Errors.Add(error);
        result.Succeeded = false;
        result.Status = result.TotalEvaluated > 0 ? RuleEvaluationStatus.PartiallyCompleted : RuleEvaluationStatus.Failed;
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
            matched: false,
            diagnostic: diagnostic);
    }
}
