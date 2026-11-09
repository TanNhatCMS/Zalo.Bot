namespace Zalo.Bot.Requests;

/// <summary>Use this method to send photos.<para>Returns: The sent <see cref="Message"/> is returned.</para></summary>
public class SendPhotoRequest() : RequestBase<Message>("sendPhoto")
{
    /// <summary>Unique identifier for the target chat</summary>
    [JsonPropertyName("chat_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required ChatId ChatId { get; set; }

    /// <summary>Photo to send: an HTTP URL or an id of a photo already uploaded to the platform</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string Photo { get; set; }

    /// <summary>Caption kèm ảnh (docs apis/sendPhoto): 1–2000 ký tự, tùy chọn.</summary>
    [JsonPropertyName("caption")]
    public string? Caption { get; set; }
}
