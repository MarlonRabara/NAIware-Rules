using FluentAssertions;
using NAIware.Rules.MethodWrappers;
using Reqnroll;

namespace NAIware.Rules.Tests.StepDefinitions;

/// <summary>
/// Step definitions that evaluate formulas end-to-end through the <see cref="LogicProcessorEngine"/> using
/// the default method wrapper registration, validating the real parser and evaluator execution path.
/// </summary>
[Binding]
public sealed class FormulaMethodIntegrationSteps
{
    private readonly MethodMap _methodMap = DefaultMethodWrapperRegistration.CreateDefaultMethodMap();
    private LogicProcessorEngine? _engine;
    private decimal _result;

    [Given(@"a logic processor for ""(.*)""")]
    public void GivenALogicProcessorFor(string expression)
    {
        _engine = new LogicProcessorEngine(expression, _methodMap, new Parameters());
    }

    [When(@"I evaluate the formula as a decimal")]
    public void WhenIEvaluateTheFormulaAsADecimal()
    {
        _engine.Should().NotBeNull();
        _result = _engine!.Evaluate<decimal>();
    }

    [Then(@"the logic processor result should be (.*)")]
    public void ThenTheLogicProcessorResultShouldBe(decimal expected)
    {
        _result.Should().Be(expected);
    }
}
