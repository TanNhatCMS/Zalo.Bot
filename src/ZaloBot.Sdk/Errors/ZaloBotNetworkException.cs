namespace ZaloBot.Sdk.Errors;

/// <summary>Represents a network/transport failure (connection error, DNS, TLS, timeout).
/// The SDK does not auto-retry network errors; callers decide.</summary>
#pragma warning disable CA1032
public class ZaloBotNetworkException : RequestException
#pragma warning restore CA1032
{
    /// <summary>Initializes a new instance of the <see cref="ZaloBotNetworkException"/> class.</summary>
    /// <param name="message">The message that describes the error. Must not contain the access token.</param>
    public ZaloBotNetworkException(string message) : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="ZaloBotNetworkException"/> class.</summary>
    /// <param name="message">The message that describes the error. Must not contain the access token.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ZaloBotNetworkException(string message, Exception innerException) : base(message, innerException) { }
}
