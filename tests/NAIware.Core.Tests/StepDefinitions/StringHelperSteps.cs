using FluentAssertions;
using NAIware.Core.Text;
using Reqnroll;

namespace NAIware.Core.Tests.StepDefinitions;

[Binding]
public class StringHelperSteps
{
    private string? _input;
    private string? _result;
    private bool _boolResult;

    [Given(@"a variable name ""(.*)""")]
    public void GivenAVariableName(string name)
    {
        _input = name;
    }

    [Given(@"an input string ""(.*)""")]
    public void GivenAnInputString(string input)
    {
        _input = input;
    }

    [When(@"I check if the variable name is valid")]
    public void WhenICheckIfTheVariableNameIsValid()
    {
        _boolResult = StringHelper.IsValidVariable(_input!);
    }

    [When(@"I convert it to a safe URL string")]
    public void WhenIConvertItToASafeUrlString()
    {
        _result = StringHelper.ToSafeUrlString(_input);
    }

    [When(@"I convert it from a safe URL string")]
    public void WhenIConvertItFromASafeUrlString()
    {
        _result = StringHelper.FromSafeUrlString(_input);
    }

    [Then(@"the validity result should be (.*)")]
    public void ThenTheValidityResultShouldBe(bool expected)
    {
        _boolResult.Should().Be(expected);
    }

    [Then(@"the safe URL string should be ""(.*)""")]
    public void ThenTheSafeUrlStringShouldBe(string expected)
    {
        _result.Should().Be(expected);
    }

    [Then(@"the restored string should be ""(.*)""")]
    public void ThenTheRestoredStringShouldBe(string expected)
    {
        _result.Should().Be(expected);
    }
}
