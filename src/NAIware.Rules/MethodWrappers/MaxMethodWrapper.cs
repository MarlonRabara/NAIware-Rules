namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>MAX</c> formula function which returns the larger of two numeric values.
/// </summary>
/// <remarks>
/// Usage: <c>MAX(left, right)</c>. Delegates to <see cref="System.Math.Max(double, double)"/>.
/// </remarks>
public sealed class MaxMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters) =>
        System.Math.Max(Convert.ToDouble(parameters[0]), Convert.ToDouble(parameters[1]));

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is { Length: 2 } && parameters[0] is not null && parameters[1] is not null;
}
