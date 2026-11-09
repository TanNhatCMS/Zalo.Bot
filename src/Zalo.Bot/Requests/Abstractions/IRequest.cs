namespace Zalo.Bot.Requests.Abstractions;

/// <summary>Represents a request to Zalo Bot API</summary>
public interface IRequest
{
    /// <summary>HTTP method of request</summary>
    HttpMethod HttpMethod { get; }

    /// <summary>API method name</summary>
    string MethodName { get; }

    /// <summary>Generate content of HTTP message</summary>
    /// <returns>Content of HTTP request</returns>
    HttpContent? ToHttpContent();
}

/// <summary>Represents a request to Zalo Bot API</summary>
/// <typeparam name="TResponse">Type of result expected in result</typeparam>
public interface IRequest<TResponse> : IRequest;
