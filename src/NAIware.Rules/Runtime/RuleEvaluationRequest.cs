namespace NAIware.Rules.Runtime;

/// <summary>
/// Encapsulates a request to evaluate rules against an input object.
/// </summary>
public class RuleEvaluationRequest
{
    /// <summary>Creates a new evaluation request.</summary>
    /// <param name="inputObject">The object to evaluate rules against.</param>
    /// <param name="categoryName">
    /// Optional category name to scope evaluation. When null, all active expressions
    /// in the resolved context are evaluated.
    /// </param>
    /// <param name="includeDiagnostics">
    /// When true, mismatch diagnostics are produced for expressions that do not match.
    /// </param>
    public RuleEvaluationRequest(object inputObject, string? categoryName = null, bool includeDiagnostics = false)
    {
        ArgumentNullException.ThrowIfNull(inputObject);
        InputObject = inputObject;
        CategoryName = categoryName;
        IncludeDiagnostics = includeDiagnostics;
    }

    /// <summary>Gets the input object to evaluate rules against.</summary>
    public object InputObject { get; }

    /// <summary>Gets the optional category name to scope evaluation.</summary>
    public string? CategoryName { get; }

    /// <summary>Gets whether mismatch diagnostics should be included in the result.</summary>
    public bool IncludeDiagnostics { get; }
}
