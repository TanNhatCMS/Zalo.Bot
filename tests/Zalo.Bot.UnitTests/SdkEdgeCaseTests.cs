using System.Text.Json;
using Zalo.Bot.Args;
using Zalo.Bot.Polling;
using Zalo.Bot.Types;

namespace Zalo.Bot.UnitTests;

/// <summary>Các nhánh biên còn thiếu của SDK: ChatId, options, thuộc tính/sự kiện của client, DropPendingUpdates và các overload polling.</summary>
public class SdkEdgeCaseTests : ZaloBotClientTestBase
{
    private static string UpdatesJson(params long[] ids)
        => """{"ok":true,"result":["""
        + string.Join(",", ids.Select(id => $$"""{"update_id":{{id}}}"""))
        + "]}";

    // ─── ChatId ───────────────────────────────────────────────────────────────────

    [Test]
    public void ChatId_ShouldParseNumbersAndKeepRawStrings()
    {
        var numeric = new ChatId("123");
        numeric.Identifier.ShouldBe(123);
        numeric.Username.ShouldBeNull();
        numeric.ToString().ShouldBe("123");

        var raw = new ChatId("@handle");
        raw.Identifier.ShouldBeNull();
        raw.Username.ShouldBe("@handle");
        raw.ToString().ShouldBe("@handle");

        Should.Throw<ArgumentNullException>(() => new ChatId((string)null!));
    }

    [Test]
    public void ChatId_ShouldCompareByValue()
    {
        ChatId a = 42;
        ChatId b = "42";
        ChatId other = "abc";

        a.Equals(b).ShouldBeTrue();
        a.Equals((object)b).ShouldBeTrue();
        a.Equals((object)"42").ShouldBeFalse();
        (a == b).ShouldBeTrue();
        (a != other).ShouldBeTrue();
        (a == null).ShouldBeFalse();
        a.GetHashCode().ShouldBe(b.GetHashCode());
        a.Equals(null).ShouldBeFalse();
    }

    [Test]
    public void ChatId_ShouldConvertImplicitlyFromLongStringAndUser()
    {
        ChatId fromLong = 7L;
        ChatId fromString = "7";
        ChatId? fromUser = new User { Id = "99" };
        ChatId? fromNullUser = (User?)null;

        fromLong.ShouldBe(fromString);
        fromUser!.ToString().ShouldBe("99");
        fromNullUser.ShouldBeNull();
    }

    [Test]
    public void ChatId_ShouldSerializeAsAString_AndReadBack()
    {
        JsonSerializer.Serialize<ChatId?>(new ChatId(5)).ShouldBe("\"5\"");
        JsonSerializer.Serialize<ChatId?>(new ChatId("@h")).ShouldBe("\"@h\"");
        JsonSerializer.Deserialize<ChatId>("\"@h\"")!.Username.ShouldBe("@h");
        JsonSerializer.Deserialize<ChatId>("12")!.Identifier.ShouldBe(12);
    }

    // ─── Options ──────────────────────────────────────────────────────────────────

    [Test]
    public void Options_ShouldRejectAnEmptyTokenAndABadBaseUrl()
    {
        Should.Throw<ArgumentException>(() => new ZaloBotClientOptions(""));
        Should.Throw<ArgumentException>(() => new ZaloBotClientOptions("token", "không phải url"));
    }

    // ─── Client ───────────────────────────────────────────────────────────────────

    [Test]
    public void Client_ShouldExposeTheTokenAndAdjustableTimeout()
    {
        var client = CreateClient(new FakeHttpMessageHandler());

        client.Token.ShouldBe(AccessToken);
        client.Timeout = TimeSpan.FromSeconds(12.2);
        client.Timeout.ShouldBe(TimeSpan.FromSeconds(13), "làm tròn lên tới giây");
    }

    [Test]
    public void Client_ShouldTakeTheTokenFromTheArgument_AndPolicyFromTheTemplate()
    {
        var template = new ZaloBotClientOptions("ignored-token", "http://other.local")
        {
            TimeoutSeconds = 77,
            RetryMaxAttempts = 5,
        };

        var fromTemplate = new ZaloBotClient("real-token", template, new HttpClient(new FakeHttpMessageHandler()));
        var plain = new ZaloBotClient("another-token", httpClient: new HttpClient(new FakeHttpMessageHandler()));

        fromTemplate.Token.ShouldBe("real-token");
        fromTemplate.Timeout.ShouldBe(TimeSpan.FromSeconds(77));
        plain.Token.ShouldBe("another-token");
    }

