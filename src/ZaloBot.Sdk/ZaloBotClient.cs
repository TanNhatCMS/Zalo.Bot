using ZaloBot.Sdk.Args;
using ZaloBot.Sdk.Internal;

namespace ZaloBot.Sdk;

/// <summary>A client to use the Zalo Bot API</summary>
public class ZaloBotClient : IZaloBotClient
{
    private readonly ZaloBotClientOptions _options;
    private readonly ZaloSdkHttpSender _sender;

    /// <summary>Bot access token</summary>
    public string Token => _options.Token;

    /// <inheritdoc/>
    public TimeSpan Timeout
    {
        get => _options.Timeout;
        set => _options.TimeoutSeconds = (int)Math.Ceiling(value.TotalSeconds);
    }

    /// <inheritdoc/>
    public event AsyncEventHandler<ApiRequestEventArgs>? OnMakingApiRequest;

    /// <inheritdoc/>
    public event AsyncEventHandler<ApiResponseEventArgs>? OnApiResponseReceived;

    /// <summary>Create a new <see cref="ZaloBotClient"/> instance.</summary>
    /// <param name="options">Configuration for <see cref="ZaloBotClient"/></param>
    /// <param name="httpClient">A custom <see cref="HttpClient"/>; when omitted, a client with a 3-minute connection lifetime is created</param>
    /// <param name="timeProvider">Time provider used for retry backoff delays; defaults to <see cref="TimeProvider.System"/></param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is <see langword="null"/></exception>
    public ZaloBotClient(ZaloBotClientOptions options, HttpClient? httpClient = default, TimeProvider? timeProvider = default)
    {
        _options = options.ThrowIfNull();
        _sender = new ZaloSdkHttpSender(
            httpClient ?? new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(3) }),
            _options,
            timeProvider,
            RaiseOnMakingApiRequest,
            RaiseOnApiResponseReceived);
    }

    /// <summary>Create a new <see cref="ZaloBotClient"/> instance.</summary>
    /// <param name="accessToken">The bot access token</param>
    /// <param name="options">Optional configuration template (base URL, timeout, retry policy); the access token always comes from <paramref name="accessToken"/></param>
    /// <param name="httpClient">A custom <see cref="HttpClient"/></param>
    /// <param name="timeProvider">Time provider used for retry backoff delays</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="accessToken"/> is empty</exception>
    public ZaloBotClient(string accessToken, ZaloBotClientOptions? options = default, HttpClient? httpClient = default, TimeProvider? timeProvider = default)
        : this(CreateOptions(accessToken, options), httpClient, timeProvider)
    { }

    /// <inheritdoc/>
    public Task<TResponse> SendRequest<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        => _sender.SendAsync(request, cancellationToken);

    /// <summary>Parses a webhook payload (JSON body of the webhook request) into an <see cref="Update"/>.
    /// Defensive: unknown/missing fields and unexpected shapes map to <see cref="ContentType.Unknown"/>
    /// (or an update with default values) instead of throwing.</summary>
    public static Update ParseWebhookUpdate(JsonElement element) => ZaloSdkPayloadMapper.ParseUpdate(element);

    private async ValueTask RaiseOnMakingApiRequest(ApiRequestEventArgs args, CancellationToken cancellationToken)
    {
        if (OnMakingApiRequest is null)
        {
            return;
        }

        foreach (AsyncEventHandler<ApiRequestEventArgs> handler in OnMakingApiRequest.GetInvocationList())
        {
            await handler(this, args, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask RaiseOnApiResponseReceived(ApiResponseEventArgs args, CancellationToken cancellationToken)
    {
        if (OnApiResponseReceived is null)
        {
            return;
        }

        foreach (AsyncEventHandler<ApiResponseEventArgs> handler in OnApiResponseReceived.GetInvocationList())
        {
            await handler(this, args, cancellationToken).ConfigureAwait(false);
        }
    }

    private static ZaloBotClientOptions CreateOptions(string accessToken, ZaloBotClientOptions? template)
    {
        if (template is null)
        {
            return new ZaloBotClientOptions(accessToken);
        }

        return new ZaloBotClientOptions(accessToken, template.BaseUrl == ZaloBotClientOptions.DefaultBaseUrl ? null : template.BaseUrl)
        {
            TimeoutSeconds = template.TimeoutSeconds,
            RetryMaxAttempts = template.RetryMaxAttempts,
            RetryBaseBackoffSeconds = template.RetryBaseBackoffSeconds,
            RetryBackoffMultiplier = template.RetryBackoffMultiplier,
        };
    }
}
