namespace NAIware.Rules.Serialization;

/// <summary>
/// Identifies where a model payload comes from: an on-disk file or in-memory content.
/// </summary>
/// <remarks>
/// A custom translator always consumes a file path (its contract is
/// <c>Deserialize(string filePath)</c>), whereas the built-in JSON/XML path consumes content.
/// Modeling the source explicitly lets <see cref="ModelHydrator"/> bridge the two: it reads a
/// file when content is needed, and materializes a temporary file when a translator needs one.
/// </remarks>
public sealed class ModelSource
{
    private ModelSource(string? filePath, string? content, ModelFormat format)
    {
        FilePath = filePath;
        Content = content;
        Format = format;
    }

    /// <summary>Gets the absolute file path when the source is a file; otherwise null.</summary>
    public string? FilePath { get; }

    /// <summary>Gets the in-memory content when the source is content; otherwise null.</summary>
    public string? Content { get; }

    /// <summary>Gets the payload format. Inferred from the file extension for file sources.</summary>
    public ModelFormat Format { get; }

    /// <summary>Creates a source backed by a file on disk. The format is inferred from the extension.</summary>
    /// <param name="filePath">The absolute path to the payload file.</param>
    public static ModelSource FromFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return new ModelSource(filePath, content: null, InferFormat(filePath));
    }

    /// <summary>Creates a source backed by a file on disk with an explicit format.</summary>
    /// <param name="filePath">The absolute path to the payload file.</param>
    /// <param name="format">The payload format.</param>
    public static ModelSource FromFile(string filePath, ModelFormat format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return new ModelSource(filePath, content: null, format);
    }

    /// <summary>Creates a source backed by in-memory content.</summary>
    /// <param name="content">The serialized payload content.</param>
    /// <param name="format">The payload format.</param>
    public static ModelSource FromContent(string content, ModelFormat format)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new ModelSource(filePath: null, content, format);
    }

    private static ModelFormat InferFormat(string filePath) =>
        Path.GetExtension(filePath).Equals(".xml", StringComparison.OrdinalIgnoreCase)
            ? ModelFormat.Xml
            : ModelFormat.Json;
}
