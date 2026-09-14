using ZaloBot.Sdk.Polling;
using ZaloBot.Sdk.Requests;

namespace ZaloBot.Sdk;

/// <summary>Provides extension methods for <see cref="IZaloBotClient"/> that allow for
/// <see cref="Types.Update"/> polling — adapted from Telegram.Bot's polling (reference
/// tmp/design/TelegramBots/TelegramBotClientExtensions.Polling.cs), pruned to Zalo's getUpdates
/// (no AllowedUpdates; UpdateId is long; long-poll timeout do người gọi chỉ định thay vì lấy từ
/// client.Timeout để tránh hai đồng hồ đua).</summary>
public static partial class ZaloBotClientExtensions
{
    /// <summary>Bỏ qua toàn bộ update đang chờ: short-poll lấy danh sách rồi advance offset mà
    /// không dispatch. Trả về offset kế tiếp (lastUpdateId + 1), null nếu không có update chờ.</summary>
    /// <param name="botClient">An instance of <see cref="IZaloBotClient"/></param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    public static async Task<long?> DropPendingUpdatesAsync(this IZaloBotClient botClient, CancellationToken cancellationToken = default)
    {
        var pending = await botClient.GetUpdatesAsync(offset: null, timeoutSeconds: 0, limit: 100, cancellationToken).ConfigureAwait(false);
        if (pending.Length == 0)
        {
            return null;
        }

        await botClient.GetUpdatesAsync(offset: pending[^1].UpdateId + 1, timeoutSeconds: 0, limit: 1, cancellationToken).ConfigureAwait(false);
        return pending[^1].UpdateId + 1;
    }

    /// <summary>Starts receiving updates on the ThreadPool, invoking the handler for each.
    /// <para>This method does not block. A background polling loop is initiated, calling getUpdates
    /// then the handler for each update.</para></summary>
    /// <typeparam name="TUpdateHandler">The <see cref="IUpdateHandler"/> used for processing updates</typeparam>
    /// <param name="botClient">The <see cref="IZaloBotClient"/> used for making getUpdates calls</param>
    /// <param name="receiverOptions">Options used to configure getUpdates requests</param>
    /// <param name="cancellationToken">The cancellation token with which you can stop receiving</param>
    public static void StartReceiving<TUpdateHandler>(this IZaloBotClient botClient, ReceiverOptions? receiverOptions = default,
        CancellationToken cancellationToken = default) where TUpdateHandler : IUpdateHandler, new()
        => StartReceiving(botClient, new TUpdateHandler(), receiverOptions, cancellationToken);

