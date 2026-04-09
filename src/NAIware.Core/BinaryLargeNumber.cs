using System.Collections;

namespace NAIware.Core;

/// <summary>
/// A struct that represents a binary large number with arbitrary precision.
/// </summary>
/// <remarks>
/// This type is an incomplete implementation stub preserved during migration.
/// </remarks>
[Obsolete("BinaryLargeNumber is an incomplete implementation stub. Use System.Numerics.BigInteger for arbitrary-precision arithmetic.")]
public struct BinaryLargeNumber
{
    private BitArray _exp;
    private BitArray _fraction;
    private bool _negative;

    /// <summary>
    /// Initializes a new instance with the specified byte precision.
    /// </summary>
    /// <param name="bytePrecision">The number of bytes of precision.</param>
    public BinaryLargeNumber(int bytePrecision)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bytePrecision);

        _exp = bytePrecision switch
        {
            1 => new BitArray(8),
            2 => new BitArray(11),
            _ => new BitArray(8 + (4 * bytePrecision))
        };

        _fraction = new BitArray(Convert.ToInt32((bytePrecision * 32) - (1 + _exp.Count)));
        _negative = false;
    }

    /// <summary>
    /// Calculates the exponent bias for the given binary large number.
    /// </summary>
    public static int Bias(BinaryLargeNumber bln)
    {
        var biasbits = new BitArray(bln._exp.Count - 1);
        biasbits.SetAll(true);
        int biasval = 0;
        for (int i = 0; i < biasbits.Count; i++)
        {
            biasval += (int)((biasbits[i] ? 1 : 0) * System.Math.Pow(2, i));
        }
        return biasval;
    }

    /// <inheritdoc/>
    public override readonly string ToString() => base.ToString() ?? string.Empty;
}
