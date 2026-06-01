namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>INT</c> formula function which converts a numeric value to a 32-bit integer.
/// </summary>
/// <remarks>
/// Usage: <c>INT(value)</c>. Conversion follows <see cref="Convert.ToInt32(object)"/> semantics, which rounds
/// to the nearest even integer for fractional values.
/// </remarks>
public sealed class IntMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters) =>
        Convert.ToInt32(parameters[0]);

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is { Length: 1 } && parameters[0] is not null;
}
