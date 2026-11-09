namespace Zalo.Bot.Errors;

/// <summary>Category of a Zalo Bot API error, based on the official error codes
/// (400 bad request, 401 token invalid/expired, 403 internal server error, 404 not found,
/// 408 request timeout, 429 quota exceeded) or on the failure origin (parse/network).</summary>
public enum ZaloErrorCategory
{
    /// <summary>Error without a recognizable official code</summary>
    Unknown = 0,
    /// <summary>401 — access token invalid or expired</summary>
    Token,
    /// <summary>408/429 — request timeout or quota exceeded; safe to retry with backoff</summary>
    Retryable,
    /// <summary>400/404 — bad request or not found; retrying will not help</summary>
    NonRetryable,
    /// <summary>403 — Zalo internal server error; retried exactly once</summary>
    ServerRetry,
    /// <summary>Response body could not be parsed</summary>
    Parse,
    /// <summary>Network/transport failure (connection, DNS, TLS, timeout)</summary>
    Network,
}
