namespace NAIware.Core.Interfaces;

/// <summary>
/// Defines a contract for logging messages within the NAIware framework.
/// </summary>
public interface IMessageLogger
{
    /// <summary>
    /// Logs a message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void LogMessage(string message);
}
