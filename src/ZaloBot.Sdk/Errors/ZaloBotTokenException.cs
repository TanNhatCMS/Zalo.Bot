namespace ZaloBot.Sdk.Errors;

/// <summary>Represents an authentication failure — the access token is invalid or expired (official code 401).
/// Never retried by the SDK; the caller should refresh/replace the token.</summary>
#pragma warning disable CA1032
public class ZaloBotTokenException : ApiRequestException
#pragma warning restore CA1032
{
    /// <summary>Initializes a new instance of the <see cref="ZaloBotTokenException"/> class.</summary>
    /// <param name="description">The description returned by the platform (used as exception message).</param>
    /// <param name="errorCode">The official error code (401).</param>
    public ZaloBotTokenException(string? description, int errorCode = 401) : base(description, errorCode) { }
}
