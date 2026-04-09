using System.Reflection;

namespace NAIware.Core.Reflection;

/// <summary>
/// Defines a contract for a hash table of reflected field members.
/// </summary>
public interface IFieldTable : IMemberTable
{
    /// <summary>Gets or sets a field by name.</summary>
    new FieldInfo? this[string memberName] { get; set; }

    /// <summary>Adds a field to the table.</summary>
    void Add(string memberName, FieldInfo value);

    /// <summary>Removes a field from the table.</summary>
    new void Remove(string memberName);
}
