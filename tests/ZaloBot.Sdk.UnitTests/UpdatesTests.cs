using System.Text.Json;
using Shouldly;

namespace ZaloBot.Sdk.UnitTests;

public class UpdatesTests : ZaloBotClientTestBase
{
    [Test]
    public async Task Should_ParseUpdatesArray_When_GetUpdatesReturnsStandardUpdates()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(
            """
            {"ok":true,"result":[
              {"update_id":1,"message":{"message_id":"m1","date":1749632637199,"chat":{"id":"5128471675483426707"},"from":{"id":"5128471675483426707","display_name":"Kiet"},"text":"hello bot"}},
              {"update_id":2,"message":{"message_id":"m2","chat":{"id":"g1"},"from":{"id":"u2","display_name":"Ana"},"photo":{"url":"https://cdn.example.com/p.jpg"}}},
              {"update_id":3,"message":{"message_id":"m3","chat":{"id":"g1"},"from":{"id":"u3"},"voice_url":"https://cdn.example.com/v.ogg"}},
              {"update_id":4,"message":{"message_id":"m4","chat":{"id":"g1"},"from":{"id":"u4"},"sticker":"https://cdn.example.com/s.png"}}
            ]}
            """);
        var client = CreateClient(handler);

        var updates = await client.GetUpdatesAsync(offset: 0);

        updates.Length.ShouldBe(4);

        updates[0].UpdateId.ShouldBe(1);
        updates[0].ContentType.ShouldBe(ContentType.Text);
        updates[0].Text.ShouldBe("hello bot");
        updates[0].ChatId.ShouldBe("5128471675483426707");
        updates[0].FromUserId.ShouldBe("5128471675483426707");
        updates[0].FromDisplayName.ShouldBe("Kiet");
        updates[0].Message!.MessageId.ShouldBe("m1");
        updates[0].Message!.Date.ShouldBe(1749632637199);

        updates[1].ContentType.ShouldBe(ContentType.Photo);
        updates[1].MediaUrl.ShouldBe("https://cdn.example.com/p.jpg");

        updates[2].ContentType.ShouldBe(ContentType.Voice);
        updates[2].MediaUrl.ShouldBe("https://cdn.example.com/v.ogg");

