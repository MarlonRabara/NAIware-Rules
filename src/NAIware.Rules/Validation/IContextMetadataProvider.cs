using NAIware.Rules.Models;

namespace NAIware.Rules.Validation;

/// <summary>
/// Abstraction that resolves reflected <see cref="ContextMetadata"/> for a <see cref="RuleContext"/>.
/// </summary>
/// <remarks>
/// This contract lets <see cref="RuleValidationService"/> remain agnostic of how a host loads model
/// assemblies and resolves types. The Rule Editor supplies an implementation backed by its
/// IntelliSense/schema services (rich property paths for completion); the Rule Service supplies an
/// implementation backed by its collectible assembly loader. It mirrors the role
/// <see cref="Serialization.IModelAssemblyResolver"/> plays for deserialization.
/// </remarks>
public interface IContextMetadataProvider
{
    /// <summary>
    /// Resolves reflected metadata for the supplied context, or <c>null</c> when the context type
    /// cannot be resolved (missing configuration, missing assembly, or missing type).
    /// </summary>
    /// <param name="context">The context to resolve.</param>
    /// <returns>The resolved metadata, or <c>null</c> when it cannot be resolved.</returns>
    ContextMetadata? GetMetadata(RuleContext context);
}
