using ZaloBot.Sdk.Args;
using ZaloBot.Sdk.Errors;

namespace ZaloBot.Sdk;

/// <summary>A client interface to use the Zalo Bot API</summary>
public interface IZaloBotClient
{
    /// <summary>Timeout for requests</summary>
    TimeSpan Timeout { get; set; }

    /// <summary>Occurs before sending a request to API</summary>
    event AsyncEventHandler<ApiRequestEventArgs>? OnMakingApiRequest;

    /// <summary>Occurs after receiving the response to an API request</summary>
    event AsyncEventHandler<ApiResponseEventArgs>? OnApiResponseReceived;

    /// <summary>Send a request to Zalo Bot API</summary>
    /// <typeparam name="TResponse">Type of expected result in the response object</typeparam>
    /// <param name="request">API request object</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result of the API request</returns>
    Task<TResponse> SendRequest<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}

/// <summary>Represents an asynchronous event handler for client events</summary>
/// <typeparam name="TArgs">Type of the event arguments</typeparam>
public delegate ValueTask AsyncEventHandler<in TArgs>(IZaloBotClient botClient, TArgs args, CancellationToken cancellationToken = default);
