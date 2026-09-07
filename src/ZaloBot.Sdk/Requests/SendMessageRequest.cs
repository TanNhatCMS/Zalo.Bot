namespace ZaloBot.Sdk.Requests;

/// <summary>Use this method to send text messages.<para>Returns: The sent <see cref="Message"/> is returned.</para></summary>
public class SendMessageRequest() : RequestBase<Message>("sendMessage")
{
    /// <summary>Unique identifier for the target chat</summary>
    [JsonPropertyName("chat_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required ChatId ChatId { get; set; }

    /// <summary>Text of the message to be sent, 1-2000 characters</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string Text { get; set; }

    /// <summary>Mode for parsing the message text: "markdown" or "html"</summary>
    [JsonPropertyName("parse_mode")]
    public string? ParseMode { get; set; }
}
