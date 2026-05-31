using System.Globalization;

namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>LEFT</c> formula function which returns the leftmost characters of a string.
/// </summary>
/// <remarks>
/// Usage: <c>LEFT(text, count)</c>. <c>count</c> is clamped to the bounds of the string, so requesting more
/// characters than are available returns the whole string and a negative count returns an empty string.
/// </remarks>
public sealed class LeftMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters)
    {
        string text = Convert.ToString(parameters[0]) ?? string.Empty;
        int count = Convert.ToInt32(parameters[1], CultureInfo.InvariantCulture);
        count = System.Math.Clamp(count, 0, text.Length);
        return text[..count];
    }

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is { Length: 2 } && parameters[0] is not null && parameters[1] is not null;
}
