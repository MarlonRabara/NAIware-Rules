using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace NAIware.Rules.Serialization;

/// <summary>
/// Single source of truth for hydrating a serialized model payload into a runtime model object,
/// shared by the Rule Editor and the Rule Service so both behave identically.
/// </summary>
/// <remarks>
/// <para>
/// When a custom translator/serializer is configured, the payload is presented to the translator's
/// <c>Deserialize(string filePath)</c> method — materializing a temporary file when the source is
/// in-memory content. This is required for formats (such as MISMO) that do not map directly onto
/// the model type via the built-in <see cref="XmlSerializer"/>. When no translator is configured,
/// the built-in <see cref="JsonSerializer"/> / <see cref="XmlSerializer"/> path is used.
/// </para>
/// <para>
/// Assembly loading and type resolution are delegated to an <see cref="IModelAssemblyResolver"/>,
/// so each host can supply its own collectible load-context strategy while sharing this logic.
/// </para>
/// </remarks>
public sealed class ModelHydrator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly IModelAssemblyResolver _resolver;

    /// <summary>Creates a new hydrator over the supplied assembly resolver.</summary>
    /// <param name="resolver">The resolver used to load assemblies and resolve model/serializer types.</param>
    public ModelHydrator(IModelAssemblyResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolver = resolver;
    }

    /// <summary>
    /// Hydrates the payload described by <paramref name="source"/> into an instance of
    /// <paramref name="modelType"/>, using a custom translator when one is supplied.
    /// </summary>
    /// <param name="source">The payload source (file or content).</param>
    /// <param name="modelType">The model type to hydrate into.</param>
    /// <param name="serializerAssemblyPath">Optional path to a custom translator/serializer assembly.</param>
    /// <param name="serializerQualifiedTypeName">
    /// Optional qualified name of a translator type exposing <c>Deserialize(string filePath)</c>.
    /// Both serializer parameters must be supplied together to use the translator path.
    /// </param>
    /// <returns>The hydrated model object.</returns>
    public object Hydrate(
        ModelSource source,
        Type modelType,
        string? serializerAssemblyPath = null,
        string? serializerQualifiedTypeName = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(modelType);

        bool useTranslator =
            !string.IsNullOrWhiteSpace(serializerAssemblyPath) &&
            !string.IsNullOrWhiteSpace(serializerQualifiedTypeName);

        return useTranslator
            ? HydrateWithTranslator(source, modelType, serializerAssemblyPath!, serializerQualifiedTypeName!)
            : HydrateBuiltInCore(source, modelType);
    }

    /// <summary>
    /// Hydrates a payload using only the built-in <see cref="JsonSerializer"/> /
    /// <see cref="XmlSerializer"/> path, without any custom translator. Useful for callers that
    /// have no model assembly resolver and know the payload maps directly onto the model type.
    /// </summary>
    /// <param name="source">The payload source (file or content).</param>
    /// <param name="modelType">The model type to hydrate into.</param>
    /// <returns>The hydrated model object.</returns>
    public static object HydrateBuiltIn(ModelSource source, Type modelType)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(modelType);

        return HydrateBuiltInCore(source, modelType);
    }

    private object HydrateWithTranslator(
        ModelSource source,
        Type modelType,
        string serializerAssemblyPath,
        string serializerQualifiedTypeName)
    {
        if (!File.Exists(serializerAssemblyPath))
            throw new FileNotFoundException("Serializer assembly not found.", serializerAssemblyPath);

        Type serializerType = _resolver.ResolveType(serializerAssemblyPath, serializerQualifiedTypeName);
        MethodInfo deserialize = FindDeserializeMethod(serializerType, serializerQualifiedTypeName);

        (string filePath, bool deleteAfter) = MaterializeFile(source);
        try
        {
            object? serializer = deserialize.IsStatic ? null : Activator.CreateInstance(serializerType);
            object result = deserialize.Invoke(serializer, [filePath])
                ?? throw new InvalidOperationException("Serializer deserialized to null.");

            if (!modelType.IsInstanceOfType(result))
            {
                throw new InvalidOperationException(
                    $"Serializer returned '{result.GetType().FullName}', which is not assignable to model type '{modelType.FullName}'.");
            }

            return result;
        }
        finally
        {
            if (deleteAfter) TryDelete(filePath);
        }
    }

    private static object HydrateBuiltInCore(ModelSource source, Type modelType)
    {
        string content = ReadContent(source);

        return source.Format switch
        {
            ModelFormat.Json =>
                JsonSerializer.Deserialize(content, modelType, JsonOptions)
                    ?? throw new InvalidOperationException("JSON deserialized to null."),
            ModelFormat.Xml =>
                DeserializeXml(content, modelType),
            _ => throw new InvalidOperationException($"Unsupported payload format '{source.Format}'.")
        };
    }

    private static object DeserializeXml(string content, Type modelType)
    {
        using var reader = new StringReader(content);
        return new XmlSerializer(modelType).Deserialize(reader)
            ?? throw new InvalidOperationException("XML deserialized to null.");
    }

    private static MethodInfo FindDeserializeMethod(Type serializerType, string serializerQualifiedTypeName) =>
        serializerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(method =>
                string.Equals(method.Name, "Deserialize", StringComparison.Ordinal)
                && method.GetParameters() is [{ ParameterType: var p }]
                && p == typeof(string)
                && method.ReturnType != typeof(void))
        ?? throw new InvalidOperationException(
            $"Serializer type '{serializerQualifiedTypeName}' must expose Deserialize(string filePath).");

    private static string ReadContent(ModelSource source)
    {
        if (source.Content is not null) return source.Content;

        if (!string.IsNullOrWhiteSpace(source.FilePath))
        {
            if (!File.Exists(source.FilePath))
                throw new FileNotFoundException("Payload file not found.", source.FilePath);
            return File.ReadAllText(source.FilePath);
        }

        throw new InvalidOperationException("The model source has neither content nor a file path.");
    }

    private static (string FilePath, bool DeleteAfter) MaterializeFile(ModelSource source)
    {
        if (!string.IsNullOrWhiteSpace(source.FilePath))
        {
            if (!File.Exists(source.FilePath))
                throw new FileNotFoundException("Payload file not found.", source.FilePath);
            return (source.FilePath, DeleteAfter: false);
        }

        if (source.Content is null)
            throw new InvalidOperationException("The model source has neither content nor a file path.");

        string extension = source.Format == ModelFormat.Xml ? ".xml" : ".json";
        string tempPath = Path.Combine(Path.GetTempPath(), $"naiware-model-{Guid.NewGuid():N}{extension}");
        File.WriteAllText(tempPath, source.Content);
        return (tempPath, DeleteAfter: true);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup of a temp file we created; never fail hydration because of it.
        }
    }
}
