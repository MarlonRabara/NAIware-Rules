using System.Globalization;

namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>LOWER</c> formula function which converts a string to lower case.
/// </summary>
/// <remarks>Usage: <c>LOWER(text)</c>. Uses the invariant culture.</remarks>
public sealed class LowerMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters) =>
        (Convert.ToString(parameters[0]) ?? string.Empty).ToLower(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is { Length: 1 } && parameters[0] is not null;
}
