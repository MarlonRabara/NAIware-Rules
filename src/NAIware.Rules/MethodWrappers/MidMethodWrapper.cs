using System.Globalization;

namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>MID</c> formula function which returns a substring of a given length starting at a
/// 1-based position.
/// </summary>
/// <remarks>
/// Usage: <c>MID(text, startPosition, length)</c>. <c>startPosition</c> is 1-based, mirroring spreadsheet
/// <c>MID</c>. Positions and lengths are clamped to the bounds of the string.
/// </remarks>
public sealed class MidMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters)
    {
        string text = Convert.ToString(parameters[0]) ?? string.Empty;
        int startPosition = Convert.ToInt32(parameters[1], CultureInfo.InvariantCulture);
        int length = Convert.ToInt32(parameters[2], CultureInfo.InvariantCulture);

        int startIndex = System.Math.Max(0, startPosition - 1);
        if (startIndex >= text.Length || length <= 0)
            return string.Empty;

        int available = System.Math.Min(length, text.Length - startIndex);
        return text.Substring(startIndex, available);
    }

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is { Length: 3 } && parameters.All(p => p is not null);
}
