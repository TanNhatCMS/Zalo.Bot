namespace Zalo.Bot.Types;

/// <summary>A QR login session issued by the Zalo session service.
/// Render <see cref="LoginUrl"/> as a QR code (the SDK never renders QR codes itself —
/// the frontend does that from this URL) and pass <see cref="Zbsk"/> to
/// <see cref="ZaloBotLoginClient.WaitForLoginAsync(ZaloLoginSession, TimeSpan?, TimeSpan?, CancellationToken)"/>
/// to wait for the user to scan it.</summary>
public sealed class ZaloLoginSession
{
    /// <summary>URL encoded in the QR code; the user opens it in Zalo to confirm the login.</summary>
    public required string LoginUrl { get; init; }

    /// <summary>Session key identifying this login attempt. Pass it back to get-login-status.
    /// Treat it like a credential: never log it or put it in exception messages.</summary>
    public required string Zbsk { get; init; }
}
