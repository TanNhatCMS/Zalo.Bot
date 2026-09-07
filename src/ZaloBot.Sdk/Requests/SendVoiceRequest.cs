namespace ZaloBot.Sdk.Requests;

/// <summary>Use this method to send voice messages.<para>Returns: The sent <see cref="Message"/> is returned.</para></summary>
public class SendVoiceRequest() : RequestBase<Message>("sendVoice")
{
    /// <summary>Unique identifier for the target chat</summary>
    [JsonPropertyName("chat_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required ChatId ChatId { get; set; }

    /// <summary>Voice message to send: an HTTP URL or an id of a voice already uploaded to the platform</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string Voice { get; set; }
}
