using NAIware.Rules.Models;
using NAIware.Rules.Processing;
using NAIware.Rules.Runtime;
using NAIware.RuleService.Contracts;

namespace NAIware.RuleService.Services;

/// <summary>
/// Orchestrates the full evaluation pipeline: deserialize the model, load the rules library,
/// resolve the matching context, evaluate via <see cref="RuleProcessor"/>, and project the
/// outcome onto a transport-friendly <see cref="EvaluateModelResponse"/>.
/// </summary>
public sealed class RuleEvaluationService
{
    private readonly ModelDeserializationService _deserializer;
    private readonly RulesLibraryLoader _libraryLoader;

    /// <summary>Creates a new evaluation service.</summary>
    public RuleEvaluationService(ModelDeserializationService deserializer, RulesLibraryLoader libraryLoader)
    {
        ArgumentNullException.ThrowIfNull(deserializer);
        ArgumentNullException.ThrowIfNull(libraryLoader);
        _deserializer = deserializer;
        _libraryLoader = libraryLoader;
    }

    /// <summary>Runs the supplied request through the evaluation pipeline.</summary>
    /// <param name="request">The evaluation request.</param>
    /// <returns>The structured evaluation response.</returns>
    public EvaluateModelResponse Evaluate(EvaluateModelRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        object model = _deserializer.Deserialize(request);
        RulesLibrary library = LoadLibrary(request);

        RuleContext context = ResolveAndAlignContext(library, model);

        var evaluationRequest = new RuleEvaluationRequest(model, request.CategoryName, request.IncludeDiagnostics)
        {
            ExecutionMode = ParseExecutionMode(request.ExecutionMode),
            IncludeInactiveRules = request.IncludeInactiveRules,
            CategoryExecutionMode = RuleCategoryExecutionMode.IncludeDescendantLeaves
        };

        var processor = new RuleProcessor(library);
        RuleEvaluationResult result = processor.Evaluate(evaluationRequest);

        return Map(result);
    }

    private RulesLibrary LoadLibrary(EvaluateModelRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.LibraryJson))
            return _libraryLoader.LoadFromJson(request.LibraryJson);

        if (!string.IsNullOrWhiteSpace(request.LibraryPath))
            return _libraryLoader.LoadFromFile(request.LibraryPath);

        throw new InvalidOperationException("Either LibraryJson or LibraryPath must be supplied.");
    }

    /// <summary>
    /// Finds the context whose type targets the model and aligns its <see cref="RuleContext.QualifiedTypeName"/>
    /// with the model's runtime assembly-qualified name so the reflection resolver matches unambiguously.
    /// </summary>
    private static RuleContext ResolveAndAlignContext(RulesLibrary library, object model)
    {
        Type modelType = model.GetType();

        RuleContext? context =
            library.FindContextByTypeName(modelType.AssemblyQualifiedName ?? modelType.FullName!)
            ?? library.FindContextByTypeName(modelType.FullName!)
            ?? MatchBySimpleTypeName(library, modelType);

        if (context is null)
        {
            throw new InvalidOperationException(
                $"The library does not contain a context for model type '{modelType.FullName}'. " +
                "Ensure a context's QualifiedTypeName matches the deserialized model type.");
        }

        context.QualifiedTypeName = modelType.AssemblyQualifiedName ?? modelType.FullName!;
        return context;
    }

    private static RuleContext? MatchBySimpleTypeName(RulesLibrary library, Type modelType)
    {
        // The library may have been authored against a different assembly version/path of the same
        // logical model. Fall back to matching on the namespace-qualified full name embedded in the
        // persisted assembly-qualified name.
        string modelFullName = modelType.FullName ?? modelType.Name;
        return library.Contexts.FirstOrDefault(c =>
            !string.IsNullOrEmpty(c.QualifiedTypeName)
            && c.QualifiedTypeName.StartsWith(modelFullName + ",", StringComparison.Ordinal));
    }

    private static RuleExecutionMode ParseExecutionMode(string? mode) =>
        Enum.TryParse(mode, ignoreCase: true, out RuleExecutionMode parsed)
            ? parsed
            : RuleExecutionMode.Lenient;

    private static EvaluateModelResponse Map(RuleEvaluationResult result)
    {
        var response = new EvaluateModelResponse
        {
            LibraryName = result.LibraryName,
            LibraryVersion = result.LibraryVersion,
            ContextName = result.ContextName,
            CategoryName = result.CategoryName,
            Succeeded = result.Succeeded,
            Status = result.Status.ToString(),
            EvaluatedUtc = result.EvaluatedUtc,
            TotalEvaluated = result.TotalEvaluated
        };

        foreach (RuleExpressionResult match in result.Matches)
        {
            response.Matches.Add(new RuleMatchResult
            {
                ExpressionIdentity = match.ExpressionIdentity,
                ExpressionName = match.ExpressionName,
                Code = match.Result?.Code,
                Message = match.Result?.Message,
                Severity = match.Result?.Severity,
                Value = match.Result?.Value
            });
        }

        foreach (RuleExpressionResult mismatch in result.Mismatches)
        {
            var dto = new RuleMismatchResult
            {
                ExpressionIdentity = mismatch.ExpressionIdentity,
                ExpressionName = mismatch.ExpressionName,
                Expression = mismatch.Diagnostic?.Expression,
                Explanation = mismatch.Diagnostic?.Explanation
            };

            if (mismatch.Diagnostic is not null)
            {
                foreach (KeyValuePair<string, string?> kvp in mismatch.Diagnostic.EvaluatedParameters)
                    dto.EvaluatedParameters[kvp.Key] = kvp.Value;
            }

            response.Mismatches.Add(dto);
        }

        foreach (RuleEvaluationError error in result.Errors)
        {
            response.Errors.Add(new RuleProblem
            {
                Code = error.Code,
                Message = error.Message,
                ContextName = error.ContextName,
                CategoryName = error.CategoryName,
                Severity = error.Severity
            });
        }

        foreach (RuleEvaluationWarning warning in result.Warnings)
        {
            response.Warnings.Add(new RuleProblem
            {
                Code = warning.Code,
                Message = warning.Message,
                ContextName = warning.ContextName,
                CategoryName = warning.CategoryName
            });
        }

        return response;
    }
}
