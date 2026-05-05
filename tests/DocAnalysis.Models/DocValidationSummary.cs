using System.Text.Json;
using System.Text.Json.Serialization;

namespace docAnalysis.Models;

/// <summary>
/// Summary of document validation results.
/// </summary>
public sealed class DocValidationSummary
{
    public DocValidationSummary()
    {
        Issues = new List<ValidationIssue>();
        ExtensionData = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True when the document analysis result requires manual or business review.
    /// </summary>
    [JsonPropertyName("requiresReview")]
    public bool RequiresReview { get; set; }

    /// <summary>
    /// Confidence threshold used to determine whether the document requires review.
    /// </summary>
    [JsonPropertyName("threshold")]
    public decimal Threshold { get; set; }

    /// <summary>
    /// Validation issues identified during document validation.
    /// </summary>
    [JsonPropertyName("issues")]
    public List<ValidationIssue>? Issues { get; set; }

    /// <summary>
    /// Preserves any provider-specific validation properties not represented by this model.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
