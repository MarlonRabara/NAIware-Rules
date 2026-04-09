using NAIware.Core.Text;

namespace NAIware.Core.Collections;

/// <summary>
/// Provides utility methods for working with enumerations.
/// </summary>
public static class EnumerationHelper
{
    /// <summary>
    /// Gets a list of bindable enumeration items for the specified enum type.
    /// </summary>
    public static List<EnumerationItem> GetEnumerationItems<T>() where T : struct, Enum
    {
        var enumeratedItems = new List<EnumerationItem>();

        foreach (T enumVal in Enum.GetValues<T>())
        {
            enumeratedItems.Add(new EnumerationItem(
                Convert.ToInt32(enumVal),
                enumVal.ToString(),
                StringHelper.FormatEnumeration(enumVal)));
        }

        return enumeratedItems;
    }

    /// <summary>
    /// Converts an enum type to a list of its values.
    /// </summary>
    public static List<T> EnumToList<T>() where T : struct, Enum =>
        [.. Enum.GetValues<T>()];

    /// <summary>
    /// Parses a string to the specified enum type.
    /// </summary>
    public static T GetEnumerationValue<T>(string enumName) where T : struct, Enum =>
        Enum.Parse<T>(enumName);

    /// <summary>
    /// Converts an enumeration value from one enum type to another.
    /// </summary>
    public static E ConvertEnumeration<E, V>(V enumValue) where E : struct, Enum where V : notnull =>
        Enum.Parse<E>(enumValue.ToString()!);
}

/// <summary>
/// Represents a bindable enumeration item with numeric value and text representations.
/// </summary>
public class EnumerationItem
{
    internal EnumerationItem(int value, string textValue, string formattedText)
    {
        Value = value;
        TextValue = textValue;
        FormattedText = formattedText;
    }

    /// <summary>Gets the numeric value.</summary>
    public int Value { get; }

    /// <summary>Gets the text value.</summary>
    public string TextValue { get; }

    /// <summary>Gets the formatted display text.</summary>
    public string FormattedText { get; }
}
