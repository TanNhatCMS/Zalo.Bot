namespace ZaloBot.Sdk.Requests;

/// <summary>Use this method to get current webhook status.<para>Returns: A <see cref="WebhookInfo"/> object.</para></summary>
public class GetWebhookInfoRequest() : ParameterlessRequest<WebhookInfo>("getWebhookInfo")
{ }
