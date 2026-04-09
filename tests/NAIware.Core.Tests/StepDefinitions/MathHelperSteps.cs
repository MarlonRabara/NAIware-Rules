using FluentAssertions;
using NAIware.Core.Math;
using Reqnroll;

namespace NAIware.Core.Tests.StepDefinitions;

[Binding]
public class MathHelperSteps
{
    private uint _num1;
    private uint _num2;
    private uint _uintResult;
    private double _doubleValue;
    private int _digits;
    private double _doubleResult;

    [Given(@"two unsigned integers (.*) and (.*)")]
    public void GivenTwoUnsignedIntegers(uint num1, uint num2)
    {
        _num1 = num1;
        _num2 = num2;
    }

    [Given(@"a double value (.*) and (.*) digits")]
    public void GivenADoubleValueAndDigits(double value, int digits)
    {
        _doubleValue = value;
        _digits = digits;
    }

    [When(@"I calculate the GCF")]
    public void WhenICalculateTheGcf()
    {
        _uintResult = MathHelper.GCF(_num1, _num2);
    }

    [When(@"I calculate the LCM")]
    public void WhenICalculateTheLcm()
    {
        _uintResult = MathHelper.LCM(_num1, _num2);
    }

    [When(@"I round up")]
    public void WhenIRoundUp()
    {
        _doubleResult = MathHelper.RoundUp(_doubleValue, _digits);
    }

    [Then(@"the GCF result should be (.*)")]
    public void ThenTheGcfResultShouldBe(uint expected)
    {
        _uintResult.Should().Be(expected);
    }

    [Then(@"the LCM result should be (.*)")]
    public void ThenTheLcmResultShouldBe(uint expected)
    {
        _uintResult.Should().Be(expected);
    }

    [Then(@"the rounded result should be (.*)")]
    public void ThenTheRoundedResultShouldBe(double expected)
    {
        _doubleResult.Should().Be(expected);
    }
}
