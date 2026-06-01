using System.Globalization;

namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>PROPER</c> formula function which converts a string to title case, capitalizing the first
/// letter of each word.
/// </summary>
/// <remarks>
/// Usage: <c>PROPER(text)</c>. The input is lower-cased first so that all-caps words are normalized, matching
/// spreadsheet <c>PROPER</c> behavior.
/// </remarks>
public sealed class ProperMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters)
    {
        string text = Convert.ToString(parameters[0]) ?? string.Empty;
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant());
    }

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is { Length: 1 } && parameters[0] is not null;
}
