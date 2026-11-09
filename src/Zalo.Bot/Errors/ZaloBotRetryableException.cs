namespace Zalo.Bot.Errors;

/// <summary>Represents a retryable API error (official code 408 request timeout or 429 quota exceeded)
/// whose retries were exhausted. Carries the code and description of the last failed attempt.</summary>
#pragma warning disable CA1032
public class ZaloBotRetryableException : ApiRequestException
#pragma warning restore CA1032
{
    /// <summary>Initializes a new instance of the <see cref="ZaloBotRetryableException"/> class.</summary>
    /// <param name="description">The description returned by the platform on the last failed attempt.</param>
    /// <param name="errorCode">The official error code (408/429).</param>
    public ZaloBotRetryableException(string? description, int errorCode) : base(description, errorCode) { }
}
