using System.Net;
using System.Text;

namespace Zalo.Bot.UnitTests;

/// <summary>Fake HttpMessageHandler: records every request (URL + body) and answers from a
/// programmable response queue. Queue factories may throw HttpRequestException to simulate transport
/// failures; delayed entries honor the request cancellation token (like a real transport would).</summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<CancellationToken, Task<HttpResponseMessage>>> _responses = new();

    public List<HttpRequestMessage> Requests { get; } = [];

    public List<string> RequestUrls { get; } = [];

    public List<string> RequestBodies { get; } = [];

    public int RequestCount => Requests.Count;

    public void EnqueueJson(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        => Enqueue(() => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });

    public void Enqueue(Func<HttpResponseMessage> responseFactory)
        => _responses.Enqueue(_ => Task.FromResult(responseFactory()));

    public void EnqueueThrow(HttpRequestException exception) => Enqueue(() => throw exception);

    public void EnqueueDelayed(TimeSpan delay, string json)
        => _responses.Enqueue(async cancellationToken =>
        {
            await Task.Delay(delay, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        });

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestUrls.Add(request.RequestUri?.ToString() ?? string.Empty);
        RequestBodies.Add(request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken));

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("FakeHttpMessageHandler has no queued response left.");
        }

        return await _responses.Dequeue()(cancellationToken);
    }
}
