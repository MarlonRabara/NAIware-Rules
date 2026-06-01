using System.Reflection;
using FluentAssertions;
using NAIware.Rules.Models;
using NAIware.Rules.Processing;
using NAIware.Rules.Runtime;
using NAIware.RuleService.Services;
using Xunit;
using Xunit.Abstractions;

namespace NAIware.Rules.Tests.Api;

/// <summary>
/// End-to-end verification that the <c>ComprehensiveMortgageRules.json</c> library evaluates every
/// authored rule correctly against the <c>comprehensive-loan-application.xml</c> MISMO document.
/// </summary>
/// <remarks>
/// This guards the shared test resources: each expression is expected to match (evaluate to true) when
/// run against the hydrated model, so a regression in any formula method wrapper, the
/// <see cref="MethodCallResolver"/>, or the resource files themselves surfaces immediately.
/// </remarks>
public sealed class ComprehensiveMortgageRulesTests
{
    private readonly ITestOutputHelper _output;

    public ComprehensiveMortgageRulesTests(ITestOutputHelper output) => _output = output;

    private static object HydrateModel()
    {
        Assembly translatorAssembly = Assembly.LoadFrom(TestResources.Path("Mortgage.Model.Translators.MISMO.dll"));
        Type translatorType = translatorAssembly.GetType(
            "Mortgage.Model.Translators.MISMO.MortgageFileMismoTranslator")!;
        MethodInfo deserialize = translatorType
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
            .First(m => m.Name == "Deserialize"
                && m.GetParameters() is [{ ParameterType: var p }]
                && p == typeof(string)
                && m.ReturnType != typeof(void));

        object? translator = deserialize.IsStatic ? null : Activator.CreateInstance(translatorType);
        return deserialize.Invoke(translator, [TestResources.Path("comprehensive-loan-application.xml")])!;
    }

    private static RuleEvaluationResult EvaluateLibrary(string? categoryName = null)
    {
        object model = HydrateModel();

        var loader = new RulesLibraryLoader();
        RulesLibrary library = loader.LoadFromFile(TestResources.Path("ComprehensiveMortgageRules.json"));

        // Align the context's persisted qualified type name with the runtime model type so the
        // reflection resolver matches unambiguously (mirrors RuleEvaluationService behavior).
        Type modelType = model.GetType();
        RuleContext context = library.Contexts.Single();
        context.QualifiedTypeName = modelType.AssemblyQualifiedName ?? modelType.FullName!;

        var processor = new RuleProcessor(library);
        var request = new RuleEvaluationRequest(model, categoryName, includeDiagnostics: true)
        {
            CategoryExecutionMode = RuleCategoryExecutionMode.IncludeDescendantLeaves
        };

        return processor.Evaluate(request);
    }

    [Fact]
    public void Every_rule_matches_against_the_comprehensive_mismo_document()
    {
        RuleEvaluationResult result = EvaluateLibrary();

        if (result.Mismatches.Count > 0)
        {
            _output.WriteLine("Unexpected mismatches:");
            foreach (RuleExpressionResult mismatch in result.Mismatches)
                _output.WriteLine($"  - {mismatch.ExpressionName}");
        }

        if (result.Errors.Count > 0)
        {
            _output.WriteLine("Errors:");
            foreach (RuleEvaluationError error in result.Errors)
                _output.WriteLine($"  - [{error.Code}] {error.Message}");
        }

        result.Errors.Should().BeEmpty("no rule expression should fail to evaluate");
        result.Mismatches.Should().BeEmpty("every authored rule is expected to match the sample data");
        result.Matches.Should().HaveCount(31, "all 31 authored expressions should match");
    }

    [Theory]
    [InlineData("TXT-001")]
    [InlineData("TXT-002")]
    [InlineData("TXT-003")]
    [InlineData("TXT-004")]
    [InlineData("TXT-005")]
    [InlineData("TXT-006")]
    [InlineData("TXT-007")]
    [InlineData("TXT-008")]
    [InlineData("TXT-009")]
    [InlineData("TXT-010")]
    [InlineData("NUM-001")]
    [InlineData("NUM-002")]
    [InlineData("NUM-003")]
    [InlineData("NUM-004")]
    [InlineData("NUM-005")]
    [InlineData("NUM-006")]
    [InlineData("NUM-007")]
    [InlineData("NUM-008")]
    [InlineData("NUM-009")]
    [InlineData("NUM-010")]
    [InlineData("NUM-011")]
    [InlineData("LOG-001")]
    [InlineData("LOG-002")]
    [InlineData("LOG-003")]
    [InlineData("COL-001")]
    [InlineData("COL-002")]
    [InlineData("COL-003")]
    [InlineData("COL-004")]
    [InlineData("COL-005")]
    [InlineData("COL-006")]
    [InlineData("COL-007")]
    public void Rule_with_result_code_matches(string resultCode)
    {
        RuleEvaluationResult result = EvaluateLibrary();

        result.Matches.Should().Contain(
            m => m.Result != null && m.Result.Code == resultCode,
            "rule producing result code '{0}' should match the sample data", resultCode);
    }
}
