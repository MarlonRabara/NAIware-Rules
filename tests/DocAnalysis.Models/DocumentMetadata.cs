using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocumentAnalysis.Models;

/// <summary>
/// High-level document metadata identified during analysis.
/// </summary>
public sealed class DocumentMetadata
{
    public DocumentMetadata()
    {
        ExtensionData = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
    }

    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    [JsonPropertyName("sourceFileName")]
    public string? SourceFileName { get; set; }

    [JsonPropertyName("documentType")]
    public string? DocumentType { get; set; }

    [JsonPropertyName("documentSubtype")]
    public string? DocumentSubtype { get; set; }

    [JsonPropertyName("formVersion")]
    public string? FormVersion { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
