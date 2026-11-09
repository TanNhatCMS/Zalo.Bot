using Shouldly;
using Zalo.Bot.Polling;
using Zalo.Bot.UnitTests;

namespace Zalo.Bot.UnitTests.Polling;

/// <summary>StartReceiving chạy nền + event sugar trên ZaloBotClient (OnUpdate/OnError tự
/// start/stop vòng nhận — adapt TelegramBotClient.cs của reference).</summary>
public class StartReceivingTests : ZaloBotClientTestBase
{
    private static string UpdatesJson(params long[] ids)
        => """{"ok":true,"result":["""
        + string.Join(",", ids.Select(id => $$"""{"update_id":{{id}}}"""))
        + "]}";

    [Test]
    public async Task Should_StopReceiving_When_Cancelled()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(UpdatesJson(1));
        handler.EnqueueJson(UpdatesJson());
        var client = CreateClient(handler);
        var received = new List<long>();
        var receivedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cts = new CancellationTokenSource();

        client.StartReceiving(
            (bot, update, ct) =>
            {
                received.Add(update.UpdateId);
                receivedTcs.TrySetResult();
                return Task.CompletedTask;
            },
            (bot, ex, ct) => { cts.Cancel(); return Task.CompletedTask; },
            new ReceiverOptions { TimeoutSeconds = 0 },
            cts.Token);

        // WaitAsync ném TimeoutException nếu handler không nhận update trong 5s
        await receivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cts.Cancel();
        received.ShouldContain(1);
    }

    [Test]
    public async Task Should_StartEventReceivingOnSubscribe_AndStopOnUnsubscribe()
    {
        // Event sugar trên ZaloBotClient (adapt Telegram.Bot): subscribe OnUpdate → tự chạy vòng
        // polling; unsubscribe subscriber cuối → tự dừng (StopEventReceiving).
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(UpdatesJson(1));
        var client = CreateClient(handler);
        var received = new List<long>();
        var receivedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        ZaloBotClient.OnUpdateHandler onUpdate = update =>
        {
            received.Add(update.UpdateId);
            receivedTcs.TrySetResult();
            return Task.CompletedTask;
        };

        client.OnUpdate += onUpdate;

        await receivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        client.OnUpdate -= onUpdate; // subscriber cuối → StopEventReceiving
        received.ShouldContain(1);
    }
}
