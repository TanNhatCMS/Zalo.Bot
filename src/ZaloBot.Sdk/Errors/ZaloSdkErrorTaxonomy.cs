namespace ZaloBot.Sdk.Errors;

/// <summary>Maps official Zalo Bot API error codes to <see cref="ZaloErrorCategory"/> and builds the
/// matching exception. Official codes: 400 bad request, 401 token invalid/expired,
/// 403 internal server error, 404 not found, 408 request timeout, 429 quota exceeded.</summary>
public static class ZaloSdkErrorTaxonomy
{
    /// <summary>Category for an official error code. Codes outside the official table map to <see cref="ZaloErrorCategory.Unknown"/>.</summary>
    public static ZaloErrorCategory CategoryOf(int errorCode) => errorCode switch
    {
        400 => ZaloErrorCategory.NonRetryable,
        401 => ZaloErrorCategory.Token,
        403 => ZaloErrorCategory.ServerRetry,
        404 => ZaloErrorCategory.NonRetryable,
        408 => ZaloErrorCategory.Retryable,
        429 => ZaloErrorCategory.Retryable,
        _ => ZaloErrorCategory.Unknown,
    };

    /// <summary>Indicates whether requests failing with this code should be retried (408/429) with backoff.</summary>
    public static bool IsRetryable(int errorCode) => CategoryOf(errorCode) == ZaloErrorCategory.Retryable;

    /// <summary>Indicates whether requests failing with this code should be retried exactly once (403).</summary>
    public static bool IsServerRetry(int errorCode) => CategoryOf(errorCode) == ZaloErrorCategory.ServerRetry;

    /// <summary>Builds the exception matching the official error code. Messages never contain the access token.</summary>
    public static ApiRequestException CreateException(int errorCode, string? description)
        => CategoryOf(errorCode) switch
        {
            ZaloErrorCategory.Token => new ZaloBotTokenException(description, errorCode),
            ZaloErrorCategory.Retryable => new ZaloBotRetryableException(description, errorCode),
            _ => new ApiRequestException(description, errorCode),
        };
}
