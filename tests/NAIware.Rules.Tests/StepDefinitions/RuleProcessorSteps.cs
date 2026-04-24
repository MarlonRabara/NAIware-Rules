using FluentAssertions;
using NAIware.Rules.Models;
using NAIware.Rules.Processing;
using NAIware.Rules.Runtime;
using NAIware.Rules.Tests.Models;
using Reqnroll;

namespace NAIware.Rules.Tests.StepDefinitions;

[Binding]
public class RuleProcessorSteps
{
    private RulesLibrary _library = null!;
    private RuleContext _context = null!;
    private RuleEvaluationResult? _evaluationResult;
    private Exception? _caughtException;

    [Given(@"a rules library with a LoanApplication context")]
    public void GivenARulesLibraryWithALoanApplicationContext()
    {
        _library = new RulesLibrary("TestLibrary", "Test rules library");
        _context = _library.AddContext(
            "LoanApplication",
            typeof(LoanApplication).FullName!,
            "Rules for loan applications");
    }

    [Given(@"the context has an expression ""(.*)"" with rule ""(.*)"" and result code ""(.*)"" and message ""(.*)""")]
    public void GivenTheContextHasAnExpressionWithRuleAndResult(string name, string rule, string code, string message)
    {
        _context.AddExpression(name, rule, $"Rule: {name}")
            .WithResult(code, message);
    }

    [Given(@"the expression is in category ""(.*)""")]
    public void GivenTheExpressionIsInCategory(string categoryName)
    {
        var category = _context.FindCategoryByName(categoryName)
            ?? _context.AddCategory(categoryName, $"Category: {categoryName}");

        var expression = _context.Expressions[^1];
        category.AddExpression(expression);
    }

    [Given(@"expression ""(.*)"" is deactivated")]
    public void GivenExpressionIsDeactivated(string name)
    {
        var expression = _context.Expressions.Find(e => e.Name == name);
        expression.Should().NotBeNull();
        expression!.IsActive = false;
    }

    [When(@"I evaluate a LoanApplication with no borrowers against category ""(.*)""")]
    public void WhenIEvaluateALoanApplicationWithNoBorrowersAgainstCategory(string categoryName)
    {
        var app = CreateLoanApplication(borrowerCount: 0);
        var request = new RuleEvaluationRequest(app, categoryName);
        var processor = new RuleProcessor(_library);
        _evaluationResult = processor.Evaluate(request);
    }

    [When(@"I evaluate a LoanApplication with (\d+) borrowers against category ""(.*)""")]
    public void WhenIEvaluateALoanApplicationWithBorrowersAgainstCategory(int borrowerCount, string categoryName)
    {
        var app = CreateLoanApplication(borrowerCount);
        var request = new RuleEvaluationRequest(app, categoryName);
        var processor = new RuleProcessor(_library);
        _evaluationResult = processor.Evaluate(request);
    }

    [When(@"I evaluate a LoanApplication with (\d+) borrowers against category ""(.*)"" with diagnostics")]
    public void WhenIEvaluateWithDiagnostics(int borrowerCount, string categoryName)
    {
        var app = CreateLoanApplication(borrowerCount);
        var request = new RuleEvaluationRequest(app, categoryName, includeDiagnostics: true);
        var processor = new RuleProcessor(_library);
        _evaluationResult = processor.Evaluate(request);
    }

    [When(@"I evaluate a LoanApplication with (\d+) borrowers without a category")]
    public void WhenIEvaluateWithoutCategory(int borrowerCount)
    {
        var app = CreateLoanApplication(borrowerCount);
        var request = new RuleEvaluationRequest(app);
        var processor = new RuleProcessor(_library);
        _evaluationResult = processor.Evaluate(request);
    }

    [When(@"I evaluate a LoanApplication with no borrowers without a category")]
    public void WhenIEvaluateNoBorrowersWithoutCategory()
    {
        var app = CreateLoanApplication(borrowerCount: 0);
        var request = new RuleEvaluationRequest(app);
        var processor = new RuleProcessor(_library);
        _evaluationResult = processor.Evaluate(request);
    }

