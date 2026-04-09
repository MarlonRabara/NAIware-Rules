using FluentAssertions;
using NAIware.Rules.Rules;
using NAIware.Rules.Tests.Models;
using Reqnroll;

namespace NAIware.Rules.Tests.StepDefinitions;

[Binding]
public class MortgageProcessingSteps
{
    private LoanApplication _loanApplication = new();
    private string _resultMessage = string.Empty;

    [Given(@"a loan application for property ""(.*)"" ""(.*)"" ""(.*)"" ""(.*)""")]
    public void GivenALoanApplicationForProperty(string street, string city, string state, string zip)
    {
        _loanApplication = new LoanApplication
        {
            Property = new Property
            {
                StreetAddress = street,
                City = city,
                State = state,
                Zip = zip
            }
        };
    }

    [Given(@"the loan application has no borrowers")]
    public void GivenTheLoanApplicationHasNoBorrowers()
    {
        _loanApplication.Borrowers.Clear();
    }

    [Given(@"the loan application has a borrower ""(.*)"" ""(.*)"" born on ""(.*)""")]
    public void GivenTheLoanApplicationHasABorrower(string firstName, string lastName, string birthDate)
    {
        _loanApplication.Borrowers.Add(new Borrower
        {
            FirstName = firstName,
            LastName = lastName,
            BirthDate = DateTime.Parse(birthDate)
        });
    }

    [When(@"I evaluate the borrower count rule")]
    public void WhenIEvaluateTheBorrowerCountRule()
    {
        // ParameterFactory auto-extracts BorrowerCount from LoanApplication
        var factory = new ParameterFactory();
        Parameters? prms = factory.CreateParameters(_loanApplication);

        var engine = new Engine();
        engine.Parameters.Add(prms);

        engine.AddRule("BorrowerCount = 0", "NoBorrowers");

        List<Identification> results = engine.Execute();

        _resultMessage = results.Exists(r => r.Name == "NoBorrowers")
            ? "Must have at least one borrower"
            : string.Empty;
    }

    [When(@"I evaluate the reverse mortgage eligibility rule")]
    public void WhenIEvaluateTheReverseMortgageEligibilityRule()
    {
        // ParameterFactory auto-extracts nested borrower properties:
        //   Borrowers.Count, Borrowers.0.Age, Borrowers.1.Age, etc.
        var factory = new ParameterFactory();
        Parameters? prms = factory.CreateParameters(_loanApplication);

        var engine = new Engine();
        engine.Parameters.Add(prms);

        // Build a rule that checks every borrower's age.
        // The extracted parameters use indexed dot-notation: Borrowers.0.Age, Borrowers.1.Age
        bool allPass = true;
        for (int i = 0; i < _loanApplication.Borrowers.Count; i++)
        {
            string ageParam = $"Borrowers.{i}.Age";

            var borrowerEngine = new Engine();
            borrowerEngine.Parameters.Add(prms);
            borrowerEngine.AddRule($"{ageParam} >= 62", "ReverseMortgageEligible");

            List<Identification> results = borrowerEngine.Execute();
            if (!results.Exists(r => r.Name == "ReverseMortgageEligible"))
            {
                allPass = false;
                break;
            }
        }

        _resultMessage = _loanApplication.Borrowers.Count > 0 && allPass
            ? "You are eligible for a Reverse Mortgage!"
            : string.Empty;
    }

    [Then(@"the result message should be ""(.*)""")]
    public void ThenTheResultMessageShouldBe(string expectedMessage)
    {
        _resultMessage.Should().Be(expectedMessage);
    }
}
