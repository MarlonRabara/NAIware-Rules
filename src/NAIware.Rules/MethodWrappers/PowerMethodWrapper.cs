namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>POWER</c> formula function which raises a number to the specified exponent.
/// </summary>
/// <remarks>Usage: <c>POWER(base, exponent)</c>. Delegates to <see cref="System.Math.Pow(double, double)"/>.</remarks>
public sealed class PowerMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters) =>
        System.Math.Pow(Convert.ToDouble(parameters[0]), Convert.ToDouble(parameters[1]));

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is { Length: 2 } && parameters[0] is not null && parameters[1] is not null;
}
