namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>FLOOR</c> formula function which returns the largest integral value less than or equal to
/// the supplied number.
/// </summary>
/// <remarks>Usage: <c>FLOOR(value)</c>. Delegates to <see cref="System.Math.Floor(double)"/>.</remarks>
public sealed class FloorMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters) =>
        System.Math.Floor(Convert.ToDouble(parameters[0]));

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is { Length: 1 } && parameters[0] is not null;
}
