using Shouldly;

namespace Zalo.Bot.UnitTests;

/// <summary>QR login via the Zalo session service — ported from openclaw-zaloclawbot
/// (packages/plugin/src/auth/login-qr.ts). The SDK returns the login URL only; it never
/// renders a QR code and never stores credentials.</summary>
public class QrLoginTests
{
    private const string LoginJson =
        """{"ok":true,"result":{"loginUrl":"https://bot.zaloplatforms.com/login?zbsk=abc123","zbsk":"abc123"}}""";

    private const string ConfirmedJson =
        """{"ok":true,"result":{"isLogin":true,"botToken":"1501641810090548646:LOGIN-TOKEN-ab12cd34","botId":1501641810090548646,"ownerId":"987654321","oaId":1122334455}}""";

    private const string PendingJson = """{"ok":true,"result":{"isLogin":false}}""";

    private static ZaloBotLoginClient CreateLoginClient(FakeHttpMessageHandler handler,
        Action<ZaloBotLoginOptions>? configure = default)
    {
        var options = new ZaloBotLoginOptions("http://test.local")
        {
            PollInterval = TimeSpan.FromMilliseconds(10),
            SessionTimeout = TimeSpan.FromMilliseconds(300),
            RequestTimeout = TimeSpan.FromSeconds(5),
        };
        configure?.Invoke(options);
        return new ZaloBotLoginClient(options, new HttpClient(handler));
    }

    [Test]
    public async Task Should_ReturnLoginUrlAndZbsk_When_RequestLoginSucceeds()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(LoginJson);
        var client = CreateLoginClient(handler);

        var session = await client.RequestLoginAsync();

