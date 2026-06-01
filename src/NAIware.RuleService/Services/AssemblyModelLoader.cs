using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using NAIware.Rules.Serialization;

namespace NAIware.RuleService.Services;

/// <summary>
/// Loads user-supplied model and serializer assemblies from disk into collectible
/// <see cref="AssemblyLoadContext"/> instances and resolves types from them.
/// </summary>
/// <remarks>
/// <para>
/// Each distinct assembly path is loaded into its own collectible context. To preserve a
/// single <see cref="Type"/> identity across related DLLs (for example, a model assembly and
/// a translator assembly that both reference it), each context first asks its siblings whether
/// they have already loaded a matching assembly before probing its own directory. This mirrors
/// the Rule Editor's <c>AssemblyTypeDiscoveryService</c> so the service and editor resolve types
/// identically.
/// </para>
/// <para>
/// Instances are thread-safe and intended to be registered as a singleton. Loaded contexts are
/// cached for the lifetime of the service so repeated requests against the same assemblies do not
/// reload them.
/// </para>
/// </remarks>
public sealed class AssemblyModelLoader : IModelAssemblyResolver, IDisposable
{
    private readonly ConcurrentDictionary<string, ModelLoadContext> _contextsByPath =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Loads (or returns the already-loaded) assembly for the specified path.</summary>
    /// <param name="assemblyPath">The absolute path to the .NET assembly (.dll).</param>
    /// <returns>The loaded assembly.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the assembly cannot be located.</exception>
    public Assembly LoadAssembly(string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
            throw new FileNotFoundException("Assembly not found.", assemblyPath);

        string fullPath = Path.GetFullPath(assemblyPath);
        return GetOrCreate(fullPath).LoadPrimary();
    }

    /// <summary>Resolves a type by qualified (assembly-qualified or full) name from the assembly at the given path.</summary>
    /// <param name="assemblyPath">The absolute path to the assembly that defines the type.</param>
    /// <param name="qualifiedTypeName">The assembly-qualified or full type name.</param>
    /// <returns>The resolved <see cref="Type"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the type cannot be resolved.</exception>
    public Type ResolveType(string assemblyPath, string qualifiedTypeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(qualifiedTypeName);

        Assembly assembly = LoadAssembly(assemblyPath);

        Type? resolved = assembly.GetType(qualifiedTypeName)
            ?? assembly.GetTypes().FirstOrDefault(t =>
                string.Equals(t.AssemblyQualifiedName, qualifiedTypeName, StringComparison.Ordinal)
                || string.Equals(t.FullName, qualifiedTypeName, StringComparison.Ordinal));

        return resolved
            ?? throw new InvalidOperationException(
                $"Type '{qualifiedTypeName}' could not be resolved from assembly '{assemblyPath}'.");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (ModelLoadContext context in _contextsByPath.Values)
            context.Unload();
        _contextsByPath.Clear();
    }

    private ModelLoadContext GetOrCreate(string assemblyPath) =>
        _contextsByPath.GetOrAdd(assemblyPath, path => new ModelLoadContext(this, path));

    private Assembly? FindAlreadyLoaded(ModelLoadContext requester, AssemblyName assemblyName)
    {
        if (string.IsNullOrEmpty(assemblyName.Name)) return null;

        foreach (ModelLoadContext context in _contextsByPath.Values)
        {
            if (ReferenceEquals(context, requester)) continue;

            foreach (Assembly loaded in context.Assemblies)
            {
                if (string.Equals(loaded.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase))
                    return loaded;
            }
        }

        return null;
    }

    private sealed class ModelLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyModelLoader _owner;
        private readonly string _primaryPath;
        private readonly string _probingDirectory;
        private Assembly? _primary;

        public ModelLoadContext(AssemblyModelLoader owner, string primaryPath)
            : base(name: $"NAIware.RuleService::{Path.GetFileNameWithoutExtension(primaryPath)}", isCollectible: true)
        {
            _owner = owner;
            _primaryPath = primaryPath;
            _probingDirectory = Path.GetDirectoryName(primaryPath) ?? string.Empty;
        }

        public Assembly LoadPrimary() => _primary ??= LoadFromAssemblyPath(_primaryPath);

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // Preserve type identity across sibling contexts (e.g. model + translator sharing a model DLL).
            Assembly? shared = _owner.FindAlreadyLoaded(this, assemblyName);
            if (shared is not null) return shared;

            if (!string.IsNullOrEmpty(_probingDirectory) && !string.IsNullOrEmpty(assemblyName.Name))
            {
                string candidate = Path.Combine(_probingDirectory, assemblyName.Name + ".dll");
                if (File.Exists(candidate)) return LoadFromAssemblyPath(candidate);
            }

            // Fall back to the default context (framework/shared assemblies).
            return null;
        }
    }
}
