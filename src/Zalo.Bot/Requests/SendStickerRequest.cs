namespace Zalo.Bot.Requests;

/// <summary>Use this method to send stickers.<para>Returns: The sent <see cref="Message"/> is returned.</para></summary>
public class SendStickerRequest() : RequestBase<Message>("sendSticker")
{
    /// <summary>Unique identifier for the target chat</summary>
    [JsonPropertyName("chat_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required ChatId ChatId { get; set; }

    /// <summary>Sticker to send: a sticker id or an HTTP URL; passed through to the platform as-is</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string Sticker { get; set; }
}
