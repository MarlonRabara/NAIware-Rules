using System.Globalization;

namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>CONCAT</c> formula function which concatenates one or more values into a single string.
/// </summary>
/// <remarks>Usage: <c>CONCAT(a, b, ...)</c>. Each argument is converted to its string form and joined.</remarks>
public sealed class ConcatMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters) =>
        string.Concat(parameters.Select(p => Convert.ToString(p, CultureInfo.InvariantCulture) ?? string.Empty));

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is { Length: > 0 } && parameters.All(p => p is not null);
}
