namespace Zalo.Bot.Polling;

/// <summary>A very simple <see cref="IUpdateHandler"/> implementation backed by two delegates.
/// (Adapted from Telegram.Bot polling — reference tmp/design/TelegramBots.)</summary>
/// <param name="updateHandler">The function to invoke when an update is received</param>
/// <param name="errorHandler">The function to invoke when an error occurs</param>
public class DefaultUpdateHandler(
    Func<IZaloBotClient, Types.Update, CancellationToken, Task> updateHandler,
    Func<IZaloBotClient, Exception, HandleErrorSource, CancellationToken, Task> errorHandler) : IUpdateHandler
{
    /// <summary>Constructs a new <see cref="DefaultUpdateHandler"/> — error handler không cần biết nguồn lỗi.</summary>
    /// <param name="updateHandler">The function to invoke when an update is received</param>
    /// <param name="errorHandler">The function to invoke when an error occurs</param>
    public DefaultUpdateHandler(
        Func<IZaloBotClient, Types.Update, CancellationToken, Task> updateHandler,
        Func<IZaloBotClient, Exception, CancellationToken, Task> errorHandler)
        : this(updateHandler, (bot, ex, _, ct) => errorHandler(bot, ex, ct))
    { }

    /// <inheritdoc/>
    public async Task HandleUpdateAsync(IZaloBotClient botClient, Types.Update update, CancellationToken cancellationToken)
        => await updateHandler(botClient, update, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task HandleErrorAsync(IZaloBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        => await errorHandler(botClient, exception, source, cancellationToken).ConfigureAwait(false);
}
