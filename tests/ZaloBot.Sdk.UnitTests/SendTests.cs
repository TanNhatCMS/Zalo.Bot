using System.Text.RegularExpressions;
using Shouldly;

namespace ZaloBot.Sdk.UnitTests;

public class SendTests : ZaloBotClientTestBase
{
    private const string MessageResult =
        """{"ok":true,"result":{"message_id":"82599fa32f56d00e8941","date":1749632637199}}""";

    [Test]
    public async Task Should_SendChatIdAndText_When_SendMessageCalled()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(MessageResult);
        var client = CreateClient(handler);

        var message = await client.SendMessageAsync("5128471675483426707", "Hello bot");

        message.MessageId.ShouldBe("82599fa32f56d00e8941");
        message.Date.ShouldBe(1749632637199);
        message.RawJson.ShouldNotBeNull();
        message.RawJson.ShouldContain("82599fa32f56d00e8941");
        handler.RequestUrls[0].ShouldBe($"{BotUrl}/sendMessage");
        handler.Requests[0].Content!.Headers.ContentType!.MediaType.ShouldBe("application/json");
        var body = handler.RequestBodies[0];
        body.ShouldContain("\"chat_id\":\"5128471675483426707\"");
        body.ShouldContain("\"text\":\"Hello bot\"");
        body.ShouldNotContain("parse_mode");
    }

    [Test]
    public async Task Should_IncludeParseMode_When_SendMessageWithMarkdown()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(MessageResult);
        var client = CreateClient(handler);

        await client.SendMessageAsync("123", "**hi**", parseMode: "markdown");

        handler.RequestBodies[0].ShouldContain("\"parse_mode\":\"markdown\"");
    }

    [Test]
    public async Task Should_IncludeTextStyles_When_SendMessageWithStyleRuns()
    {
        // Docs bot.zapps.me/docs/apis/sendMessage: text_styles = [{start, len, st[]}], offset theo UTF-16 code units
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(MessageResult);
        var client = CreateClient(handler);

        await client.SendMessageAsync("123", "Xin chào bạn", textStyles:
        [
            TextStyle.Of(0, 7, TextStyleCode.Bold, TextStyleCode.ColorRed),
            TextStyle.Of(9, 3, TextStyleCode.Italic),
        ]);

        var body = handler.RequestBodies[0];
        body.ShouldContain("\"text_styles\":");
        body.ShouldContain("{\"start\":0,\"len\":7,\"st\":[\"b\",\"c_db342e\"]}");
        body.ShouldContain("{\"start\":9,\"len\":3,\"st\":[\"i\"]}");
    }

    [Test]
    public async Task Should_OmitTextStyles_When_SendMessageWithoutStyleRuns()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(MessageResult);
        var client = CreateClient(handler);

        await client.SendMessageAsync("123", "plain");

        handler.RequestBodies[0].ShouldNotContain("text_styles");
    }

    [Test]
    public void Should_ThrowLocally_When_TextIsLongerThan2000Characters()
    {
        var handler = new FakeHttpMessageHandler();
        var client = CreateClient(handler);

        var exception = Should.Throw<ArgumentException>(() => client.SendMessageAsync("123", new string('a', 2001)));

        exception.ParamName.ShouldBe("text");
        handler.RequestCount.ShouldBe(0);
    }

    [Test]
    public void Should_ThrowLocally_When_TextIsEmpty()
    {
        var handler = new FakeHttpMessageHandler();
        var client = CreateClient(handler);

        Should.Throw<ArgumentException>(() => client.SendMessageAsync("123", string.Empty));

        handler.RequestCount.ShouldBe(0);
    }

    [Test]
    public async Task Should_SendPhotoUrl_When_SendPhotoCalled()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(MessageResult);
        var client = CreateClient(handler);

        await client.SendPhotoAsync("123", "https://example.com/photo.jpg");

        handler.RequestUrls[0].ShouldBe($"{BotUrl}/sendPhoto");
        handler.RequestBodies[0].ShouldContain("\"photo\":\"https://example.com/photo.jpg\"");
    }

    [Test]
    public async Task Should_SendStickerId_When_SendStickerCalled()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(MessageResult);
        var client = CreateClient(handler);

        await client.SendStickerAsync("123", "sticker-id-1");

        handler.RequestUrls[0].ShouldBe($"{BotUrl}/sendSticker");
        handler.RequestBodies[0].ShouldContain("\"sticker\":\"sticker-id-1\"");
    }

    [Test]
    public async Task Should_SendVoiceUrl_When_SendVoiceCalled()
    {
        // Docs apis/sendVoice: field là voice_url (không phải voice) — bắt buộc .aac, chỉ chat 1-1
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(MessageResult);
        var client = CreateClient(handler);

        await client.SendVoiceAsync("123", "https://example.com/voice.aac");

        handler.RequestUrls[0].ShouldBe($"{BotUrl}/sendVoice");
        handler.RequestBodies[0].ShouldContain("\"voice_url\":\"https://example.com/voice.aac\"");
    }

    [Test]
    public async Task Should_SendPhotoWithCaption_When_CaptionGiven()
    {
        // Docs apis/sendPhoto: caption 1–2000 ký tự, tùy chọn
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(MessageResult);
        var client = CreateClient(handler);

        await client.SendPhotoAsync("123", "https://example.com/photo.jpg", caption: "Anh minh hoa");

        handler.RequestBodies[0].ShouldContain("\"photo\":\"https://example.com/photo.jpg\"");
        handler.RequestBodies[0].ShouldContain("\"caption\":\"Anh minh hoa\"");
    }

    [Test]
    public async Task Should_OmitCaption_When_SendPhotoWithoutCaption()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(MessageResult);
        var client = CreateClient(handler);

        await client.SendPhotoAsync("123", "https://example.com/photo.jpg");

        handler.RequestBodies[0].ShouldNotContain("caption");
    }

    [Test]
    public async Task Should_ThrowBeforeSending_When_SecretTokenOutsideDocumentedRange()
    {
        // Docs apis/setWebhook: secret_token 8–256 ký tự — validate tại client, không gửi request
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(MessageResult);
        var client = CreateClient(handler);

        Should.Throw<ArgumentException>(() => client.SetWebhookAsync("https://app.example.com/hook", "short"));
        Should.Throw<ArgumentException>(() => client.SetWebhookAsync("https://app.example.com/hook", new string('x', 257)));
        Should.Throw<ArgumentException>(() => client.SetWebhookAsync("https://app.example.com/hook"));

        handler.RequestCount.ShouldBe(0); // không request nào được gửi
    }

    [Test]
    public async Task Should_ReturnTrue_When_SendChatActionTyping()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":true,"result":true}""");
        var client = CreateClient(handler);

        var result = await client.SendChatActionAsync("123", ChatAction.Typing);

        result.ShouldBeTrue();
        handler.RequestUrls[0].ShouldBe($"{BotUrl}/sendChatAction");
        handler.RequestBodies[0].ShouldContain("\"action\":\"typing\"");
    }

    [Test]
    public async Task Should_ParseVerification_When_SetWebhookSucceeds()
    {
        // Sample docs bot.zapps.me: setWebhook trả result kèm verification block
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""
            {"ok":true,"result":{"url":"https://zalo.tannhatcms.io.vn/api/webhook/zalo/1","updated_at":1789021705096,"verification":{"ok":true,"url":"https://zalo.tannhatcms.io.vn/api/webhook/zalo/1","outcome":"webhook.ok","hint":"Your endpoint responded successfully."}},"error_code":0}
            """);
        var client = CreateClient(handler);

        var result = await client.SetWebhookAsync("https://zalo.tannhatcms.io.vn/api/webhook/zalo/1", "s3cret-t0ken");

        result.Url.ShouldBe("https://zalo.tannhatcms.io.vn/api/webhook/zalo/1");
        result.UpdatedAt.ShouldBe(1789021705096);
        result.Verification!.Ok.ShouldBe(true);
        result.Verification.Outcome.ShouldBe("webhook.ok");
        var body = handler.RequestBodies[0];
        body.ShouldContain("\"url\":\"https://zalo.tannhatcms.io.vn/api/webhook/zalo/1\"");
        body.ShouldContain("\"secret_token\":\"s3cret-t0ken\"");
    }

    [Test]
    public async Task Should_ParseVerificationFailure_When_EndpointRejectedByWaf()
    {
        // Endpoint bị chặn (Cloudflare 403): API call vẫn ok=true nhưng verification.ok=false
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""
            {"ok":true,"result":{"url":"https://zalo.tannhatcms.io.vn/api/webhook/zalo/1","updated_at":1789021753697,"verification":{"ok":false,"url":"https://zalo.tannhatcms.io.vn/api/webhook/zalo/1","outcome":"webhook.http.403","hint":"Your server or CDN rejected the request with 403. Check WAF / Cloudflare rules, any IP allowlist, and that the User-Agent \"Java/<version>\" is permitted."}},"error_code":0}
            """);
        var client = CreateClient(handler);

        var result = await client.SetWebhookAsync("https://zalo.tannhatcms.io.vn/api/webhook/zalo/1", "s3cret-t0ken");

        result.Verification!.Ok.ShouldBe(false);
        result.Verification.Outcome.ShouldBe("webhook.http.403");
        result.Verification.Hint.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Should_TolerateMissingVerification_When_SetWebhookOkWithoutResult()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":true}""");
        var client = CreateClient(handler);

        var result = await client.SetWebhookAsync("https://app.example.com/hook", "s3cret-t0ken");

        result.Verification.ShouldBeNull();
    }

    [Test]
    public async Task Should_SendSecretToken_When_SetWebhookWithSecret()
    {
        // Docs apis/setWebhook: secret_token BẮT BUỘC 8–256 ký tự — client validate trước khi gửi
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":true,"result":true}""");
        var client = CreateClient(handler);

        await client.SetWebhookAsync("https://app.example.com/hook", "s3cret-t0ken");

        handler.RequestBodies[0].ShouldContain("\"secret_token\":\"s3cret-t0ken\"");
    }

    [Test]
    public async Task Should_NotContainToken_When_SendFailsWith400()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson($$"""{"ok":false,"error_code":400,"description":"chat not found for {{AccessToken}}"}""");
        var client = CreateClient(handler);

        var exception = await ThrowAsync<ApiRequestException>(() => client.SendMessageAsync("123", "hello"));

        exception.ErrorCode.ShouldBe(400);
        exception.Category.ShouldBe(ZaloErrorCategory.NonRetryable);
        exception.Message.ShouldNotContain(AccessToken);
    }

    [Test]
    public async Task Should_SendChatIdAsString_When_ChatIdIsNumeric()
    {
        // Zalo chat_id is a string on the wire even for numeric ids
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(MessageResult);
        var client = CreateClient(handler);

        await client.SendMessageAsync("5128471675483426707", "hi");

        Regex.IsMatch(handler.RequestBodies[0], "\"chat_id\":\"5128471675483426707\"").ShouldBeTrue();
    }
}
