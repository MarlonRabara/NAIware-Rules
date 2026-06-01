namespace NAIware.Rules.Validation;

/// <summary>
/// Reflected metadata for a context type, consumed by <see cref="RuleValidationService"/> and by
/// completion tooling.
/// </summary>
/// <param name="Type">The resolved .NET type the context binds to.</param>
/// <param name="PropertyPaths">
/// All reachable property paths up to a host-configured depth. May be empty when a host only needs
/// reflection-based path validation (which walks <paramref name="Type"/> directly) and does not
/// supply a pre-computed path list.
/// </param>
public sealed record ContextMetadata(Type Type, IReadOnlyList<string> PropertyPaths);