    [Test]
    public async Task Client_ShouldRaiseBothApiEventsToEveryHandler()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":true,"result":{"id":"42"}}""");
        var client = CreateClient(handler);
        var requests = 0;
        var responses = 0;
        client.OnMakingApiRequest += (_, args, _) =>
        {
            requests++;
            args.Request.ShouldNotBeNull();
            return ValueTask.CompletedTask;
        };
        client.OnMakingApiRequest += (_, _, _) =>
        {
            requests++;
            return ValueTask.CompletedTask;
        };
        client.OnApiResponseReceived += (_, _, _) =>
        {
            responses++;
            return ValueTask.CompletedTask;
        };
        client.OnApiResponseReceived += (_, _, _) =>
        {
            responses++;
            return ValueTask.CompletedTask;
        };

        await client.GetMeAsync();

        requests.ShouldBe(2);
        responses.ShouldBe(2);
    }

    // ─── Webhook info ─────────────────────────────────────────────────────────────

    [Test]
    public async Task WebhookInfo_ShouldMapEveryField_AndYieldNullForANonObject()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":true,"result":{"url":"https://x/hook","pending_update_count":3,"last_error_message":"boom","last_error_date":1700000000}}""");
        handler.EnqueueJson("""{"ok":true,"result":{"url":"https://x/hook","last_error_date":0}}""");
        handler.EnqueueJson("""{"ok":true,"result":"không phải object"}""");
        var client = CreateClient(handler);

        var full = await client.GetWebhookInfoAsync();
        var partial = await client.GetWebhookInfoAsync();
        var none = await client.GetWebhookInfoAsync();

        full!.Url.ShouldBe("https://x/hook");
        full.PendingUpdateCount.ShouldBe(3);
        full.LastErrorMessage.ShouldBe("boom");
        full.LastErrorDate.ShouldBe(new DateTime(2023, 11, 14, 22, 13, 20, DateTimeKind.Utc));
        partial!.PendingUpdateCount.ShouldBeNull();
        partial.LastErrorDate.ShouldBeNull("mốc 0 nghĩa là chưa từng lỗi");
        none.ShouldBeNull();
    }

    // ─── DropPendingUpdates ───────────────────────────────────────────────────────

    [Test]
    public async Task DropPendingUpdates_ShouldReturnNull_WhenNothingIsWaiting()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(UpdatesJson());
        var client = CreateClient(handler);

        (await client.DropPendingUpdatesAsync()).ShouldBeNull();

        handler.RequestCount.ShouldBe(1);
    }

    [Test]
    public async Task DropPendingUpdates_ShouldAdvanceTheOffsetPastTheLastPendingUpdate()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(UpdatesJson(5, 6, 9));
        handler.EnqueueJson(UpdatesJson());
        var client = CreateClient(handler);

        var next = await client.DropPendingUpdatesAsync();

        next.ShouldBe(10);
        handler.RequestCount.ShouldBe(2);
        handler.RequestBodies[1].ShouldContain("10");
    }

    // ─── Polling: overload và đường lỗi ───────────────────────────────────────────

    private sealed class RecordingHandler(CancellationTokenSource? stopAfterFirstUpdate = null) : IUpdateHandler
    {
        public List<long> Updates { get; } = [];

        public List<HandleErrorSource> Errors { get; } = [];

        public TaskCompletionSource Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task HandleUpdateAsync(IZaloBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            Updates.Add(update.UpdateId);
            stopAfterFirstUpdate?.Cancel();
            Done.TrySetResult();
            return Task.CompletedTask;
        }

        public Task HandleErrorAsync(IZaloBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            Errors.Add(source);
            Done.TrySetResult();
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task StartReceiving_Generic_ShouldCreateTheHandlerItself()
    {
        GenericHandler.Reset();
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(UpdatesJson(1));
        handler.EnqueueJson(UpdatesJson());
        var client = CreateClient(handler);
        using var cts = new CancellationTokenSource();

        client.StartReceiving<GenericHandler>(new ReceiverOptions { TimeoutSeconds = 0 }, cts.Token);
        await GenericHandler.Received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cts.CancelAsync();

        GenericHandler.Ids.ShouldContain(1);
    }

    public sealed class GenericHandler : IUpdateHandler
    {
        public static List<long> Ids { get; } = [];

        public static TaskCompletionSource Received { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public static void Reset()
        {
            Ids.Clear();
            Received = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task HandleUpdateAsync(IZaloBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            Ids.Add(update.UpdateId);
            Received.TrySetResult();
            return Task.CompletedTask;
        }

        public Task HandleErrorAsync(IZaloBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    [Test]
    public async Task StartReceiving_WithPlainActions_ShouldDispatchUpdatesAndErrors()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(UpdatesJson(3));
        handler.EnqueueJson(UpdatesJson());
        var client = CreateClient(handler);
        var seen = new List<long>();
        var got = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = new CancellationTokenSource();

        client.StartReceiving(
            (_, update, _) =>
            {
                seen.Add(update.UpdateId);
                got.TrySetResult();
            },
            (_, _, _) => { },
            new ReceiverOptions { TimeoutSeconds = 0 },
            cts.Token);
        await got.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cts.CancelAsync();

        seen.ShouldBe([3]);
    }

    [Test]
    public async Task ReceiveAsync_WithPlainActions_ShouldRunUntilCancelled()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(UpdatesJson(4));
        handler.EnqueueJson(UpdatesJson());
        var client = CreateClient(handler);
        var seen = new List<long>();
        using var cts = new CancellationTokenSource();

        var run = client.ReceiveAsync(
            (_, update, _) =>
            {
                seen.Add(update.UpdateId);
                cts.Cancel();
            },
            (_, _, _) => { },
            new ReceiverOptions { TimeoutSeconds = 0 },
            cts.Token);
        await run.WaitAsync(TimeSpan.FromSeconds(5));

        seen.ShouldBe([4]);
    }

    [Test]
    public async Task ReceiveAsync_ShouldReportPollingAndHandlerErrors_WithTheirSource()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson("""{"ok":false,"error_code":500,"description":"lỗi máy chủ"}""", System.Net.HttpStatusCode.InternalServerError);
        handler.EnqueueJson(UpdatesJson(8));
        handler.EnqueueJson(UpdatesJson());
        var client = CreateClient(handler, o => o.RetryMaxAttempts = 1);
        var errors = new List<HandleErrorSource>();
        using var cts = new CancellationTokenSource();

        var run = client.ReceiveAsync(
            (_, _, _) => throw new InvalidOperationException("handler hỏng"),
            (_, _, source, _) =>
            {
                errors.Add(source);
                if (errors.Count == 2)
                {
                    cts.Cancel();
                }

                return Task.CompletedTask;
            },
            new ReceiverOptions { TimeoutSeconds = 0 },
            cts.Token);
        await run.WaitAsync(TimeSpan.FromSeconds(5));

        errors.ShouldBe([HandleErrorSource.PollingError, HandleErrorSource.HandleUpdateError]);
    }

    [Test]
    public async Task ReceiveAsync_ShouldDropPendingUpdatesFirst_WhenAsked()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueJson(UpdatesJson(1, 2));
        handler.EnqueueJson(UpdatesJson(3));
        handler.EnqueueJson(UpdatesJson());
        var client = CreateClient(handler);
        using var cts = new CancellationTokenSource();
        var seen = new RecordingHandler(stopAfterFirstUpdate: cts);

        await client.ReceiveAsync(seen, new ReceiverOptions { TimeoutSeconds = 0, DropPendingUpdates = true }, cts.Token)
            .WaitAsync(TimeSpan.FromSeconds(5));

        seen.Updates.ShouldBe([3], "update 1 và 2 đã bị bỏ qua");
    }

    [Test]
    public async Task ReceiveAsync_ShouldReturnQuietly_WhenCancelledWhileDroppingPendingUpdates()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueDelayed(TimeSpan.FromSeconds(30), UpdatesJson());
        var client = CreateClient(handler);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var recording = new RecordingHandler();

        await client.ReceiveAsync(recording, new ReceiverOptions { DropPendingUpdates = true, TimeoutSeconds = 0 }, cts.Token)
            .WaitAsync(TimeSpan.FromSeconds(5));

        recording.Updates.ShouldBeEmpty();
        recording.Errors.ShouldBeEmpty();
    }

    [Test]
    public async Task StartReceiving_ShouldReportAFatalError_WhenDroppingPendingUpdatesFails()
    {
        var recording = new RecordingHandler();
        // Không có phản hồi nào được xếp hàng: getUpdates của bước bỏ update chờ ném lỗi (không phải huỷ),
        // lỗi thoát khỏi vòng nhận và được báo là FatalError.
        var client = CreateClient(new FakeHttpMessageHandler());

        client.StartReceiving(recording, new ReceiverOptions { DropPendingUpdates = true, TimeoutSeconds = 0 });
        await recording.Done.Task.WaitAsync(TimeSpan.FromSeconds(5));

        recording.Errors.ShouldBe([HandleErrorSource.FatalError]);
    }
}
