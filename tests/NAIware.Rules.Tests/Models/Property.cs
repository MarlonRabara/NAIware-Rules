namespace NAIware.Rules.Tests.Models;

/// <summary>
/// Represents a property address on a loan application.
/// </summary>
public class Property
{
    /// <summary>Gets or sets the street address.</summary>
    public string StreetAddress { get; set; } = string.Empty;

    /// <summary>Gets or sets the city.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the state abbreviation.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets the zip code.</summary>
    public string Zip { get; set; } = string.Empty;
}
