namespace NAIware.Core.Math;

/// <summary>
/// Defines the contract for a fraction supporting arithmetic operations.
/// </summary>
public interface IFraction : IComparable
{
    /// <summary>Gets or sets the numerator.</summary>
    uint Numerator { get; set; }

    /// <summary>Gets or sets whether the fraction is negative.</summary>
    bool IsNegative { get; set; }

    /// <summary>Gets or sets the denominator.</summary>
    uint Denominator { get; set; }

    /// <summary>Gets or sets the decimal value of the fraction.</summary>
    decimal Value { get; set; }

    /// <summary>Renders the string representation of the fraction.</summary>
    string ToString();

    /// <summary>Adds a fraction to the current instance.</summary>
    void Add(IFraction fraction);

    /// <summary>Subtracts a fraction from the current instance.</summary>
    void Subtract(IFraction fraction);

    /// <summary>Multiplies the current instance by the specified fraction.</summary>
    void Multiply(IFraction fraction);

    /// <summary>Divides the current instance by the specified fraction.</summary>
    void Divide(IFraction fraction);
}
