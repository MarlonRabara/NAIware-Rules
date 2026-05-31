using System.Globalization;

namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>UPPER</c> formula function which converts a string to upper case.
/// </summary>
/// <remarks>Usage: <c>UPPER(text)</c>. Uses the invariant culture.</remarks>
public sealed class UpperMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters) =>
        (Convert.ToString(parameters[0]) ?? string.Empty).ToUpper(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is { Length: 1 } && parameters[0] is not null;
}