    [When(@"I revise expression ""(.*)"" to ""(.*)"" with note ""(.*)""")]
    public void WhenIReviseExpression(string name, string newExpression, string note)
    {
        var expression = _context.Expressions.Find(e => e.Name == name);
        expression.Should().NotBeNull();
        expression!.Revise(newExpression, note);
    }

    [When(@"I evaluate an unregistered object type")]
    public void WhenIEvaluateAnUnregisteredObjectType()
    {
        try
        {
            var processor = new RuleProcessor(_library);
            processor.Evaluate(new RuleEvaluationRequest("some string"));
        }
        catch (Exception ex)
        {
            _caughtException = ex;
        }
    }

    [Then(@"the evaluation result should have (\d+) match(?:es)?")]
    public void ThenTheEvaluationResultShouldHaveMatches(int expectedCount)
    {
        _evaluationResult.Should().NotBeNull();
        _evaluationResult!.Matches.Count.Should().Be(expectedCount);
    }

    [Then(@"the evaluation result should have (\d+) mismatch(?:es)?")]
    public void ThenTheEvaluationResultShouldHaveMismatches(int expectedCount)
    {
        _evaluationResult.Should().NotBeNull();
        _evaluationResult!.Mismatches.Count.Should().Be(expectedCount);
    }

    [Then(@"the first match should have code ""(.*)"" and message ""(.*)""")]
    public void ThenTheFirstMatchShouldHaveCodeAndMessage(string code, string message)
    {
        _evaluationResult.Should().NotBeNull();
        _evaluationResult!.Matches.Should().NotBeEmpty();
        var match = _evaluationResult.Matches[0];
        match.Result.Should().NotBeNull();
        match.Result!.Code.Should().Be(code);
        match.Result.Message.Should().Be(message);
    }

    [Then(@"the first mismatch diagnostic should contain parameter ""(.*)""")]
    public void ThenTheFirstMismatchDiagnosticShouldContainParameter(string parameterName)
    {
        _evaluationResult.Should().NotBeNull();
        _evaluationResult!.Mismatches.Should().NotBeEmpty();
        var mismatch = _evaluationResult.Mismatches[0];
        mismatch.Diagnostic.Should().NotBeNull();
        mismatch.Diagnostic!.EvaluatedParameters.Should().ContainKey(parameterName);
    }

    [Then(@"expression ""(.*)"" should be at version (\d+)")]
    public void ThenExpressionShouldBeAtVersion(string name, int expectedVersion)
    {
        var expression = _context.Expressions.Find(e => e.Name == name);
        expression.Should().NotBeNull();
        expression!.Version.Should().Be(expectedVersion);
    }

    [Then(@"expression ""(.*)"" should have (\d+) version history entries")]
    public void ThenExpressionShouldHaveVersionHistoryEntries(string name, int expectedCount)
    {
        var expression = _context.Expressions.Find(e => e.Name == name);
        expression.Should().NotBeNull();
        expression!.Versions.Count.Should().Be(expectedCount);
    }

    [Then(@"a context resolution error should be raised")]
    public void ThenAContextResolutionErrorShouldBeRaised()
    {
        _caughtException.Should().NotBeNull();
        _caughtException.Should().BeOfType<InvalidOperationException>();
        _caughtException!.Message.Should().Contain("No rule context found");
    }

    private static LoanApplication CreateLoanApplication(int borrowerCount)
    {
        var app = new LoanApplication
        {
            Property = new Property
            {
                StreetAddress = "123 Test St",
                City = "TestCity",
                State = "TX",
                Zip = "75001"
            }
        };

        for (int i = 0; i < borrowerCount; i++)
        {
            app.Borrowers.Add(new Borrower
            {
                FirstName = $"Borrower{i}",
                LastName = "Test",
                BirthDate = new DateTime(1960, 1, 1)
            });
        }

        return app;
    }
}
