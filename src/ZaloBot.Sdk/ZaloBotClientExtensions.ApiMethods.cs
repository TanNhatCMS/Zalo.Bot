using ZaloBot.Sdk.Requests;

namespace ZaloBot.Sdk;

/// <summary>Provides extension methods for <see cref="IZaloBotClient"/> that map to the 11 methods
/// the Zalo Bot API exposes.</summary>
public static partial class ZaloBotClientExtensions
{
    /// <summary>Maximum length of sendMessage text accepted by the platform</summary>
    public const int MaxMessageTextLength = 2000;

    /// <summary>Use this method to test your bot's access token and get basic information about it.</summary>
    /// <param name="botClient">An instance of <see cref="IZaloBotClient"/></param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>On success, basic information about the bot.</returns>
    public static Task<User> GetMeAsync(this IZaloBotClient botClient, CancellationToken cancellationToken = default)
        => botClient.SendRequest(new GetMeRequest(), cancellationToken);

    /// <summary>Use this method to receive incoming updates using long polling.</summary>
    /// <param name="botClient">An instance of <see cref="IZaloBotClient"/></param>
    /// <param name="offset">Identifier of the first update to be returned (usually lastUpdateId + 1).</param>
    /// <param name="timeoutSeconds">Timeout in seconds for long polling; 0 means short polling (default).</param>
    /// <param name="limit">Limits the number of updates to be retrieved (1-100).</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>On success, an array of <see cref="Update"/> objects (empty when the response is not an array).</returns>
    public static Task<Update[]> GetUpdatesAsync(this IZaloBotClient botClient, long? offset = default, int timeoutSeconds = 0, int limit = 100,
        CancellationToken cancellationToken = default)
        => botClient.SendRequest(new GetUpdatesRequest
        {
            Offset = offset,
            Timeout = timeoutSeconds > 0 ? timeoutSeconds : null, // 0 = short polling → omit the field
            Limit = limit,
        }, cancellationToken);

