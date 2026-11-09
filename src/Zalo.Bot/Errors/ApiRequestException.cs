namespace Zalo.Bot.Errors;

/// <summary>Represents an API error — the platform answered with an error wrapper (ok=false).
/// Carries the official error code and its <see cref="ZaloErrorCategory"/>.</summary>
#pragma warning disable CA1032
public class ApiRequestException : RequestException
#pragma warning restore CA1032
{
    /// <summary>Gets the official error code (400/401/403/404/408/429), or 0 when the response carried no code.</summary>
    public int ErrorCode { get; }

    /// <summary>Gets the category derived from the official error code.</summary>
    public override ZaloErrorCategory Category => ZaloSdkErrorTaxonomy.CategoryOf(ErrorCode);

    /// <summary>Initializes a new instance of the <see cref="ApiRequestException"/> class.</summary>
    /// <param name="description">The description returned by the platform (used as exception message).</param>
    /// <param name="errorCode">The official error code.</param>
    public ApiRequestException(string? description, int errorCode)
        : base(description ?? "Zalo Bot API call failed.") => ErrorCode = errorCode;

    /// <inheritdoc/>
    public override string ToString()
    {
        var str = base.ToString();
        if (str.IndexOf(':') is >= 0 and int colon)
            str = $"Zalo Bot API error {ErrorCode}{str[colon..]}";
        return str;
    }
}
