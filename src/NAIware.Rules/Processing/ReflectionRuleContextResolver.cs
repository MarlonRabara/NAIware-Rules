using NAIware.Rules.Models;

namespace NAIware.Rules.Processing;

/// <summary>
/// Resolves a <see cref="RuleContext"/> by matching the input object's
/// <see cref="Type.FullName"/> against <see cref="RuleContext.QualifiedTypeName"/>.
/// Supports exact match and base type / interface traversal.
/// </summary>
public class ReflectionRuleContextResolver : IRuleContextResolver
{
    private readonly RulesLibrary _library;

    /// <summary>Creates a resolver backed by the specified rules library.</summary>
    public ReflectionRuleContextResolver(RulesLibrary library)
    {
        ArgumentNullException.ThrowIfNull(library);
        _library = library;
    }

    /// <inheritdoc/>
    public RuleContext? Resolve(object inputObject)
    {
        ArgumentNullException.ThrowIfNull(inputObject);

        Type inputType = inputObject.GetType();

        // Exact match first
        RuleContext? context = _library.FindContextByTypeName(inputType.FullName!);
        if (context is not null) return context;

        // Walk base types
        Type? current = inputType.BaseType;
        while (current is not null && current != typeof(object))
        {
            context = _library.FindContextByTypeName(current.FullName!);
            if (context is not null) return context;
            current = current.BaseType;
        }

        // Check interfaces
        foreach (Type iface in inputType.GetInterfaces())
        {
            context = _library.FindContextByTypeName(iface.FullName!);
            if (context is not null) return context;
        }

        return null;
    }
}
