namespace ZaloBot.Sdk.Requests;

/// <summary>A simple method for testing your bot's access token.<para>Returns: Basic information about the bot in form of a <see cref="User"/> object.</para></summary>
public class GetMeRequest() : ParameterlessRequest<User>("getMe")
{ }
