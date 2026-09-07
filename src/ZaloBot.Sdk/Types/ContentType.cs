namespace ZaloBot.Sdk.Types;

/// <summary>Type of content carried by an <see cref="Update"/>. Anything the platform sends that the SDK
/// does not recognize maps to <see cref="Unknown"/> (defensive parsing, never throws).</summary>
public enum ContentType
{
    /// <summary>Unrecognized or missing content</summary>
    Unknown = 0,
    /// <summary>Text message (text or caption)</summary>
    Text,
    /// <summary>Photo message</summary>
    Photo,
    /// <summary>Sticker message</summary>
    Sticker,
    /// <summary>Voice message</summary>
    Voice,
    /// <summary>Chat action notification (e.g. typing)</summary>
    ChatAction,
}
