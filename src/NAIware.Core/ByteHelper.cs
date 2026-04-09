using System.Security.Cryptography;
using System.Text;

namespace NAIware.Core;

/// <summary>
/// Provides extension methods for byte array manipulation, encoding, and hashing.
/// </summary>
public static class ByteHelper
{
    /// <summary>
    /// Converts a byte array to its hexadecimal string representation.
    /// </summary>
    /// <param name="source">The source byte array.</param>
    /// <returns>A hexadecimal string (e.g., "A1B2C3").</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <c>null</c>.</exception>
    public static string ToHexString(this byte[] source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Convert.ToHexString(source);
    }

    /// <summary>
    /// Converts a string to a UTF-8 encoded byte array.
    /// </summary>
    /// <param name="anyString">The string to encode.</param>
    /// <returns>A UTF-8 byte array, or <c>null</c> if the input is <c>null</c>.</returns>
    public static byte[]? ToBytes(this string? anyString)
    {
        if (anyString is null) return null;
        return Encoding.UTF8.GetBytes(anyString);
    }

    /// <summary>
    /// Computes a SHA-384 hash for the given byte data.
    /// </summary>
    /// <param name="byteDataToHash">The source data to hash.</param>
    /// <returns>The computed hash, or <c>null</c> if hashing fails.</returns>
    public static byte[]? ComputeHash(this byte[] byteDataToHash)
    {
        try
        {
            return SHA384.HashData(byteDataToHash);
        }
        catch
        {
            return null;
        }
    }
}
