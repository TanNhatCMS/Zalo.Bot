namespace Zalo.Bot.Requests;

/// <summary>Use this method to send voice messages.<para>Returns: The sent <see cref="Message"/> is returned.</para></summary>
public class SendVoiceRequest() : RequestBase<Message>("sendVoice")
{
    /// <summary>Unique identifier for the target chat</summary>
    [JsonPropertyName("chat_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required ChatId ChatId { get; set; }

    /// <summary>Voice to send: an HTTP URL to an .aac file (docs apis/sendVoice: bắt buộc đường dẫn
    /// hợp lệ có phần mở rộng .aac; chỉ hỗ trợ chat 1-1, không hỗ trợ nhóm).</summary>
    [JsonPropertyName("voice_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string VoiceUrl { get; set; }
}
