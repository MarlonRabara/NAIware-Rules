namespace NAIware.Rules.Models;

/// <summary>
/// Obsolete placeholder retained only for backward source compatibility.
/// Rule expressions are no longer independently versioned; versioning belongs to <see cref="RulesLibrary"/>.
/// </summary>
public class ExpressionVersion
{
    /// <summary>Creates an empty obsolete expression version record.</summary>
    public ExpressionVersion()
    {
        Expression = string.Empty;
    }

    /// <summary>Creates an obsolete expression version record.</summary>
    public ExpressionVersion(int version, string expression, string? changeNote = null)
    {
        Version = version;
        Expression = expression;
        ChangeNote = changeNote;
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets or sets the obsolete expression version identifier.</summary>
    public Guid Identity { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the obsolete version number for the expression snapshot.</summary>
    public int Version { get; set; }

    /// <summary>Gets or sets the serialized expression content captured in this obsolete record.</summary>
    public string Expression { get; set; }

    /// <summary>Gets or sets the UTC timestamp when this obsolete record was created.</summary>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets the optional change note for this obsolete record.</summary>
    public string? ChangeNote { get; set; }
}
