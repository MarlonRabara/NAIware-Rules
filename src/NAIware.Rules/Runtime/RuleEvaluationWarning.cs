namespace NAIware.Rules.Runtime;

/// <summary>
/// Structured runtime warning produced during evaluation.
/// </summary>
public class RuleEvaluationWarning
{
    /// <summary>Creates a new runtime warning.</summary>
    public RuleEvaluationWarning(string code, string message, string? contextName = null, string? categoryName = null, Guid? expressionIdentity = null)
    {
        Code = code;
        Message = message;
        ContextName = contextName;
        CategoryName = categoryName;
        ExpressionIdentity = expressionIdentity;
    }

    /// <summary>Gets the stable machine-readable warning code.</summary>
    public string Code { get; }

    /// <summary>Gets the human-readable warning message.</summary>
    public string Message { get; }

    /// <summary>Gets the related context name, when available.</summary>
    public string? ContextName { get; }

    /// <summary>Gets the related category name, when available.</summary>
    public string? CategoryName { get; }

    /// <summary>Gets the related expression identity, when available.</summary>
    public Guid? ExpressionIdentity { get; }
}
