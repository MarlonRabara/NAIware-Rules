using System.Collections;
using System.Reflection;

namespace NAIware.Core.Reflection;

/// <summary>
/// A hash-table-backed collection of reflected field members for quick lookup.
/// </summary>
public class FieldTable : MemberTable, IFieldTable
{
    /// <summary>Creates a new field table by reflecting the given object.</summary>
    /// <param name="reflectionObject">The object to reflect.</param>
    public FieldTable(object reflectionObject) : base(reflectionObject)
    {
        var fieldarray = reflectionObject.GetType()
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

        for (int i = 0; i < fieldarray.Length; i++)
            _innerhash[fieldarray[i].Name] = fieldarray[i];
    }

    /// <summary>Gets all simple-type fields from the table.</summary>
    public FieldInfo[]? GetSimpleMembers()
    {
        if (_simplemembers is not null) return _simplemembers as FieldInfo[];

        var templist = new List<FieldInfo>();
        var en = GetEnumerator() as IDictionaryEnumerator;

        if (en is not null)
        {
            while (en.MoveNext())
            {
                if (en.Value is FieldInfo currfield && TypeHelper.IsSimpleType(currfield.FieldType))
                    templist.Add(currfield);
            }
        }

        _simplemembers = [.. templist];
        return _simplemembers as FieldInfo[];
    }

    /// <inheritdoc/>
    public FieldInfo? this[string memberName]
    {
        get => _innerhash[memberName] as FieldInfo;
        set => _innerhash[memberName] = value;
    }

    /// <inheritdoc/>
    public void Add(string memberName, FieldInfo value) => _innerhash.Add(memberName, value);

    /// <inheritdoc/>
    public void Remove(string memberName) => _innerhash.Remove(memberName);
}
