using FluentAssertions;
using NAIware.Rules.Processing;
using Xunit;

namespace NAIware.Rules.Tests.Processing;

/// <summary>
/// Unit tests for <see cref="MethodCallResolver"/>, which pre-evaluates formula method calls inside rule
/// expressions and rewrites them as strongly-typed synthetic parameters so the boolean rule engine can
/// evaluate comparisons such as <c>LEFT(PrimaryBorrower.FirstName, 4) = "Marl"</c>.
/// </summary>
public sealed class MethodCallResolverTests
{
    public sealed class Borrower
    {
        public string FirstName { get; set; } = "Marlon";
        public int Age { get; set; } = 42;
        public decimal Income { get; set; } = 95000m;
    }

    public sealed class App
    {
        public Borrower PrimaryBorrower { get; set; } = new();
    }

    private static Parameters BuildParameters(App app) =>
        new ParameterFactory().CreateParameters(app)!;

    private static bool Evaluate(string expression, Parameters parameters)
    {
        MethodResolutionResult resolved = new MethodCallResolver().Resolve(expression, parameters);

        var engine = new NAIware.Rules.Rules.Engine();
        engine.Parameters.Add(resolved.Parameters);
        engine.AddRule(resolved.Expression, "r");
        return engine.Execute().Exists(i => i.Name == "r");
    }

    [Fact]
    public void Left_of_string_property_equals_literal_matches()
    {
        var parameters = BuildParameters(new App());

        Evaluate("LEFT(PrimaryBorrower.FirstName, 4) = \"Marl\"", parameters).Should().BeTrue();
    }

    [Fact]
    public void Left_of_string_property_not_equal_when_literal_differs()
    {
        var parameters = BuildParameters(new App());

        Evaluate("LEFT(PrimaryBorrower.FirstName, 4) = \"Jane\"", parameters).Should().BeFalse();
    }

    [Fact]
    public void Upper_of_string_property_matches()
    {
        var parameters = BuildParameters(new App());

        Evaluate("UPPER(PrimaryBorrower.FirstName) = \"MARLON\"", parameters).Should().BeTrue();
    }

    [Fact]
    public void Numeric_method_round_compares_against_number()
    {
        var parameters = BuildParameters(new App());

        Evaluate("ROUND(PrimaryBorrower.Income, 0) >= 95000", parameters).Should().BeTrue();
    }

    [Fact]
    public void Min_with_arithmetic_argument_resolves()
    {
        var parameters = BuildParameters(new App());

        Evaluate("MIN(PrimaryBorrower.Age, 30) = 30", parameters).Should().BeTrue();
    }

    [Fact]
    public void Nested_methods_resolve_innermost_first()
    {
        var parameters = BuildParameters(new App());

        Evaluate("UPPER(LEFT(PrimaryBorrower.FirstName, 4)) = \"MARL\"", parameters).Should().BeTrue();
    }

    [Fact]
    public void Expression_without_methods_is_unchanged()
    {
        var parameters = BuildParameters(new App());
        const string expression = "PrimaryBorrower.Age >= 18";

        MethodResolutionResult resolved = new MethodCallResolver().Resolve(expression, parameters);

        resolved.Expression.Should().Be(expression);
        Evaluate(expression, parameters).Should().BeTrue();
    }

    [Fact]
    public void Method_on_left_and_right_of_comparison_both_resolve()
    {
        var parameters = BuildParameters(new App());

        Evaluate("UPPER(PrimaryBorrower.FirstName) = UPPER(\"marlon\")", parameters).Should().BeTrue();
    }
}
