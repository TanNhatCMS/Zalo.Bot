namespace Zalo.Bot.Types;

/// <summary>Result of waiting for a QR login session. When <see cref="Status"/> is
/// <see cref="ZaloLoginStatus.Connected"/>, <see cref="BotToken"/> is set — store it securely
/// yourself and use it to create a <see cref="ZaloBotClient"/>; the SDK never stores credentials.</summary>
public sealed class ZaloLoginResult
{
    /// <summary>Outcome of the wait.</summary>
    public required ZaloLoginStatus Status { get; init; }

    /// <summary>True when <see cref="Status"/> is <see cref="ZaloLoginStatus.Connected"/>.</summary>
    public bool IsConnected => Status == ZaloLoginStatus.Connected;

    /// <summary>Bot access token issued by the platform. Set only when connected.
    /// Never log it or include it in exception messages.</summary>
    public string? BotToken { get; init; }

    /// <summary>Bot id reported by the platform, when available.</summary>
    public string? BotId { get; init; }

    /// <summary>Owner id reported by the platform, when available.</summary>
    public string? OwnerId { get; init; }

    /// <summary>OA id reported by the platform, when available.</summary>
    public string? OaId { get; init; }

    /// <summary>Human-readable outcome, e.g. "Login confirmed." or "QR/session expired (498)".</summary>
    public required string Message { get; init; }
}
