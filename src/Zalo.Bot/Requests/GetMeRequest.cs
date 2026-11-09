namespace Zalo.Bot.Requests;

/// <summary>A simple method for testing your bot's access token.<para>Returns: Basic information about the bot in form of a <see cref="User"/> object.</para></summary>
/// <remarks>Read-only API → HTTP GET per docs bot.zapps.me/docs/call-api.</remarks>
public class GetMeRequest() : ParameterlessGetRequest<User>("getMe")
{ }
