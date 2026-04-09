using System.Security.Cryptography;
using System.Text;

namespace NAIware.Core.Security.Cryptography;

/// <summary>
/// Enumerates the supported symmetric encryption algorithms.
/// </summary>
public enum SymmetricAlgorithms
{
    /// <summary>The DES symmetric algorithm.</summary>
    DES,
    /// <summary>The RC2 symmetric algorithm.</summary>
    RC2,
    /// <summary>The Rijndael (AES) symmetric algorithm.</summary>
    Rijndael
}

/// <summary>
/// Provides simplified symmetric encryption and decryption services.
/// </summary>
public sealed class SymmetricServices : IDisposable
{
    private readonly SymmetricAlgorithm _cryptoservice;

    /// <summary>
    /// Creates a new instance using the DES algorithm as default.
    /// </summary>
    public SymmetricServices() : this(SymmetricAlgorithms.DES) { }

    /// <summary>
    /// Creates a new instance for the specified algorithm.
    /// </summary>
    /// <param name="cryptographyAlgorithm">The symmetric algorithm to use.</param>
    public SymmetricServices(SymmetricAlgorithms cryptographyAlgorithm)
    {
        _cryptoservice = cryptographyAlgorithm switch
        {
#pragma warning disable SYSLIB0021 // DES and RC2 are obsolete but preserved for backward compatibility
            SymmetricAlgorithms.DES => DES.Create(),
            SymmetricAlgorithms.RC2 => RC2.Create(),
#pragma warning restore SYSLIB0021
            SymmetricAlgorithms.Rijndael => Aes.Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(cryptographyAlgorithm))
        };
    }

    /// <summary>
    /// Creates a new instance for any custom symmetric algorithm provider.
    /// </summary>
    /// <param name="cryptoProvider">A custom symmetric algorithm instance.</param>
    public SymmetricServices(SymmetricAlgorithm cryptoProvider)
    {
        ArgumentNullException.ThrowIfNull(cryptoProvider);
        _cryptoservice = cryptoProvider;
    }

    /// <summary>
    /// Generates a legal-sized key from the provided key string.
    /// </summary>
    private byte[] GetLegalKey(string key)
    {
        string sTemp;
        if (_cryptoservice.LegalKeySizes.Length > 0)
        {
            int min = _cryptoservice.LegalKeySizes[0].MinSize;
            int max = _cryptoservice.LegalKeySizes[0].MaxSize;
            int keybits = key.Length * 8;

            sTemp = keybits < min
                ? key.PadRight(min / 8, ' ')
                : key[..(max / 8)];
        }
        else
        {
            sTemp = key;
        }

        return Encoding.ASCII.GetBytes(sTemp);
    }

    /// <summary>
    /// Encrypts a source string and returns a Base64-encoded result.
    /// </summary>
    /// <param name="source">The source string to encrypt.</param>
    /// <param name="key">The encryption key.</param>
    /// <returns>A Base64 string of the encrypted data.</returns>
    public string Encrypt(string source, string key)
    {
        byte[] bytIn = Encoding.ASCII.GetBytes(source);
        using var ms = new MemoryStream();

        byte[] bytKey = GetLegalKey(key);
        _cryptoservice.Key = bytKey;
        _cryptoservice.IV = bytKey;

        using var encrypto = _cryptoservice.CreateEncryptor();
        using var cs = new CryptoStream(ms, encrypto, CryptoStreamMode.Write);
        cs.Write(bytIn, 0, bytIn.Length);
        cs.FlushFinalBlock();

        byte[] bytOut = ms.GetBuffer();
        int i = Array.IndexOf(bytOut, (byte)0);
        if (i < 0) i = bytOut.Length;

        return System.Convert.ToBase64String(bytOut, 0, i);
    }

    /// <summary>
    /// Decrypts a Base64-encoded encrypted string.
    /// </summary>
    /// <param name="source">The Base64 encrypted string.</param>
    /// <param name="key">The decryption key.</param>
    /// <returns>The original decrypted string.</returns>
    public string Decrypt(string source, string key)
    {
        byte[] bytIn = System.Convert.FromBase64String(source);
        using var memoryStream = new MemoryStream(bytIn, 0, bytIn.Length);

        byte[] bytKey = GetLegalKey(key);
        _cryptoservice.Key = bytKey;
        _cryptoservice.IV = bytKey;

        using var decrypto = _cryptoservice.CreateDecryptor();
        using var cs = new CryptoStream(memoryStream, decrypto, CryptoStreamMode.Read);
        using var streamReader = new StreamReader(cs);
        return streamReader.ReadToEnd();
    }

    /// <inheritdoc/>
    public void Dispose() => _cryptoservice.Dispose();
}
