namespace Zalo.Bot.Errors;

/// <summary>Represents a request error. Base class for all exceptions thrown by the SDK.</summary>
#pragma warning disable CA1032
public class RequestException : Exception
#pragma warning restore CA1032
{
    /// <summary>Category of the failure</summary>
    public virtual ZaloErrorCategory Category => ZaloErrorCategory.Network;

    /// <summary>Initializes a new instance of the <see cref="RequestException"/> class.</summary>
    /// <param name="message">The message that describes the error.</param>
    public RequestException(string message) : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="RequestException"/> class.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
    public RequestException(string message, Exception innerException) : base(message, innerException) { }
}
