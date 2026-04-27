namespace NAIware.Rules.Models;

/// <summary>
/// Audit/history record for an immutable published rules library snapshot.
/// </summary>
public class LibraryVersion
{
    /// <summary>Gets or sets the unique identity of the captured snapshot.</summary>
    public Guid SnapshotIdentity { get; set; }

    /// <summary>Gets or sets the version number represented by this snapshot.</summary>
    public int Version { get; set; }

    /// <summary>Gets or sets when the snapshot was published.</summary>
    public DateTimeOffset PublishedUtc { get; set; }

    /// <summary>Gets or sets an optional description of the change.</summary>
    public string? ChangeNote { get; set; }

    /// <summary>Gets or sets the captured library state.</summary>
    public RulesLibrary? LibrarySnapshot { get; set; }
}
