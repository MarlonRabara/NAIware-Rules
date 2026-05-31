using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NAIware.RuleService.Contracts;
using Xunit;

namespace NAIware.Rules.Tests.Api;

/// <summary>
/// End-to-end integration tests that boot the <c>NAIware.RuleService</c> Web API in-process and
/// call it over HTTP, exercising the full pipeline: a MISMO XML loan file is translated into the
/// <c>Mortgage.Model</c> domain model by <c>Mortgage.Model.Translators.MISMO</c>, then evaluated
/// against a rules library.
/// </summary>
/// <remarks>
/// The model and translator assemblies and the sample MISMO document live in
/// <c>tests/resources</c>. The service loads those assemblies on demand via reflection, so they
/// are referenced by absolute path rather than as project references.
/// </remarks>
public sealed class RuleServiceApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ModelTypeName =
        "Mortgage.Model.Loans.LoanApplication, Mortgage.Model, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

    private const string TranslatorTypeName =
        "Mortgage.Model.Translators.MISMO.MortgageFileMismoTranslator, Mortgage.Model.Translators.MISMO, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

    private readonly WebApplicationFactory<Program> _factory;

    public RuleServiceApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/rules/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Evaluate_translates_mismo_xml_and_runs_rules_via_translator()
    {
        HttpClient client = _factory.CreateClient();

        var request = new EvaluateModelRequest
        {
            ModelAssemblyPath = TestResources.Path("Mortgage.Model.dll"),
            ModelQualifiedTypeName = ModelTypeName,
            SerializerAssemblyPath = TestResources.Path("Mortgage.Model.Translators.MISMO.dll"),
            SerializerQualifiedTypeName = TranslatorTypeName,
            Format = ModelPayloadFormat.Xml,
            PayloadPath = TestResources.Path("sample-loan-application.xml"),
            LibraryPath = TestResources.Path("MortgageEligibilityRules.json"),
            IncludeDiagnostics = true,
            ExecutionMode = "Lenient"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/rules/evaluate", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        EvaluateModelResponse? result = await response.Content.ReadFromJsonAsync<EvaluateModelResponse>();

        result.Should().NotBeNull();
        result!.ContextName.Should().Be("Loan");
        result.Succeeded.Should().BeTrue();
        result.TotalEvaluated.Should().Be(3);

        // The sample applicant is Marlon with a $450,000 requested loan amount.
        result.Matches.Should().Contain(m => m.Code == "BORR-001");
        result.Matches.Should().Contain(m => m.Code == "AMT-001");

        // The high-balance rule (> $1,000,000) should not fire for a $450,000 loan.
        result.Mismatches.Should().Contain(m => m.ExpressionName == "High Balance Loan");
    }

    [Fact]
    public async Task Evaluate_accepts_inline_xml_payload()
    {
        HttpClient client = _factory.CreateClient();
        string xml = await File.ReadAllTextAsync(TestResources.Path("sample-loan-application.xml"));
        string libraryJson = await File.ReadAllTextAsync(TestResources.Path("MortgageEligibilityRules.json"));

        var request = new EvaluateModelRequest
        {
            ModelAssemblyPath = TestResources.Path("Mortgage.Model.dll"),
            ModelQualifiedTypeName = ModelTypeName,
            SerializerAssemblyPath = TestResources.Path("Mortgage.Model.Translators.MISMO.dll"),
            SerializerQualifiedTypeName = TranslatorTypeName,
            Format = ModelPayloadFormat.Xml,
            Payload = xml,
            LibraryJson = libraryJson,
            IncludeDiagnostics = true
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/rules/evaluate", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        EvaluateModelResponse? result = await response.Content.ReadFromJsonAsync<EvaluateModelResponse>();

        result.Should().NotBeNull();
        result!.TotalEvaluated.Should().Be(3);
        result.Matches.Should().Contain(m => m.Code == "BORR-001");
    }

    [Fact]
    public async Task Evaluate_returns_bad_request_when_model_assembly_missing()
    {
        HttpClient client = _factory.CreateClient();

        var request = new EvaluateModelRequest
        {
            ModelAssemblyPath = TestResources.Directory + "\\DoesNotExist.dll",
            ModelQualifiedTypeName = ModelTypeName,
            Format = ModelPayloadFormat.Xml,
            Payload = "<MESSAGE />",
            LibraryPath = TestResources.Path("MortgageEligibilityRules.json")
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/rules/evaluate", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
