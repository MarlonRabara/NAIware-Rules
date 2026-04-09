namespace NAIware.Core.Collections;

/// <summary>
/// A class that provides collection search utilities.
/// </summary>
public static class CollectionHelper
{
    /// <summary>
    /// Gets an indicator that determines if a string is contained in the string array (case sensitive).
    /// </summary>
    /// <param name="stringArray">The string array to search.</param>
    /// <param name="value">The string value to search for.</param>
    /// <returns><c>true</c> if the array contains the value; otherwise <c>false</c>.</returns>
    public static bool Contains(string[] stringArray, string value) =>
        Contains(stringArray, value, caseSensitive: true);

    /// <summary>
    /// Gets an indicator that determines if a string is contained in the string array.
    /// </summary>
    /// <param name="stringArray">The string array to search.</param>
    /// <param name="value">The string value to search for.</param>
    /// <param name="caseSensitive">An indicator if this is case sensitive or not.</param>
    /// <returns><c>true</c> if the array contains the value; otherwise <c>false</c>.</returns>
    public static bool Contains(string[] stringArray, string value, bool caseSensitive)
    {
        if (stringArray is null || stringArray.Length == 0 || value is null) return false;

        var comparison = caseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        foreach (var arrayItem in stringArray)
        {
            if (string.Equals(arrayItem, value, comparison)) return true;
        }

        return false;
    }
}
