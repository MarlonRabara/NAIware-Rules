using System.Reflection;
using System.Xml;

namespace NAIware.Core.Reflection;

/// <summary>
/// Provides access to embedded assembly resources as text or XML.
/// </summary>
public class EmbeddedResource : IDisposable
{
    private Stream? _embeddedStream;
    private bool _isDisposed;

    /// <summary>Creates a new instance for the specified resource ID.</summary>
    /// <param name="resourceId">The resource identity (e.g., MyAssembly.EmbeddedFile.xml).</param>
    public EmbeddedResource(string resourceId)
    {
        _embeddedStream = Assembly.GetCallingAssembly().GetManifestResourceStream(resourceId);
    }

    /// <summary>Gets the embedded resource stream.</summary>
    public Stream? EmbeddedStream => _embeddedStream;

    /// <summary>Gets the text content of the embedded resource.</summary>
    public string? GetText()
    {
        var txtreader = GetTextReader();
        return txtreader?.ReadToEnd();
    }

    /// <summary>Gets the XML document of the embedded resource.</summary>
    public XmlDocument? GetXml()
    {
        var txtreader = GetTextReader();
        if (txtreader is null) return null;

        var xdoc = new XmlDocument();
        xdoc.LoadXml(txtreader.ReadToEnd());
        return xdoc;
    }

    private TextReader? GetTextReader()
    {
        if (_isDisposed || _embeddedStream is null) return null;
        _embeddedStream.Seek(0, SeekOrigin.Begin);
        return new StreamReader(_embeddedStream);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Disposes the embedded resource stream.</summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        if (disposing)
            _embeddedStream?.Dispose();
        _embeddedStream = null;
        _isDisposed = true;
    }
}
