using System.Reflection;

namespace NAIware.Core.Reflection;

/// <summary>
/// Represents version information for an assembly.
/// </summary>
public class AssemblyVersion
{
    private readonly Version _version;

    /// <summary>Creates version info from an assembly.</summary>
    public AssemblyVersion(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        _version = assembly.GetName().Version
            ?? throw new InvalidOperationException("Assembly does not have version information.");
    }

    /// <summary>Gets the major release number.</summary>
    public int Major => _version.Major;

    /// <summary>Gets the minor release number.</summary>
    public int Minor => _version.Minor;

    /// <summary>Gets the build number.</summary>
    public int Build => _version.Build;

    /// <summary>Gets the revision number.</summary>
    public int Revision => _version.Revision;

    private DateTime _builddate = DateTime.MinValue;

    /// <summary>Gets the build date computed from build and revision numbers.</summary>
    public DateTime BuildDate
    {
        get
        {
            if (_builddate != DateTime.MinValue) return _builddate;
            _builddate = new DateTime(2000, 1, 1)
                .AddDays(_version.Build)
                .AddSeconds(_version.Revision * 2);
            return _builddate;
        }
    }

    /// <inheritdoc/>
    public override string ToString() => ToLongVersionString();

    /// <summary>Returns the short version string (Major.Minor).</summary>
    public virtual string ToShortVersionString() => $"{_version.Major}.{_version.Minor}";

    /// <summary>Returns the full version string.</summary>
    public string ToLongVersionString() => $"{_version.Major}.{_version.Minor}.{_version.Build}.{_version.Revision}";

    /// <summary>Returns a short date-based version string.</summary>
    public string ToShortDateVersionString() => $"{_version.Major}.{_version.Minor} build {BuildDate:yyyyMMdd}";

    /// <summary>Returns a long date-based version string.</summary>
    public string ToLongDateVersionString() => $"{_version.Major}.{_version.Minor} build {BuildDate:yyyyMMdd.HHmm}";
}
