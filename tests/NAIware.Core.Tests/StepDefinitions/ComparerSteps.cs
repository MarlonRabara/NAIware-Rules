using FluentAssertions;
using NAIware.Core.Collections;
using Reqnroll;

namespace NAIware.Core.Tests.StepDefinitions;

[Binding]
public class ComparerSteps
{
    private object? _first;
    private object? _second;
    private int _comparisonResult;

    private class TestObject
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    [Given(@"two identical test objects with Name ""(.*)"" and Age (.*)")]
    public void GivenTwoIdenticalTestObjects(string name, int age)
    {
        _first = new TestObject { Name = name, Age = age };
        _second = new TestObject { Name = name, Age = age };
    }

    [Given(@"a test object with Name ""(.*)"" and Age (.*)")]
    public void GivenATestObjectWithNameAndAge(string name, int age)
    {
        _first = new TestObject { Name = name, Age = age };
    }

    [Given(@"a second test object with Name ""(.*)"" and Age (.*)")]
    public void GivenASecondTestObjectWithNameAndAge(string name, int age)
    {
        _second = new TestObject { Name = name, Age = age };
    }

    [Given(@"two null test objects")]
    public void GivenTwoNullTestObjects()
    {
        _first = null;
        _second = null;
    }

    [Given(@"a null first test object")]
    public void GivenANullFirstTestObject()
    {
        _first = null;
    }

    [When(@"I compare them using the comparer")]
    public void WhenICompareThemUsingTheComparer()
    {
        var comparer = new Comparer(typeof(TestObject));
        _comparisonResult = comparer.Compare(_first, _second);
    }

    [When(@"I compare them using properties only")]
    public void WhenICompareThemUsingPropertiesOnly()
    {
        var comparer = new Comparer(typeof(TestObject), ReflectionComparisons.Properties);
        _comparisonResult = comparer.Compare(_first, _second);
    }

    [When(@"I compare them excluding ""(.*)""")]
    public void WhenICompareThemExcluding(string excludedMember)
    {
        var comparer = new Comparer(typeof(TestObject), ReflectionComparisons.Properties, excludedMember);
        _comparisonResult = comparer.Compare(_first, _second);
    }

    [Then(@"the comparison result should be (.*)")]
    public void ThenTheComparisonResultShouldBe(int expected)
    {
        _comparisonResult.Should().Be(expected);
    }

    [Then(@"the comparison result should not be 0")]
    public void ThenTheComparisonResultShouldNotBeZero()
    {
        _comparisonResult.Should().NotBe(0);
    }
}
