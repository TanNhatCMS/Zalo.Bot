using Zalo.Bot.Errors;
using Zalo.Bot.Internal;
using Zalo.Bot.Types;

namespace Zalo.Bot;

/// <summary>QR login against the Zalo session service (bot.zaloplatforms.com/agent/*), ported from
/// openclaw-zaloclawbot (packages/plugin/src/auth/login-qr.ts). The SDK only obtains the login URL
/// and waits for confirmation — it NEVER renders a QR code (the frontend renders one from
/// <see cref="ZaloLoginSession.LoginUrl"/>) and NEVER stores credentials: the caller keeps the
/// returned <see cref="ZaloLoginResult.BotToken"/> and uses it to create a <see cref="ZaloBotClient"/>.
/// This client talks to a different host than the Bot API and needs no bot token, so it is
/// deliberately separate from <see cref="ZaloBotClient"/>.</summary>
public class ZaloBotLoginClient
{
    private readonly ZaloBotLoginOptions _options;
    private readonly HttpClient _httpClient;
    private readonly TimeProvider _timeProvider;

    /// <summary>Session service base URL in use.</summary>
    public string SessionServiceUrl => _options.SessionServiceUrl;

    /// <summary>Create a new <see cref="ZaloBotLoginClient"/> instance.</summary>
    /// <param name="options">Configuration for the login client; defaults match the openclaw-zaloclawbot flow
    /// (1.5 s poll interval, 5 min session timeout, 3 attempts).</param>
    /// <param name="httpClient">A custom <see cref="HttpClient"/>; when omitted, a client with a 3-minute
    /// connection lifetime is created.</param>
    /// <param name="timeProvider">Time provider used for the session deadline and poll delays;
    /// defaults to <see cref="TimeProvider.System"/>.</param>
    public ZaloBotLoginClient(ZaloBotLoginOptions? options = default, HttpClient? httpClient = default,
        TimeProvider? timeProvider = default)
    {
        _options = options ?? new ZaloBotLoginOptions();
        _httpClient = httpClient ?? new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(3) });
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Request a login session: GET {sessionService}/agent/request-login.
    /// Returns the URL to render as a QR code plus the session key (zbsk).
    /// The session (and its QR) expires after <see cref="ZaloBotLoginOptions.SessionTimeout"/>.</summary>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>The login session: QR URL + session key.</returns>
    /// <exception cref="ZaloBotNetworkException">Thrown on transport failure.</exception>
    /// <exception cref="ApiRequestException">Thrown when the platform answers with an error wrapper.</exception>
    /// <exception cref="InvalidJsonException">Thrown when the response is not valid JSON or lacks loginUrl/zbsk.</exception>
    public async Task<ZaloLoginSession> RequestLoginAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{_options.SessionServiceUrl}/agent/request-login";
        var body = await GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        var (payload, errorCode, description) = ParseEnvelope(body, zbskToMask: null);

        if (errorCode != 0)
            throw new ApiRequestException(description ?? "request-login failed.", errorCode);

        var loginUrl = GetString(payload, "loginUrl");
        var zbsk = GetString(payload, "zbsk");
        if (string.IsNullOrWhiteSpace(loginUrl) || string.IsNullOrWhiteSpace(zbsk))
            throw new InvalidJsonException(
                "request-login: malformed response (missing loginUrl or zbsk)",
                TokenSanitizer.Sanitize(body, zbsk ?? string.Empty));

        return new ZaloLoginSession { LoginUrl = loginUrl, Zbsk = zbsk };
    }

    /// <summary>Wait for the user to scan the QR of <paramref name="session"/>:
    /// GET {sessionService}/agent/get-login-status?zbsk=... every poll interval until the session is
    /// confirmed, expires, or the timeout elapses. Network errors are retried until the deadline,
    /// like the openclaw-zaloclawbot flow. Cancellation throws <see cref="OperationCanceledException"/>.</summary>
    /// <param name="session">Login session from <see cref="RequestLoginAsync"/>.</param>
    /// <param name="timeout">How long to wait; defaults to <see cref="ZaloBotLoginOptions.SessionTimeout"/> (5 min).</param>
    /// <param name="pollInterval">Interval between status polls; defaults to <see cref="ZaloBotLoginOptions.PollInterval"/> (1.5 s).</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>The login outcome; <see cref="ZaloLoginResult.BotToken"/> is set only when connected.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="session"/> has an empty zbsk.</exception>
    /// <exception cref="ApiRequestException">Thrown when the platform answers with an error other than 202/498.</exception>
    /// <exception cref="InvalidJsonException">Thrown when a status response is not valid JSON.</exception>
    public Task<ZaloLoginResult> WaitForLoginAsync(ZaloLoginSession session, TimeSpan? timeout = default,
        TimeSpan? pollInterval = default, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (string.IsNullOrWhiteSpace(session.Zbsk))
            throw new ArgumentException("Login session zbsk must not be empty", nameof(session));
        return WaitForLoginAsync(session.Zbsk, timeout, pollInterval, cancellationToken);
    }

    /// <summary>Wait for the user to scan the QR of the session identified by <paramref name="zbsk"/>.
    /// See <see cref="WaitForLoginAsync(ZaloLoginSession, TimeSpan?, TimeSpan?, CancellationToken)"/>.</summary>
    public async Task<ZaloLoginResult> WaitForLoginAsync(string zbsk, TimeSpan? timeout = default,
        TimeSpan? pollInterval = default, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(zbsk))
            throw new ArgumentException("zbsk must not be empty", nameof(zbsk));

        var interval = pollInterval ?? _options.PollInterval;
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(pollInterval), interval, "Poll interval must be positive");
        var waitTimeout = timeout ?? _options.SessionTimeout;
        if (waitTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), waitTimeout, "Timeout must be positive");

        var deadline = _timeProvider.GetUtcNow() + waitTimeout;
        var url = $"{_options.SessionServiceUrl}/agent/get-login-status?zbsk={Uri.EscapeDataString(zbsk)}";

        while (_timeProvider.GetUtcNow() < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string body;
            try
            {
                body = await GetStringAsync(url, cancellationToken).ConfigureAwait(false);
            }
            catch (ZaloBotNetworkException)
            {
                // Transient transport failure: keep polling until the deadline (openclaw-zaloclawbot parity).
                await DelayAsync(interval, cancellationToken).ConfigureAwait(false);
                continue;
            }

            // HTTP 498 is surfaced by GetStringAsync as an ApiRequestException with that code.
            // It is handled below via the parsed error code as well; keep this catch narrow.
            var (payload, errorCode, description) = ParseEnvelope(body, zbsk);

            if (GetBool(payload, "isLogin") && GetString(payload, "botToken") is { Length: > 0 } botToken)
            {
                return new ZaloLoginResult
                {
                    Status = ZaloLoginStatus.Connected,
                    BotToken = botToken,
                    BotId = GetStringOrNumber(payload, "botId"),
                    OwnerId = GetStringOrNumber(payload, "ownerId"),
                    OaId = GetStringOrNumber(payload, "oaId"),
                    Message = "Login confirmed.",
                };
            }

            if (errorCode == 498 || GetInt32(payload, "code") == 498)
                return ExpiredResult("QR/session expired (498).");

            if (errorCode != 0 && errorCode != 202)
                // The description may echo the session key — sanitize it like a token.
                throw new ApiRequestException(
                    TokenSanitizer.Sanitize(description, zbsk) ?? $"get-login-status failed: error_code {errorCode}.",
                    errorCode);

            await DelayAsync(interval, cancellationToken).ConfigureAwait(false);
        }

        return new ZaloLoginResult { Status = ZaloLoginStatus.TimedOut, Message = "Login timeout (5 min)." };
    }

    /// <summary>Run a full QR login: request a session, hand its URL to <paramref name="onSessionReady"/>
    /// (the frontend renders the QR from <see cref="ZaloLoginSession.LoginUrl"/>), then wait for the
    /// user to scan it. When a session expires or times out, a fresh session is requested (up to
    /// <see cref="ZaloBotLoginOptions.MaxLoginAttempts"/> times) and the callback is invoked again with
    /// the new URL so the frontend can re-render the QR — the caller always sees every new QR.
    /// Returns the first connected result, or the last expired/timed-out result.</summary>
    /// <param name="onSessionReady">Invoked with each fresh login session, before waiting for its scan.</param>
    /// <param name="maxAttempts">How many sessions to try at most; defaults to
    /// <see cref="ZaloBotLoginOptions.MaxLoginAttempts"/> (3).</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    public async Task<ZaloLoginResult> LoginAsync(Func<ZaloLoginSession, Task> onSessionReady,
        int? maxAttempts = default, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onSessionReady);
        var attempts = maxAttempts ?? _options.MaxLoginAttempts;
        if (attempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), attempts, "Max attempts must be at least 1");

        ZaloLoginResult last = new() { Status = ZaloLoginStatus.TimedOut, Message = "Login timeout (5 min)." };
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            var session = await RequestLoginAsync(cancellationToken).ConfigureAwait(false);
            await onSessionReady(session).ConfigureAwait(false);
            last = await WaitForLoginAsync(session, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (last.IsConnected)
                return last;
        }

        return last;
    }

    private static ZaloLoginResult ExpiredResult(string message)
        => new() { Status = ZaloLoginStatus.Expired, Message = message };

    /// <summary>One GET against the session service. HTTP 498 (session expired) is not an exception here:
    /// it is returned as a body so the caller can map it to <see cref="ZaloLoginStatus.Expired"/> —
    /// the platform answers 498 with an error wrapper body. Other non-success statuses throw.</summary>
    private async Task<string> GetStringAsync(string url, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_options.RequestTimeout);

        HttpResponseMessage response;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            response = await _httpClient.SendAsync(request, timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ZaloBotNetworkException("Zalo session service request timed out");
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException)
        {
            throw new ZaloBotNetworkException($"Zalo session service request failed: {exception.Message}", exception);
        }

        using (response)
        {
            string body;
            try
            {
                body = await response.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ZaloBotNetworkException("Zalo session service response could not be read: timed out");
            }
            catch (Exception exception) when (exception is HttpRequestException or IOException)
            {
                throw new ZaloBotNetworkException($"Zalo session service response could not be read: {exception.Message}", exception);
            }

            if (!response.IsSuccessStatusCode && (int)response.StatusCode != 498)
            {
                var (_, _, errorCode, description) = TryParseEnvelope(body);
                // The request URL carries the zbsk query param — a description echoing it
                // must not leak the session key into the exception.
                var safeDescription = TokenSanitizer.Sanitize(description, ExtractZbskFromUrl(url) ?? string.Empty);
                throw new ApiRequestException(
                    safeDescription ?? $"Session service request failed: HTTP {(int)response.StatusCode}.", errorCode);
            }

            return body;
        }
    }

    /// <summary>Extracts the zbsk query value from a get-login-status URL, if present.</summary>
    private static string? ExtractZbskFromUrl(string url)
    {
        const string marker = "zbsk=";
        var index = url.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
            return null;
        var start = index + marker.Length;
        var end = url.IndexOf('&', start);
        var encoded = end < 0 ? url[start..] : url[start..end];
        return Uri.UnescapeDataString(encoded);
    }

    /// <summary>Unwrap the {ok,result,error_code,description} envelope; a bare object without those
    /// keys is treated as the payload itself (openclaw-zaloclawbot parity). Invalid JSON throws.</summary>
    private static (JsonElement Payload, int ErrorCode, string? Description) ParseEnvelope(string body, string? zbskToMask)
    {
        var (ok, payload, errorCode, description) = TryParseEnvelope(body);
        if (!ok)
            throw new InvalidJsonException(
                "Zalo session service response is not a valid JSON object",
                TokenSanitizer.Sanitize(body, zbskToMask ?? string.Empty));
        return (payload, errorCode, description);
    }

    private static (bool Ok, JsonElement Payload, int ErrorCode, string? Description) TryParseEnvelope(string body)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return (false, default, 0, null);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return (false, default, 0, null);

            var hasEnvelopeKeys = root.TryGetProperty("ok", out _)
                || root.TryGetProperty("result", out _)
                || root.TryGetProperty("error_code", out _);
            var payload = hasEnvelopeKeys
                && root.TryGetProperty("result", out var result)
                && result.ValueKind == JsonValueKind.Object
                    ? result.Clone()
                    : root.Clone();

            var errorCode = root.TryGetProperty("error_code", out var codeElement)
                && codeElement.ValueKind == JsonValueKind.Number
                && codeElement.TryGetInt32(out var parsed)
                    ? parsed
                    : 0;
            var description = root.TryGetProperty("description", out var descElement)
                && descElement.ValueKind == JsonValueKind.String
                    ? descElement.GetString()
                    : null;

            return (true, payload, errorCode, description);
        }
    }

    private static string? GetString(JsonElement payload, string name)
        => payload.ValueKind == JsonValueKind.Object
            && payload.TryGetProperty(name, out var element)
            && element.ValueKind == JsonValueKind.String
                ? element.GetString()
                : null;

    private static bool GetBool(JsonElement payload, string name)
        => payload.ValueKind == JsonValueKind.Object
            && payload.TryGetProperty(name, out var element)
            && element.ValueKind == JsonValueKind.True;

    private static int? GetInt32(JsonElement payload, string name)
        => payload.ValueKind == JsonValueKind.Object
            && payload.TryGetProperty(name, out var element)
            && element.ValueKind == JsonValueKind.Number
            && element.TryGetInt32(out var value)
                ? value
                : null;

    /// <summary>IDs may arrive as strings or numbers (openclaw-zaloclawbot accepts both).</summary>
    private static string? GetStringOrNumber(JsonElement payload, string name)
    {
        if (payload.ValueKind != JsonValueKind.Object
            || !payload.TryGetProperty(name, out var element))
            return null;
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            _ => null,
        };
    }

    private async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (delay <= TimeSpan.Zero)
            return;
        await Task.Delay(delay, _timeProvider, cancellationToken).ConfigureAwait(false);
    }
}
