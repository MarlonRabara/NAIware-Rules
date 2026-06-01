using System.Globalization;

namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>SUM</c> formula function which returns the sum of one or more numeric values.
/// </summary>
/// <remarks>Usage: <c>SUM(a, b, ...)</c>. Accepts a variable number of numeric arguments.</remarks>
public sealed class SumMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters) =>
        parameters.Sum(p => Convert.ToDouble(p, CultureInfo.InvariantCulture));

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is { Length: > 0 } && parameters.All(p => p is not null);
}
