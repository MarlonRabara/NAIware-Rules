using FluentAssertions;
using NAIware.Rules;
using NAIware.Rules.Rules;
using Reqnroll;

namespace NAIware.Rules.Tests.StepDefinitions;

[Binding]
public class RulesEngineSteps
{
    private Engine _engine = new();
    private List<Identification>? _executionResults;
    private RuleTree? _parsedRuleTree;

    [Given(@"a rules engine with an integer parameter ""(.*)"" set to (.*)")]
    public void GivenARulesEngineWithAnIntegerParameter(string name, int value)
    {
        _engine = new Engine();
        _engine.AddParameter(new GenericParameter<int>(name, name, value));
    }

    [Given(@"a rules engine with a string parameter ""(.*)"" set to ""(.*)""")]
    public void GivenARulesEngineWithAStringParameter(string name, string value)
    {
        _engine = new Engine();
        _engine.AddParameter(new GenericParameter<string>(name, name, value));
    }

    [Given(@"a rules engine with integer parameters ""(.*)"" set to (.*) and ""(.*)"" set to (.*)")]
    public void GivenARulesEngineWithIntegerParameters(string name1, int value1, string name2, int value2)
    {
        _engine = new Engine();
        _engine.AddParameter(new GenericParameter<int>(name1, name1, value1));
        _engine.AddParameter(new GenericParameter<int>(name2, name2, value2));
    }

    [When(@"I add a rule ""(.*)""")]
    public void WhenIAddARule(string expression)
    {
        _engine.AddRule(expression);
    }

    [When(@"I execute the rules engine")]
    public void WhenIExecuteTheRulesEngine()
    {
        _executionResults = _engine.Execute();
    }

    [When(@"I parse the rule ""(.*)""")]
    public void WhenIParseTheRule(string expression)
    {
        _parsedRuleTree = _engine.Parse(expression);
    }

    [Then(@"the executed rule count should be (.*)")]
    public void ThenTheExecutedRuleCountShouldBe(int expected)
    {
        _executionResults.Should().NotBeNull();
        _executionResults!.Count.Should().Be(expected);
    }

    [Then(@"the rendered expression should contain ""(.*)""")]
    public void ThenTheRenderedExpressionShouldContain(string expected)
    {
        _parsedRuleTree.Should().NotBeNull();
        _parsedRuleTree!.RenderExpression().Should().Contain(expected);
    }
}
