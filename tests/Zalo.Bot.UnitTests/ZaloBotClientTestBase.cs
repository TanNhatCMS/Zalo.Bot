using Shouldly;

namespace Zalo.Bot.UnitTests;

/// <summary>Shared fixture helpers for SDK tests.</summary>
public abstract class ZaloBotClientTestBase
{
    /// <summary>Token used across tests — assertions rely on it never appearing in exception messages.</summary>
    protected const string AccessToken = "1501641810090548646:SECRET-TOKEN-ab12cd34";

    protected static readonly string BotUrl = $"http://test.local/bot{AccessToken}";

    protected static ZaloBotClient CreateClient(FakeHttpMessageHandler handler, Action<ZaloBotClientOptions>? configure = default)
    {
        var options = new ZaloBotClientOptions(AccessToken, "http://test.local")
        {
            RetryMaxAttempts = 3,
            RetryBaseBackoffSeconds = 0, // no real waiting in tests; backoff math covered separately
            RetryBackoffMultiplier = 2.0,
        };
        configure?.Invoke(options);
        return new ZaloBotClient(options, new HttpClient(handler));
    }

    protected static Task<T> ThrowAsync<T>(Func<Task> action) where T : Exception
        => Should.ThrowAsync<T>(action);
}
