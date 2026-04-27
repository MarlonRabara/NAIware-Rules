namespace NAIware.Rules.Runtime;

/// <summary>
/// Structured runtime error produced when evaluation cannot complete cleanly.
/// </summary>
public class RuleEvaluationError
{
    /// <summary>Creates a new runtime error.</summary>
    public RuleEvaluationError(string code, string message, string? contextName = null, string? categoryName = null, Guid? expressionIdentity = null, string severity = "Error")
    {
        Code = code;
        Message = message;
        ContextName = contextName;
        CategoryName = categoryName;
        ExpressionIdentity = expressionIdentity;
        Severity = severity;
    }

    /// <summary>Gets the stable machine-readable error code.</summary>
    public string Code { get; }

    /// <summary>Gets the human-readable error message.</summary>
    public string Message { get; }

    /// <summary>Gets the related context name, when available.</summary>
    public string? ContextName { get; }

    /// <summary>Gets the related category name, when available.</summary>
    public string? CategoryName { get; }

    /// <summary>Gets the related expression identity, when available.</summary>
    public Guid? ExpressionIdentity { get; }

    /// <summary>Gets the error severity.</summary>
    public string Severity { get; }
}
