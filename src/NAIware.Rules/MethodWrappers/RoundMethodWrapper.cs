namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>ROUND</c> formula function which rounds a number to a specified number of decimal places
/// using banker's rounding.
/// </summary>
/// <remarks>
/// Usage: <c>ROUND(value, decimals)</c>. Delegates to <see cref="System.Math.Round(double, int)"/>.
/// </remarks>
public sealed class RoundMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters) =>
        System.Math.Round(Convert.ToDouble(parameters[0]), Convert.ToInt32(parameters[1]));

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is { Length: 2 } && parameters[0] is not null && parameters[1] is not null;
}
