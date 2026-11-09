namespace Zalo.Bot.Requests;

/// <summary>Use this method to get current webhook status.<para>Returns: A <see cref="WebhookInfo"/> object.</para></summary>
/// <remarks>Read-only API → HTTP GET per docs bot.zapps.me/docs/call-api.</remarks>
public class GetWebhookInfoRequest() : ParameterlessGetRequest<WebhookInfo>("getWebhookInfo")
{ }
