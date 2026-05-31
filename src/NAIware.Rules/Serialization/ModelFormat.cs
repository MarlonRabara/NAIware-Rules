namespace NAIware.Rules.Serialization;

/// <summary>
/// Describes the serialization format of a model payload handled by <see cref="ModelHydrator"/>.
/// </summary>
public enum ModelFormat
{
    /// <summary>The payload is JSON.</summary>
    Json,

    /// <summary>The payload is XML.</summary>
    Xml
}
