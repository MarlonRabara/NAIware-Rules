using NAIware.Core.Math;

namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>ROUNDUP</c> formula function which rounds a number up (away from zero) to a specified
/// number of decimal places.
/// </summary>
/// <remarks>
/// Usage: <c>ROUNDUP(value, decimals)</c>. Delegates to <see cref="MathHelper.RoundUp(double, int)"/>.
/// </remarks>
public sealed class RoundUpMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters)
    {
        double value = Convert.ToDouble(parameters[0]);
        int decimals = Convert.ToInt32(parameters[1]);
        return MathHelper.RoundUp(value, decimals);
    }

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is { Length: 2 } && parameters[0] is not null && parameters[1] is not null;
}
