using System.ComponentModel.DataAnnotations;

namespace NAIware.RuleService.Contracts;

/// <summary>
/// A request to validate an entire rules library document.
/// </summary>
/// <remarks>
/// Supply exactly one of <see cref="LibraryJson"/> or <see cref="LibraryPath"/>. Each context in the
/// library must carry a resolvable <c>SourceAssemblyPath</c> / <c>QualifiedTypeName</c> so the
/// validator can reflect over the model type.
/// </remarks>
public sealed class ValidateLibraryRequest
{
    /// <summary>Gets or sets the rules library as a JSON document. Mutually exclusive with <see cref="LibraryPath"/>.</summary>
    public string? LibraryJson { get; set; }

    /// <summary>Gets or sets an absolute path to a rules library JSON file. Mutually exclusive with <see cref="LibraryJson"/>.</summary>
    public string? LibraryPath { get; set; }
}
