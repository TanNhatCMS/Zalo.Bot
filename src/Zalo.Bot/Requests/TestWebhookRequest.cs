namespace Zalo.Bot.Requests;

/// <summary>Use this method to verify webhook delivery works end to end. No parameters.<para>Returns: <see langword="true"/> on success.</para></summary>
public class TestWebhookRequest() : ParameterlessRequest<bool>("testWebhook")
{ }
