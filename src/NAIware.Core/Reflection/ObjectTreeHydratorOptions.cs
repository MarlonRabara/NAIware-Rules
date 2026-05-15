using System.ComponentModel;
using System.Reflection;

namespace NAIware.Core.Reflection;

/// <summary>
/// Options that control how <see cref="ObjectTreeHydrator"/> traverses a type graph.
/// </summary>
public sealed class ObjectTreeHydratorOptions
{
    /// <summary>The default maximum depth used when no override is supplied.</summary>
    public const int DefaultMaxDepth = 8;

    /// <summary>The default maximum total node count used when no override is supplied.</summary>
    public const int DefaultMaxNodes = 5_000;

    /// <summary>Gets or sets the maximum depth (number of property hops) to traverse. Must be at least one.</summary>
    public int MaxDepth { get; set; } = DefaultMaxDepth;

    /// <summary>
    /// Gets or sets the maximum total number of nodes (including the root) that the
    /// hydrator will produce. Traversal stops once the budget is exhausted. Must be at least one.
    /// </summary>
    public int MaxNodes { get; set; } = DefaultMaxNodes;

    /// <summary>
    /// Gets or sets the synthetic name and path segment used to represent a collection
    /// item. Defaults to <c>[]</c>.
    /// </summary>
    public string CollectionItemSegment { get; set; } = "[]";

    /// <summary>
    /// Gets or sets an optional filter applied to each <see cref="PropertyInfo"/> after
    /// the built-in readability checks. Return <see langword="false"/> to skip the property.
    /// The default filter excludes properties marked with <see cref="BrowsableAttribute"/>(false).
    /// </summary>
    public Func<PropertyInfo, bool>? PropertyFilter { get; set; } = DefaultPropertyFilter;

    /// <summary>
    /// Gets or sets an optional predicate that classifies a type as a leaf (non-recursed).
    /// Returns <see langword="true"/> to short-circuit recursion. The hydrator additionally
    /// treats primitives, enums, and common scalar types as leaves regardless of this hook.
    /// </summary>
    public Func<Type, bool>? LeafTypeOverride { get; set; }

    /// <summary>The default property filter: excludes <c>[Browsable(false)]</c> properties.</summary>
    public static bool DefaultPropertyFilter(PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);
        var browsable = property.GetCustomAttribute<BrowsableAttribute>();
        return browsable is null || browsable.Browsable;
    }
}
