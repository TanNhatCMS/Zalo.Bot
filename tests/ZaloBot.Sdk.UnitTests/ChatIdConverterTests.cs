using System.Text.Json;

namespace ZaloBot.Sdk.UnitTests;

/// <summary>ChatId luôn được ghi thành chuỗi vì Zalo Bot API yêu cầu chat_id là string.</summary>
public class ChatIdConverterTests
{
    [Test]
    public void Serialize_WritesNumericIdsAsStrings()
    {
        JsonSerializer.Serialize<ChatId?>(new ChatId(123456789012L)).ShouldBe("\"123456789012\"");
    }

    [Test]
    public void Serialize_WritesRawStringIdsAsIs()
    {
        JsonSerializer.Serialize<ChatId?>(new ChatId("abc-def")).ShouldBe("\"abc-def\"");
    }

    [Test]
    public void Serialize_WritesNull()
    {
        JsonSerializer.Serialize<ChatId?>(null).ShouldBe("null");
    }

    [Test]
    public void Deserialize_ReadsAStringId()
    {
        var chatId = JsonSerializer.Deserialize<ChatId?>("\"abc-def\"");

        chatId.ShouldNotBeNull();
        chatId.Username.ShouldBe("abc-def");
    }

    [Test]
    public void Deserialize_ReadsANumericId()
    {
        var chatId = JsonSerializer.Deserialize<ChatId?>("123");

        chatId.ShouldNotBeNull();
        chatId.Identifier.ShouldBe(123L);
    }

    [Test]
    public void RoundTripsANumericId()
    {
        var json = JsonSerializer.Serialize<ChatId?>(new ChatId(42));

        JsonSerializer.Deserialize<ChatId?>(json).ShouldBe(new ChatId(42));
    }
}