    /// <summary>Starts receiving updates on the ThreadPool, invoking the handler for each.
    /// <para>This method does not block.</para></summary>
    /// <param name="botClient">The <see cref="IZaloBotClient"/> used for making getUpdates calls</param>
    /// <param name="updateHandler">The <see cref="IUpdateHandler"/> used for processing updates</param>
    /// <param name="receiverOptions">Options used to configure getUpdates requests</param>
    /// <param name="cancellationToken">The cancellation token with which you can stop receiving</param>
    public static void StartReceiving(this IZaloBotClient botClient, IUpdateHandler updateHandler, ReceiverOptions? receiverOptions = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(botClient);
        ArgumentNullException.ThrowIfNull(updateHandler);

        _ = Task.Run(async () =>
        {
            try
            {
                await ReceiveAsync(botClient, updateHandler, receiverOptions, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // ignored — dừng theo token
            }
            catch (Exception ex)
            {
                try
                {
                    await updateHandler.HandleErrorAsync(botClient, ex, HandleErrorSource.FatalError, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // ignored
                }
            }
        }, cancellationToken);
    }

    /// <summary>Starts receiving updates on the ThreadPool, invoking the handler for each.
    /// <para>This method does not block.</para></summary>
    /// <param name="botClient">The <see cref="IZaloBotClient"/> used for making getUpdates calls</param>
    /// <param name="updateHandler">Delegate used for processing updates</param>
    /// <param name="errorHandler">Delegate used for processing polling errors (có kèm nguồn lỗi)</param>
    /// <param name="receiverOptions">Options used to configure getUpdates requests</param>
    /// <param name="cancellationToken">The cancellation token with which you can stop receiving</param>
    public static void StartReceiving(this IZaloBotClient botClient,
        Func<IZaloBotClient, Types.Update, CancellationToken, Task> updateHandler,
        Func<IZaloBotClient, Exception, HandleErrorSource, CancellationToken, Task> errorHandler,
        ReceiverOptions? receiverOptions = default, CancellationToken cancellationToken = default)
        => StartReceiving(botClient, new DefaultUpdateHandler(updateHandler, errorHandler), receiverOptions, cancellationToken);

    /// <summary>Starts receiving updates on the ThreadPool, invoking the handler for each.
    /// <para>This method does not block.</para></summary>
    /// <param name="botClient">The <see cref="IZaloBotClient"/> used for making getUpdates calls</param>
    /// <param name="updateHandler">Delegate used for processing updates</param>
    /// <param name="errorHandler">Delegate used for processing polling errors</param>
    /// <param name="receiverOptions">Options used to configure getUpdates requests</param>
    /// <param name="cancellationToken">The cancellation token with which you can stop receiving</param>
    public static void StartReceiving(this IZaloBotClient botClient,
        Func<IZaloBotClient, Types.Update, CancellationToken, Task> updateHandler,
        Func<IZaloBotClient, Exception, CancellationToken, Task> errorHandler,
        ReceiverOptions? receiverOptions = default, CancellationToken cancellationToken = default)
        => StartReceiving(botClient, new DefaultUpdateHandler(updateHandler, errorHandler), receiverOptions, cancellationToken);

    /// <summary>Starts receiving updates on the ThreadPool, invoking the handler for each.
    /// <para>This method does not block.</para></summary>
    /// <param name="botClient">The <see cref="IZaloBotClient"/> used for making getUpdates calls</param>
    /// <param name="updateHandler">Action invoked for each update</param>
    /// <param name="errorHandler">Action invoked for each polling error</param>
    /// <param name="receiverOptions">Options used to configure getUpdates requests</param>
    /// <param name="cancellationToken">The cancellation token with which you can stop receiving</param>
    public static void StartReceiving(this IZaloBotClient botClient,
        Action<IZaloBotClient, Types.Update, CancellationToken> updateHandler,
        Action<IZaloBotClient, Exception, CancellationToken> errorHandler,
        ReceiverOptions? receiverOptions = default, CancellationToken cancellationToken = default)
        => StartReceiving(botClient, new DefaultUpdateHandler(
                (bot, update, token) => { updateHandler(bot, update, token); return Task.CompletedTask; },
                (bot, exception, _, token) => { errorHandler(bot, exception, token); return Task.CompletedTask; }),
            receiverOptions, cancellationToken);

    /// <summary>Wait for incoming updates, invoking the handler for each.
    /// <para>This method will block if awaited — vòng lặp getUpdates + handler chạy tới khi cancel.</para></summary>
    /// <param name="botClient">The <see cref="IZaloBotClient"/> used for making getUpdates calls</param>
    /// <param name="updateHandler">Delegate used for processing updates</param>
    /// <param name="errorHandler">Delegate used for processing polling errors (có kèm nguồn lỗi)</param>
    /// <param name="receiverOptions">Options used to configure getUpdates requests</param>
    /// <param name="cancellationToken">The cancellation token with which you can stop receiving</param>
    public static async Task ReceiveAsync(this IZaloBotClient botClient,
        Func<IZaloBotClient, Types.Update, CancellationToken, Task> updateHandler,
        Func<IZaloBotClient, Exception, HandleErrorSource, CancellationToken, Task> errorHandler,
        ReceiverOptions? receiverOptions = default, CancellationToken cancellationToken = default)
        => await ReceiveAsync(botClient, new DefaultUpdateHandler(updateHandler, errorHandler), receiverOptions, cancellationToken).ConfigureAwait(false);

    /// <summary>Wait for incoming updates, invoking the handler for each. This method will block if awaited.</summary>
    /// <param name="botClient">The <see cref="IZaloBotClient"/> used for making getUpdates calls</param>
    /// <param name="updateHandler">Delegate used for processing updates</param>
    /// <param name="errorHandler">Delegate used for processing polling errors</param>
    /// <param name="receiverOptions">Options used to configure getUpdates requests</param>
    /// <param name="cancellationToken">The cancellation token with which you can stop receiving</param>
    public static async Task ReceiveAsync(this IZaloBotClient botClient,
        Func<IZaloBotClient, Types.Update, CancellationToken, Task> updateHandler,
        Func<IZaloBotClient, Exception, CancellationToken, Task> errorHandler,
        ReceiverOptions? receiverOptions = default, CancellationToken cancellationToken = default)
        => await ReceiveAsync(botClient, new DefaultUpdateHandler(updateHandler, errorHandler), receiverOptions, cancellationToken).ConfigureAwait(false);

    /// <summary>Wait for incoming updates, invoking the handler for each. This method will block if awaited.</summary>
    /// <param name="botClient">The <see cref="IZaloBotClient"/> used for making getUpdates calls</param>
    /// <param name="updateHandler">Action invoked for each update</param>
    /// <param name="errorHandler">Action invoked for each polling error</param>
    /// <param name="receiverOptions">Options used to configure getUpdates requests</param>
    /// <param name="cancellationToken">The cancellation token with which you can stop receiving</param>
    public static async Task ReceiveAsync(this IZaloBotClient botClient,
        Action<IZaloBotClient, Types.Update, CancellationToken> updateHandler,
        Action<IZaloBotClient, Exception, CancellationToken> errorHandler,
        ReceiverOptions? receiverOptions = default, CancellationToken cancellationToken = default)
        => await ReceiveAsync(botClient, new DefaultUpdateHandler(
                (bot, update, token) => { updateHandler(bot, update, token); return Task.CompletedTask; },
                (bot, exception, _, token) => { errorHandler(bot, exception, token); return Task.CompletedTask; }),
            receiverOptions, cancellationToken).ConfigureAwait(false);

    /// <summary>Wait for incoming updates, invoking <see cref="IUpdateHandler.HandleUpdateAsync"/> for each.
    /// <para>This method will block if awaited. Vòng lặp: getUpdates (long-poll theo
    /// <see cref="ReceiverOptions.TimeoutSeconds"/>) → dispatch từng update → advance offset =
    /// lastUpdateId + 1. Lỗi getUpdates/handler được đưa vào error handler, vòng lặp tiếp tục;
    /// <see cref="OperationCanceledException"/> kết thúc vòng lặp.</para></summary>
    /// <param name="botClient">The <see cref="IZaloBotClient"/> used for making getUpdates calls</param>
    /// <param name="updateHandler">The <see cref="IUpdateHandler"/> used for processing updates</param>
    /// <param name="receiverOptions">Options used to configure getUpdates requests</param>
    /// <param name="cancellationToken">The cancellation token with which you can stop receiving</param>
    public static async Task ReceiveAsync(this IZaloBotClient botClient, IUpdateHandler updateHandler, ReceiverOptions? receiverOptions = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateHandler);

        var limit = receiverOptions?.Limit ?? 100;
        var messageOffset = receiverOptions?.Offset ?? 0;
        var timeoutSeconds = Math.Max(0, receiverOptions?.TimeoutSeconds ?? 25);
        var emptyUpdates = Array.Empty<Types.Update>();

        if (receiverOptions?.DropPendingUpdates is true)
        {
            try
            {
                var pending = await botClient.GetUpdatesAsync(messageOffset, timeoutSeconds: 0, limit: 100, cancellationToken).ConfigureAwait(false);
                messageOffset = pending.Length == 0 ? messageOffset : pending[^1].UpdateId + 1;
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        var request = new GetUpdatesRequest
        {
            Limit = limit,
            Offset = messageOffset,
            Timeout = timeoutSeconds > 0 ? timeoutSeconds : null,
        };

        while (!cancellationToken.IsCancellationRequested)
        {
            var updates = emptyUpdates;
            try
            {
                updates = await botClient.SendRequest(request, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                try
                {
                    await updateHandler.HandleErrorAsync(botClient, exception, HandleErrorSource.PollingError, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            foreach (var update in updates)
            {
                try
                {
                    request.Offset = update.UpdateId + 1;
                    await updateHandler.HandleUpdateAsync(botClient, update, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    try
                    {
                        await updateHandler.HandleErrorAsync(botClient, ex, HandleErrorSource.HandleUpdateError, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // ignored
                    }
                }
            }
        }
    }
}
