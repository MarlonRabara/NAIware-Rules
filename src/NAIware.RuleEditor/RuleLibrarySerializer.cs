using System.Text.Json;
using System.Text.Json.Serialization;

namespace NAIware.RuleEditor;

/// <summary>
/// JSON serializer for <see cref="RuleLibraryDocument"/>. Uses indented output for
/// human-readable files and case-insensitive property matching on read.
/// </summary>
public static class RuleLibrarySerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Loads a library document from the specified JSON file path.
    /// </summary>
    public static RuleLibraryDocument Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string json = File.ReadAllText(path);
        RuleLibraryDocument? library = JsonSerializer.Deserialize<RuleLibraryDocument>(json, Options);
        return library ?? throw new InvalidOperationException("Unable to deserialize rule library — file is empty or invalid.");
    }

    /// <summary>
    /// Serializes and saves the library to the specified JSON file path.
    /// Updates the library's <see cref="RuleLibraryDocument.SavedUtc"/> timestamp.
    /// </summary>
    public static void Save(string path, RuleLibraryDocument library)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(library);

        library.SavedUtc = DateTimeOffset.UtcNow;
        string json = JsonSerializer.Serialize(library, Options);
        File.WriteAllText(path, json);
    }
}
