namespace NAIware.Rules.Tests.Models;

/// <summary>
/// Represents a borrower on a loan application.
/// </summary>
public class Borrower
{
    /// <summary>Gets or sets the borrower's first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Gets or sets the borrower's last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Gets or sets the borrower's date of birth.</summary>
    public DateTime BirthDate { get; set; }

    /// <summary>Gets the borrower's current age in whole years.</summary>
    public int Age
    {
        get
        {
            DateTime today = DateTime.Today;
            int age = today.Year - BirthDate.Year;
            if (BirthDate.Date > today.AddYears(-age))
                age--;
            return age;
        }
    }
}
