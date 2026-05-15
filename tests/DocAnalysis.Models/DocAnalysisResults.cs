using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocumentAnalysis.Models;

/// <summary>
/// Represents extracted metadata, normalized fields, and captured OCR/document fields
/// returned by a document analysis system.
/// </summary>
public sealed class DocAnalysisResults
{
    public DocAnalysisResults()
    {
        Metadata = new DocumentMetadata();
        Fields = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        CapturedFields = new Dictionary<string, CapturedField>(StringComparer.OrdinalIgnoreCase);
        ExtensionData = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
    }

    [JsonPropertyName("metadata")]
    public DocumentMetadata? Metadata { get; set; }

    /// <summary>
    /// Normalized key/value fields. Values are JsonElement so strings, numbers, booleans, nulls,
    /// arrays, or nested provider-specific objects can be preserved.
    /// </summary>
    [JsonPropertyName("fields")]
    public Dictionary<string, JsonElement>? Fields { get; set; }

    /// <summary>
    /// Captured fields keyed by provider-generated field id, such as field001, field002, etc.
    /// </summary>
    [JsonPropertyName("capturedFields")]
    public Dictionary<string, CapturedField>? CapturedFields { get; set; }

    /// <summary>
    /// Preserves any provider-specific root properties not represented by this model.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
