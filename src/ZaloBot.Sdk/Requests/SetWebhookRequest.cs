using ZaloBot.Sdk.Types;

namespace ZaloBot.Sdk.Requests;

/// <summary>Use this method to specify a URL and receive incoming updates via an outgoing webhook.<para>Returns: <see cref="SetWebhookResult"/> including the platform's verification outcome.</para></summary>
public class SetWebhookRequest() : RequestBase<SetWebhookResult>("setWebhook")
{
    /// <summary>HTTPS URL to send updates to</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string Url { get; set; }

    /// <summary>A secret token to be sent in the "X-Bot-Api-Secret-Token" header in every webhook request</summary>
    [JsonPropertyName("secret_token")]
    public string? SecretToken { get; set; }
}
