namespace Zalo.Bot.Types;

/// <summary>An incoming Zalo update (from getUpdates or a webhook payload), already flattened
/// defensively by the SDK. Unknown/unrecognized payloads map to <see cref="ContentType.Unknown"/>
/// instead of throwing.</summary>
public partial class Update
{
    /// <summary>Unique identifier of the update</summary>
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    /// <summary>Webhook event name per docs bot.zapps.me/docs/webhook, e.g. "message.text.received",
    /// "message.unsupported.received". Null for getUpdates elements (no event_name there).</summary>
    public string? EventName { get; set; }

    /// <summary>The message of the update (normalized from message/edited_message); only message_id and date are kept</summary>
    public Message? Message { get; set; }

    /// <summary>Id of the chat the message belongs to (string)</summary>
    public string? ChatId { get; set; }

    /// <summary>Id of the user who sent the message (string)</summary>
    public string? FromUserId { get; set; }

    /// <summary>Display name of the user who sent the message</summary>
    public string? FromDisplayName { get; set; }

    /// <summary>Text of the message (text or caption)</summary>
    public string? Text { get; set; }

    /// <summary>Content type of the message; unknown shapes map to <see cref="Zalo.Bot.Types.ContentType.Unknown"/></summary>
    public ContentType ContentType { get; set; } = ContentType.Unknown;

    /// <summary>URL of the media (photo/voice/sticker) when applicable</summary>
    public string? MediaUrl { get; set; }

    /// <summary>Raw <c>message.message_type</c> per docs (22/10/2026): "CHAT_TEXT", "CHAT_PHOTO",
    /// "CHAT_STICKER" or "CHAT_VOICE". Null when the platform omits it (optional field).</summary>
    public string? MessageType { get; set; }

    /// <summary>Raw <c>message.chat.chat_type</c>: "PRIVATE" (1-1 conversation) or "GROUP" (group, Beta).</summary>
    public string? ChatType { get; set; }

    /// <summary><c>message.from.is_bot</c> — whether the sender is a bot. Null when absent.</summary>
    public bool? FromIsBot { get; set; }
}
