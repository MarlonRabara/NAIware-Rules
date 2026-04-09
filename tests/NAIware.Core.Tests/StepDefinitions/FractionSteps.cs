using FluentAssertions;
using NAIware.Core.Math;
using Reqnroll;

namespace NAIware.Core.Tests.StepDefinitions;

[Binding]
public class FractionSteps
{
    private Fraction _fraction = null!;
    private Fraction? _secondFraction;
    private Fraction? _clonedFraction;

    [Given(@"a fraction with numerator (.*) and denominator (.*)")]
    public void GivenAFractionWithNumeratorAndDenominator(uint numerator, uint denominator)
    {
        _fraction = new Fraction(numerator, denominator);
    }

    [Given(@"a fraction with numerator (.*) and denominator (.*) that is negative")]
    public void GivenAFractionWithNumeratorAndDenominatorThatIsNegative(uint numerator, uint denominator)
    {
        _fraction = new Fraction(numerator, denominator, true);
    }

    [Given(@"a fraction from decimal (.*)")]
    public void GivenAFractionFromDecimal(decimal value)
    {
        _fraction = new Fraction(value);
    }

    [Given(@"a fraction from integer (.*)")]
    public void GivenAFractionFromInteger(int value)
    {
        _fraction = new Fraction(value);
    }

    [Given(@"a second fraction with numerator (.*) and denominator (.*)")]
    public void GivenASecondFractionWithNumeratorAndDenominator(uint numerator, uint denominator)
    {
        _secondFraction = new Fraction(numerator, denominator);
    }

    [When(@"I reduce the fraction")]
    public void WhenIReduceTheFraction()
    {
        _fraction.Reduce();
    }

    [When(@"I add the fractions")]
    public void WhenIAddTheFractions()
    {
        _fraction = (_fraction + _secondFraction)!;
    }

    [When(@"I subtract the fractions")]
    public void WhenISubtractTheFractions()
    {
        _fraction = (_fraction - _secondFraction)!;
    }

    [When(@"I multiply the fractions")]
    public void WhenIMultiplyTheFractions()
    {
        _fraction = (_fraction * _secondFraction)!;
    }

    [When(@"I divide the fractions")]
    public void WhenIDivideTheFractions()
    {
        _fraction = (_fraction / _secondFraction)!;
    }

    [When(@"I clone the fraction")]
    public void WhenICloneTheFraction()
    {
        _clonedFraction = _fraction.Clone();
    }

    [Then(@"the fraction value should be (.*)")]
    public void ThenTheFractionValueShouldBe(decimal expected)
    {
        _fraction.Value.Should().Be(expected);
    }

    [Then(@"the fraction string should be ""(.*)""")]
    public void ThenTheFractionStringShouldBe(string expected)
    {
        _fraction.ToString().Should().Be(expected);
    }

    [Then(@"the fraction numerator should be (.*)")]
    public void ThenTheFractionNumeratorShouldBe(uint expected)
    {
        _fraction.Numerator.Should().Be(expected);
    }

    [Then(@"the fraction denominator should be (.*)")]
    public void ThenTheFractionDenominatorShouldBe(uint expected)
    {
        _fraction.Denominator.Should().Be(expected);
    }

    [Then(@"the cloned fraction value should equal the original")]
    public void ThenTheClonedFractionValueShouldEqualTheOriginal()
    {
        _clonedFraction.Should().NotBeNull();
        _clonedFraction!.Value.Should().Be(_fraction.Value);
        _clonedFraction.Should().NotBeSameAs(_fraction);
    }
}
