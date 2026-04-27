using System.Reflection;

namespace NAIware.RuleEditor;

/// <summary>
/// Discovers public concrete classes from a .NET assembly on disk.
/// Used by the context picker to present available types when the user selects a DLL.
/// </summary>
public sealed class AssemblyTypeDiscoveryService
{
    /// <summary>
    /// Loads the assembly at the specified path and returns all public concrete classes,
    /// ordered by namespace then name.
    /// </summary>
    /// <param name="assemblyPath">The absolute path to the .NET assembly (.dll).</param>
    /// <returns>A read-only list of discovered types.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the assembly cannot be located.</exception>
    public IReadOnlyList<ReflectedTypeInfo> DiscoverTypes(string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
            throw new FileNotFoundException("Assembly not found.", assemblyPath);

        Assembly assembly = Assembly.LoadFrom(assemblyPath);

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
    /// Attempts to resolve a <see cref="Type"/> from the specified context document.
    /// </summary>
    /// <param name="context">The UI context document.</param>
    /// <returns>The resolved <see cref="Type"/>, or null if the type cannot be located.</returns>
    public Type? ResolveContextType(RuleContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.AssemblyPath) && File.Exists(context.AssemblyPath))
        {
            Assembly assembly = Assembly.LoadFrom(context.AssemblyPath);
            Type? resolved = assembly.GetType(context.QualifiedTypeName)
                ?? assembly.GetTypes().FirstOrDefault(t => string.Equals(t.AssemblyQualifiedName, context.QualifiedTypeName, StringComparison.Ordinal)
                    || string.Equals(t.FullName, context.QualifiedTypeName, StringComparison.Ordinal));
            if (resolved is not null) return resolved;
        }

        return Type.GetType(context.QualifiedTypeName);
    }
}
