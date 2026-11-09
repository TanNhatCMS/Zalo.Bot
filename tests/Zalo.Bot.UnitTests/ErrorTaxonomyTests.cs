using System.Net;
using Shouldly;

namespace Zalo.Bot.UnitTests;

public class ErrorTaxonomyTests : ZaloBotClientTestBase
{
    [TestCase(400, ZaloErrorCategory.NonRetryable)]
    [TestCase(401, ZaloErrorCategory.Token)]
    [TestCase(403, ZaloErrorCategory.ServerRetry)]
    [TestCase(404, ZaloErrorCategory.NonRetryable)]
    [TestCase(408, ZaloErrorCategory.Retryable)]
    [TestCase(429, ZaloErrorCategory.Retryable)]
    public async Task Should_MapOfficialCodeToCategory_When_PlatformReturnsError(int errorCode, ZaloErrorCategory expectedCategory)
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson($$"""{"ok":false,"error_code":{{errorCode}},"description":"boom {{errorCode}}"}""");
        // single attempt — this test asserts classification, retry behavior is covered below
        var client = CreateClient(handler, options => options.RetryMaxAttempts = 1);

        var exception = await ThrowAsync<ApiRequestException>(() => client.GetMeAsync());

        exception.Category.ShouldBe(expectedCategory);
        exception.ErrorCode.ShouldBe(errorCode);
        exception.Message.ShouldContain($"boom {errorCode}");
    }

    [Test]
    public async Task Should_ThrowTokenException_When_ErrorCodeIs401()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":false,"error_code":401,"description":"token expired"}""");
        var client = CreateClient(handler);

        var exception = await ThrowAsync<ZaloBotTokenException>(() => client.GetMeAsync());

        exception.Category.ShouldBe(ZaloErrorCategory.Token);
        exception.Message.ShouldBe("token expired");
    }

    [Test]
    public async Task Should_ThrowUnknownCategory_When_OkFalseWithoutErrorCode()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":false,"description":"something went wrong"}""");
        var client = CreateClient(handler);

        var exception = await ThrowAsync<ApiRequestException>(() => client.GetMeAsync());

        exception.Category.ShouldBe(ZaloErrorCategory.Unknown);
        exception.ErrorCode.ShouldBe(0);
        exception.Message.ShouldBe("something went wrong");
    }

    [Test]
    public async Task Should_DefaultDescription_When_OkFalseWithoutDescription()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":false}""");
        var client = CreateClient(handler);

        var exception = await ThrowAsync<ApiRequestException>(() => client.GetMeAsync());

        exception.Category.ShouldBe(ZaloErrorCategory.Unknown);
        exception.Message.ShouldNotBeEmpty();
    }

    [Test]
    public async Task Should_SucceedOnThirdAttempt_When_429TwiceThenOk()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":false,"error_code":429,"description":"quota exceeded"}""");
        handler.EnqueueJson("""{"ok":false,"error_code":429,"description":"quota exceeded"}""");
        handler.EnqueueJson("""{"ok":true,"result":{"id":"1"}}""");
        var client = CreateClient(handler);

        var me = await client.GetMeAsync();

        me.Id.ShouldBe("1");
        handler.RequestCount.ShouldBe(3);
    }

    [Test]
    public async Task Should_ThrowRetryableExhausted_When_429OnEveryAttempt()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":false,"error_code":429,"description":"quota 1"}""");
        handler.EnqueueJson("""{"ok":false,"error_code":429,"description":"quota 2"}""");
        handler.EnqueueJson("""{"ok":false,"error_code":429,"description":"quota 3"}""");
        var client = CreateClient(handler);

        var exception = await ThrowAsync<ZaloBotRetryableException>(() => client.GetMeAsync());

        handler.RequestCount.ShouldBe(3); // RetryMaxAttempts total attempts
        exception.ErrorCode.ShouldBe(429);
        exception.Category.ShouldBe(ZaloErrorCategory.Retryable);
        exception.Message.ShouldBe("quota 3"); // last attempt's description
    }

    [Test]
    public async Task Should_RespectConfiguredMaxAttempts_When_429Always()
    {
        var handler = new FakeHttpMessageHandler();
        for (var i = 0; i < 5; i++)
        {
            handler.EnqueueJson("""{"ok":false,"error_code":429,"description":"quota"}""");
        }

        var client = CreateClient(handler, options => options.RetryMaxAttempts = 5);

        await ThrowAsync<ZaloBotRetryableException>(() => client.GetMeAsync());

        handler.RequestCount.ShouldBe(5);
    }

    [Test]
    public async Task Should_RetryExactlyOnce_When_403()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":false,"error_code":403,"description":"internal error 1"}""");
        handler.EnqueueJson("""{"ok":false,"error_code":403,"description":"internal error 2"}""");
        var client = CreateClient(handler);

        var exception = await ThrowAsync<ApiRequestException>(() => client.GetMeAsync());

        handler.RequestCount.ShouldBe(2); // initial attempt + exactly 1 retry
        exception.ErrorCode.ShouldBe(403);
        exception.Category.ShouldBe(ZaloErrorCategory.ServerRetry);
        exception.Message.ShouldBe("internal error 2");
    }

    [TestCase(400)]
    [TestCase(404)]
    public async Task Should_MakeSingleRequest_When_NonRetryableError(int errorCode)
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson($$"""{"ok":false,"error_code":{{errorCode}},"description":"nope"}""");
        var client = CreateClient(handler);

        await ThrowAsync<ApiRequestException>(() => client.GetMeAsync());

        handler.RequestCount.ShouldBe(1);
    }

    [Test]
    public async Task Should_MakeSingleRequest_When_401()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":false,"error_code":401,"description":"token expired"}""");
        var client = CreateClient(handler);

        await ThrowAsync<ZaloBotTokenException>(() => client.GetMeAsync());

        handler.RequestCount.ShouldBe(1);
    }

    [Test]
    public async Task Should_NoRetryServerCategory_When_MaxAttemptsIsOne()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":false,"error_code":403,"description":"internal"}""");
        var client = CreateClient(handler, options => options.RetryMaxAttempts = 1);

        await ThrowAsync<ApiRequestException>(() => client.GetMeAsync());

        handler.RequestCount.ShouldBe(1);
    }

    [Test]
    public async Task Should_ThrowNetworkException_When_HandlerThrowsHttpRequestException()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueThrow(new HttpRequestException("No connection could be made"));
        var client = CreateClient(handler);

        var exception = await ThrowAsync<ZaloBotNetworkException>(() => client.GetMeAsync());

        exception.Category.ShouldBe(ZaloErrorCategory.Network);
        handler.RequestCount.ShouldBe(1); // network errors are never auto-retried
        exception.Message.ShouldContain("No connection could be made");
    }

    [Test]
    public async Task Should_ThrowNetworkException_When_RequestTimesOut()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueDelayed(TimeSpan.FromSeconds(5), """{"ok":true,"result":{"id":"1"}}""");
        var client = CreateClient(handler, options => options.TimeoutSeconds = 1);

        var exception = await ThrowAsync<ZaloBotNetworkException>(() => client.GetMeAsync());

        exception.Category.ShouldBe(ZaloErrorCategory.Network);
        exception.Message.ShouldNotContain(AccessToken);
    }

    [Test]
    public async Task Should_ThrowInvalidJson_When_BodyIsNotJson()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("<html>502 Bad Gateway</html>");
        var client = CreateClient(handler);

        var exception = await ThrowAsync<InvalidJsonException>(() => client.GetMeAsync());

        exception.Category.ShouldBe(ZaloErrorCategory.Parse);
        exception.BodyExcerpt.ShouldNotBeNull();
    }

    [Test]
    public async Task Should_TruncateBodyExcerpt_When_BodyIsVeryLong()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(new string('x', 5000));
        var client = CreateClient(handler);

        var exception = await ThrowAsync<InvalidJsonException>(() => client.GetMeAsync());

        exception.BodyExcerpt!.Length.ShouldBeLessThanOrEqualTo(201); // 200 chars + ellipsis marker
        exception.Message.Length.ShouldBeLessThan(500);
    }

    [Test]
    public async Task Should_NotContainToken_When_NetworkErrorMessageContainsToken()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueThrow(new HttpRequestException($"connection refused for {AccessToken} at /bot/..."));
        var client = CreateClient(handler);

        var exception = await ThrowAsync<ZaloBotNetworkException>(() => client.GetMeAsync());

        exception.Message.ShouldNotContain(AccessToken);
        exception.Message.ShouldContain("***");
        exception.ToString().ShouldNotContain(AccessToken);
    }

    [Test]
    public async Task Should_NotContainTokenInExcerpt_When_InvalidJsonBodyContainsToken()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson($"not-json-at-all {AccessToken} trailing garbage {new string('y', 400)}");
        var client = CreateClient(handler);

        var exception = await ThrowAsync<InvalidJsonException>(() => client.GetMeAsync());

        exception.Message.ShouldNotContain(AccessToken);
        exception.BodyExcerpt.ShouldNotBeNull().ShouldNotContain(AccessToken);
        exception.ToString().ShouldNotContain(AccessToken);
    }

    [Test]
    public void Should_ComputeExponentialBackoff_When_RetryNumberIncreases()
    {
        var options = new ZaloBotClientOptions(AccessToken);

        options.GetRetryDelay(1).ShouldBe(TimeSpan.FromSeconds(2));
        options.GetRetryDelay(2).ShouldBe(TimeSpan.FromSeconds(4));
        options.GetRetryDelay(3).ShouldBe(TimeSpan.FromSeconds(8));
    }

    [Test]
    public void Should_UseConfiguredBackoff_When_OptionsCustomized()
    {
        var options = new ZaloBotClientOptions(AccessToken)
        {
            RetryBaseBackoffSeconds = 3,
            RetryBackoffMultiplier = 3.0,
        };

        options.GetRetryDelay(1).ShouldBe(TimeSpan.FromSeconds(3));
        options.GetRetryDelay(2).ShouldBe(TimeSpan.FromSeconds(9));
    }

    [Test]
    public async Task Should_NotContainTokenInAnyExceptionPath_When_AllMethodsFailWith401()
    {
        var methods = new List<Func<IZaloBotClient, Task>>
        {
            client => client.GetMeAsync(),
            client => client.GetUpdatesAsync(),
            client => client.SendMessageAsync("123", "hi"),
            client => client.SendPhotoAsync("123", "u"),
            client => client.SendStickerAsync("123", "s"),
            client => client.SendVoiceAsync("123", "v"),
            client => client.SendChatActionAsync("123", ChatAction.Typing),
            client => client.SetWebhookAsync("https://app/hook", "s3cret-t0ken"),
            client => client.TestWebhookAsync(),
            client => client.DeleteWebhookAsync(),
            client => client.GetWebhookInfoAsync(),
        };

        foreach (var method in methods)
        {
            var handler = new FakeHttpMessageHandler();
            handler.EnqueueJson($$"""{"ok":false,"error_code":401,"description":"deny {{AccessToken}}"}""");
            var client = CreateClient(handler);

            var exception = await ThrowAsync<ZaloBotTokenException>(() => method(client));

            exception.Message.ShouldNotContain(AccessToken);
            exception.ToString().ShouldNotContain(AccessToken);
        }
    }
}
