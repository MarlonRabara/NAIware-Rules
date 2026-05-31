using System.Reflection;

namespace NAIware.Rules.Serialization;

/// <summary>
/// Abstraction over loading user-supplied model and serializer assemblies from disk and resolving
/// types from them. Both the Rule Editor and the Rule Service supply their own collectible
/// <see cref="System.Runtime.Loader.AssemblyLoadContext"/>-based implementations; this contract lets
/// <see cref="ModelHydrator"/> remain agnostic of the hosting concern (long-lived service vs.
/// reloadable editor session) while sharing a single deserialization implementation.
/// </summary>
public interface IModelAssemblyResolver
{
    /// <summary>Loads (or returns the already-loaded) assembly for the specified path.</summary>
    /// <param name="assemblyPath">The absolute path to the .NET assembly (.dll).</param>
    /// <returns>The loaded assembly.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the assembly cannot be located.</exception>
    Assembly LoadAssembly(string assemblyPath);

    /// <summary>
    /// Resolves a type by its assembly-qualified or full name from the assembly at the given path.
    /// </summary>
    /// <param name="assemblyPath">The absolute path to the assembly that defines the type.</param>
    /// <param name="qualifiedTypeName">The assembly-qualified or full type name.</param>
    /// <returns>The resolved <see cref="Type"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the type cannot be resolved.</exception>
    Type ResolveType(string assemblyPath, string qualifiedTypeName);
}
