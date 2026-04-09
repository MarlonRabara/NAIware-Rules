using System.Text;
using System.Text.RegularExpressions;

namespace NAIware.Core.Text;

/// <summary>
/// Provides static utility methods for common string manipulation operations.
/// </summary>
public static class StringHelper
{
    /// <summary>
    /// Determines whether the specified string is a valid variable name.
    /// </summary>
    /// <param name="anyVariable">The string to analyze.</param>
    /// <returns><c>true</c> if the string is a valid variable declaration; otherwise, <c>false</c>.</returns>
    public static bool IsValidVariable(string anyVariable)
    {
        var regexPattern = new Regex("[a-zA-Z_$][a-zA-Z0-9_$]*", RegexOptions.Compiled);
        var matches = regexPattern.Matches(anyVariable);
        if (matches.Count != 1)
            return false;

        var matchedVariable = matches[0].Value;

        if (string.Equals(matchedVariable, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(matchedVariable, "false", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return matchedVariable == anyVariable;
    }

    /// <summary>
    /// Encodes a string for safe use in URLs by escaping single quotes and ampersands.
    /// </summary>
    public static string? ToSafeUrlString(string? any)
    {
        if (string.IsNullOrEmpty(any)) return any;
        return any.Replace("'", "%27").Replace("&", "%26");
    }

    /// <summary>
    /// Decodes a URL-safe string by restoring escaped single quotes and ampersands.
    /// </summary>
    public static string? FromSafeUrlString(string? any)
    {
        if (string.IsNullOrEmpty(any)) return any;
        return any.Replace("%27", "'").Replace("%26", "&");
    }

    /// <summary>
    /// Counts the number of characters in the specified string that belong to the given character class.
    /// </summary>
    /// <param name="any">The string to analyze.</param>
    /// <param name="characterClass">The character class to count.</param>
    /// <returns>The number of matching characters.</returns>
    public static int GetCharacterCount(string any, CharacterClass characterClass)
    {
        if (string.IsNullOrEmpty(any)) return 0;

        int count = 0;
        for (int i = 0; i < any.Length; i++)
        {
            count += characterClass switch
            {
                CharacterClass.LowerCase when char.IsLower(any[i]) => 1,
                CharacterClass.UpperCase when char.IsUpper(any[i]) => 1,
                CharacterClass.Letter when char.IsLetter(any[i]) => 1,
                CharacterClass.Numeric when char.IsNumber(any[i]) => 1,
                CharacterClass.Symbol when !char.IsNumber(any[i]) && !char.IsLetter(any[i]) => 1,
                _ => 0
            };
        }

        return count;
    }

    /// <summary>
    /// Extracts only alphanumeric characters from the specified string.
    /// </summary>
    public static string? ExtractAlphaNumericCharacters(string? any)
    {
        if (string.IsNullOrEmpty(any)) return any;

        var sb = new StringBuilder();
        foreach (char c in any)
            if (char.IsLetterOrDigit(c))
                sb.Append(c);

        return sb.ToString();
    }

    /// <summary>
    /// Removes all space characters from the specified string.
    /// </summary>
    public static string? RemoveSpaces(string? any)
    {
        if (string.IsNullOrEmpty(any)) return any;

        var sb = new StringBuilder();
        foreach (char c in any)
        {
            if (c == ' ') continue;
            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a nullable phone number as (###) ###-####.
    /// </summary>
    public static string? FormatPhone(long? phoneNumber)
    {
        if (!phoneNumber.HasValue) return null;
        return FormatPhone(phoneNumber.Value);
    }

    /// <summary>
    /// Formats a phone number as (###) ###-####.
    /// </summary>
    public static string FormatPhone(long phoneNumber) =>
        string.Format("{0:(###) ###-####}", phoneNumber);

    /// <summary>
    /// Formats a nullable zip code with leading zeros.
    /// </summary>
    public static string FormatZip(int? zip) =>
        zip is null ? string.Empty : $"{zip.Value:00000}";

    /// <summary>
    /// Extracts only alphanumeric characters from the string representation of the object.
    /// </summary>
    public static string? ToAlphaNumeric(object? any) =>
        any is null ? null : ToAlphaNumeric(any.ToString());

    /// <summary>
    /// Determines whether the specified string represents a numeric value.
    /// </summary>
    public static bool IsNumeric(string any) =>
        decimal.TryParse(any, out _);

    /// <summary>
    /// Formats a value as currency with optional decimal places and dollar sign.
    /// </summary>
    public static string CurrencyFormat(object? any, bool displayDecimalPlaces, bool displayDollarSign = true)
    {
        if (any is null) return string.Empty;

        if (displayDollarSign)
            return string.Format(displayDecimalPlaces ? "{0:C}" : "{0:C0}", any);

        var nfi = new System.Globalization.NumberFormatInfo { CurrencySymbol = string.Empty };
        return string.Format(nfi, displayDecimalPlaces ? "{0:C}" : "{0:C0}", any);
    }

    /// <summary>
    /// Extracts only alphanumeric characters from the specified string.
    /// </summary>
    public static string? ToAlphaNumeric(string? any)
    {
        if (string.IsNullOrEmpty(any)) return any;

        var sb = new StringBuilder();
        foreach (char c in any)
            if (char.IsLetterOrDigit(c))
                sb.Append(c);

        return sb.ToString();
    }

    /// <summary>
    /// Extracts only digit characters from the specified string.
    /// </summary>
    public static string? ExtractDigits(string? any)
    {
        if (string.IsNullOrEmpty(any)) return any;

        var sb = new StringBuilder();
        foreach (char c in any)
            if (char.IsDigit(c))
                sb.Append(c);

        return sb.ToString();
    }

    /// <summary>
    /// Formats an enumeration value by inserting spaces before uppercase letters.
    /// </summary>
    /// <param name="any">The enumeration value to format.</param>
    /// <returns>A human-readable string (e.g., "RateType" becomes "Rate Type").</returns>
    public static string FormatEnumeration(Enum any) =>
        InsertSpaceInProperCaseString(any.ToString());

    /// <summary>
    /// Inserts spaces before uppercase letters in a PascalCase string.
    /// </summary>
    public static string? InsertSpaceInProperCaseString(string? properCasedString)
    {
        if (string.IsNullOrEmpty(properCasedString)) return properCasedString;

        var sb = new StringBuilder();
        foreach (char c in properCasedString)
        {
            if (char.IsUpper(c) && sb.Length > 0)
                sb.Append(' ').Append(c);
            else
                sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes single quotes in a SQL string to prevent injection.
    /// </summary>
    public static string? SafeSQLString(string? sql) =>
        sql?.Replace("'", "''");

    /// <summary>
    /// Returns a trimmed, non-null string from any object.
    /// </summary>
    public static string SafeString(object? any) =>
        any is null ? string.Empty : SafeString(any.ToString());

    /// <summary>
    /// Returns a trimmed, non-null string.
    /// </summary>
    public static string SafeString(string? any) =>
        any?.Trim() ?? string.Empty;

    /// <summary>
    /// Combines a list of strings into a single delimited string.
    /// </summary>
    /// <param name="stringList">The list of strings to combine.</param>
    /// <param name="delimiter">The delimiter to place between items.</param>
    /// <returns>A single combined string, or <c>null</c> if the list is empty.</returns>
    public static string? RenderList(List<string>? stringList, string? delimiter)
    {
        if (stringList is null || stringList.Count == 0) return null;

        var sb = new StringBuilder();
        for (int i = 0; i < stringList.Count; i++)
        {
            if (!string.IsNullOrEmpty(delimiter) && i < stringList.Count - 1)
                sb.Append(stringList[i]).Append(delimiter);
            else
                sb.Append(stringList[i]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Removes the first occurrence of each specified character from the string.
    /// </summary>
    public static string? Remove(char[]? characterToRemove, string? valueToCheck)
    {
        if (characterToRemove is not null && !string.IsNullOrEmpty(valueToCheck))
        {
            for (int i = 0; i < characterToRemove.Length; i++)
            {
                int indexOf = valueToCheck.IndexOf(characterToRemove[i]);
                if (indexOf >= 0)
                    valueToCheck = valueToCheck.Remove(indexOf, 1);
            }
        }
        return valueToCheck;
    }

    /// <summary>
    /// Gets a single character at the specified index as a string.
    /// </summary>
    public static string? GetStringByIndex(string? stringToParse, int indexOf)
    {
        if (string.IsNullOrEmpty(stringToParse)) return null;
        return stringToParse[indexOf].ToString();
    }
}
