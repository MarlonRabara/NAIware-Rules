using System.Globalization;
using FluentAssertions;
using NAIware.Rules.MethodWrappers;
using Reqnroll;

namespace NAIware.Rules.Tests.StepDefinitions;

/// <summary>
/// Step definitions that exercise the extended numeric, text, and date method wrappers directly through their
/// <see cref="IMethodWrapper"/> contract. Arguments are supplied caret-delimited ('^') so that significant
/// whitespace is preserved and the delimiter does not collide with Gherkin's pipe-delimited Examples tables.
/// </summary>
[Binding]
public sealed class ExtendedMethodWrapperSteps
{
    private static readonly MethodMap MethodMap = DefaultMethodWrapperRegistration.CreateDefaultMethodMap();

    private IMethodWrapper? _wrapper;
    private object? _result;
    private Exception? _caughtException;

    [Given(@"the extended ""(.*)"" wrapper")]
    public void GivenTheExtendedWrapper(string name)
    {
        MethodMap.ContainsKey(name).Should().BeTrue($"the '{name}' wrapper should be registered by default");
        _wrapper = MethodMap[name];
    }

    [When(@"I invoke it with arguments ""(.*)""")]
    public void WhenIInvokeItWithArguments(string delimitedArguments)
    {
        object[] arguments = delimitedArguments
            .Split('^')
            .Select(ParseArgument)
            .ToArray();

        Execute(arguments);
    }

    [When(@"I invoke it with no arguments")]
    public void WhenIInvokeItWithNoArguments() => Execute(Array.Empty<object>());

    [Then(@"the numeric wrapper result should be (.*)")]
    public void ThenTheNumericWrapperResultShouldBe(decimal expected)
    {
        _caughtException.Should().BeNull();
        _result.Should().NotBeNull();
        Convert.ToDecimal(_result, CultureInfo.InvariantCulture).Should().Be(expected);
    }

    [Then(@"the text wrapper result should be ""(.*)""")]
    public void ThenTheTextWrapperResultShouldBe(string expected)
    {
        _caughtException.Should().BeNull();
        _result.Should().NotBeNull();
        Convert.ToString(_result, CultureInfo.InvariantCulture).Should().Be(expected);
    }

    [Then(@"the text wrapper result should be empty")]
    public void ThenTheTextWrapperResultShouldBeEmpty()
    {
        _caughtException.Should().BeNull();
        _result.Should().NotBeNull();
        Convert.ToString(_result, CultureInfo.InvariantCulture).Should().BeEmpty();
    }

    [Then(@"the wrapper result should be a date on or after today")]
    public void ThenTheWrapperResultShouldBeADateOnOrAfterToday()
    {
        _caughtException.Should().BeNull();
        _result.Should().BeOfType<DateTime>();
        ((DateTime)_result!).Should().BeOnOrAfter(DateTime.Today);
    }

    [Then(@"the wrapper result should be a date at midnight")]
    public void ThenTheWrapperResultShouldBeADateAtMidnight()
    {
        _caughtException.Should().BeNull();
        _result.Should().BeOfType<DateTime>();
        var value = (DateTime)_result!;
        value.TimeOfDay.Should().Be(TimeSpan.Zero);
        value.Date.Should().Be(DateTime.Today);
    }

    [Then(@"the extended wrapper execution should fail validation")]
    public void ThenTheExtendedWrapperExecutionShouldFailValidation()
    {
        _caughtException.Should().BeOfType<LogicMethodArgumentException>();
    }

    private void Execute(object[] arguments)
    {
        _wrapper.Should().NotBeNull();
        try
        {
            _result = _wrapper!.ExecuteMethod(arguments);
        }
        catch (Exception ex)
        {
            _caughtException = ex;
        }
    }

    private static object ParseArgument(string raw)
    {
        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
            return intValue;

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
            return doubleValue;

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateValue))
            return dateValue;

        return raw;
    }
}
