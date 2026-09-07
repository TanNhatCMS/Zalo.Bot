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
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(MessageResult);
        var client = CreateClient(handler);

        await client.SendVoiceAsync("123", "https://example.com/voice.ogg");

        handler.RequestUrls[0].ShouldBe($"{BotUrl}/sendVoice");
        handler.RequestBodies[0].ShouldContain("\"voice\":\"https://example.com/voice.ogg\"");
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
    public async Task Should_ReturnTrue_When_SetWebhookOkWithoutResult()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":true}""");
        var client = CreateClient(handler);

        var result = await client.SetWebhookAsync("https://app.example.com/hook", "s3cret");

        result.ShouldBeTrue();
        var body = handler.RequestBodies[0];
        body.ShouldContain("\"url\":\"https://app.example.com/hook\"");
        body.ShouldContain("\"secret_token\":\"s3cret\"");
    }

    [Test]
    public async Task Should_OmitSecretToken_When_SetWebhookWithoutSecret()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":true,"result":true}""");
        var client = CreateClient(handler);

        await client.SetWebhookAsync("https://app.example.com/hook");

        handler.RequestBodies[0].ShouldNotContain("secret_token");
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