        updates[3].ContentType.ShouldBe(ContentType.Sticker);
        updates[3].MediaUrl.ShouldBe("https://cdn.example.com/s.png");
    }

    [Test]
    public async Task Should_ReturnEmptyArray_When_GetUpdatesReturnsNonArray()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":true,"result":null}""");
        var client = CreateClient(handler);

        var updates = await client.GetUpdatesAsync();

        updates.ShouldBeEmpty();
    }

    [Test]
    public async Task Should_IncludeOffsetAndLimitInBody_When_GetUpdatesWithOffset()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":true,"result":[]}""");
        var client = CreateClient(handler);

        await client.GetUpdatesAsync(offset: 42, timeoutSeconds: 0, limit: 100);

        var body = handler.RequestBodies[0];
        body.ShouldContain("\"offset\":42");
        body.ShouldContain("\"limit\":100");
        body.ShouldNotContain("timeout");
    }

    [Test]
    public async Task Should_IncludeTimeoutInBody_When_LongPolling()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":true,"result":[]}""");
        var client = CreateClient(handler);

        await client.GetUpdatesAsync(offset: 5, timeoutSeconds: 25, limit: 50);

        var body = handler.RequestBodies[0];
        body.ShouldContain("\"offset\":5");
        body.ShouldContain("\"timeout\":25");
        body.ShouldContain("\"limit\":50");
    }

    [Test]
    public void Should_MapUnknownContentType_When_WebhookPayloadHasUnknownMedia()
    {
        var update = ParseWebhook("""{"update_id":9,"message":{"message_id":"m9","from":{"id":"u9"},"fancy_new_media":{"url":"https://x"}}}""");

        update.UpdateId.ShouldBe(9);
        update.ContentType.ShouldBe(ContentType.Unknown);
        update.MediaUrl.ShouldBeNull();
    }

    [Test]
    public void Should_UnwrapResultAndParseEventName_When_WebhookPayloadFollowsOfficialDocs()
    {
        // Docs bot.zapps.me/docs/webhook: payload = {"ok":true,"result":{...,"event_name":"..."}}
        var update = ParseWebhook("""
            {"ok":true,"result":{"event_name":"message.text.received","message":{"from":{"id":"6ede9afa","display_name":"Ted","is_bot":false},"chat":{"id":"6ede9afa","chat_type":"PRIVATE"},"text":"Xin chào","message_id":"2d758cb5","date":1750316131602}}}
            """);

        update.EventName.ShouldBe("message.text.received");
        update.ContentType.ShouldBe(ContentType.Text);
        update.Text.ShouldBe("Xin chào");
        update.ChatId.ShouldBe("6ede9afa");
        update.FromUserId.ShouldBe("6ede9afa");
        update.FromDisplayName.ShouldBe("Ted");
        update.Message!.MessageId.ShouldBe("2d758cb5");
    }

    [Test]
    public void Should_ParseEventNameOnly_When_WebhookSendsUnsupportedEvent()
    {
        // message.unsupported.received: có event_name, không có message object
        var update = ParseWebhook("""{"ok":true,"result":{"event_name":"message.unsupported.received"}}""");

        update.EventName.ShouldBe("message.unsupported.received");
        update.ContentType.ShouldBe(ContentType.Unknown);
    }

    [Test]
    public void Should_ParseVoiceUrlAndStickerUrl_When_WebhookSendsDocsFieldNames()
    {
        // Docs: voice = voice_url; sticker = url (hoặc sticker là string URL)
        var voice = ParseWebhook("""{"ok":true,"result":{"event_name":"message.voice.received","message":{"message_id":"v1","chat":{"id":"c"},"from":{"id":"u"},"voice_url":"https://s1.zalo.me/a.mp3"}}}""");
        voice.ContentType.ShouldBe(ContentType.Voice);
        voice.MediaUrl.ShouldBe("https://s1.zalo.me/a.mp3");

        var sticker = ParseWebhook("""{"ok":true,"result":{"event_name":"message.sticker.received","message":{"message_id":"s1","chat":{"id":"c"},"from":{"id":"u"},"sticker":"cat-sticker-id","url":"https://stickers.zaloapp.com/cat.png"}}}""");
        sticker.ContentType.ShouldBe(ContentType.Sticker);
        sticker.MediaUrl.ShouldBe("https://stickers.zaloapp.com/cat.png");
    }

    [Test]
    public void Should_MapUnknownContentType_When_WebhookPayloadMissingMessage()
    {
        var update = ParseWebhook("""{"update_id":10,"callback_query":{"id":"x"}}""");

        update.UpdateId.ShouldBe(10);
        update.ContentType.ShouldBe(ContentType.Unknown);
        update.ChatId.ShouldBeNull();
        update.FromUserId.ShouldBeNull();
    }

    [Test]
    public void Should_MapUnknownUpdate_When_WebhookPayloadIsEmptyArray()
    {
        var update = ParseWebhook("[]");

        update.UpdateId.ShouldBe(0);
        update.ContentType.ShouldBe(ContentType.Unknown);
    }

    [Test]
    public void Should_MapUnknownUpdate_When_WebhookPayloadIsEmptyObject()
    {
        var update = ParseWebhook("{}");

        update.UpdateId.ShouldBe(0);
        update.ContentType.ShouldBe(ContentType.Unknown);
    }

    [Test]
    public void Should_MapEditedMessage_When_WebhookPayloadUsesEditedMessage()
    {
        var update = ParseWebhook("""{"update_id":11,"edited_message":{"message_id":"m11","chat":{"id":"c1"},"from":{"id":"u1","username":"@kiet"},"text":"edited"}}""");

        update.UpdateId.ShouldBe(11);
        update.Text.ShouldBe("edited");
        update.ContentType.ShouldBe(ContentType.Text);
        update.ChatId.ShouldBe("c1");
        update.FromDisplayName.ShouldBe("@kiet");
    }

    [Test]
    public void Should_MapChatActionUpdate_When_WebhookPayloadCarriesAction()
    {
        var update = ParseWebhook("""{"update_id":12,"message":{"message_id":"m12","chat":{"id":"c1"},"from":{"id":"u1"},"action":"typing"}}""");

        update.ContentType.ShouldBe(ContentType.ChatAction);
    }

    [Test]
    public void Should_NeverThrow_When_WebhookPayloadIsScalarOrNull()
    {
        ZaloBotClient.ParseWebhookUpdate(JsonDocument.Parse("null").RootElement).ContentType.ShouldBe(ContentType.Unknown);
        ZaloBotClient.ParseWebhookUpdate(JsonDocument.Parse("\"weird\"").RootElement).ContentType.ShouldBe(ContentType.Unknown);
        ZaloBotClient.ParseWebhookUpdate(JsonDocument.Parse("123").RootElement).ContentType.ShouldBe(ContentType.Unknown);
    }

    [Test]
    public void Should_ParseStringNumericFields_WhenPlatformReturnsStringNumbers()
    {
        // Zalo API có thể trả số dưới dạng string — SDK phải parse cả hai
        var update = ParseWebhook("""{"update_id":"42","message":{"message_id":"m42","date":"1749632637199","chat":{"id":"c1"},"from":{"id":"u1"},"text":"hi"}}""");

        update.UpdateId.ShouldBe(42);
        update.Message!.MessageId.ShouldBe("m42");
        update.Message!.Date.ShouldBe(1749632637199);
    }

    [Test]
    public void Should_ParseStringBoolFields_WhenPlatformReturnsStringBooleans()
    {
        // can_join_groups dạng string "true"/"false" → vẫn parse được
        var update = ParseWebhook("""{"update_id":7,"message":{"message_id":"m7","chat":{"id":"c1"},"from":{"id":"u1","display_name":"Bot","can_join_groups":"true"},"text":"x"}}""");

        update.FromDisplayName.ShouldBe("Bot");
    }

    [Test]
    public void Should_ReturnNullForUnparsableStringNumbers()
    {
        // String không parse được → default(long) (không throw)
        var update = ParseWebhook("""{"update_id":"not-a-number","message":{"message_id":"m1","chat":{"id":"c"},"from":{"id":"u"},"text":"x"}}""");

        update.UpdateId.ShouldBe(0);
    }

    [Test]
    public async Task Should_UseUpdateIdPlusOneOffset_When_OffsetFollowsLastUpdate()
    {
        // Spec US3-AC3: with offset = id of the last update, the next call carries offset = id + 1 (caller's responsibility, asserted here as pass-through)
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":true,"result":[{"update_id":41,"message":{"message_id":"m41","chat":{"id":"c"},"from":{"id":"u"},"text":"x"}}]}""");
        handler.EnqueueJson("""{"ok":true,"result":[]}""");
        var client = CreateClient(handler);

        var updates = await client.GetUpdatesAsync();
        var lastId = updates[^1].UpdateId;
        await client.GetUpdatesAsync(offset: lastId + 1);

        handler.RequestBodies[1].ShouldContain("\"offset\":42");
    }

    private static Update ParseWebhook(string json)
        => ZaloBotClient.ParseWebhookUpdate(JsonDocument.Parse(json).RootElement);
}
