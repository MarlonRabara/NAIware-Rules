using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocumentAnalysis.Models;

/// <summary>
/// Root object for document analysis output. This can contain the extracted document analysis results,
/// the document validation summary, or both.
/// </summary>
public sealed class DocumentAnalysisPackage
{
    /// <summary>
    /// Creates a JSON-serializable document analysis package with empty child objects.
    /// </summary>
    public DocumentAnalysisPackage()
    {
        DocAnalysisResults = new DocAnalysisResults();
        DocValidationSummary = new DocValidationSummary();
    }

    [JsonPropertyName("docAnalysisResults")]
    public DocAnalysisResults? DocAnalysisResults { get; set; }

    [JsonPropertyName("docValidationSummary")]
    public DocValidationSummary? DocValidationSummary { get; set; }

    /// <summary>
    /// Shared JSON serializer options for this model library.
    /// </summary>
    public static JsonSerializerOptions DefaultJsonSerializerOptions => new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Deserialize a document analysis package from a JSON string.
    /// </summary>
    public static DocumentAnalysisPackage FromJson(string json, JsonSerializerOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return JsonSerializer.Deserialize<DocumentAnalysisPackage>(json, options ?? DefaultJsonSerializerOptions)
            ?? new DocumentAnalysisPackage();
    }

    /// <summary>
    /// Read and deserialize a document analysis package from a JSON file.
    /// </summary>
    public static DocumentAnalysisPackage FromJsonFile(string filePath, JsonSerializerOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var json = File.ReadAllText(filePath);
        return FromJson(json, options);
    }

    /// <summary>
    /// Serialize this document analysis package to a JSON string.
    /// </summary>
    public string ToJson(JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(this, options ?? DefaultJsonSerializerOptions);
    }

    /// <summary>
    /// Serialize this document analysis package and write it to a JSON file.
    /// </summary>
    public void ToJsonFile(string filePath, JsonSerializerOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        File.WriteAllText(filePath, ToJson(options));
    }
}
