namespace NAIware.Rules;

/// <summary>
/// An interface for an expression component that supports cloning and text representation.
/// </summary>
public interface IExpressionComponent : ICloneable
{
    /// <summary>Gets the text representation of the expression component.</summary>
    string Text { get; }
}
