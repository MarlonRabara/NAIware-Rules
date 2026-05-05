using System.Text.Json;
using System.Text.Json.Serialization;

namespace docAnalysis.Models;

/// <summary>
/// A validation error, warning, or informational message.
/// </summary>
public sealed class ValidationMessage
{
    public ValidationMessage()
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

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
