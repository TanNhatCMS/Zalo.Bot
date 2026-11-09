using System.Globalization;

namespace Zalo.Bot;

/// <summary>This class is used to provide configuration for <see cref="ZaloBotClient"/></summary>
public class ZaloBotClientOptions
{
    /// <summary>Official Zalo Bot API server</summary>
    public const string DefaultBaseUrl = "https://bot-api.zaloplatforms.com";

    private int _timeoutSeconds = 30;

    /// <summary>Bot access token</summary>
    public string Token { get; }

    /// <summary>Base URL of the Zalo Bot API server. Path, query and fragment will be omitted if present.</summary>
    public string BaseUrl { get; }

    /// <summary>Request timeout in seconds. Applied per request (the shared <see cref="HttpClient"/> is never mutated).</summary>
    public int TimeoutSeconds
    {
        get => _timeoutSeconds;
        set => _timeoutSeconds = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "Timeout must be positive");
    }

    /// <summary>Total number of attempts for retryable errors (408/429). Values below 1 are treated as 1 (single attempt, no retry).</summary>
    public int RetryMaxAttempts { get; set; } = 3;

    /// <summary>Base backoff duration in seconds before the first retry. 0 disables waiting (useful for tests).</summary>
    public int RetryBaseBackoffSeconds { get; set; } = 2;

    /// <summary>Backoff growth factor: delay before retry i is <see cref="RetryBaseBackoffSeconds"/> * <see cref="RetryBackoffMultiplier"/>^(i-1).</summary>
    public double RetryBackoffMultiplier { get; set; } = 2.0;

    /// <summary>Contains base url for making requests. Contains the access token — never log or include it in errors.</summary>
    public string BaseRequestUrl => $"{BaseUrl}/bot{Token}";

    /// <summary>Request timeout as <see cref="TimeSpan"/></summary>
    internal TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds);

    /// <summary>Create a new <see cref="ZaloBotClientOptions"/> instance.</summary>
    /// <param name="token">Bot access token</param>
    /// <param name="baseUrl">Used to change base URL to a Zalo Bot API compatible server. It looks like https://bot-api.zaloplatforms.com. Path, query and fragment will be omitted if present.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="token"/> is empty or <paramref name="baseUrl"/> format is invalid</exception>
    public ZaloBotClientOptions(string token, string? baseUrl = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Bot access token must not be empty", nameof(token));
        Token = token;
        BaseUrl = baseUrl is null ? DefaultBaseUrl : ExtractBaseUrl(baseUrl);
    }

    private static string ExtractBaseUrl(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)
            || string.IsNullOrEmpty(baseUri.Scheme) || string.IsNullOrEmpty(baseUri.Authority))
            throw new ArgumentException("Invalid format. A valid base URL should look like \"https://bot-api.zaloplatforms.com\"", nameof(baseUrl));
        return $"{baseUri.Scheme}://{baseUri.Authority}";
    }

    /// <summary>Backoff delay to wait before retry number <paramref name="retryNumber"/> (1-based) after a retryable failure.</summary>
    internal TimeSpan GetRetryDelay(int retryNumber)
        => TimeSpan.FromSeconds(
            Math.Max(0, RetryBaseBackoffSeconds)
            * Math.Pow(Math.Max(1.0, RetryBackoffMultiplier), Math.Max(0, retryNumber - 1)));
}
