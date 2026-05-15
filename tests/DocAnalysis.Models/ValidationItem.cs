using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocumentAnalysis.Models;

/// <summary>
/// A single validation check performed against the analyzed document.
/// </summary>
public sealed class ValidationItem
{
    public ValidationItem()
    {
        ExtensionData = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
    }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("fieldName")]
    public string? FieldName { get; set; }

    [JsonPropertyName("passed")]
    public bool? Passed { get; set; }

    [JsonPropertyName("severity")]
    public string? Severity { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("expectedValue")]
    public string? ExpectedValue { get; set; }

    [JsonPropertyName("actualValue")]
    public string? ActualValue { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
