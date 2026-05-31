using FluentAssertions;
using NAIware.Rules.MethodWrappers;
using NAIware.RuleIntelligence;
using Xunit;

namespace NAIware.Rules.Tests.IntelliSense;

/// <summary>
/// Verifies that the IntelliSense function catalog and completion service advertise every formula
/// method wrapper registered with the runtime engine.
/// </summary>
public sealed class RuleFunctionCompletionTests
{
    private static readonly string[] ExpectedFunctions =
    [
        "IF", "INT", "ROUNDUP", "ROUND", "MIN", "MAX", "ABS", "SUM", "AVERAGE", "POWER",
        "CEILING", "FLOOR", "CONCAT", "TRIM", "REPLACE", "SUBSTITUTE", "LEFT", "RIGHT",
        "MID", "UPPER", "LOWER", "PROPER", "NOW", "TODAY", "DATEDIFF"
    ];

    [Fact]
    public void Provider_advertises_every_registered_method_wrapper()
    {
        var provider = new DefaultRuleFunctionProvider();

        var names = provider.GetFunctions().Select(f => f.Name).ToList();

        names.Should().BeEquivalentTo(ExpectedFunctions);
    }

    [Fact]
    public void Provider_stays_aligned_with_runtime_registration()
    {
        var methodMap = DefaultMethodWrapperRegistration.CreateDefaultMethodMap();
        var provider = new DefaultRuleFunctionProvider(methodMap);

        var providerNames = provider.GetFunctions()
            .Select(f => f.Name.ToUpperInvariant())
            .OrderBy(n => n)
            .ToList();
        var runtimeNames = methodMap.Keys
            .Select(k => k.ToUpperInvariant())
            .OrderBy(n => n)
            .ToList();

        providerNames.Should().BeEquivalentTo(runtimeNames);
    }

    [Fact]
    public void Each_function_completion_item_opens_an_argument_list()
    {
        var provider = new DefaultRuleFunctionProvider();

        foreach (var function in provider.GetFunctions())
        {
            var item = function.ToCompletionItem();
            item.Kind.Should().Be(RuleCompletionItemKind.Function);
            item.InsertText.Should().Be($"{function.Name}(");
            item.Detail.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Completion_service_suggests_functions_at_root()
    {
        var service = new RuleIntelliSenseService();
        var schema = BuildSchema();

        var response = service.GetCompletions(new RuleCompletionRequest
        {
            Schema = schema,
            Expression = string.Empty,
            CursorPosition = 0,
            MaxItems = 200
        });

        response.Items.Should().Contain(i =>
            i.Kind == RuleCompletionItemKind.Function && i.Label == "ROUND");
    }

    [Fact]
    public void Completion_service_filters_functions_by_typed_prefix()
    {
        var service = new RuleIntelliSenseService();
        var schema = BuildSchema();
        const string expression = "ROU";

        var response = service.GetCompletions(new RuleCompletionRequest
        {
            Schema = schema,
            Expression = expression,
            CursorPosition = expression.Length,
            MaxItems = 200
        });

        var functionLabels = response.Items
            .Where(i => i.Kind == RuleCompletionItemKind.Function)
            .Select(i => i.Label)
            .ToList();

        functionLabels.Should().Contain("ROUND");
        functionLabels.Should().Contain("ROUNDUP");
        functionLabels.Should().NotContain("CONCAT");
    }

    private static RuleSchema BuildSchema()
        => new ObjectTreeRuleSchemaProvider().Build(typeof(SampleContext), nameof(SampleContext));

    private sealed class SampleContext
    {
        public int Amount { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
