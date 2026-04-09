using System.Reflection;

namespace NAIware.Core.Collections;

/// <summary>
/// A class that represents a collection base with reflection utilities.
/// </summary>
public class CollectionBase : System.Collections.CollectionBase
{
    /// <summary>
    /// Extracts the inner list from a <see cref="System.Collections.CollectionBase"/>.
    /// </summary>
    /// <param name="collection">The collection base to extract from.</param>
    /// <returns>An <see cref="System.Collections.ArrayList"/> that is the inner list for the collection base object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is <c>null</c>.</exception>
    public static System.Collections.ArrayList? GetInnerList(System.Collections.CollectionBase collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        var propArray = collection.GetType()
            .GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var prop in propArray)
        {
            if (prop.Name == "InnerList")
            {
                return prop.GetValue(collection, null) as System.Collections.ArrayList;
            }
        }

        return null;
    }
}
