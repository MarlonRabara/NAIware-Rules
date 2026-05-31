using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NAIware.Rules.Tests.Api;
using NAIware.RuleService.Contracts;
using Reqnroll;

namespace NAIware.Rules.Tests.StepDefinitions;

/// <summary>
/// Step definitions backing <c>RuleServiceApi.feature</c>. They boot the
/// <c>NAIware.RuleService</c> Web API in-process via <see cref="WebApplicationFactory{TEntryPoint}"/>
/// and exercise the full evaluation pipeline over HTTP: a serialized model (MISMO XML translated by
/// <c>Mortgage.Model.Translators.MISMO</c>) is evaluated against a rules library.
/// </summary>
/// <remarks>
/// The factory is expensive to start, so it is shared for the whole test run and disposed in a
/// <see cref="BeforeTestRunAttribute"/>/<see cref="AfterTestRunAttribute"/> pair. Per-scenario state
/// (the request under construction and the HTTP response) lives on the instance, which Reqnroll
/// creates fresh for each scenario.
/// </remarks>
[Binding]
public sealed class RuleServiceApiSteps
{
    private const string ModelTypeName =
        "Mortgage.Model.Loans.LoanApplication, Mortgage.Model, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

    private const string TranslatorTypeName =
        "Mortgage.Model.Translators.MISMO.MortgageFileMismoTranslator, Mortgage.Model.Translators.MISMO, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

    private static WebApplicationFactory<Program> _factory = null!;

    private readonly EvaluateModelRequest _request = new();
    private readonly ValidateExpressionRequest _validateRequest = new();
    private readonly ValidateLibraryRequest _validateLibraryRequest = new();
    private HttpResponseMessage _response = null!;
    private EvaluateModelResponse? _result;
    private ValidationResponse? _validation;

    [BeforeTestRun]
    public static void StartService() => _factory = new WebApplicationFactory<Program>();

    [AfterTestRun]
    public static void StopService() => _factory.Dispose();

    [Given(@"a request for the ""(.*)"" loan application model")]
    public void GivenARequestForTheLoanApplicationModel(string _)
    {
        _request.ModelAssemblyPath = TestResources.Path("Mortgage.Model.dll");
        _request.ModelQualifiedTypeName = ModelTypeName;
    }

    [Given(@"a request referencing a missing model assembly ""(.*)""")]
    public void GivenARequestReferencingAMissingModelAssembly(string fileName)
    {
        _request.ModelAssemblyPath = System.IO.Path.Combine(TestResources.Directory, fileName);
        _request.ModelQualifiedTypeName = ModelTypeName;
    }

    [Given(@"the request uses the MISMO translator")]
    public void GivenTheRequestUsesTheMismoTranslator()
    {
        _request.SerializerAssemblyPath = TestResources.Path("Mortgage.Model.Translators.MISMO.dll");
        _request.SerializerQualifiedTypeName = TranslatorTypeName;
    }

    [Given(@"the request payload is the file ""(.*)"" as ""(.*)""")]
    public void GivenTheRequestPayloadIsTheFile(string fileName, string format)
    {
        _request.PayloadPath = TestResources.Path(fileName);
        _request.Format = ParseFormat(format);
    }

    [Given(@"the request payload is the inline content of file ""(.*)"" as ""(.*)""")]
    public async Task GivenTheRequestPayloadIsTheInlineContentOfFile(string fileName, string format)
    {
        _request.Payload = await File.ReadAllTextAsync(TestResources.Path(fileName));
        _request.Format = ParseFormat(format);
    }

    [Given(@"the request payload is the inline content ""(.*)"" as ""(.*)""")]
    public void GivenTheRequestPayloadIsTheInlineContent(string content, string format)
    {
        _request.Payload = content;
        _request.Format = ParseFormat(format);
    }

    [Given(@"the request uses the rules library file ""(.*)""")]
    public void GivenTheRequestUsesTheRulesLibraryFile(string fileName)
    {
        _request.LibraryPath = TestResources.Path(fileName);
    }

    [Given(@"the request uses the inline rules library from file ""(.*)""")]
    public async Task GivenTheRequestUsesTheInlineRulesLibraryFromFile(string fileName)
    {
        _request.LibraryJson = await File.ReadAllTextAsync(TestResources.Path(fileName));
    }

    [Given(@"the request execution mode is ""(.*)""")]
    public void GivenTheRequestExecutionModeIs(string mode)
    {
        _request.ExecutionMode = mode;
        _request.IncludeDiagnostics = true;
    }

    [When(@"I request the service health endpoint")]
    public async Task WhenIRequestTheServiceHealthEndpoint()
    {
        HttpClient client = _factory.CreateClient();
        _response = await client.GetAsync("/api/rules/health");
    }

    [When(@"I post the evaluation request")]
    public async Task WhenIPostTheEvaluationRequest()
    {
        HttpClient client = _factory.CreateClient();
        _response = await client.PostAsJsonAsync("/api/rules/evaluate", _request);

        if (_response.StatusCode == HttpStatusCode.OK)
        {
            _result = await _response.Content.ReadFromJsonAsync<EvaluateModelResponse>();
        }
    }

