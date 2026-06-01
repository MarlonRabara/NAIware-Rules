using NAIware.Rules.Models;
using NAIware.Rules.Validation;
using NAIware.RuleService.Contracts;

namespace NAIware.RuleService.Services;

/// <summary>
/// Orchestrates rule validation for the HTTP API: validates either a single draft expression against
/// a model type or an entire rules library, and projects the resulting
/// <see cref="ValidationIssue"/> records onto a transport-friendly <see cref="ValidationResponse"/>.
/// </summary>
public sealed class RuleValidationApiService
{
    private readonly RuleValidationService _validator;
    private readonly RulesLibraryLoader _libraryLoader;

    /// <summary>Creates a new validation API service.</summary>
    public RuleValidationApiService(RuleValidationService validator, RulesLibraryLoader libraryLoader)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(libraryLoader);
        _validator = validator;
        _libraryLoader = libraryLoader;
    }

    /// <summary>Validates a single draft expression against a model type.</summary>
    /// <param name="request">The draft expression request.</param>
    /// <returns>The structured validation response.</returns>
    public ValidationResponse ValidateExpression(ValidateExpressionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Expression))
            throw new ArgumentException("Expression is required.", nameof(request));

        var context = new RuleContext
        {
            Name = string.IsNullOrWhiteSpace(request.ContextName)
                ? DeriveSimpleName(request.ModelQualifiedTypeName)
                : request.ContextName,
            QualifiedTypeName = request.ModelQualifiedTypeName,
            SourceAssemblyPath = request.ModelAssemblyPath
        };

        List<ValidationIssue> issues = _validator.ValidateExpression(
            context,
            request.Expression,
            request.ResultCode,
            request.ResultMessage,
            request.RuleName);

        return Map(issues);
    }

    /// <summary>Validates an entire rules library document.</summary>
    /// <param name="request">The library validation request.</param>
    /// <returns>The structured validation response.</returns>
    public ValidationResponse ValidateLibrary(ValidateLibraryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        RulesLibrary library = LoadLibrary(request);
        List<ValidationIssue> issues = _validator.Validate(library);
        return Map(issues);
    }

    private RulesLibrary LoadLibrary(ValidateLibraryRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.LibraryJson))
            return _libraryLoader.LoadFromJson(request.LibraryJson);

        if (!string.IsNullOrWhiteSpace(request.LibraryPath))
            return _libraryLoader.LoadFromFile(request.LibraryPath);

        throw new InvalidOperationException("Either LibraryJson or LibraryPath must be supplied.");
    }

    private static ValidationResponse Map(List<ValidationIssue> issues)
    {
        int errors = issues.Count(i => string.Equals(i.Severity, "Error", StringComparison.OrdinalIgnoreCase));
        int warnings = issues.Count(i => string.Equals(i.Severity, "Warning", StringComparison.OrdinalIgnoreCase));

        return new ValidationResponse
        {
            IsValid = errors == 0,
            ErrorCount = errors,
            WarningCount = warnings,
            Issues = issues.Select(i => new ValidationIssueResult
            {
                Severity = i.Severity,
                Message = i.Message,
                Context = i.Context,
                Category = i.Category,
                Rule = i.Rule,
                RuleId = i.RuleId
            }).ToList()
        };
    }

    private static string DeriveSimpleName(string qualifiedTypeName)
    {
        if (string.IsNullOrWhiteSpace(qualifiedTypeName)) return "Context";

        string fullName = qualifiedTypeName.Split(',')[0].Trim();
        int lastDot = fullName.LastIndexOf('.');
        return lastDot >= 0 && lastDot < fullName.Length - 1 ? fullName[(lastDot + 1)..] : fullName;
    }
}
