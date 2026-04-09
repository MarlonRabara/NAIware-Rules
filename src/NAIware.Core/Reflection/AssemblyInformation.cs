using System.Reflection;

namespace NAIware.Core.Reflection;

/// <summary>
/// Represents detailed information about an assembly including title, description, and company metadata.
/// </summary>
public class AssemblyInformation : AssemblyVersion
{
    private readonly string _name;
    private readonly string _path;
    private readonly string _title = string.Empty;
    private readonly string _description = string.Empty;
    private readonly string _company = string.Empty;
    private readonly string _product = string.Empty;
    private readonly string _copyright = string.Empty;
    private readonly string _trademark = string.Empty;

    /// <summary>Creates assembly information from a type's assembly.</summary>
    public AssemblyInformation(Type type) : this(type.Assembly) { }

    /// <summary>Creates assembly information from an assembly.</summary>
    public AssemblyInformation(Assembly assembly) : base(assembly)
    {
        _name = assembly.GetName().Name ?? string.Empty;
        _path = string.IsNullOrEmpty(assembly.Location)
            ? string.Empty
            : System.IO.Path.GetDirectoryName(assembly.Location) ?? string.Empty;

        _title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? string.Empty;
        _description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? string.Empty;
        _company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? string.Empty;
        _product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? string.Empty;
        _copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty;
        _trademark = assembly.GetCustomAttribute<AssemblyTrademarkAttribute>()?.Trademark ?? string.Empty;
    }

    /// <summary>Gets the assembly name.</summary>
    public string Name => _name;

    /// <summary>Gets the assembly directory path.</summary>
    public string Path => _path;

    /// <summary>Gets the assembly title.</summary>
    public string Title => _title;

    /// <summary>Gets the assembly description.</summary>
    public string Description => _description;

    /// <summary>Gets the company that developed the assembly.</summary>
    public string Company => _company;

    /// <summary>Gets the product name.</summary>
    public string Product => _product;

    /// <summary>Gets the copyright information.</summary>
    public string Copyright => _copyright;

    /// <summary>Gets the trademark information.</summary>
    public string Trademark => _trademark;

    /// <inheritdoc/>
    public override string ToShortVersionString() => $"{_title} {base.ToShortVersionString()}";

    /// <summary>Gets the full path to an associated resource with the given extension.</summary>
    public string GetAssociatedResource(string fileExtension) => $"{_path}{System.IO.Path.DirectorySeparatorChar}{_name}.{fileExtension}";
}
