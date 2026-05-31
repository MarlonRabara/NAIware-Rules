using NAIware.Rules.Serialization;
using NAIware.RuleService.Contracts;

namespace NAIware.RuleService.Services;

/// <summary>
/// Deserializes an inbound serialized model payload (JSON or XML) into a runtime model object.
/// </summary>
/// <remarks>
/// This is a thin adapter that maps the HTTP <see cref="EvaluateModelRequest"/> contract onto the
/// shared <see cref="ModelHydrator"/> in <c>NAIware.Rules</c>, so the service and the Rule Editor
/// share a single deserialization implementation (including MISMO-style translator support).
/// </remarks>
public sealed class ModelDeserializationService
{
    private readonly ModelHydrator _hydrator;
    private readonly AssemblyModelLoader _loader;

    /// <summary>Creates a new deserialization service.</summary>
    /// <param name="loader">The assembly loader used to resolve model and serializer types.</param>
    public ModelDeserializationService(AssemblyModelLoader loader)
    {
        ArgumentNullException.ThrowIfNull(loader);
        _loader = loader;
        _hydrator = new ModelHydrator(loader);
    }

    /// <summary>
    /// Deserializes the request payload into an instance of the configured model type.
    /// </summary>
    /// <param name="request">The evaluation request carrying the payload and model/serializer metadata.</param>
    /// <returns>The hydrated model object.</returns>
    public object Deserialize(EvaluateModelRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Type modelType = _loader.ResolveType(request.ModelAssemblyPath, request.ModelQualifiedTypeName);
        ModelSource source = BuildSource(request);

        return _hydrator.Hydrate(
            source,
            modelType,
            request.SerializerAssemblyPath,
            request.SerializerQualifiedTypeName);
    }

    private static ModelSource BuildSource(EvaluateModelRequest request)
    {
        ModelFormat format = request.Format == ModelPayloadFormat.Xml ? ModelFormat.Xml : ModelFormat.Json;

        if (!string.IsNullOrEmpty(request.Payload))
            return ModelSource.FromContent(request.Payload, format);

        if (!string.IsNullOrWhiteSpace(request.PayloadPath))
            return ModelSource.FromFile(request.PayloadPath, format);

        throw new InvalidOperationException("Either Payload or PayloadPath must be supplied.");
    }
}
