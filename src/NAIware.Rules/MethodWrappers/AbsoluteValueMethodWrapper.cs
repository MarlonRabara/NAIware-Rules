namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>ABS</c> formula function which returns the absolute value of a numeric value.
/// </summary>
/// <remarks>
/// Usage: <c>ABS(value)</c>. Delegates to <see cref="System.Math.Abs(double)"/>.
/// </remarks>
public sealed class AbsoluteValueMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters) =>
        System.Math.Abs(Convert.ToDouble(parameters[0]));

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is { Length: 1 } && parameters[0] is not null;
}
