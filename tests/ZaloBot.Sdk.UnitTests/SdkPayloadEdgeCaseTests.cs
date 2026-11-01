using System.Net;
using System.Text;

namespace ZaloBot.Sdk.UnitTests;

/// <summary>Phản hồi lệch chuẩn từ Zalo: SDK phải ném lỗi có tên/kèm trích đoạn đã che token, hoặc ánh xạ lỏng thay vì sập.</summary>
public class SdkPayloadEdgeCaseTests : ZaloBotClientTestBase
{
    private static (ZaloBotClient Client, FakeHttpMessageHandler Handler) ClientWith(params string[] bodies)
    {
        var handler = new FakeHttpMessageHandler();
        foreach (var body in bodies)
        {
            handler.EnqueueJson(body);
        }

        return (CreateClient(handler), handler);
    }

    [Test]
    public async Task Response_ShouldBeRejected_WhenTheBodyIsNotAJsonObject()
    {
        var (client, _) = ClientWith($"[\"{AccessToken}\"]");

        var ex = await Should.ThrowAsync<InvalidJsonException>(() => client.GetMeAsync());

        ex.Message.ShouldContain("not a JSON object");
        (ex.BodyExcerpt ?? string.Empty).ShouldNotContain(AccessToken);
    }

    [Test]
    public async Task Response_ShouldBeRejected_WhenItIsNotValidJsonAtAll()
    {
        var (client, _) = ClientWith("<html>502 Bad Gateway</html>");

        await Should.ThrowAsync<InvalidJsonException>(() => client.GetMeAsync());
    }

    [Test]
    public async Task Response_ShouldBeRejected_WhenItHasNoWrapperFields()
    {
        var (client, _) = ClientWith("""{"something":"else"}""");

        var ex = await Should.ThrowAsync<InvalidJsonException>(() => client.GetMeAsync());

        ex.Message.ShouldContain("getMe");
        ex.Message.ShouldContain("not a valid API wrapper");
    }

    [Test]
    public async Task Results_ShouldFallBackToEmptyObjects_WhenThePayloadIsNotAnObject()
    {
        var (client, _) = ClientWith("""{"ok":true,"result":5}""", """{"ok":true,"result":"x"}""");

        var me = await client.GetMeAsync();
        var message = await client.SendMessageAsync(1, "xin chào");

        me.Id.ShouldBe(string.Empty);
        message.MessageId.ShouldBeNull();
    }

    [Test]
    public async Task Bool_ShouldBeReadLeniently_FromStringsNumbersAndOtherShapes()
    {
        var (client, _) = ClientWith(
            """{"ok":true,"result":"true"}""",
            """{"ok":true,"result":"nope"}""",
            """{"ok":true,"result":1}""",
            """{"ok":true,"result":0}""",
            """{"ok":true,"result":{"a":1}}""",
            """{"ok":true}""");

        (await client.TestWebhookAsync()).ShouldBeTrue();
        (await client.TestWebhookAsync()).ShouldBeFalse();
        (await client.TestWebhookAsync()).ShouldBeTrue();
        (await client.TestWebhookAsync()).ShouldBeFalse();
        (await client.TestWebhookAsync()).ShouldBeFalse();
        (await client.TestWebhookAsync()).ShouldBeTrue("ok=true không có result nghĩa là thành công");
    }

    [Test]
    public async Task Fields_ShouldBeReadLeniently_FromStringsAndNumbers()
    {
        var (client, _) = ClientWith(
            """{"ok":true,"result":{"id":12345,"account_name":"a","can_join_groups":"true"}}""",
            """{"ok":true,"result":{"id":"9","can_join_groups":"không rõ"}}""");

        var numericId = await client.GetMeAsync();
        var badFlag = await client.GetMeAsync();

        numericId.Id.ShouldBe("12345");
        numericId.CanJoinGroups.ShouldBe(true);
        badFlag.CanJoinGroups.ShouldBeNull();
    }

    [Test]
    public async Task WebhookInfo_ShouldReadStringEncodedNumbers()
    {
        var (client, _) = ClientWith("""{"ok":true,"result":{"pending_update_count":"7","last_error_date":"1700000000"}}""");

        var info = await client.GetWebhookInfoAsync();

        info!.PendingUpdateCount.ShouldBe(7);
        info.LastErrorDate.ShouldBe(new DateTime(2023, 11, 14, 22, 13, 20, DateTimeKind.Utc));
    }

    [Test]
    public async Task ErrorResponse_ShouldBeRaised_WithoutLeakingTheToken()
    {
        var (client, _) = ClientWith($$"""{"ok":false,"error_code":400,"description":"invalid token {{AccessToken}}"}""");

        var ex = await Should.ThrowAsync<Exception>(() => client.GetMeAsync());

        ex.Message.ShouldNotContain(AccessToken);
    }

    private sealed class FailingContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => throw new HttpRequestException($"đứt kết nối khi đọc {AccessToken}");

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
            => throw new HttpRequestException($"đứt kết nối khi đọc {AccessToken}");
    }

    [Test]
    public async Task AResponseThatBreaksMidTransfer_ShouldBeReportedAsANetworkError_WithoutLeakingTheToken()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new FailingContent() });
        var client = CreateClient(handler, o => o.RetryMaxAttempts = 1);

        var ex = await Should.ThrowAsync<ZaloBotNetworkException>(() => client.GetMeAsync());

        ex.Message.ShouldNotContain(AccessToken);
        ex.Message.ShouldContain("request failed");
    }

    private sealed class SlowContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => Task.Delay(TimeSpan.FromSeconds(30));

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
            => Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }

    [Test]
    public async Task AStalledResponse_ShouldTimeOut()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new SlowContent() });
        var client = CreateClient(handler, o =>
        {
            o.RetryMaxAttempts = 1;
            o.TimeoutSeconds = 1;
        });

        var ex = await Should.ThrowAsync<ZaloBotNetworkException>(() => client.GetMeAsync());

        ex.Message.ShouldContain("timed out");
    }
}
