using System.Net;
using Shouldly;
using ZaloBot.Sdk.Polling;
using ZaloBot.Sdk.UnitTests;

namespace ZaloBot.Sdk.UnitTests.Polling;

/// <summary>Vòng lặp ReceiveAsync — adapt từ reference
/// tmp/design/TelegramBots/Telegram.Bot/test/Telegram.Bot.Tests.Unit/Polling/ReceiveAsyncTests.cs.</summary>
public class ReceiveAsyncTests : ZaloBotClientTestBase
{
    private static string UpdatesJson(params long[] ids)
        => """{"ok":true,"result":["""
        + string.Join(",", ids.Select(id => $$"""{"update_id":{{id}}}"""))
        + "]}";

    [Test]
    public async Task Should_DispatchUpdatesInOrder_AndAdvanceOffset_When_ReceiveAsyncRuns()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(UpdatesJson(1, 2));
        handler.EnqueueJson(UpdatesJson(3));
        var client = CreateClient(handler);
        var received = new List<long>();
        var cts = new CancellationTokenSource();

        await client.ReceiveAsync(
            (bot, update, ct) =>
            {
                received.Add(update.UpdateId);
                if (received.Count >= 3)
                {
                    cts.Cancel();
                }

                return Task.CompletedTask;
            },
            (bot, ex, ct) => throw new InvalidOperationException("unexpected error", ex),
            new ReceiverOptions { Offset = 1, TimeoutSeconds = 0 },
            cts.Token);

        received.ShouldBe([1, 2, 3], ignoreOrder: false);
        handler.RequestBodies[0].ShouldContain("\"offset\":1");
        handler.RequestBodies[1].ShouldContain("\"offset\":3"); // sau batch [1,2] → offset = 2+1
    }

    [Test]
    public async Task Should_CallPollingError_AndContinue_When_GetUpdatesFails()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":false,"error_code":400,"description":"bad request"}""", HttpStatusCode.BadRequest);
        handler.EnqueueJson(UpdatesJson(7));
        var client = CreateClient(handler);
        var received = new List<long>();
        Exception? recorded = null;
        var cts = new CancellationTokenSource();

        await client.ReceiveAsync(
            (bot, update, ct) =>
            {
                received.Add(update.UpdateId);
                cts.Cancel();
                return Task.CompletedTask;
            },
            (bot, ex, ct) =>
            {
                recorded = ex;
                return Task.CompletedTask;
            },
            new ReceiverOptions { Offset = 1, TimeoutSeconds = 0 },
            cts.Token);

        recorded.ShouldBeOfType<ApiRequestException>().ErrorCode.ShouldBe(400);
        received.ShouldBe([7]); // vòng lặp tiếp tục sau lỗi polling
    }

    [Test]
    public async Task Should_ReportHandleUpdateError_When_HandlerThrows()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(UpdatesJson(1));
        var client = CreateClient(handler);
        var sources = new List<HandleErrorSource>();
        var cts = new CancellationTokenSource();

        await client.ReceiveAsync(
            (bot, update, ct) => throw new InvalidOperationException("handler boom"),
            (bot, ex, source, ct) =>
            {
                sources.Add(source);
                cts.Cancel();
                return Task.CompletedTask;
            },
            new ReceiverOptions { TimeoutSeconds = 0 },
            cts.Token);

        sources.ShouldContain(HandleErrorSource.HandleUpdateError);
        handler.RequestCount.ShouldBe(1); // handler ném lỗi không làm vòng lặp crash — chỉ dừng theo cancel
    }

    [Test]
    public async Task Should_DropPendingUpdates_When_OptionEnabled()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(UpdatesJson(1, 2)); // pending — sẽ bị bỏ qua
        handler.EnqueueJson(UpdatesJson(3));
        var client = CreateClient(handler);
        var received = new List<long>();
        var cts = new CancellationTokenSource();

        await client.ReceiveAsync(
            (bot, update, ct) =>
            {
                received.Add(update.UpdateId);
                cts.Cancel();
                return Task.CompletedTask;
            },
            (bot, ex, ct) => Task.CompletedTask,
            new ReceiverOptions { Offset = 1, DropPendingUpdates = true, TimeoutSeconds = 0 },
            cts.Token);

        received.ShouldBe([3]); // [1,2] pending bị drop
        handler.RequestBodies[0].ShouldContain("\"offset\":1");
        handler.RequestBodies[1].ShouldContain("\"offset\":3"); // advance qua pending
    }
}
