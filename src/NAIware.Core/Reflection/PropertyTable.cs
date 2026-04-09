using System.Collections;
using System.Reflection;

namespace NAIware.Core.Reflection;

/// <summary>
/// A hash-table-backed collection of reflected property members for quick lookup.
/// </summary>
public class PropertyTable : MemberTable, IPropertyTable
{
    /// <summary>Creates a new property table by reflecting the given object.</summary>
    /// <param name="reflectionObject">The object to reflect.</param>
    public PropertyTable(object reflectionObject) : base(reflectionObject)
    {
        var propertyarray = reflectionObject.GetType()
            .GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

        for (int i = 0; i < propertyarray.Length; i++)
            _innerhash[propertyarray[i].Name] = propertyarray[i];
    }

    /// <summary>Gets all simple-type properties from the table.</summary>
    public PropertyInfo[]? GetSimpleMembers()
    {
        if (_simplemembers is not null) return _simplemembers as PropertyInfo[];

        var templist = new List<PropertyInfo>();
        var en = GetEnumerator() as IDictionaryEnumerator;

        if (en is not null)
        {
            while (en.MoveNext())
            {
                if (en.Value is PropertyInfo currprop && TypeHelper.IsSimpleType(currprop.PropertyType))
                    templist.Add(currprop);
            }
        }

        _simplemembers = [.. templist];
        return _simplemembers as PropertyInfo[];
    }

    /// <inheritdoc/>
    public PropertyInfo? this[string memberName]
    {
        get => _innerhash[memberName] as PropertyInfo;
        set => _innerhash[memberName] = value;
    }

    /// <inheritdoc/>
    public void Add(string memberName, PropertyInfo value) => _innerhash.Add(memberName, value);

    /// <inheritdoc/>
    public void Remove(string memberName) => _innerhash.Remove(memberName);
}
