using NAIware.Rules.Models;
using NAIware.Rules.Validation;

namespace NAIware.RuleService.Services;

/// <summary>
/// Rule Service implementation of <see cref="IContextMetadataProvider"/> that resolves a context's
/// model <see cref="Type"/> through the shared collectible <see cref="AssemblyModelLoader"/>.
/// </summary>
/// <remarks>
/// The service does not pre-compute a property-path list — reflection over the resolved
/// <see cref="Type"/> in <see cref="RuleValidationService"/> is sufficient for path and operand-type
/// checks. Returning an empty path list keeps the identifier heuristics conservative without
/// affecting the reflection-based property validation that does the real work.
/// </remarks>
public sealed class AssemblyContextMetadataProvider : IContextMetadataProvider
{
    private static readonly IReadOnlyList<string> NoPaths = [];

    private readonly AssemblyModelLoader _loader;

    /// <summary>Creates a new metadata provider over the supplied assembly loader.</summary>
    public AssemblyContextMetadataProvider(AssemblyModelLoader loader)
    {
        ArgumentNullException.ThrowIfNull(loader);
        _loader = loader;
    }

    /// <inheritdoc/>
    public ContextMetadata? GetMetadata(RuleContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string? assemblyPath = context.SourceAssemblyPath;
        if (string.IsNullOrWhiteSpace(assemblyPath)
            || string.IsNullOrWhiteSpace(context.QualifiedTypeName)
            || !File.Exists(assemblyPath))
        {
            return null;
        }

        try
        {
            Type type = _loader.ResolveType(assemblyPath, context.QualifiedTypeName);
            return new ContextMetadata(type, NoPaths);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException)
        {
            // The validator surfaces a precise "unresolved context" diagnostic when metadata is null.
            return null;
        }
    }
}
