using FluentAssertions;
using NAIware.Rules;
using NAIware.Rules.Formulae;
using Reqnroll;

namespace NAIware.Rules.Tests.StepDefinitions;

[Binding]
public class FormulaeEngineSteps
{
    private Engine _engine = new();
    private FormulaTree? _parsedFormulaTree;

    [Given(@"a formulae engine with parameters ""(.*)"" set to (.*) and ""(.*)"" set to (.*)")]
    public void GivenAFormulaeEngineWithParameters(string name1, int value1, string name2, int value2)
    {
        _engine = new Engine();
        _engine.AddParameter(new GenericParameter<decimal>(name1, name1, value1));
        _engine.AddParameter(new GenericParameter<decimal>(name2, name2, value2));
    }

    [Given(@"a formulae engine with a single parameter ""(.*)"" set to (.*)")]
    public void GivenAFormulaeEngineWithASingleParameter(string name, int value)
    {
        _engine = new Engine();
        _engine.AddParameter(new GenericParameter<decimal>(name, name, value));
    }

    [When(@"I add a formula ""(.*)""")]
    public void WhenIAddAFormula(string expression)
    {
        _engine.AddFormula(expression);
    }

    [When(@"I evaluate formula (.*)")]
    public void WhenIEvaluateFormula(int index)
    {
        // Evaluation is done in the Then step using the index
    }

    [When(@"I parse the formula ""(.*)""")]
    public void WhenIParseTheFormula(string expression)
    {
        _parsedFormulaTree = _engine.Parse(expression);
    }

    [Then(@"the formula result should be (.*)")]
    public void ThenTheFormulaResultShouldBe(decimal expected)
    {
        var formulas = _engine.GetFormulae(null);
        formulas.Should().NotBeNullOrEmpty();
        decimal? result = formulas![0].Evaluate();
        result.Should().Be(expected);
    }

    [Then(@"the rendered formula should contain ""(.*)""")]
    public void ThenTheRenderedFormulaShouldContain(string expected)
    {
        _parsedFormulaTree.Should().NotBeNull();
        _parsedFormulaTree!.RenderExpression().Should().Contain(expected);
    }
}
