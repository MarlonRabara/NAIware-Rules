using System.ComponentModel.DataAnnotations;

namespace NAIware.RuleService.Contracts;

/// <summary>
/// Describes the serialization format of an inbound model payload.
/// </summary>
public enum ModelPayloadFormat
{
    /// <summary>The payload is JSON.</summary>
    Json,

    /// <summary>The payload is XML.</summary>
    Xml
}

/// <summary>
/// A request to evaluate a serialized model against a rules library.
/// </summary>
/// <remarks>
/// The model is supplied as a serialized payload (JSON or XML). The service deserializes it
/// into the model type identified by <see cref="ModelQualifiedTypeName"/> — loaded from
/// <see cref="ModelAssemblyPath"/> — and then evaluates it against the supplied library.
/// <para>
/// When a custom translator/serializer is configured via <see cref="SerializerAssemblyPath"/>
/// and <see cref="SerializerQualifiedTypeName"/>, the payload is written to a temporary file
/// and passed to the translator's <c>Deserialize(string filePath)</c> method (mirroring the
/// Rule Editor's behavior). Otherwise the service falls back to the built-in
/// <see cref="System.Text.Json"/> / <see cref="System.Xml.Serialization.XmlSerializer"/> path.
/// </para>
/// </remarks>
public sealed class EvaluateModelRequest
{
    /// <summary>Gets or sets the absolute path to the model assembly that defines the model type.</summary>
    [Required]
    public string ModelAssemblyPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the qualified (assembly-qualified or full) type name of the model to deserialize into.
    /// </summary>
    [Required]
    public string ModelQualifiedTypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the format of <see cref="Payload"/> / <see cref="PayloadPath"/>.</summary>
    public ModelPayloadFormat Format { get; set; } = ModelPayloadFormat.Json;

    /// <summary>
    /// Gets or sets the serialized model content (JSON or XML). Mutually exclusive with
    /// <see cref="PayloadPath"/>; supply exactly one.
    /// </summary>
    public string? Payload { get; set; }

    /// <summary>
    /// Gets or sets an absolute path to a file containing the serialized model. Useful for
    /// large inputs or sample fixtures. Mutually exclusive with <see cref="Payload"/>.
    /// </summary>
    public string? PayloadPath { get; set; }

    /// <summary>Gets or sets the optional path to a custom translator/serializer assembly.</summary>
    public string? SerializerAssemblyPath { get; set; }

    /// <summary>
    /// Gets or sets the optional qualified type name of a custom translator/serializer that exposes
    /// <c>Deserialize(string filePath)</c>.
    /// </summary>
    public string? SerializerQualifiedTypeName { get; set; }

    /// <summary>
    /// Gets or sets the rules library as a JSON document. Mutually exclusive with
    /// <see cref="LibraryPath"/>; supply exactly one.
    /// </summary>
    public string? LibraryJson { get; set; }

    /// <summary>
    /// Gets or sets an absolute path to a rules library JSON file. Mutually exclusive with
    /// <see cref="LibraryJson"/>.
    /// </summary>
    public string? LibraryPath { get; set; }

    /// <summary>Gets or sets an optional category name to scope evaluation. When null, all active expressions run.</summary>
    public string? CategoryName { get; set; }

    /// <summary>Gets or sets whether mismatch diagnostics are included in the response.</summary>
    public bool IncludeDiagnostics { get; set; } = true;

    /// <summary>
    /// Gets or sets how runtime evaluation errors are handled.
    /// Defaults to <c>Lenient</c> so a single malformed expression does not abort the run.
    /// </summary>
    public string? ExecutionMode { get; set; }

    /// <summary>Gets or sets whether inactive expressions are also evaluated.</summary>
    public bool IncludeInactiveRules { get; set; }
}
