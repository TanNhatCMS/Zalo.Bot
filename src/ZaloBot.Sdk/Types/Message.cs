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

    /// <summary>Raw JSON of the `result` element as received from the platform — for audit logging. Never contains the token (token lives in the URL only).</summary>
    [JsonPropertyName("raw_json")]
    public string? RawJson { get; set; }

    /// <summary>Raw JSON of the request body sent to the platform (set for send* calls) — for audit logging. Never contains the token.</summary>
    [JsonIgnore]
    public string? RequestJson { get; set; }
}
