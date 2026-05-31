namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>NOW</c> formula function which returns the current local date and time.
/// </summary>
/// <remarks>
/// Usage: <c>NOW()</c>. Takes no arguments and returns a <see cref="DateTime"/>. The value is non-deterministic;
/// callers that require deterministic behavior should inject the current time as a parameter instead.
/// </remarks>
public sealed class NowMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters) => DateTime.Now;

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is null || parameters.Length == 0;
}
