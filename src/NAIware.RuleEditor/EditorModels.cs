using System.Text.Json.Serialization;

namespace NAIware.RuleEditor;

/// <summary>
/// Describes a public concrete class discovered inside an assembly.
/// Used by <see cref="ContextTypePickerDialog"/> to present selectable types.
/// </summary>
public sealed class ReflectedTypeInfo
{
    /// <summary>Gets the display name (namespace + type name).</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets the fully qualified type name.</summary>
    public required string FullName { get; init; }

    /// <summary>Gets the originating assembly path.</summary>
    public required string AssemblyPath { get; init; }

    /// <summary>Gets the reflected <see cref="Type"/> instance (not serialized).</summary>
    [JsonIgnore] public Type? Type { get; init; }
}