    /// <summary>Use this method to send text messages.</summary>
    /// <param name="botClient">An instance of <see cref="IZaloBotClient"/></param>
    /// <param name="chatId">Unique identifier for the target chat.</param>
    /// <param name="text">Text of the message to be sent, 1-2000 characters. Validated locally before any network call.</param>
    /// <param name="parseMode">Optional: "markdown" or "html". Takes precedence over <paramref name="textStyles"/>:
    /// the platform ignores text_styles when parse_mode is present.</param>
    /// <param name="textStyles">Optional explicit rich-text style runs over the raw text; offsets are UTF-16 code
    /// units (same as .NET string indexing) — see <see cref="Types.TextStyle"/> and <see cref="Types.TextStyleCode"/>.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>On success, the sent <see cref="Message"/> is returned.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="text"/> is empty or longer than 2000 characters (no request is sent).</exception>
    public static Task<Message> SendMessageAsync(this IZaloBotClient botClient, ChatId chatId, string text, string? parseMode = default,
        IEnumerable<TextStyle>? textStyles = default, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text) || text.Length > MaxMessageTextLength)
        {
            throw new ArgumentException($"Message text must be between 1 and {MaxMessageTextLength} characters.", nameof(text));
        }

        return botClient.SendRequest(new SendMessageRequest
        {
            ChatId = chatId,
            Text = text,
            ParseMode = parseMode,
            TextStyles = textStyles?.ToArray(),
        }, cancellationToken);
    }

    /// <summary>Use this method to send photos.</summary>
    /// <param name="botClient">An instance of <see cref="IZaloBotClient"/></param>
    /// <param name="chatId">Unique identifier for the target chat.</param>
    /// <param name="photo">Photo to send: an HTTP URL or an id of a photo already uploaded to the platform.</param>
    /// <param name="caption">Optional caption 1–2000 characters sent along with the photo.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>On success, the sent <see cref="Message"/> is returned.</returns>
    public static Task<Message> SendPhotoAsync(this IZaloBotClient botClient, ChatId chatId, string photo,
        string? caption = default, CancellationToken cancellationToken = default)
        => botClient.SendRequest(new SendPhotoRequest { ChatId = chatId, Photo = photo, Caption = caption }, cancellationToken);

    /// <summary>Use this method to send stickers.</summary>
    /// <param name="botClient">An instance of <see cref="IZaloBotClient"/></param>
    /// <param name="chatId">Unique identifier for the target chat.</param>
    /// <param name="sticker">Sticker to send: a sticker id or an HTTP URL; passed through to the platform as-is.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>On success, the sent <see cref="Message"/> is returned.</returns>
    public static Task<Message> SendStickerAsync(this IZaloBotClient botClient, ChatId chatId, string sticker,
        CancellationToken cancellationToken = default)
        => botClient.SendRequest(new SendStickerRequest { ChatId = chatId, Sticker = sticker }, cancellationToken);

    /// <summary>Use this method to send voice messages.</summary>
    /// <param name="botClient">An instance of <see cref="IZaloBotClient"/></param>
    /// <param name="chatId">Unique identifier for the target chat (docs: chỉ hỗ trợ chat 1-1, không hỗ trợ nhóm).</param>
    /// <param name="voiceUrl">HTTP URL of the .aac file to send (docs apis/sendVoice: bắt buộc đuôi .aac).</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>On success, the sent <see cref="Message"/> is returned.</returns>
    public static Task<Message> SendVoiceAsync(this IZaloBotClient botClient, ChatId chatId, string voiceUrl,
        CancellationToken cancellationToken = default)
        => botClient.SendRequest(new SendVoiceRequest { ChatId = chatId, VoiceUrl = voiceUrl }, cancellationToken);

    /// <summary>Use this method when you need to tell the user that something is happening on the bot's side.</summary>
    /// <param name="botClient">An instance of <see cref="IZaloBotClient"/></param>
    /// <param name="chatId">Unique identifier for the target chat.</param>
    /// <param name="action">Type of action to broadcast, e.g. <see cref="ChatAction.Typing"/>.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>On success, <see langword="true"/> is returned.</returns>
    public static Task<bool> SendChatActionAsync(this IZaloBotClient botClient, ChatId chatId, string action,
        CancellationToken cancellationToken = default)
        => botClient.SendRequest(new SendChatActionRequest { ChatId = chatId, Action = action }, cancellationToken);

    /// <summary>Use this method to specify a URL and receive incoming updates via an outgoing webhook.</summary>
    /// <param name="botClient">An instance of <see cref="IZaloBotClient"/></param>
    /// <param name="url">HTTPS URL to send updates to.</param>
    /// <param name="secretToken">Secret token sent in the X-Bot-Api-Secret-Token header of every webhook
    /// request — docs apis/setWebhook: BẮT BUỘC, 8–256 ký tự.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns><see cref="Types.SetWebhookResult"/> with the platform's verification outcome — check
    /// its <c>Verification.Ok</c>: the call can succeed (ok=true) while the endpoint itself is rejected
    /// (e.g. WAF 403), meaning events will never arrive.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="secretToken"/> is null/not 8–256 characters (no request is sent).</exception>
    public static Task<SetWebhookResult> SetWebhookAsync(this IZaloBotClient botClient, string url, string? secretToken = default,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretToken) || secretToken.Length is < 8 or > 256)
        {
            throw new ArgumentException("Secret token must be between 8 and 256 characters.", nameof(secretToken));
        }

        return botClient.SendRequest(new SetWebhookRequest { Url = url, SecretToken = secretToken }, cancellationToken);
    }

    /// <summary>Use this method to verify webhook delivery works end to end.</summary>
    /// <param name="botClient">An instance of <see cref="IZaloBotClient"/></param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>On success, <see langword="true"/> is returned.</returns>
    public static Task<bool> TestWebhookAsync(this IZaloBotClient botClient, CancellationToken cancellationToken = default)
        => botClient.SendRequest(new TestWebhookRequest(), cancellationToken);

    /// <summary>Use this method to remove webhook integration if you decide to switch back to getUpdates.</summary>
    /// <param name="botClient">An instance of <see cref="IZaloBotClient"/></param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>On success, <see langword="true"/> is returned.</returns>
    public static Task<bool> DeleteWebhookAsync(this IZaloBotClient botClient, CancellationToken cancellationToken = default)
        => botClient.SendRequest(new DeleteWebhookRequest(), cancellationToken);

    /// <summary>Use this method to get current webhook status.</summary>
    /// <param name="botClient">An instance of <see cref="IZaloBotClient"/></param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>On success, a <see cref="WebhookInfo"/> object is returned (<see langword="null"/> for non-object payloads).</returns>
    public static async Task<WebhookInfo?> GetWebhookInfoAsync(this IZaloBotClient botClient, CancellationToken cancellationToken = default)
        => await botClient.SendRequest(new GetWebhookInfoRequest(), cancellationToken).ConfigureAwait(false);
}
