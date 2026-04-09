namespace NAIware.Rules.Tests.Models;

/// <summary>
/// Represents a loan application containing borrowers and a subject property.
/// </summary>
public class LoanApplication
{
    /// <summary>Gets or sets the list of borrowers on the loan.</summary>
    public List<Borrower> Borrowers { get; set; } = [];

    /// <summary>Gets or sets the subject property.</summary>
    public Property Property { get; set; } = new();

    /// <summary>Gets the number of borrowers on the loan.</summary>
    public int BorrowerCount => Borrowers.Count;
}
