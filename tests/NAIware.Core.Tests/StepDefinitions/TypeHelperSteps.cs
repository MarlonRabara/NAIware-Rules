using FluentAssertions;
using NAIware.Core;
using Reqnroll;

namespace NAIware.Core.Tests.StepDefinitions;

[Binding]
public class TypeHelperSteps
{
    private object? _value;
    private object? _secondValue;
    private object? _result;
    private Type? _type;
    private bool _boolResult;

    [Given(@"a value of (.*) as an object")]
    public void GivenAValueAsAnObject(int value)
    {
        _value = value;
    }

    [Given(@"a string value ""(.*)""")]
    public void GivenAStringValue(string value)
    {
        _value = value;
    }

    [Given(@"a nullable int type")]
    public void GivenANullableIntType()
    {
        _type = typeof(int?);
    }

    [Given(@"a regular int type")]
    public void GivenARegularIntType()
    {
        _type = typeof(int);
    }

    [Given(@"a null value")]
    public void GivenANullValue()
    {
        _value = null;
    }

    [Given(@"a null value and a value of (.*)")]
    public void GivenANullValueAndAValue(int secondValue)
    {
        _value = null;
        _secondValue = secondValue;
    }

    [When(@"I convert it to decimal")]
    public void WhenIConvertItToDecimal()
    {
        _result = TypeHelper.Convert<decimal>(_value);
    }

    [When(@"I convert it to integer")]
    public void WhenIConvertItToInteger()
    {
        _result = TypeHelper.Convert<int>(_value);
    }

    [When(@"I check if the type is nullable")]
    public void WhenICheckIfTheTypeIsNullable()
    {
        _boolResult = TypeHelper.IsNullable(_type!);
    }

    [When(@"I get the underlying type from nullable")]
    public void WhenIGetTheUnderlyingTypeFromNullable()
    {
        _type = TypeHelper.GetTypeFromNullableType(_type!);
    }

    [When(@"I check if the value is empty")]
    public void WhenICheckIfTheValueIsEmpty()
    {
        _boolResult = TypeHelper.IsEmpty(_value);
    }

    [When(@"I coalesce the values")]
    public void WhenICoalesceTheValues()
    {
        _result = TypeHelper.Coalesce<int>(_value, _secondValue);
    }

    [Then(@"the result should be (.*)")]
    public void ThenTheResultShouldBe(string expected)
    {
        if (bool.TryParse(expected, out bool boolExpected))
            _boolResult.Should().Be(boolExpected);
        else if (decimal.TryParse(expected, out decimal decExpected))
            Convert.ToDecimal(_result).Should().Be(decExpected);
    }

    [Then(@"the integer result should be (.*)")]
    public void ThenTheIntegerResultShouldBe(int expected)
    {
        ((int)_result!).Should().Be(expected);
    }

    [Then(@"the underlying type should be (.*)")]
    public void ThenTheUnderlyingTypeShouldBe(string expectedTypeName)
    {
        _type!.FullName.Should().Be(expectedTypeName);
    }

    [Then(@"the coalesced result should be (.*)")]
    public void ThenTheCoalescedResultShouldBe(int expected)
    {
        ((int)_result!).Should().Be(expected);
    }
}
