namespace ZaloBot.Sdk.Errors;

/// <summary>Represents a failure to parse the response body as a Zalo Bot API JSON wrapper.
/// Carries a short, token-free excerpt of the offending body for diagnostics.</summary>
#pragma warning disable CA1032
public class InvalidJsonException : RequestException
#pragma warning restore CA1032
{
    /// <summary>Safe (sanitized, truncated) excerpt of the response body</summary>
    public string? BodyExcerpt { get; }

    /// <inheritdoc/>
    public override ZaloErrorCategory Category => ZaloErrorCategory.Parse;

    /// <summary>Initializes a new instance of the <see cref="InvalidJsonException"/> class.</summary>
    /// <param name="message">The message that describes the error. Must not contain the access token.</param>
    /// <param name="bodyExcerpt">Sanitized, truncated excerpt of the response body (optional).</param>
    public InvalidJsonException(string message, string? bodyExcerpt = default) : base(message)
        => BodyExcerpt = bodyExcerpt;
}
