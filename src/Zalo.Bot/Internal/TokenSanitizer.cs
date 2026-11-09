using System.Text;

namespace Zalo.Bot.Internal;

/// <summary>Token hygiene helpers (FR-009): the access token must NEVER appear in exception messages,
/// logged text or body excerpts. All SDK exception paths funnel their dynamic content through here.</summary>
internal static class TokenSanitizer
{
    /// <summary>Max length of any body excerpt included in an exception message.</summary>
    public const int MaxExcerptLength = 200;

    /// <summary>Replaces occurrences of the token with a mask and truncates the text to <see cref="MaxExcerptLength"/> chars.</summary>
    public static string? Sanitize(string? text, string token)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var safe = token.Length == 0 ? text : text.Replace(token, "***", StringComparison.Ordinal);
        safe = Truncate(safe, MaxExcerptLength);
        return safe;
    }

    /// <summary>Truncates text to <paramref name="maxLength"/> chars, appending an ellipsis when cut.</summary>
    public static string Truncate(string text, int maxLength)
        => text.Length <= maxLength ? text : $"{text[..maxLength]}…";

    /// <summary>Asserts a message is token-free; used by tests and as a last-resort guard in exception factories.</summary>
    public static bool ContainsToken(string? text, string token)
        => !string.IsNullOrEmpty(text) && token.Length > 0 && text.Contains(token, StringComparison.Ordinal);

    /// <summary>Builds a message that is guaranteed token-free: sanitize, then hard-verify.</summary>
    public static string BuildSafeMessage(string message, string token, string? excerpt = default)
    {
        var sb = new StringBuilder(Sanitize(message, token) ?? message);
        if (excerpt is not null)
        {
            var safeExcerpt = Sanitize(excerpt, token);
            if (safeExcerpt is not null)
                sb.Append(" Body excerpt: ").Append(safeExcerpt);
        }

        var result = sb.ToString();
        return ContainsToken(result, token) ? "Zalo Bot API error (details withheld: token detected)" : result;
    }
}
