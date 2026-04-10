namespace NAIware.Rules.Catalog;

/// <summary>
/// A snapshot of a <see cref="RuleExpression"/> at a specific version.
/// Provides an audit trail of expression changes without duplicating the
/// context or category structure.
/// </summary>
public class ExpressionVersion
{
    private readonly Guid _identity;

    /// <summary>Creates a new expression version snapshot.</summary>
    public ExpressionVersion(int version, string expression, string? changeNote = null)
    {
        _identity = Guid.NewGuid();
        Version = version;
        Expression = expression;
        ChangeNote = changeNote;
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets the unique identity of this version snapshot.</summary>
    public Guid Identity => _identity;

    /// <summary>Gets the version number.</summary>
    public int Version { get; }

    /// <summary>Gets the expression text at this version.</summary>
    public string Expression { get; }

    /// <summary>Gets the UTC timestamp when this version was created.</summary>
    public DateTimeOffset CreatedUtc { get; }

    /// <summary>Gets the optional change note describing why this version was created.</summary>
    public string? ChangeNote { get; }
}
