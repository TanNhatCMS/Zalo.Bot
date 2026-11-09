using System.Text.Json;
using Shouldly;

namespace Zalo.Bot.UnitTests;

/// <summary>Khoá SDK theo đúng JSON mẫu của tài liệu chính thức cập nhật 22/10/2026:
/// docs/apis/getUpdates.md (result là MỘT object sự kiện) và docs/webhook.md (body không bọc ok/result,
/// có message_type, photo_url, chat_type, is_bot).</summary>
public class DocsSamples2026Tests : ZaloBotClientTestBase
{
    private const string TextEventBody = """
        {
          "event_name": "message.text.received",
          "message": {
            "message_id": "2d758cb5e222177a4e35",
            "date": 1750316131602,
            "message_type": "CHAT_TEXT",
            "from": { "id": "6ede9afa66b88fe6d6a9", "display_name": "Ted", "is_bot": false },
            "chat": { "id": "6ede9afa66b88fe6d6a9", "chat_type": "PRIVATE" },
            "text": "Xin chào"
          }
        }
        """;

    private const string ImageEventBody = """
        {
          "event_name": "message.image.received",
          "message": {
            "message_id": "2d758cb5e222177a4e36",
            "date": 1750316135240,
            "message_type": "CHAT_PHOTO",
            "from": { "id": "6ede9afa66b88fe6d6a9", "display_name": "Ted", "is_bot": false },
            "chat": { "id": "6ede9afa66b88fe6d6a9", "chat_type": "PRIVATE" },
            "photo_url": "https://example.com/photo.jpg",
            "caption": "Ảnh sản phẩm"
          }
        }
        """;

    [Test]
    public async Task ShouldReturnOneUpdate_WhenGetUpdatesResultIsSingleEventObject()
    {
        // Arrange — mẫu "Sample response" của docs/apis/getUpdates (22/10/2026)
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":true,"result":""" + TextEventBody + "}");
        var client = CreateClient(handler);

        // Act
        var updates = await client.GetUpdatesAsync(timeoutSeconds: 30);

        // Assert
        updates.Length.ShouldBe(1);
        var update = updates[0];
        update.EventName.ShouldBe("message.text.received");
        update.ContentType.ShouldBe(ContentType.Text);
        update.Text.ShouldBe("Xin chào");
        update.ChatId.ShouldBe("6ede9afa66b88fe6d6a9");
        update.Message!.MessageId.ShouldBe("2d758cb5e222177a4e35");
        update.Message.Date.ShouldBe(1750316131602);
    }

    [Test]
    public async Task ShouldReturnEmpty_WhenGetUpdatesResultIsEmptyObject()
    {
        // Arrange — long-poll hết hạn không có sự kiện: object rỗng không được thành một update ma
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":true,"result":{}}""");
        var client = CreateClient(handler);

        // Act
        var updates = await client.GetUpdatesAsync();

        // Assert
        updates.ShouldBeEmpty();
    }

    [Test]
    public void ShouldExposeMessageTypeChatTypeAndIsBot_WhenWebhookBodyFollowsNewDocs()
    {
        // Act — body webhook mới: không bọc ok/result
        var update = Parse(TextEventBody);

        // Assert
        update.MessageType.ShouldBe("CHAT_TEXT");
        update.ChatType.ShouldBe("PRIVATE");
        update.FromIsBot.ShouldBe(false);
        update.FromDisplayName.ShouldBe("Ted");
    }

    [Test]
    public void ShouldParsePhotoUrlAndCaption_WhenWebhookSendsImageEvent()
    {
        // Act
        var update = Parse(ImageEventBody);

        // Assert
        update.EventName.ShouldBe("message.image.received");
        update.MessageType.ShouldBe("CHAT_PHOTO");
        update.ContentType.ShouldBe(ContentType.Photo);
        update.MediaUrl.ShouldBe("https://example.com/photo.jpg");
        update.Text.ShouldBe("Ảnh sản phẩm");
    }

    [Test]
    public void ShouldParsePhotoUrl_WhenImageHasNoCaptionAndNoMessageType()
    {
        // Act
        var update = Parse("""{"event_name":"message.image.received","message":{"message_id":"p1","chat":{"id":"c"},"from":{"id":"u"},"photo_url":"https://example.com/a.jpg"}}""");

        // Assert
        update.ContentType.ShouldBe(ContentType.Photo);
        update.MediaUrl.ShouldBe("https://example.com/a.jpg");
        update.Text.ShouldBeNull();
    }

    [TestCase("CHAT_STICKER", ContentType.Sticker)]
    [TestCase("CHAT_VOICE", ContentType.Voice)]
    public void ShouldClassifyByMessageType_WhenMessageTypePresent(string messageType, ContentType expected)
    {
        // Act
        var update = Parse("""{"message":{"message_id":"x","message_type":"__TYPE__","chat":{"id":"c"},"from":{"id":"u"},"sticker":"cat","url":"https://s/cat.png","voice_url":"https://s/v.mp3"}}"""
            .Replace("__TYPE__", messageType, StringComparison.Ordinal));

        // Assert
        update.ContentType.ShouldBe(expected);
    }

    [Test]
    public void ShouldExposeGroupChatType_WhenChatIsGroup()
    {
        // Act
        var update = Parse("""{"event_name":"message.text.received","message":{"message_id":"g","message_type":"CHAT_TEXT","chat":{"id":"grp","chat_type":"GROUP"},"from":{"id":"u","is_bot":false},"text":"hi"}}""");

        // Assert
        update.ChatType.ShouldBe("GROUP");
        update.ChatId.ShouldBe("grp");
    }

    private static Update Parse(string json) => ZaloBotClient.ParseWebhookUpdate(JsonDocument.Parse(json).RootElement);
}
