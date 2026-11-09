using Zalo.Bot.Types;

namespace Zalo.Bot.Args;

/// <summary>Provides data for ApiResponseReceived event</summary>
/// <param name="responseMessage">HTTP response received from API</param>
/// <param name="apiRequestEventArgs">Event arguments of this request</param>
/// <param name="responseBody">Raw token-free JSON body of response</param>
/// <param name="apiResponse">Parsed ApiResponse wrapper</param>
public class ApiResponseEventArgs(
    HttpResponseMessage responseMessage,
    ApiRequestEventArgs apiRequestEventArgs,
    string? responseBody = null,
    ApiResponse? apiResponse = null)
{
    /// <summary>HTTP response received from API</summary>
    public HttpResponseMessage ResponseMessage { get; } = responseMessage;

    /// <summary>Event arguments of this request</summary>
    public ApiRequestEventArgs ApiRequestEventArgs { get; } = apiRequestEventArgs;

    /// <summary>Raw JSON body of response (token-free)</summary>
    public string? ResponseBody { get; } = responseBody;

    /// <summary>Parsed ApiResponse wrapper</summary>
    public ApiResponse? ApiResponse { get; } = apiResponse;
}
