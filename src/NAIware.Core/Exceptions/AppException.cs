namespace NAIware.Core.Exceptions;

/// <summary>
/// Represents an application-level exception within the NAIware framework.
/// </summary>
[Serializable]
public class AppException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="AppException"/> with a formatted message.
    /// </summary>
    /// <param name="message">A composite format string for the error message.</param>
    /// <param name="msgParams">An array of objects to format.</param>
    public AppException(string message, params object[] msgParams)
        : base(string.Format(message, msgParams))
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AppException"/> with a formatted message and inner exception.
    /// </summary>
    /// <param name="message">A composite format string for the error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    /// <param name="msgParams">An array of objects to format.</param>
    public AppException(string message, Exception innerException, params object[] msgParams)
        : base(string.Format(message, msgParams), innerException)
    {
    }
}
