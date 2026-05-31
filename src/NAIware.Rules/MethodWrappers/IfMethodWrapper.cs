namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>IF</c> formula function which returns one of two values based on a boolean condition.
/// </summary>
/// <remarks>
/// Usage: <c>IF(condition, whenTrue, whenFalse)</c>. The first argument must be a boolean; the second and
/// third arguments are returned as-is depending on the condition.
/// </remarks>
public sealed class IfMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters) =>
        Convert.ToBoolean(parameters[0]) ? parameters[1] : parameters[2];

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is { Length: 3 };
}
