namespace Zalo.Bot.Requests;

/// <summary>Use this method to remove webhook integration if you decide to switch back to getUpdates.<para>Returns: <see langword="true"/> on success.</para></summary>
public class DeleteWebhookRequest() : ParameterlessRequest<bool>("deleteWebhook")
{ }
