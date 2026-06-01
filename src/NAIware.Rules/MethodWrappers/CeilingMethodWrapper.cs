namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>CEILING</c> formula function which returns the smallest integral value greater than or
/// equal to the supplied number.
/// </summary>
/// <remarks>Usage: <c>CEILING(value)</c>. Delegates to <see cref="System.Math.Ceiling(double)"/>.</remarks>
public sealed class CeilingMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters) =>
        System.Math.Ceiling(Convert.ToDouble(parameters[0]));

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is { Length: 1 } && parameters[0] is not null;
}
