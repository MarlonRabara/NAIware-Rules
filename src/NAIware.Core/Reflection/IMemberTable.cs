using System.Reflection;

namespace NAIware.Core.Reflection;

/// <summary>
/// Defines a contract for a hash table of reflected class members.
/// </summary>
public interface IMemberTable
{
    /// <summary>Gets the total number of members in the table.</summary>
    int Count { get; }

    /// <summary>Gets or sets a member by name.</summary>
    MemberInfo? this[string memberName] { get; set; }

    /// <summary>Adds a member to the table.</summary>
    void Add(string memberName, MemberInfo value);

    /// <summary>Removes a member from the table.</summary>
    void Remove(string memberName);

    /// <summary>Gets the reflected type.</summary>
    Type Type { get; }
}
