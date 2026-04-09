using System.IO.Compression;
using System.Reflection;

namespace NAIware.Core.IO;

/// <summary>
/// Provides utility methods for IO operations including GZip compression,
/// stream reading, embedded resources, and temporary file management.
/// </summary>
public static class IOHelper
{
    /// <summary>
    /// Compresses byte data using GZip compression.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <returns>The compressed byte array.</returns>
    public static byte[] Compress(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var outputMemoryStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputMemoryStream, CompressionMode.Compress, leaveOpen: true))
        {
            gzipStream.Write(data, 0, data.Length);
        }

        return outputMemoryStream.ToArray();
    }

    /// <summary>
    /// Decompresses GZip-compressed byte data.
    /// </summary>
    /// <param name="data">The compressed data.</param>
    /// <returns>The decompressed byte array.</returns>
    public static byte[] Decompress(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var inputMemoryStream = new MemoryStream(data);
        using var gzipStream = new GZipStream(inputMemoryStream, CompressionMode.Decompress);
        using var outputMemoryStream = new MemoryStream();
        gzipStream.CopyTo(outputMemoryStream);
        return outputMemoryStream.ToArray();
    }

    /// <summary>
    /// Reads all bytes from a stream, chunking if necessary.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <returns>A byte array of the stream data, or <c>null</c> if the stream is <c>null</c>.</returns>
    public static byte[]? GetBytes(Stream? stream)
    {
        if (stream is null) return null;
        return GetBytes(stream, System.Convert.ToInt32(stream.Length));
    }

    /// <summary>
    /// Reads bytes from a stream with an initial buffer length, chunking if necessary.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <param name="initialLength">The initial buffer size.</param>
    /// <returns>A byte array of the stream data, or <c>null</c> if the stream is <c>null</c>.</returns>
    public static byte[]? GetBytes(Stream? stream, int initialLength)
    {
        if (stream is null) return null;

        if (initialLength < 1)
            initialLength = 32768;

        byte[] buffer = new byte[initialLength];
        int read = 0;

        int chunk;
        while ((chunk = stream.Read(buffer, read, buffer.Length - read)) > 0)
        {
            read += chunk;

            if (read == buffer.Length)
            {
                int nextByte = stream.ReadByte();
                if (nextByte == -1)
                    return buffer;

                byte[] newBuffer = new byte[buffer.Length * 2];
                Array.Copy(buffer, newBuffer, buffer.Length);
                newBuffer[read] = (byte)nextByte;
                buffer = newBuffer;
                read++;
            }
        }

        byte[] ret = new byte[read];
        Array.Copy(buffer, ret, read);
        return ret;
    }

    /// <summary>
    /// Gets the text content of an embedded resource.
    /// </summary>
    /// <param name="resourceName">The resource name (e.g., Namespace.ResourceName).</param>
    /// <param name="assembly">The assembly containing the resource.</param>
    /// <returns>The text content of the embedded resource.</returns>
    public static string GetEmbeddedResource(string resourceName, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        using var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Resource '{resourceName}' not found in assembly."));
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Writes binary data to a temporary file and returns the file path.
    /// </summary>
    /// <param name="bytes">The byte data to write.</param>
    /// <returns>The path of the temporary file.</returns>
    public static string WriteToTempFile(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        string tempFileName = Path.GetTempFileName();
        using var fs = new FileStream(tempFileName, FileMode.Append, FileAccess.Write);
        using var writer = new BinaryWriter(fs);
        writer.BaseStream.Seek(0, SeekOrigin.End);
        writer.Write(bytes);
        writer.Flush();

        return tempFileName;
    }
}
