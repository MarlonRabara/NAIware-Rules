using System.Reflection;
using System.Runtime.Loader;

namespace NAIware.RuleEditor;

/// <summary>
/// Reflects public concrete classes from a .NET assembly on disk, and loads user-supplied
/// assemblies (context DLLs and serializer DLLs) into collectible
/// <see cref="AssemblyLoadContext"/> instances so they can be released when the user
/// re-points a context at an updated copy.
/// </summary>
/// <remarks>
/// <para>
/// Each loaded path lives in its own collectible context. To preserve a single
/// <see cref="Type"/> identity across related DLLs (for example a model assembly and a
/// serializer assembly that both reference it), every context's
/// <see cref="AssemblyLoadContext.Load(AssemblyName)"/> override first asks every other
/// managed context whether it has already loaded a matching assembly. Only when no
/// sibling has it loaded does the context probe its own directory.
/// </para>
/// <para>
/// Callers should invoke <see cref="Invalidate(string)"/> (or <see cref="InvalidateAll"/>)
/// after replacing a DLL on disk and discard any cached <see cref="Type"/> references
/// that originated from it before the GC can actually unload the context.
/// </para>
/// </remarks>
public sealed class AssemblyTypeDiscoveryService : IDisposable
{
    private readonly Dictionary<string, ContextLoadContext> _contextsByPath =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Loads the assembly at the specified path and returns all public concrete classes,
    /// ordered by namespace then name. If the path was loaded previously, the prior
    /// load context is unloaded first so an updated DLL is reflected.
    /// </summary>
    /// <param name="assemblyPath">The absolute path to the .NET assembly (.dll).</param>
    /// <returns>A read-only list of discovered types.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the assembly cannot be located.</exception>
    public IReadOnlyList<ReflectedTypeInfo> DiscoverTypes(string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
            throw new FileNotFoundException("Assembly not found.", assemblyPath);

        // Re-loading the same path should pick up changes on disk.
        Invalidate(assemblyPath);

        Assembly assembly = LoadAssembly(assemblyPath);

        Type[] exported;
        try
        {
            exported = assembly.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Best-effort: return the types that did load.
            exported = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
        }

        return exported
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
            .OrderBy(t => t.Namespace)
            .ThenBy(t => t.Name)
            .Select(t => new ReflectedTypeInfo
            {
                DisplayName = string.IsNullOrWhiteSpace(t.Namespace) ? t.Name : $"{t.Namespace}.{t.Name}",
                FullName = t.AssemblyQualifiedName ?? t.FullName ?? t.Name,
                AssemblyPath = assemblyPath,
                Type = t
            })
            .ToList();
    }

    /// <summary>
    /// Loads (or returns the already-loaded) primary <see cref="Assembly"/> for the
    /// specified path. Use this when callers need the <see cref="Assembly"/> itself
    /// (for example to invoke a serializer type) rather than enumerated metadata.
    /// </summary>
    /// <param name="assemblyPath">The absolute path to the .NET assembly (.dll).</param>
    /// <returns>The loaded assembly.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the assembly cannot be located.</exception>
    public Assembly LoadAssembly(string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
            throw new FileNotFoundException("Assembly not found.", assemblyPath);

        return GetOrCreate(assemblyPath).LoadPrimary();
    }

