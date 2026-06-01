using System.Collections.Generic;
using FluentAssertions;
using NAIware.Rules.Models;
using NAIware.Rules.Validation;
using Xunit;

namespace NAIware.Rules.Tests.Api;

/// <summary>
/// Regression tests for <see cref="RuleValidationService"/> property-path resolution against
/// collection members. These guard the editor's Validate behavior so that collection-level paths
/// such as <c>AdditionalBorrowers.Count</c>, indexed element access (<c>AdditionalBorrowers.0.FirstName</c>),
/// and member-after-collection (<c>Assets.CashOrMarketValueAmount</c>) resolve the same way the
/// runtime <c>ParameterFactory</c> emits them.
/// </summary>
public sealed class RuleValidationServiceCollectionPathTests
{
    private sealed class Borrower
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    private sealed class Asset
    {
        public decimal CashOrMarketValueAmount { get; set; }
    }

    private enum LoanPurposeType
    {
        Unknown,
        Purchase,
        Refinance,
        CashOutRefinance
    }

    private sealed class LoanTerms
    {
        public LoanPurposeType LoanPurposeType { get; set; }
    }

    private sealed class LoanApplication
    {
        public IReadOnlyList<Borrower> AdditionalBorrowers { get; set; } = new List<Borrower>();
        public List<Asset> Assets { get; set; } = new();
        public LoanTerms Terms { get; set; } = new();
    }

    private sealed class StubMetadataProvider : IContextMetadataProvider
    {
        public ContextMetadata? GetMetadata(RuleContext context) =>
            new(typeof(LoanApplication), new List<string>());
    }

    private static List<ValidationIssue> Validate(string expression)
    {
        var service = new RuleValidationService(new StubMetadataProvider());
        var context = new RuleContext { Name = "Loan", QualifiedTypeName = typeof(LoanApplication).AssemblyQualifiedName! };
        return service.ValidateExpression(context, expression, resultCode: "X", resultMessage: "Y", ruleName: "draft");
    }

    [Theory]
    [InlineData("AdditionalBorrowers.Count = 1")]
    [InlineData("Assets.Count > 2")]
    [InlineData("AdditionalBorrowers.0.FirstName = \"Maria\"")]
    [InlineData("Assets.0.CashOrMarketValueAmount >= 40000")]
    public void Valid_collection_paths_produce_no_resolution_error(string expression)
    {
        List<ValidationIssue> issues = Validate(expression);

        issues.Should().NotContain(
            i => i.Message.Contains("could not be resolved"),
            "collection path in '{0}' should resolve", expression);
    }

    [Theory]
    [InlineData("AdditionalBorrowers.Nonexistent = 1")]
    [InlineData("Assets.MissingProperty >= 40000")]
    public void Invalid_collection_paths_produce_a_resolution_error(string expression)
    {
        List<ValidationIssue> issues = Validate(expression);

        issues.Should().Contain(
            i => i.Message.Contains("could not be resolved"),
            "invalid path in '{0}' should be reported", expression);
    }

    [Theory]
    [InlineData("Terms.LoanPurposeType = LoanPurposeType.Refinance")]
    [InlineData("Terms.LoanPurposeType <> LoanPurposeType.Refinance")]
    [InlineData("Terms.LoanPurposeType != LoanPurposeType.Purchase")]
    public void Enum_literal_operands_do_not_produce_a_resolution_error(string expression)
    {
        List<ValidationIssue> issues = Validate(expression);

        issues.Should().NotContain(
            i => i.Message.Contains("could not be resolved"),
            "enum literal in '{0}' should be recognized", expression);
        issues.Should().NotContain(
            i => i.Message.Contains("Type mismatch"),
            "enum literal in '{0}' should be type-compatible with the enum property", expression);
    }

    [Fact]
    public void Unknown_enum_member_is_still_reported()
    {
        List<ValidationIssue> issues = Validate("Terms.LoanPurposeType = LoanPurposeType.DoesNotExist");

        issues.Should().Contain(
            i => i.Message.Contains("could not be resolved"),
            "an undefined enum member should not be treated as a valid literal");
    }
}
