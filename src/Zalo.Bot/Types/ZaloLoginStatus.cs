namespace Zalo.Bot.Types;

/// <summary>Outcome of waiting for a QR login session.</summary>
public enum ZaloLoginStatus
{
    /// <summary>The user scanned the QR and the platform issued a bot token.</summary>
    Connected = 0,

    /// <summary>The session expired (platform error 498) before the user scanned.</summary>
    Expired = 1,

    /// <summary>The session was waited on for the full timeout without a scan.</summary>
    TimedOut = 2,
}
