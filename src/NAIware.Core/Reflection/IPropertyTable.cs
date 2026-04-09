using System.Reflection;

namespace NAIware.Core.Reflection;

/// <summary>
/// Defines a contract for a hash table of reflected property members.
/// </summary>
public interface IPropertyTable : IMemberTable
{
    /// <summary>Gets or sets a property by name.</summary>
    new PropertyInfo? this[string memberName] { get; set; }

    /// <summary>Adds a property to the table.</summary>
    void Add(string memberName, PropertyInfo value);

    /// <summary>Removes a property from the table.</summary>
    new void Remove(string memberName);
}
