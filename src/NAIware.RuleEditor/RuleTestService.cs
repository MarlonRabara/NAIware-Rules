using System.Text;
using NAIware.Rules.Catalog;
using NAIware.Rules.Processing;
using NAIware.Rules.Runtime;

namespace NAIware.RuleEditor;

/// <summary>
/// Coordinates a test-run of the current rule library against a hydrated input object.
/// Builds a <see cref="RulesLibrary"/> from the UI document, creates a
/// <see cref="RuleProcessor"/>, and invokes it with diagnostics enabled so the user
/// can see why non-matching rules did not fire.
/// </summary>
public sealed class RuleTestService
{
    /// <summary>
    /// Runs the rules in the library against the supplied input object.
    /// </summary>
    /// <param name="library">The UI library document.</param>
    /// <param name="contextDoc">The context document whose type matches the input object.</param>
    /// <param name="inputObject">The hydrated input to evaluate.</param>
    /// <param name="includeDiagnostics">Whether to include mismatch diagnostics in the result.</param>
    /// <returns>The evaluation result produced by <see cref="RuleProcessor"/>.</returns>
    public RuleEvaluationResult Run(
        RuleLibraryDocument library,
        RuleContextDocument contextDoc,
        object inputObject,
        bool includeDiagnostics = true)
    {
        ArgumentNullException.ThrowIfNull(library);
        ArgumentNullException.ThrowIfNull(contextDoc);
        ArgumentNullException.ThrowIfNull(inputObject);

        RulesLibrary domainLibrary = CatalogMapper.ToDomain(library);

        // Ensure the in-memory context matches the qualified type name of the hydrated input.
        // The hydrated object's runtime type is the authoritative resolver target.
        RuleContext? matchingContext = domainLibrary.FindContextByTypeName(inputObject.GetType().FullName!)
            ?? domainLibrary.FindContextByName(contextDoc.Name);

        if (matchingContext is null)
        {
            throw new InvalidOperationException(
                $"The library does not contain a context for '{inputObject.GetType().FullName}'. " +
                "Ensure the context's QualifiedTypeName matches the runtime type of the hydrated object.");
        }

        // Align the resolved context's QualifiedTypeName with the input's actual runtime type
        // so the reflection resolver can match it unambiguously.
        matchingContext.QualifiedTypeName = inputObject.GetType().FullName!;

        var processor = new RuleProcessor(domainLibrary);
        var request = new RuleEvaluationRequest(inputObject, categoryName: null, includeDiagnostics);

        return processor.Evaluate(request);
    }

    /// <summary>
    /// Formats a <see cref="RuleEvaluationResult"/> as a human-readable summary string
    /// suitable for rendering in a read-only test-results panel.
    /// </summary>
    public static string FormatReport(RuleEvaluationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var builder = new StringBuilder();
        builder.AppendLine($"Context: {result.ContextName}");
        builder.AppendLine($"Evaluated: {result.EvaluatedUtc:u}");
        builder.AppendLine($"Total evaluated: {result.TotalEvaluated}  |  Matches: {result.Matches.Count}  |  Mismatches: {result.Mismatches.Count}");
        builder.AppendLine();

        if (result.Matches.Count > 0)
        {
            builder.AppendLine("--- Matches ---");
            foreach (RuleExpressionResult match in result.Matches)
            {
                string code = match.Result?.Code ?? "(no code)";
                string message = match.Result?.Message ?? string.Empty;
                string severity = match.Result?.Severity ?? "Info";
                builder.AppendLine($"  [{severity}] {code}: {match.ExpressionName} — {message}");
            }
            builder.AppendLine();
        }

        if (result.Mismatches.Count > 0)
        {
            builder.AppendLine("--- Mismatches ---");
            foreach (RuleExpressionResult mismatch in result.Mismatches)
            {
                builder.AppendLine($"  {mismatch.ExpressionName}: did not match");
                if (mismatch.Diagnostic is not null)
                {
                    builder.AppendLine($"    Expression: {mismatch.Diagnostic.Expression}");
                    if (!string.IsNullOrWhiteSpace(mismatch.Diagnostic.Explanation))
                        builder.AppendLine($"    Explanation: {mismatch.Diagnostic.Explanation}");
                    foreach (var kvp in mismatch.Diagnostic.EvaluatedParameters)
                        builder.AppendLine($"      {kvp.Key} = {kvp.Value ?? "(null)"}");
                }
            }
        }

        return builder.ToString();
    }
}
