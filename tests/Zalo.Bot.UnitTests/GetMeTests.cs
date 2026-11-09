using Shouldly;

namespace Zalo.Bot.UnitTests;

public class GetMeTests : ZaloBotClientTestBase
{
    private const string StandardGetMeResponse =
        """{"ok":true,"result":{"id":"1501641810090548646","account_name":"bot.OodbOvGw","account_type":"BASIC","can_join_groups":true,"display_name":"Bot PNH Note"}}""";

    [Test]
    public async Task Should_MapAllFiveFields_When_GetMeReturnsStandardJson()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(StandardGetMeResponse);
        var client = CreateClient(handler);

        var me = await client.GetMeAsync();

        me.Id.ShouldBe("1501641810090548646");
        me.AccountName.ShouldBe("bot.OodbOvGw");
        me.AccountType.ShouldBe("BASIC");
        me.CanJoinGroups.ShouldBe(true);
        me.DisplayName.ShouldBe("Bot PNH Note");
    }

    [Test]
    public async Task Should_DefaultMissingFields_When_GetMeReturnsPartialJson()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":true,"result":{"id":"42"}}""");
        var client = CreateClient(handler);

        var me = await client.GetMeAsync();

        me.Id.ShouldBe("42");
        me.AccountName.ShouldBeNull();
        me.AccountType.ShouldBeNull();
        me.CanJoinGroups.ShouldBeNull();
        me.DisplayName.ShouldBeNull();
    }

    [Test]
    public async Task Should_SendGetToBotPath_When_GetMeCalled()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(StandardGetMeResponse);
        var client = CreateClient(handler);

        await client.GetMeAsync();

        handler.RequestCount.ShouldBe(1);
        handler.RequestUrls[0].ShouldBe($"{BotUrl}/getMe");
        handler.Requests[0].Method.ShouldBe(HttpMethod.Get); // docs/call-api: GET cho API đọc
        handler.RequestBodies[0].ShouldBe(string.Empty); // parameterless request has no body
    }

    [Test]
    public async Task Should_ThrowTokenException_When_PlatformReturns401()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":false,"error_code":401,"description":"Access token invalid or expired"}""");
        var client = CreateClient(handler);

        var exception = await ThrowAsync<ZaloBotTokenException>(() => client.GetMeAsync());

        exception.ErrorCode.ShouldBe(401);
        exception.Category.ShouldBe(ZaloErrorCategory.Token);
        exception.Message.ShouldContain("Access token invalid or expired");
    }

    [Test]
    public async Task Should_NotContainToken_When_TokenExceptionThrown()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson($$"""{"ok":false,"error_code":401,"description":"Invalid token for {{AccessToken}}"}""");
        var client = CreateClient(handler);

        var exception = await ThrowAsync<ZaloBotTokenException>(() => client.GetMeAsync());

        exception.Message.ShouldNotContain(AccessToken);
        exception.ToString().ShouldNotContain(AccessToken);
    }
}