    /// <summary>
    /// Attempts to resolve a <see cref="Type"/> from the specified context document.
    /// Reuses an existing collectible load context for the same DLL when present.
    /// </summary>
    /// <param name="context">The UI context document.</param>
    /// <returns>The resolved <see cref="Type"/>, or null if the type cannot be located.</returns>
    public Type? ResolveContextType(RuleContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.AssemblyPath) && File.Exists(context.AssemblyPath))
        {
            Assembly assembly = LoadAssembly(context.AssemblyPath);

            Type? resolved = assembly.GetType(context.QualifiedTypeName)
                ?? assembly.GetTypes().FirstOrDefault(t =>
                    string.Equals(t.AssemblyQualifiedName, context.QualifiedTypeName, StringComparison.Ordinal)
                    || string.Equals(t.FullName, context.QualifiedTypeName, StringComparison.Ordinal));

            if (resolved is not null) return resolved;
        }

        return Type.GetType(context.QualifiedTypeName);
    }

    /// <summary>
    /// Unloads any collectible load context previously created for <paramref name="assemblyPath"/>.
    /// Call this before retrying after a referenced DLL is replaced on disk. The actual
    /// unload happens once the GC observes that no <see cref="Type"/> or <see cref="Assembly"/>
    /// references from that context are still rooted, so callers must drop their caches first.
    /// </summary>
    /// <param name="assemblyPath">The path that was previously loaded.</param>
    public void Invalidate(string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath)) return;

        if (_contextsByPath.Remove(assemblyPath, out ContextLoadContext? existing))
        {
            existing.Unload();
        }
    }

    /// <summary>
    /// Unloads every collectible load context held by this service.
    /// </summary>
    public void InvalidateAll()
    {
        foreach (ContextLoadContext context in _contextsByPath.Values)
        {
            context.Unload();
        }
        _contextsByPath.Clear();
    }

    /// <inheritdoc />
    public void Dispose() => InvalidateAll();

    private ContextLoadContext GetOrCreate(string assemblyPath)
    {
        if (_contextsByPath.TryGetValue(assemblyPath, out ContextLoadContext? existing))
            return existing;

        var created = new ContextLoadContext(this, assemblyPath);
        _contextsByPath[assemblyPath] = created;
        return created;
    }

    /// <summary>
    /// Looks across every managed load context (excluding <paramref name="requester"/>)
    /// for an assembly already loaded under <paramref name="assemblyName"/>. This is what
    /// keeps a single <see cref="Type"/> identity when, e.g., a serializer DLL and a
    /// context DLL both reference the same model assembly: whichever one loads first
    /// wins, and subsequent contexts reuse that <see cref="Assembly"/> instance.
    /// </summary>
    private Assembly? FindAlreadyLoaded(ContextLoadContext requester, AssemblyName assemblyName)
    {
        if (string.IsNullOrEmpty(assemblyName.Name)) return null;

        foreach (ContextLoadContext context in _contextsByPath.Values)
        {
            if (ReferenceEquals(context, requester)) continue;

            foreach (Assembly loaded in context.Assemblies)
            {
                AssemblyName loadedName = loaded.GetName();
                if (string.Equals(loadedName.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase))
                    return loaded;
            }
        }

        return null;
    }

    /// <summary>
    /// Collectible <see cref="AssemblyLoadContext"/> that loads a primary DLL and resolves
    /// its dependencies first from sibling contexts (to preserve type identity across
    /// related user assemblies), then from the same directory as the primary DLL.
    /// </summary>
    private sealed class ContextLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyTypeDiscoveryService _owner;
        private readonly string _primaryPath;
        private readonly string _probingDirectory;
        private Assembly? _primary;

        public ContextLoadContext(AssemblyTypeDiscoveryService owner, string primaryPath)
            : base(name: $"NAIware.RuleEditor::{Path.GetFileNameWithoutExtension(primaryPath)}",
                   isCollectible: true)
        {
            _owner = owner;
            _primaryPath = primaryPath;
            _probingDirectory = Path.GetDirectoryName(primaryPath) ?? string.Empty;
        }

        public Assembly LoadPrimary() => _primary ??= LoadFromAssemblyPath(_primaryPath);

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // 1) If a sibling managed context already loaded this assembly, reuse it.
            //    This preserves a single Type identity for shared model DLLs across
            //    multiple user-supplied assemblies (context DLL + serializer DLL).
            Assembly? shared = _owner.FindAlreadyLoaded(this, assemblyName);
            if (shared is not null) return shared;

            // 2) Otherwise probe alongside the primary DLL for sibling dependencies.
            //    Returning null delegates back to the default ALC, which is what we
            //    want for framework / shared-runtime references so we don't end up
            //    with duplicated framework types.
            if (string.IsNullOrEmpty(_probingDirectory) || string.IsNullOrEmpty(assemblyName.Name))
                return null;

            string candidate = Path.Combine(_probingDirectory, assemblyName.Name + ".dll");
            return File.Exists(candidate) ? LoadFromAssemblyPath(candidate) : null;
        }
    }
}
