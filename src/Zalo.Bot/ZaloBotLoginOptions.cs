namespace Zalo.Bot;

/// <summary>Configuration for <see cref="ZaloBotLoginClient"/> (QR login via the Zalo session service).
/// Defaults mirror the openclaw-zaloclawbot login flow: 1.5 s poll interval, 5 min session lifetime.</summary>
public class ZaloBotLoginOptions
{
    /// <summary>Default Zalo session service used for QR login.</summary>
    public const string DefaultSessionServiceUrl = "https://bot.zaloplatforms.com";

    /// <summary>Default interval between get-login-status polls.</summary>
    public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(1500);

    /// <summary>Default lifetime of one login session (zbsk); the QR expires after this.</summary>
    public static readonly TimeSpan DefaultSessionTimeout = TimeSpan.FromMinutes(5);

    /// <summary>Default number of fresh QR sessions <see cref="ZaloBotLoginClient.LoginAsync"/> requests before giving up.</summary>
    public const int DefaultMaxLoginAttempts = 3;

    private TimeSpan _pollInterval = DefaultPollInterval;
    private TimeSpan _sessionTimeout = DefaultSessionTimeout;
    private TimeSpan _requestTimeout = TimeSpan.FromSeconds(30);
    private int _maxLoginAttempts = DefaultMaxLoginAttempts;

    /// <summary>Base URL of the Zalo session service (request-login / get-login-status).
    /// Path, query and fragment will be omitted if present.</summary>
    public string SessionServiceUrl { get; }

    /// <summary>Interval between get-login-status polls. Must be positive.</summary>
    public TimeSpan PollInterval
    {
        get => _pollInterval;
        set => _pollInterval = value > TimeSpan.Zero
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Poll interval must be positive");
    }

    /// <summary>How long one login session is waited on before it counts as timed out. Must be positive.</summary>
    public TimeSpan SessionTimeout
    {
        get => _sessionTimeout;
        set => _sessionTimeout = value > TimeSpan.Zero
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Session timeout must be positive");
    }

    /// <summary>Per-request HTTP timeout for the session-service calls. Must be positive.</summary>
    public TimeSpan RequestTimeout
    {
        get => _requestTimeout;
        set => _requestTimeout = value > TimeSpan.Zero
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Request timeout must be positive");
    }

    /// <summary>How many fresh QR sessions <see cref="ZaloBotLoginClient.LoginAsync"/> requests at most
    /// when a session expires/times out. Must be at least 1.</summary>
    public int MaxLoginAttempts
    {
        get => _maxLoginAttempts;
        set => _maxLoginAttempts = value >= 1
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Max login attempts must be at least 1");
    }

    /// <summary>Create a new <see cref="ZaloBotLoginOptions"/> instance.</summary>
    /// <param name="sessionServiceUrl">Used to change the session service to a compatible server.
    /// It looks like https://bot.zaloplatforms.com. Path, query and fragment will be omitted if present.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="sessionServiceUrl"/> format is invalid</exception>
    public ZaloBotLoginOptions(string? sessionServiceUrl = default)
    {
        SessionServiceUrl = sessionServiceUrl is null
            ? DefaultSessionServiceUrl
            : ExtractBaseUrl(sessionServiceUrl);
    }

    private static string ExtractBaseUrl(string sessionServiceUrl)
    {
        if (!Uri.TryCreate(sessionServiceUrl, UriKind.Absolute, out var baseUri)
            || string.IsNullOrEmpty(baseUri.Scheme) || string.IsNullOrEmpty(baseUri.Authority))
            throw new ArgumentException(
                "Invalid format. A valid session service URL should look like \"https://bot.zaloplatforms.com\"",
                nameof(sessionServiceUrl));
        return $"{baseUri.Scheme}://{baseUri.Authority}";
    }
}
