namespace NAIware.RuleService.Contracts;

/// <summary>
/// The structured result of evaluating a serialized model against a rules library.
/// This is a transport-friendly projection of <c>NAIware.Rules.Runtime.RuleEvaluationResult</c>.
/// </summary>
public sealed class EvaluateModelResponse
{
    /// <summary>Gets or sets the name of the rules library that was evaluated, when available.</summary>
    public string? LibraryName { get; set; }

    /// <summary>Gets or sets the library version used for evaluation.</summary>
    public int LibraryVersion { get; set; }

    /// <summary>Gets or sets the name of the rule context that was resolved and evaluated.</summary>
    public string ContextName { get; set; } = string.Empty;

    /// <summary>Gets or sets the category that was evaluated, or null when all expressions were evaluated.</summary>
    public string? CategoryName { get; set; }

    /// <summary>Gets or sets whether the evaluation completed without fatal errors.</summary>
    public bool Succeeded { get; set; }

    /// <summary>Gets or sets the aggregate evaluation status (Completed, Failed, PartiallyCompleted).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when evaluation completed.</summary>
    public DateTimeOffset EvaluatedUtc { get; set; }

    /// <summary>Gets or sets the total number of expressions evaluated.</summary>
    public int TotalEvaluated { get; set; }

    /// <summary>Gets the expressions that matched (evaluated to true).</summary>
    public List<RuleMatchResult> Matches { get; init; } = [];

    /// <summary>Gets the expressions that did not match (evaluated to false).</summary>
    public List<RuleMismatchResult> Mismatches { get; init; } = [];

    /// <summary>Gets structured runtime errors.</summary>
    public List<RuleProblem> Errors { get; init; } = [];

    /// <summary>Gets structured runtime warnings.</summary>
    public List<RuleProblem> Warnings { get; init; } = [];
}

/// <summary>A single expression that matched, with its configured result payload.</summary>
public sealed class RuleMatchResult
{
    /// <summary>Gets or sets the identity of the matched expression.</summary>
    public Guid ExpressionIdentity { get; set; }

    /// <summary>Gets or sets the name of the matched expression.</summary>
    public string ExpressionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the application-defined result code.</summary>
    public string? Code { get; set; }

    /// <summary>Gets or sets the human-readable result message.</summary>
    public string? Message { get; set; }

    /// <summary>Gets or sets the result severity hint, such as Info, Warning, or Error.</summary>
    public string? Severity { get; set; }

    /// <summary>Gets or sets an optional result value payload.</summary>
    public string? Value { get; set; }
}

/// <summary>A single expression that did not match, with optional diagnostics.</summary>
public sealed class RuleMismatchResult
{
    /// <summary>Gets or sets the identity of the evaluated expression.</summary>
    public Guid ExpressionIdentity { get; set; }

    /// <summary>Gets or sets the name of the evaluated expression.</summary>
    public string ExpressionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the raw expression text that was evaluated, when diagnostics are requested.</summary>
    public string? Expression { get; set; }

    /// <summary>Gets or sets an optional human-readable explanation of why the rule did not match.</summary>
    public string? Explanation { get; set; }

    /// <summary>Gets the parameter values used during evaluation, keyed by parameter name.</summary>
    public Dictionary<string, string?> EvaluatedParameters { get; init; } = [];
}

/// <summary>A structured runtime error or warning.</summary>
public sealed class RuleProblem
{
    /// <summary>Gets or sets the stable machine-readable code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the related context name, when available.</summary>
    public string? ContextName { get; set; }

    /// <summary>Gets or sets the related category name, when available.</summary>
    public string? CategoryName { get; set; }

    /// <summary>Gets or sets the severity, when available.</summary>
    public string? Severity { get; set; }
}
