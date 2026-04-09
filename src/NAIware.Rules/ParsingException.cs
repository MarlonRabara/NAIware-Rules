using System.Text;

namespace NAIware.Rules;

/// <summary>
/// An exception thrown during expression parsing when invalid characters or structure is detected.
/// </summary>
public class ParsingException : Exception
{
    /// <summary>Creates a parsing exception with source context information.</summary>
    public ParsingException(string originalExceptionMessage, string expressionSource, string sourceIdentifier)
        : base($"Expression Source: {expressionSource}, Identity: {sourceIdentifier}, {originalExceptionMessage}")
    {
    }

    /// <summary>Creates a parsing exception from a list of invalid character trackers.</summary>
    internal ParsingException(List<CharacterTracker> invalidChars, string originalExpression)
        : base(ConvertToMessage(invalidChars) +
               (string.IsNullOrEmpty(originalExpression) ? string.Empty : " Original Expression: " + originalExpression))
    {
    }

    private static string ConvertToMessage(List<CharacterTracker> invalidChars)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < invalidChars.Count; i++)
        {
            if (sb.Length == 0)
                sb.Append("Parsing exception detected: ");

            if (invalidChars[i].IsValid)
                sb.AppendFormat("No corresponding match for '{0}' at position {1}{2}",
                    invalidChars[i].Character,
                    invalidChars[i].Position,
                    i < invalidChars.Count - 1 ? ", " : ".");
            else
                sb.AppendFormat("Invalid character '{0}' detected at position {1}{2}",
                    invalidChars[i].Character,
                    invalidChars[i].Position,
                    i < invalidChars.Count - 1 ? ", " : ".");
        }

        return sb.ToString();
    }
}
