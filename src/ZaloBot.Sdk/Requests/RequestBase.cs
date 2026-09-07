using System.Net.Http.Json;

namespace ZaloBot.Sdk.Requests;

/// <summary>Represents an API request</summary>
/// <typeparam name="TResponse">Type of result expected in result</typeparam>
/// <param name="methodName">Zalo Bot API method</param>
public abstract class RequestBase<TResponse>(string methodName) : IRequest<TResponse>
{
    /// <inheritdoc/>
    [JsonIgnore]
    public HttpMethod HttpMethod { get; set; } = HttpMethod.Post;

    /// <inheritdoc/>
    [JsonIgnore]
    public string MethodName { get; } = methodName;

    /// <summary>Generate content of HTTP message</summary>
    /// <returns>Content of HTTP request</returns>
    public virtual HttpContent? ToHttpContent()
        => JsonContent.Create(this, GetType(), options: JsonBotAPI.Options);
}

/// <summary>Represents a request that doesn't require any parameters</summary>
/// <param name="methodName">Name of request method</param>
public abstract class ParameterlessRequest<TResult>(string methodName) : RequestBase<TResult>(methodName)
{
    /// <inheritdoc/>
    public override HttpContent? ToHttpContent() => default;
}
