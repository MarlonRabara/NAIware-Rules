using System.Globalization;

namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>REPLACE</c> formula function which replaces a span of characters in a string, identified
/// by a 1-based start position and a length, with replacement text.
/// </summary>
/// <remarks>
/// Usage: <c>REPLACE(text, startPosition, length, replacement)</c>. <c>startPosition</c> is 1-based and
/// <c>length</c> is the number of characters to remove. This mirrors spreadsheet <c>REPLACE</c> semantics, in
/// contrast to <see cref="SubstituteMethodWrapper"/> which replaces by matching text.
/// </remarks>
public sealed class ReplaceMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters)
    {
        string text = Convert.ToString(parameters[0]) ?? string.Empty;
        int startPosition = Convert.ToInt32(parameters[1], CultureInfo.InvariantCulture);
        int length = Convert.ToInt32(parameters[2], CultureInfo.InvariantCulture);
        string replacement = Convert.ToString(parameters[3]) ?? string.Empty;

        int startIndex = System.Math.Max(0, startPosition - 1);
        if (startIndex > text.Length)
            startIndex = text.Length;

        int removable = System.Math.Max(0, System.Math.Min(length, text.Length - startIndex));

        return string.Concat(text[..startIndex], replacement, text[(startIndex + removable)..]);
    }

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is { Length: 4 } && parameters.All(p => p is not null);
}
