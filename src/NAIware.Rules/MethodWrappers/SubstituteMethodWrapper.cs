namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>SUBSTITUTE</c> formula function which replaces every occurrence of a matched substring
/// with replacement text.
/// </summary>
/// <remarks>
/// Usage: <c>SUBSTITUTE(text, oldText, newText)</c>. Replacement is ordinal and case-sensitive, mirroring
/// spreadsheet <c>SUBSTITUTE</c>. In contrast to <see cref="ReplaceMethodWrapper"/>, this matches by text
/// rather than by position. When <c>oldText</c> is empty the original text is returned unchanged.
/// </remarks>
public sealed class SubstituteMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters)
    {
        string text = Convert.ToString(parameters[0]) ?? string.Empty;
        string oldText = Convert.ToString(parameters[1]) ?? string.Empty;
        string newText = Convert.ToString(parameters[2]) ?? string.Empty;

        return oldText.Length == 0
            ? text
            : text.Replace(oldText, newText, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is { Length: 3 } && parameters.All(p => p is not null);
}
