namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>TODAY</c> formula function which returns the current local date with the time component
/// set to midnight.
/// </summary>
/// <remarks>
/// Usage: <c>TODAY()</c>. Takes no arguments and returns a <see cref="DateTime"/> at midnight. The value is
/// non-deterministic; callers that require deterministic behavior should inject the date as a parameter instead.
/// </remarks>
public sealed class TodayMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters) => DateTime.Today;

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is null || parameters.Length == 0;
}
