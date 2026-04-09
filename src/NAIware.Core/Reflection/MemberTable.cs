using System.Collections;
using System.Reflection;

namespace NAIware.Core.Reflection;

/// <summary>
/// Abstract base class for a hash-table-backed collection of reflected members.
/// </summary>
public abstract class MemberTable : IMemberTable, ICollection
{
    /// <summary>The inner hash table managing the members.</summary>
    protected readonly Hashtable _innerhash = new(2);

    /// <summary>Cached array of simple members.</summary>
    protected object[]? _simplemembers;

    /// <summary>The type being reflected.</summary>
    protected Type _type;

    /// <summary>Creates a new member table for the given object.</summary>
    protected MemberTable(object reflectionObject)
    {
        ArgumentNullException.ThrowIfNull(reflectionObject);
        _type = reflectionObject.GetType();
    }

    /// <summary>Gets the names of all members in the table.</summary>
    public List<string> GetMemberNames()
    {
        var memberNames = new List<string>();
        foreach (object o in _innerhash.Keys)
            memberNames.Add((o as string)!);
        return memberNames;
    }

    /// <inheritdoc/>
    public int Count => _innerhash.Count;

    MemberInfo? IMemberTable.this[string memberName]
    {
        get => _innerhash[memberName] as MemberInfo;
        set => _innerhash[memberName] = value;
    }

    void IMemberTable.Add(string memberName, MemberInfo value) => _innerhash.Add(memberName, value);
    void IMemberTable.Remove(string memberName) => _innerhash.Remove(memberName);

    /// <inheritdoc/>
    public Type Type => _type;

    /// <inheritdoc/>
    public bool IsSynchronized => _innerhash.IsSynchronized;

    void ICollection.CopyTo(Array array, int index) => _innerhash.CopyTo(array, index);

    /// <inheritdoc/>
    public object SyncRoot => _innerhash.SyncRoot;

    /// <inheritdoc/>
    public IEnumerator GetEnumerator() => _innerhash.GetEnumerator();
}