        session.LoginUrl.ShouldBe("https://bot.zaloplatforms.com/login?zbsk=abc123");
        session.Zbsk.ShouldBe("abc123");
        handler.RequestCount.ShouldBe(1);
        handler.RequestUrls[0].ShouldBe("http://test.local/agent/request-login");
        handler.Requests[0].Method.ShouldBe(HttpMethod.Get);
    }

    [Test]
    public async Task Should_AcceptBareObject_When_RequestLoginHasNoEnvelope()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"loginUrl":"https://example.test/qr","zbsk":"z9"}""");
        var client = CreateLoginClient(handler);

        var session = await client.RequestLoginAsync();

        session.LoginUrl.ShouldBe("https://example.test/qr");
        session.Zbsk.ShouldBe("z9");
    }

    [Test]
    public async Task Should_ThrowInvalidJson_When_RequestLoginMissesZbsk()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":true,"result":{"loginUrl":"https://example.test/qr"}}""");
        var client = CreateLoginClient(handler);

        var exception = await Should.ThrowAsync<InvalidJsonException>(() => client.RequestLoginAsync());

        exception.Message.ShouldContain("missing loginUrl or zbsk");
    }

    [Test]
    public async Task Should_ThrowApiException_When_RequestLoginReturnsErrorWrapper()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":false,"error_code":400,"description":"bad request"}""", System.Net.HttpStatusCode.BadRequest);
        var client = CreateLoginClient(handler);

        var exception = await Should.ThrowAsync<ApiRequestException>(() => client.RequestLoginAsync());

        exception.ErrorCode.ShouldBe(400);
        exception.Message.ShouldContain("bad request");
    }

    [Test]
    public async Task Should_ThrowInvalidJson_When_RequestLoginReturnsNonJson()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("not json at all");
        var client = CreateLoginClient(handler);

        await Should.ThrowAsync<InvalidJsonException>(() => client.RequestLoginAsync());
    }

    [Test]
    public async Task Should_ReturnConnected_When_StatusPollPendingThenConfirmed()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(PendingJson, System.Net.HttpStatusCode.Accepted); // 202 = not scanned yet
        handler.EnqueueJson(PendingJson);
        handler.EnqueueJson(ConfirmedJson);
        var client = CreateLoginClient(handler);

        var result = await client.WaitForLoginAsync("abc123");

        result.IsConnected.ShouldBeTrue();
        result.Status.ShouldBe(ZaloLoginStatus.Connected);
        result.BotToken.ShouldBe("1501641810090548646:LOGIN-TOKEN-ab12cd34");
        result.BotId.ShouldBe("1501641810090548646");
        result.OwnerId.ShouldBe("987654321");
        result.OaId.ShouldBe("1122334455");
        result.Message.ShouldBe("Login confirmed.");
        handler.RequestCount.ShouldBe(3);
        handler.RequestUrls[0].ShouldBe("http://test.local/agent/get-login-status?zbsk=abc123");
    }

    [Test]
    public async Task Should_ReturnExpired_When_StatusReturnsHttp498()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":false,"error_code":498,"description":"session expired"}""",
            (System.Net.HttpStatusCode)498);
        var client = CreateLoginClient(handler);

        var result = await client.WaitForLoginAsync("abc123");

        result.Status.ShouldBe(ZaloLoginStatus.Expired);
        result.IsConnected.ShouldBeFalse();
        result.BotToken.ShouldBeNull();
        result.Message.ShouldContain("498");
    }

    [Test]
    public async Task Should_ReturnExpired_When_StatusBodyCarriesCode498()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":true,"result":{"code":498}}""");
        var client = CreateLoginClient(handler);

        var result = await client.WaitForLoginAsync("abc123");

        result.Status.ShouldBe(ZaloLoginStatus.Expired);
    }

    [Test]
    public async Task Should_ReturnTimedOut_When_NoScanWithinTimeout()
    {
        var handler = new FakeHttpMessageHandler();
        for (var i = 0; i < 50; i++)
            handler.EnqueueJson(PendingJson);
        var client = CreateLoginClient(handler);

        var result = await client.WaitForLoginAsync("abc123");

        result.Status.ShouldBe(ZaloLoginStatus.TimedOut);
        result.IsConnected.ShouldBeFalse();
        result.BotToken.ShouldBeNull();
    }

    [Test]
    public async Task Should_ThrowOnCancel_When_WaitIsCancelled()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueDelayed(TimeSpan.FromSeconds(5), PendingJson);
        var client = CreateLoginClient(handler);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        await Should.ThrowAsync<OperationCanceledException>(() => client.WaitForLoginAsync("abc123", cancellationToken: cts.Token));
    }

    [Test]
    public async Task Should_RetryAndConnect_When_PollHitsNetworkErrorFirst()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueThrow(new HttpRequestException("connection reset"));
        handler.EnqueueJson(ConfirmedJson);
        var client = CreateLoginClient(handler);

        var result = await client.WaitForLoginAsync("abc123");

        result.IsConnected.ShouldBeTrue();
        handler.RequestCount.ShouldBe(2);
    }

    [Test]
    public async Task Should_NotLeakZbsk_When_StatusErrorDescriptionEchoesIt()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":false,"error_code":400,"description":"bad zbsk abc123"}""",
            System.Net.HttpStatusCode.BadRequest);
        var client = CreateLoginClient(handler);

        var exception = await Should.ThrowAsync<ApiRequestException>(() => client.WaitForLoginAsync("abc123"));

        exception.Message.ShouldNotContain("abc123");
    }

    [Test]
    public async Task Should_RequestFreshSession_When_LoginAsyncFirstSessionExpires()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(LoginJson);
        handler.EnqueueJson("""{"ok":false,"error_code":498,"description":"session expired"}""",
            (System.Net.HttpStatusCode)498);
        handler.EnqueueJson("""{"ok":true,"result":{"loginUrl":"https://bot.zaloplatforms.com/login?zbsk=def456","zbsk":"def456"}}""");
        handler.EnqueueJson(ConfirmedJson);
        var client = CreateLoginClient(handler);
        var seenUrls = new List<string>();

        var result = await client.LoginAsync(session =>
        {
            seenUrls.Add(session.LoginUrl);
            return Task.CompletedTask;
        });

        result.IsConnected.ShouldBeTrue();
        seenUrls.Count.ShouldBe(2);
        seenUrls[0].ShouldContain("abc123");
        seenUrls[1].ShouldContain("def456");
        handler.RequestCount.ShouldBe(4);
    }

    [Test]
    public async Task Should_StopAfterMaxAttempts_When_LoginAsyncKeepsExpiring()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(LoginJson);
        handler.EnqueueJson("""{"ok":false,"error_code":498,"description":"session expired"}""",
            (System.Net.HttpStatusCode)498);
        var client = CreateLoginClient(handler);
        var seenUrls = new List<string>();

        var result = await client.LoginAsync(session =>
        {
            seenUrls.Add(session.LoginUrl);
            return Task.CompletedTask;
        }, maxAttempts: 1);

        result.Status.ShouldBe(ZaloLoginStatus.Expired);
        seenUrls.Count.ShouldBe(1);
    }

    [Test]
    public void Should_RejectInvalidOptions_When_ValuesAreOutOfRange()
    {
        Should.Throw<ArgumentException>(() => new ZaloBotLoginOptions("not a url"));
        Should.Throw<ArgumentOutOfRangeException>(() => new ZaloBotLoginOptions { PollInterval = TimeSpan.Zero });
        Should.Throw<ArgumentOutOfRangeException>(() => new ZaloBotLoginOptions { SessionTimeout = TimeSpan.Zero });
        Should.Throw<ArgumentOutOfRangeException>(() => new ZaloBotLoginOptions { MaxLoginAttempts = 0 });
    }
}
