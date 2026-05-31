using System.Globalization;
using FluentAssertions;
using NAIware.Rules.MethodWrappers;
using Reqnroll;

namespace NAIware.Rules.Tests.StepDefinitions;

/// <summary>
/// Step definitions that exercise the default method wrappers directly through their
/// <see cref="IMethodWrapper"/> contract.
/// </summary>
[Binding]
public sealed class MethodWrapperSteps
{
    private static readonly MethodMap MethodMap = DefaultMethodWrapperRegistration.CreateDefaultMethodMap();

    private IMethodWrapper? _wrapper;
    private object? _result;
    private Exception? _caughtException;

    [Given(@"the ""(.*)"" wrapper")]
    public void GivenTheWrapper(string name)
    {
        MethodMap.ContainsKey(name).Should().BeTrue($"the '{name}' wrapper should be registered by default");
        _wrapper = MethodMap[name];
    }

    [When(@"I execute the wrapper with arguments")]
    public void WhenIExecuteTheWrapperWithArguments(Table table)
    {
        _wrapper.Should().NotBeNull();

        object[] arguments = table.Rows
            .Select(row => ParseArgument(row["value"]))
            .ToArray();

        Execute(arguments);
    }

    [When(@"I execute the wrapper with delimited arguments ""(.*)""")]
    public void WhenIExecuteTheWrapperWithDelimitedArguments(string delimitedArguments)
    {
        object[] arguments = delimitedArguments
            .Split('^')
            .Select(ParseArgument)
            .ToArray();

        Execute(arguments);
    }

    [When(@"I execute the wrapper with no arguments")]
    public void WhenIExecuteTheWrapperWithNoArguments() => Execute(Array.Empty<object>());

    [Then(@"the wrapper result should be (.*)")]
    public void ThenTheWrapperResultShouldBe(decimal expected)
    {
        _caughtException.Should().BeNull();
        _result.Should().NotBeNull();
        Convert.ToDecimal(_result, CultureInfo.InvariantCulture).Should().Be(expected);
    }

    [Then(@"the wrapper execution should fail validation")]
    public void ThenTheWrapperExecutionShouldFailValidation()
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
        if (bool.TryParse(raw, out bool boolValue))
            return boolValue;

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
            return intValue;

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
            return doubleValue;

        return raw;
    }
}
