using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocumentAnalysis.Models;

/// <summary>
/// A single issue found during document validation.
/// </summary>
public sealed class ValidationIssue
{
    public ValidationIssue()
    {
        ExtensionData = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
    }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("fieldName")]
    public string? FieldName { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("severity")]
    public string? Severity { get; set; }

    [JsonPropertyName("expectedValue")]
    public string? ExpectedValue { get; set; }

    [JsonPropertyName("actualValue")]
    public string? ActualValue { get; set; }

    [JsonPropertyName("confidence")]
    public decimal? Confidence { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
