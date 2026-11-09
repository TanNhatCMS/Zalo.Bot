namespace Zalo.Bot.Types;

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

/// <summary>setWebhook result per docs bot.zapps.me: platform pings the URL immediately and
/// reports the outcome. verification.ok = false means the URL was rejected (WAF/403/...) and
/// events will NOT be delivered — even though the API call itself returned ok=true.</summary>
public sealed class SetWebhookResult
{
    /// <summary>URL the platform registered</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Unix milliseconds of when the webhook was updated</summary>
    [JsonPropertyName("updated_at")]
    public long? UpdatedAt { get; set; }

    /// <summary>Outcome of the platform's immediate verification ping (null on responses without it)</summary>
    [JsonPropertyName("verification")]
    public WebhookVerification? Verification { get; set; }
}

/// <summary>Verification block inside a setWebhook result.</summary>
public sealed class WebhookVerification
{
    /// <summary>True when the endpoint responded successfully to the platform's check request</summary>
    [JsonPropertyName("ok")]
    public bool? Ok { get; set; }

    /// <summary>URL the platform verified</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Machine-readable outcome code, e.g. "webhook.ok", "webhook.http.403"</summary>
    [JsonPropertyName("outcome")]
    public string? Outcome { get; set; }

    /// <summary>Human-readable hint about the failure and how to fix it</summary>
    [JsonPropertyName("hint")]
    public string? Hint { get; set; }
}
