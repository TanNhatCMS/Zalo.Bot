namespace ZaloBot.Sdk.Requests;

/// <summary>Use this method when you need to tell the user that something is happening on the bot's side.<para>Returns: <see langword="true"/> on success.</para></summary>
public class SendChatActionRequest() : RequestBase<bool>("sendChatAction")
{
    /// <summary>Unique identifier for the target chat</summary>
    [JsonPropertyName("chat_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required ChatId ChatId { get; set; }

    /// <summary>Type of action to broadcast, e.g. <see cref="ChatAction.Typing"/></summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string Action { get; set; }
}
