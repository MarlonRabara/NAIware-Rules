using FluentAssertions;
using NAIware.Rules.Models;
using NAIware.Rules.Processing;
using NAIware.Rules.Runtime;
using NAIware.Rules.Tests.Models;
using Xunit;

namespace NAIware.Rules.Tests.Processing;

/// <summary>
/// End-to-end tests that exercise <see cref="RuleProcessor"/> with rule expressions that contain formula
/// method calls. These reproduce the original failure where an expression such as
/// <c>LEFT(Borrowers.0.FirstName, 4) = "Marl"</c> could not be evaluated by the boolean rule engine.
/// </summary>
public sealed class RuleProcessorMethodCallTests
{
    private static (RulesLibrary Library, RuleContext Context) CreateLibrary()
    {
        var library = new RulesLibrary("TestLibrary", "Test rules library");
        var context = library.AddContext(
            "LoanApplication",
            typeof(LoanApplication).FullName!,
            "Rules for loan applications");
        return (library, context);
    }

    private static LoanApplication CreateApplication(string firstName)
    {
        var app = new LoanApplication
        {
            Property = new Property { StreetAddress = "123 Test St", City = "TestCity", State = "TX", Zip = "75001" }
        };
        app.Borrowers.Add(new Borrower
        {
            FirstName = firstName,
            LastName = "Test",
            BirthDate = new DateTime(1980, 1, 1)
        });
        return app;
    }

    private static RuleEvaluationResult Evaluate(string expression, LoanApplication app)
    {
        var (library, context) = CreateLibrary();
        context.AddExpression("MethodRule", expression, "Method rule")
            .WithResult("OK", "Matched");

        var processor = new RuleProcessor(library);
        return processor.Evaluate(new RuleEvaluationRequest(app, categoryName: null, includeDiagnostics: true));
    }

    [Fact]
    public void Left_of_borrower_name_equals_literal_matches()
    {
        RuleEvaluationResult result = Evaluate(
            "LEFT(Borrowers.0.FirstName, 4) = \"Marl\"",
            CreateApplication("Marlon"));

        result.Errors.Should().BeEmpty();
        result.Matches.Should().ContainSingle(m => m.ExpressionName == "MethodRule");
    }

    [Fact]
    public void Left_of_borrower_name_does_not_match_other_literal()
    {
        RuleEvaluationResult result = Evaluate(
            "LEFT(Borrowers.0.FirstName, 4) = \"Marl\"",
            CreateApplication("Jane"));

        result.Errors.Should().BeEmpty();
        result.Matches.Should().BeEmpty();
        result.Mismatches.Should().ContainSingle(m => m.ExpressionName == "MethodRule");
    }
}
