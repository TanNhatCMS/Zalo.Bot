namespace ZaloBot.Sdk.Types;

/// <summary>Current webhook status (result of getWebhookInfo).</summary>
public partial class WebhookInfo
{
    /// <summary>Webhook URL; empty when the bot uses polling</summary>
    public string? Url { get; set; }

    /// <summary>Number of updates waiting to be delivered</summary>
    [JsonPropertyName("pending_update_count")]
    public long? PendingUpdateCount { get; set; }

    /// <summary>Error message from the most recent failed webhook delivery</summary>
    [JsonPropertyName("last_error_message")]
    public string? LastErrorMessage { get; set; }

    /// <summary>Unix time of the most recent failed webhook delivery</summary>
    [JsonPropertyName("last_error_date")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTime? LastErrorDate { get; set; }
}
