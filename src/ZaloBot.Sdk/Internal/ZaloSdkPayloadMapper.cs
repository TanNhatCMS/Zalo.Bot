namespace ZaloBot.Sdk.Internal;

/// <summary>Defensive JSON → model mapping (FR-005/FR-011). Unknown/missing fields never throw:
/// unrecognized payloads map to <see cref="ContentType.Unknown"/> updates or models with empty fields.</summary>
internal static class ZaloSdkPayloadMapper
{
    /// <summary>Parses a webhook payload or a getUpdates element into an <see cref="Update"/>.
    /// Accepts any JSON — unknown shapes return an <see cref="ContentType.Unknown"/> update.</summary>
    public static Update ParseUpdate(JsonElement element)
    {
        var update = new Update { ContentType = ContentType.Unknown };

        if (element.ValueKind != JsonValueKind.Object)
        {
            return update; // arrays, strings, numbers, null → Unknown
        }

        update.UpdateId = TryGetLong(element, "update_id") ?? 0;

        var message = element.TryGetProperty("message", out var messageElement) ? messageElement
            : element.TryGetProperty("edited_message", out var editedElement) ? editedElement
            : default;

        if (message.ValueKind != JsonValueKind.Object)
        {
            return update; // no message payload (e.g. unsupported event kinds) → Unknown
        }

        update.Message = new Message
        {
            MessageId = TryGetString(message, "message_id"),
            Date = TryGetLong(message, "date"),
        };

        var text = TryGetString(message, "text") ?? TryGetString(message, "caption");
        update.Text = text;
        if (text is not null)
        {
            update.ContentType = ContentType.Text;
        }
        else if (message.TryGetProperty("photo", out var photo))
        {
            update.ContentType = ContentType.Photo;
            update.MediaUrl = ExtractUrl(photo);
        }
        else if (message.TryGetProperty("voice", out var voice))
        {
            update.ContentType = ContentType.Voice;
            update.MediaUrl = ExtractUrl(voice);
        }
        else if (message.TryGetProperty("sticker", out var sticker))
        {
            update.ContentType = ContentType.Sticker;
            update.MediaUrl = ExtractUrl(sticker);
        }
        else if (message.TryGetProperty("action", out var action) && action.ValueKind == JsonValueKind.String)
        {
            update.ContentType = ContentType.ChatAction;
            update.MediaUrl = null;
        }

        update.ChatId = TryGetString(message, "chat", "id");
        update.FromUserId = TryGetString(message, "from", "id");
        update.FromDisplayName = TryGetString(message, "from", "display_name")
            ?? TryGetString(message, "from", "username")
            ?? TryGetString(message, "from", "first_name");

        return update;
    }

    /// <summary>Parses a getMe result. Missing fields stay empty; malformed payloads never throw.</summary>
    public static User ParseUser(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return new User();
        }

        return new User
        {
            Id = TryGetString(element, "id") ?? string.Empty,
            AccountName = TryGetString(element, "account_name"),
            AccountType = TryGetString(element, "account_type"),
            CanJoinGroups = TryGetBool(element, "can_join_groups"),
            DisplayName = TryGetString(element, "display_name"),
        };
    }

    /// <summary>Parses a sendMessage/sendPhoto/sendSticker/sendVoice result. Malformed payloads never throw.</summary>
    public static Message ParseMessage(JsonElement element)
        => element.ValueKind != JsonValueKind.Object
            ? new Message()
            : new Message
            {
                MessageId = TryGetString(element, "message_id"),
                Date = TryGetLong(element, "date"),
            };

    /// <summary>Parses a getWebhookInfo result. Non-object payloads map to <see langword="null"/>.</summary>
    public static WebhookInfo? ParseWebhookInfo(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new WebhookInfo
        {
            Url = TryGetString(element, "url"),
            PendingUpdateCount = TryGetLong(element, "pending_update_count"),
            LastErrorMessage = TryGetString(element, "last_error_message"),
            LastErrorDate = TryGetUnixSecondsDateTime(element, "last_error_date"),
        };
    }

    /// <summary>Parses a boolean result leniently (JSON true, "true", numbers ≠ 0).</summary>
    public static bool ParseBool(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.String => bool.TryParse(element.GetString(), out var value) && value,
        JsonValueKind.Number => element.TryGetInt64(out var number) && number != 0,
        _ => false,
    };

    /// <summary>Extracts a media URL from a property that may be a string or an object with a "url" field.</summary>
    private static string? ExtractUrl(JsonElement property) => property.ValueKind switch
    {
        JsonValueKind.String => property.GetString(),
        JsonValueKind.Object => TryGetString(property, "url"),
        _ => null,
    };

    private static string? TryGetString(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var key in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(key, out var next))
            {
                return null;
            }

            current = next;
        }

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString(),
            JsonValueKind.Number => current.GetRawText(),
            _ => null,
        };
    }

    private static long? TryGetLong(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var el))
        {
            return null;
        }

        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt64(out var numValue))
        {
            return numValue;
        }

        if (el.ValueKind == JsonValueKind.String && long.TryParse(el.GetString(), out var strValue))
        {
            return strValue;
        }

        return null;
    }

    private static bool? TryGetBool(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var el))
        {
            return null;
        }

        if (el.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return el.GetBoolean();
        }

        if (el.ValueKind == JsonValueKind.String && bool.TryParse(el.GetString(), out var strValue))
        {
            return strValue;
        }

        return null;
    }

    private static DateTime? TryGetUnixSecondsDateTime(JsonElement element, string propertyName)
        => TryGetLong(element, propertyName) is { } unix && unix > 0
            ? DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime
            : null;
}
