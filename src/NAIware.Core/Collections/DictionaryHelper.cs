namespace NAIware.Core.Collections;

/// <summary>
/// Provides static utility methods for dictionaries.
/// </summary>
public static class DictionaryHelper
{
    /// <summary>
    /// Parses a connection-string-style string into key-value pairs.
    /// </summary>
    /// <param name="connectionString">The connection string to parse (e.g., "Key1=Value1;Key2=Value2").</param>
    /// <returns>A dictionary of key-value pairs.</returns>
    public static Dictionary<string, string> GetDictionaryFromConnectionString(string? connectionString)
    {
        var cstringDict = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(connectionString)) return cstringDict;

        string[] settings = connectionString.Split(';');
        foreach (string setting in settings)
        {
            if (string.IsNullOrEmpty(setting)) continue;

            string[] kvpair = setting.Split('=');
            if (kvpair.Length < 2) continue;

            cstringDict.Add(kvpair[0], kvpair[1]);
        }

        return cstringDict;
    }
}