    [Then(@"the response status should be ""(.*)""")]
    public void ThenTheResponseStatusShouldBe(string expectedStatus)
    {
        HttpStatusCode expected = Enum.Parse<HttpStatusCode>(expectedStatus, ignoreCase: true);
        _response.StatusCode.Should().Be(expected);
    }

    [Then(@"the evaluated context name should be ""(.*)""")]
    public void ThenTheEvaluatedContextNameShouldBe(string contextName)
    {
        RequireResult().ContextName.Should().Be(contextName);
    }

    [Then(@"the evaluation should have succeeded")]
    public void ThenTheEvaluationShouldHaveSucceeded()
    {
        RequireResult().Succeeded.Should().BeTrue();
    }

    [Then(@"the total evaluated rules should be (\d+)")]
    public void ThenTheTotalEvaluatedRulesShouldBe(int total)
    {
        RequireResult().TotalEvaluated.Should().Be(total);
    }

    [Then(@"the matches should contain code ""(.*)""")]
    public void ThenTheMatchesShouldContainCode(string code)
    {
        RequireResult().Matches.Should().Contain(m => m.Code == code);
    }

    [Then(@"the mismatches should contain expression ""(.*)""")]
    public void ThenTheMismatchesShouldContainExpression(string expressionName)
    {
        RequireResult().Mismatches.Should().Contain(m => m.ExpressionName == expressionName);
    }

    [Given(@"a validation request for the ""(.*)"" loan application model")]
    public void GivenAValidationRequestForTheLoanApplicationModel(string _)
    {
        _validateRequest.ModelAssemblyPath = TestResources.Path("Mortgage.Model.dll");
        _validateRequest.ModelQualifiedTypeName = ModelTypeName;
        _validateRequest.ContextName = "Loan";
    }

    [Given(@"the draft expression is ""(.*)""")]
    public void GivenTheDraftExpressionIs(string expression)
    {
        _validateRequest.Expression = expression;
    }

    [Given(@"the draft result code is ""(.*)"" and message ""(.*)""")]
    public void GivenTheDraftResultCodeAndMessage(string code, string message)
    {
        _validateRequest.ResultCode = code;
        _validateRequest.ResultMessage = message;
    }

    [Given(@"a library validation request using the inline rules library from file ""(.*)""")]
    public async Task GivenALibraryValidationRequestUsingTheInlineRulesLibrary(string fileName)
    {
        // The persisted library stores SourceAssemblyPath as a file name relative to the library;
        // rewrite it to the absolute test-resource path so the service can reflect over the model.
        string json = await File.ReadAllTextAsync(TestResources.Path(fileName));
        string absoluteAssembly = TestResources.Path("Mortgage.Model.dll").Replace("\\", "\\\\");
        json = json.Replace("\"Mortgage.Model.dll\"", $"\"{absoluteAssembly}\"");
        _validateLibraryRequest.LibraryJson = json;
    }

    [When(@"I post the validation request")]
    public async Task WhenIPostTheValidationRequest()
    {
        HttpClient client = _factory.CreateClient();
        _response = await client.PostAsJsonAsync("/api/rules/validate", _validateRequest);

        if (_response.StatusCode == HttpStatusCode.OK)
        {
            _validation = await _response.Content.ReadFromJsonAsync<ValidationResponse>();
        }
    }

    [When(@"I post the library validation request")]
    public async Task WhenIPostTheLibraryValidationRequest()
    {
        HttpClient client = _factory.CreateClient();
        _response = await client.PostAsJsonAsync("/api/rules/validate-library", _validateLibraryRequest);

        if (_response.StatusCode == HttpStatusCode.OK)
        {
            _validation = await _response.Content.ReadFromJsonAsync<ValidationResponse>();
        }
    }

    [Then(@"the draft should be valid")]
    public void ThenTheDraftShouldBeValid()
    {
        RequireValidation().IsValid.Should().BeTrue();
    }

    [Then(@"the draft should be invalid")]
    public void ThenTheDraftShouldBeInvalid()
    {
        RequireValidation().IsValid.Should().BeFalse();
    }

    [Then(@"the validation should report (\d+) errors")]
    public void ThenTheValidationShouldReportErrors(int errors)
    {
        RequireValidation().ErrorCount.Should().Be(errors);
    }

    [Then(@"the validation issues should contain ""(.*)""")]
    public void ThenTheValidationIssuesShouldContain(string fragment)
    {
        RequireValidation().Issues.Should().Contain(i => i.Message.Contains(fragment));
    }

    private EvaluateModelResponse RequireResult() =>
        _result ?? throw new InvalidOperationException(
            "No evaluation result was parsed. The response status was " + _response.StatusCode + ".");

    private ValidationResponse RequireValidation() =>
        _validation ?? throw new InvalidOperationException(
            "No validation result was parsed. The response status was " + _response.StatusCode + ".");

    private static ModelPayloadFormat ParseFormat(string format) =>
        Enum.Parse<ModelPayloadFormat>(format, ignoreCase: true);
}
