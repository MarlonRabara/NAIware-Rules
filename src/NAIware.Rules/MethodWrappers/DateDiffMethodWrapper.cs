using System.Globalization;

namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Implements the <c>DATEDIFF</c> formula function which returns the whole-unit difference between two dates.
/// </summary>
/// <remarks>
/// <para>
/// Usage: <c>DATEDIFF(unit, startDate, endDate)</c>. The result is <c>endDate - startDate</c> expressed in the
/// requested <paramref name="unit"/> and truncated toward zero, so a later end date yields a positive value.
/// </para>
/// <para>Supported units (case-insensitive):</para>
/// <list type="bullet">
///   <item><description><c>year</c> / <c>yyyy</c> / <c>yy</c> — whole calendar years.</description></item>
///   <item><description><c>month</c> / <c>mm</c> / <c>m</c> — whole calendar months.</description></item>
///   <item><description><c>day</c> / <c>dd</c> / <c>d</c> — whole days.</description></item>
///   <item><description><c>hour</c> / <c>hh</c> / <c>h</c> — whole hours.</description></item>
///   <item><description><c>minute</c> / <c>mi</c> / <c>n</c> — whole minutes.</description></item>
///   <item><description><c>second</c> / <c>ss</c> / <c>s</c> — whole seconds.</description></item>
/// </list>
/// </remarks>
public sealed class DateDiffMethodWrapper : MethodWrapperBase
{
    /// <inheritdoc />
    protected override object DescendantExecute(params object[] parameters)
    {
        string unit = (Convert.ToString(parameters[0]) ?? string.Empty).Trim().ToLowerInvariant();
        DateTime start = Convert.ToDateTime(parameters[1], CultureInfo.InvariantCulture);
        DateTime end = Convert.ToDateTime(parameters[2], CultureInfo.InvariantCulture);

        TimeSpan delta = end - start;

        return unit switch
        {
            "year" or "yyyy" or "yy" => CalculateYears(start, end),
            "month" or "mm" or "m" => CalculateMonths(start, end),
            "day" or "dd" or "d" => (long)delta.TotalDays,
            "hour" or "hh" or "h" => (long)delta.TotalHours,
            "minute" or "mi" or "n" => (long)delta.TotalMinutes,
            "second" or "ss" or "s" => (long)delta.TotalSeconds,
            _ => throw new LogicMethodArgumentException()
        };
    }

    /// <inheritdoc />
    protected override void ValidateParameters(ref bool isValid, params object[] parameters) =>
        isValid = parameters is { Length: 3 } && parameters.All(p => p is not null);

    private static long CalculateYears(DateTime start, DateTime end)
    {
        int years = end.Year - start.Year;
        if (end < start.AddYears(years))
            years--;
        return years;
    }

    private static long CalculateMonths(DateTime start, DateTime end)
    {
        int months = ((end.Year - start.Year) * 12) + end.Month - start.Month;
        if (end < start.AddMonths(months))
            months--;
        return months;
    }
}
