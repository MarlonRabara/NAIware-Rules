namespace NAIware.Rules.Runtime;

/// <summary>
/// Overall completion status for a rule evaluation request.
/// </summary>
public enum RuleEvaluationStatus
{
    /// <summary>Evaluation completed successfully.</summary>
    Completed,

    /// <summary>Evaluation could not complete.</summary>
    Failed,

    /// <summary>Evaluation completed for valid expressions but recorded one or more errors.</summary>
    PartiallyCompleted
}
