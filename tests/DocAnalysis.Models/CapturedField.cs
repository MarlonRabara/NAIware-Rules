using System.Text.Json;
using System.Text.Json.Serialization;

namespace docAnalysis.Models;

/// <summary>
/// Represents a field captured from the document, typically from OCR or form-recognition output.
/// </summary>
public sealed class CapturedField
{
    public CapturedField()
    {
        ExtensionData = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
    }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>
    /// Optional confidence score when supplied by the provider.
    /// </summary>
    [JsonPropertyName("confidence")]
    public decimal? Confidence { get; set; }

    /// <summary>
    /// Optional page number when supplied by the provider.
    /// </summary>
    [JsonPropertyName("pageNumber")]
    public int? PageNumber { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
