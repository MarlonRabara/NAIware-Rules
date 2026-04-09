using System.Reflection;

namespace NAIware.Core.Reflection;

/// <summary>
/// Provides extension methods for retrieving public properties, including from interfaces.
/// </summary>
public static class Properties
{
    /// <summary>
    /// Gets all public properties for a type, including those inherited from interfaces.
    /// </summary>
    /// <param name="type">The type to reflect.</param>
    /// <returns>An array of <see cref="PropertyInfo"/> for all public properties.</returns>
    public static PropertyInfo[] GetPublicProperties(this Type type)
    {
        if (!type.IsInterface)
            return type.GetProperties();

        return (new Type[] { type })
               .Concat(type.GetInterfaces())
               .SelectMany(i => i.GetProperties())
               .ToArray();
    }
}
