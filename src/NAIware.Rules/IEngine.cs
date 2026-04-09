namespace NAIware.Rules;

/// <summary>
/// An interface for a processing engine.
/// </summary>
public interface IEngine
{
    /// <summary>
    /// A collection of parameters associated to the respective engine.
    /// </summary>
    Parameters Parameters { get; }

    /// <summary>
    /// A collection of groups associated to the respective engine.
    /// </summary>
    Dictionary<string, ExpressionGroup> ExpressionGroups { get; }
}
