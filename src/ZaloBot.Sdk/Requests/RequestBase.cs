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

    /// <summary>Allows this object to be used as a response in webhooks</summary>
    [JsonIgnore]
    public bool IsWebhookResponse { get; set; }

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

/// <summary>Parameterless READ-ONLY request sent via HTTP GET — docs bot.zapps.me/docs/call-api
/// khuyến nghị GET cho API truy xuất dữ liệu (POST vẫn được hỗ trợ cho mọi API).
/// Lưu ý method name trên URL phân biệt hoa thường.</summary>
/// <typeparam name="TResponse">Type of result expected in result</typeparam>
/// <param name="methodName">Name of request method</param>
public abstract class ParameterlessGetRequest<TResult> : ParameterlessRequest<TResult>
{
    /// <summary>Initializes a new GET request</summary>
    /// <param name="methodName">Name of request method</param>
    protected ParameterlessGetRequest(string methodName) : base(methodName)
    {
        HttpMethod = HttpMethod.Get;
    }
}
