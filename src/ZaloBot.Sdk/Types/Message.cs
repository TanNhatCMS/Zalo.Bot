namespace ZaloBot.Sdk.Types;

/// <summary>A Zalo message (result of sendMessage/sendPhoto/sendSticker/sendVoice).
/// The platform only guarantees message_id (a STRING) and date (epoch milliseconds).</summary>
public partial class Message
{
    /// <summary>Unique identifier of the message (string)</summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    /// <summary>Date the message was sent, in epoch milliseconds</summary>
    public long? Date { get; set; }
}
