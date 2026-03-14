namespace Virabis.Core.Nexus.Manifest;

/// <summary>
/// Exception thrown when a feature manifest is invalid or fails validation.
/// </summary>
/// <remarks>
/// This exception is thrown by <see cref="ManifestLoader"/> when:
/// - Required fields are missing (e.g., Name, Version, Capabilities)
/// - Field values are invalid (e.g., negative InitTimeMs, invalid version format)
/// - JSON deserialization fails
/// - File not found or inaccessible
/// </remarks>
public class InvalidManifestException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidManifestException"/> class.
    /// </summary>
    public InvalidManifestException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidManifestException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InvalidManifestException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidManifestException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InvalidManifestException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
