namespace Zalo.Bot.Polling;

/// <summary>Processes <see cref="Types.Update"/>s and errors during polling.
/// <para>See <see cref="DefaultUpdateHandler"/> for a simple delegate-based implementation.</para>
/// (Adapted from Telegram.Bot polling — reference tmp/design/TelegramBots.)</summary>
public interface IUpdateHandler
{
    /// <summary>Handles an <see cref="Types.Update"/>.</summary>
    /// <param name="botClient">The <see cref="IZaloBotClient"/> instance of the bot receiving the update</param>
    /// <param name="update">The <see cref="Types.Update"/> to handle</param>
    /// <param name="cancellationToken">Token notifying that processing should stop</param>
    Task HandleUpdateAsync(IZaloBotClient botClient, Types.Update update, CancellationToken cancellationToken);

    /// <summary>Handles an <see cref="Exception"/> raised while polling.</summary>
    /// <param name="botClient">The <see cref="IZaloBotClient"/> instance of the bot receiving the update</param>
    /// <param name="exception">The exception to handle</param>
    /// <param name="source">Where the error occurred</param>
    /// <param name="cancellationToken">Token notifying that processing should stop</param>
    Task HandleErrorAsync(IZaloBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken);
}

/// <summary>The source of the error raised during polling</summary>
public enum HandleErrorSource
{
    /// <summary>Exception occurred during getUpdates. Polling of updates will continue</summary>
    PollingError,

    /// <summary>A fatal uncaught exception occurred. Polling of updates will stop</summary>
    FatalError,

    /// <summary>Exception was thrown by <see cref="IUpdateHandler.HandleUpdateAsync"/>. Polling of updates will continue</summary>
    HandleUpdateError,
}
