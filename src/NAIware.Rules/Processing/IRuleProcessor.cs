using NAIware.Rules.Runtime;

namespace NAIware.Rules.Processing;

/// <summary>
/// Defines the contract for a high-level rule processor that evaluates library-defined
/// rules against input objects with automatic context resolution.
/// </summary>
public interface IRuleProcessor
{
    /// <summary>
    /// Evaluates rules against the input object.
    /// The rule context is inferred automatically from the input object's type.
    /// </summary>
    /// <param name="request">The evaluation request containing the input object and options.</param>
    /// <returns>The evaluation result with matches, mismatches, and optional diagnostics.</returns>
    RuleEvaluationResult Evaluate(RuleEvaluationRequest request);
}
