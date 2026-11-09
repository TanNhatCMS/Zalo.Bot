using Zalo.Bot.Args;

namespace Zalo.Bot.Internal;

/// <summary>Core HTTP pipeline: POST {BaseUrl}/bot{token}/{method} with a JSON body, parse the
/// {"ok",result,error_code,description} wrapper, apply the retry taxonomy (408/429 retry with
/// exponential backoff, 403 retry exactly once, 400/401/404 never) and enforce token hygiene
/// (FR-007/FR-008/FR-009). Exceptions never contain the access token or the request URL.</summary>
internal sealed class ZaloSdkHttpSender
{
    private readonly HttpClient _httpClient;
    private readonly ZaloBotClientOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly Func<ApiRequestEventArgs, CancellationToken, ValueTask>? _onMakingApiRequest;
    private readonly Func<ApiResponseEventArgs, CancellationToken, ValueTask>? _onApiResponseReceived;

    public ZaloSdkHttpSender(
        HttpClient httpClient,
        ZaloBotClientOptions options,
        TimeProvider? timeProvider = default,
        Func<ApiRequestEventArgs, CancellationToken, ValueTask>? onMakingApiRequest = default,
        Func<ApiResponseEventArgs, CancellationToken, ValueTask>? onApiResponseReceived = default)
    {
        _httpClient = httpClient;
        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _onMakingApiRequest = onMakingApiRequest;
        _onApiResponseReceived = onApiResponseReceived;
    }

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // The URL contains the access token — it lives only in this local and is never logged or embedded in errors.
        var url = $"{_options.BaseRequestUrl}/{request.MethodName}";
        // No 'using' for HttpContent here: each retry attempt must create a fresh instance
        // because HttpRequestMessage.Content disposal (inside SendOnceAsync) invalidates it.
        var maxAttempts = Math.Max(1, _options.RetryMaxAttempts);
        for (int attempt = 1; ; attempt++)
        {
            using var httpContent = request.ToHttpContent();
            var (success, hasResult, result, errorCode, description, body) = await SendOnceAsync(request, url, httpContent, cancellationToken)
                .ConfigureAwait(false);

            if (success)
            {
                var mapped = MapResult<TResponse>(result, hasResult);
                // Audit: gắn JSON body đã gửi và ApiResponse JSON nhận về từ platform để log theo message
                if (mapped is Message message)
                {
                    message.RequestJson = SerializeRequest(request);
                    message.RawJson = body;
                }
                return mapped;
            }

            var category = ZaloSdkErrorTaxonomy.CategoryOf(errorCode);
            var canRetry = category switch
            {
                ZaloErrorCategory.Retryable => attempt < maxAttempts,
                ZaloErrorCategory.ServerRetry => attempt == 1 && maxAttempts >= 2,
                _ => false,
            };
            if (canRetry)
            {
                await DelayAsync(_options.GetRetryDelay(attempt), cancellationToken).ConfigureAwait(false);
                continue;
            }

            // Retries exhausted → throw with the code/description of the LAST failed attempt.
            if (category == ZaloErrorCategory.Retryable)
            {
                throw new ZaloBotRetryableException(description, errorCode);
            }

            throw ZaloSdkErrorTaxonomy.CreateException(errorCode, description);
        }
    }

    /// <summary>Sends one HTTP attempt and returns the parsed wrapper outcome and raw response body.
    /// Network/timeout failures throw <see cref="ZaloBotNetworkException"/> (never retried here).</summary>
    private async Task<(bool Success, bool HasResult, JsonElement Result, int ErrorCode, string? Description, string Body)> SendOnceAsync(
        IRequest request, string url, HttpContent? httpContent, CancellationToken cancellationToken)
    {
        HttpResponseMessage? httpResponse = null;
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_options.Timeout);

            // Verb theo từng request: GET cho API đọc (docs/call-api khuyến nghị), POST cho còn lại —
            // docs hỗ trợ cả 2 cho mọi API; method name trên URL phân biệt hoa thường.
            using var httpRequest = new HttpRequestMessage(request.HttpMethod, url) { Content = httpContent };
            if (_onMakingApiRequest is not null)
            {
                await _onMakingApiRequest(new ApiRequestEventArgs(request, httpRequest), timeoutCts.Token).ConfigureAwait(false);
            }

            try
            {
                httpResponse = await _httpClient.SendAsync(httpRequest, timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ZaloBotNetworkException("Zalo Bot API request timed out");
            }
            catch (Exception exception) when (exception is HttpRequestException or System.IO.IOException)
            {
                var safeMessage = TokenSanitizer.BuildSafeMessage(
                    $"Zalo Bot API request failed: {exception.Message}", _options.Token);
                throw new ZaloBotNetworkException(safeMessage, SanitizeInner(exception));
            }

            string body;
            try
            {
                body = await httpResponse.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ZaloBotNetworkException("Zalo Bot API request timed out while reading the response");
            }
            catch (Exception exception) when (exception is HttpRequestException or System.IO.IOException)
            {
                var safeMessage = TokenSanitizer.BuildSafeMessage(
                    $"Zalo Bot API response could not be read: {exception.Message}", _options.Token);
                throw new ZaloBotNetworkException(safeMessage, SanitizeInner(exception));
            }

            var outcome = ParseWrapper(body, request.MethodName);

            if (_onApiResponseReceived is not null)
            {
                var apiRequestEventArgs = new ApiRequestEventArgs(request, httpRequest);
                var apiResponse = new Types.ApiResponse<JsonElement>
                {
                    Ok = outcome.Success,
                    ErrorCode = outcome.ErrorCode,
                    Description = outcome.Description,
                    Result = outcome.HasResult ? outcome.Result : default,
                };
                var args = new ApiResponseEventArgs(httpResponse, apiRequestEventArgs, body, apiResponse);
                await _onApiResponseReceived(args, timeoutCts.Token).ConfigureAwait(false);
            }

            return (outcome.Success, outcome.HasResult, outcome.Result, outcome.ErrorCode, outcome.Description, body);
        }
        finally
        {
            httpResponse?.Dispose();
        }
    }

    /// <summary>Returns the transport exception when its full string representation is token-free;
    /// otherwise replaces it with a token-free copy so no leak path (message, ToString, logging) survives.</summary>
    private Exception SanitizeInner(Exception exception)
        => TokenSanitizer.ContainsToken(exception.ToString(), _options.Token)
            ? new HttpRequestException(TokenSanitizer.BuildSafeMessage(exception.Message, _options.Token))
            : exception;

    /// <summary>Parses the {"ok":bool,"result":...,"error_code":int,"description":string?} wrapper.
    /// ok=true → success outcome (result may be absent); ok=false or code/description present → failure outcome;
    /// anything else → <see cref="InvalidJsonException"/> with a safe body excerpt.</summary>
    private (bool Success, bool HasResult, JsonElement Result, int ErrorCode, string? Description) ParseWrapper(string body, string methodName)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            var excerpt = TokenSanitizer.Sanitize(body, _options.Token);
            throw new InvalidJsonException("There was an exception during parsing of the Zalo Bot API response", excerpt);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                var nonObjectExcerpt = TokenSanitizer.Sanitize(body, _options.Token);
                throw new InvalidJsonException("Zalo Bot API response is not a JSON object", nonObjectExcerpt);
            }

            var hasOk = root.TryGetProperty("ok", out var okElement) && okElement.ValueKind == JsonValueKind.True;
            if (hasOk)
            {
                var hasResult = root.TryGetProperty("result", out var resultElement)
                    && resultElement.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined);
                // Clone detaches the element from this document (which is disposed on return).
                return (true, hasResult, hasResult ? resultElement.Clone() : default, 0, null);
            }

            var errorCode = 0;
            if (root.TryGetProperty("error_code", out var errorCodeElement)
                && errorCodeElement.ValueKind == JsonValueKind.Number
                && errorCodeElement.TryGetInt32(out var parsedErrorCode))
            {
                errorCode = parsedErrorCode;
            }

            string? description = root.TryGetProperty("description", out var descriptionElement)
                && descriptionElement.ValueKind == JsonValueKind.String
                    ? descriptionElement.GetString()
                    : null;

            if (!root.TryGetProperty("ok", out _)
                && !root.TryGetProperty("error_code", out _)
                && !root.TryGetProperty("description", out _))
            {
                var unknownShapeExcerpt = TokenSanitizer.Sanitize(body, _options.Token);
                throw new InvalidJsonException(
                    $"Zalo Bot API response for {methodName} is not a valid API wrapper", unknownShapeExcerpt);
            }

            // The description may echo the access token (e.g. "invalid token for bot123:secret") — sanitize it.
            return (false, false, default, errorCode, TokenSanitizer.Sanitize(description, _options.Token));
        }
    }

    /// <summary>Serialize a request to the JSON body it sends — same options as <see cref="RequestBase.ToHttpContent"/>.
    /// Parameterless requests (testWebhook...) return null (no body). Never contains the token.</summary>
    private static string? SerializeRequest(IRequest request)
        => request.ToHttpContent() is null
            ? null // parameterless (testWebhook...) — no body
            : JsonSerializer.Serialize(request, request.GetType(), JsonBotAPI.Options);

    /// <summary>Maps the `result` element to <typeparamref name="TResponse"/> defensively.</summary>
    private static TResponse MapResult<TResponse>(JsonElement result, bool hasResult)
    {
        if (typeof(TResponse) == typeof(Update[]))
        {
            var updates = hasResult && result.ValueKind == JsonValueKind.Array
                ? result.EnumerateArray().Select(ZaloSdkPayloadMapper.ParseUpdate).ToArray()
                : [];
            return (TResponse)(object)updates;
        }

        if (!hasResult)
        {
            // ok=true without result: treat boolean operations as succeeded, others as no data.
            if (typeof(TResponse) == typeof(bool)) return (TResponse)(object)true;
            if (typeof(TResponse) == typeof(SetWebhookResult)) return (TResponse)(object)new SetWebhookResult();
            return default(TResponse)!;
        }

        if (typeof(TResponse) == typeof(User)) return (TResponse)(object)ZaloSdkPayloadMapper.ParseUser(result);
        if (typeof(TResponse) == typeof(Message)) return (TResponse)(object)ZaloSdkPayloadMapper.ParseMessage(result);
        if (typeof(TResponse) == typeof(WebhookInfo)) return (TResponse)(object)ZaloSdkPayloadMapper.ParseWebhookInfo(result)!;
        if (typeof(TResponse) == typeof(bool)) return (TResponse)(object)ZaloSdkPayloadMapper.ParseBool(result);
        if (typeof(TResponse) == typeof(SetWebhookResult)) return (TResponse)(object)ZaloSdkPayloadMapper.ParseSetWebhookResult(result);

        return result.Deserialize<TResponse>(JsonBotAPI.Options)!;
    }

    private async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (delay <= TimeSpan.Zero)
        {
            return;
        }

        await Task.Delay(delay, _timeProvider, cancellationToken).ConfigureAwait(false);
    }
}
