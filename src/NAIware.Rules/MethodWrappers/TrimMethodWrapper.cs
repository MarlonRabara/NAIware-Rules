namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>TRIM</c> formula function which removes leading and trailing whitespace from a string.
/// </summary>
/// <remarks>Usage: <c>TRIM(text)</c>.</remarks>
public sealed class TrimMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters) =>
        Convert.ToString(parameters[0])?.Trim() ?? string.Empty;

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is { Length: 1 } && parameters[0] is not null;
}
