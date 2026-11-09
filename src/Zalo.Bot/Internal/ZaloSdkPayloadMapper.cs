namespace Zalo.Bot.Internal;

/// <summary>Defensive JSON → model mapping (FR-005/FR-011). Unknown/missing fields never throw:
/// unrecognized payloads map to <see cref="ContentType.Unknown"/> updates or models with empty fields.</summary>
internal static class ZaloSdkPayloadMapper
{
    /// <summary>Parses a webhook payload or a getUpdates element into an <see cref="Update"/>.
    /// Accepts any JSON — unknown shapes return an <see cref="ContentType.Unknown"/> update.
    /// Webhook payloads (docs bot.zapps.me/docs/webhook) arrive wrapped as {ok, result:{message, event_name}}
    /// — this unwraps <c>result</c> automatically.</summary>
    public static Update ParseUpdate(JsonElement element)
    {
        var update = new Update { ContentType = ContentType.Unknown };

        if (element.ValueKind != JsonValueKind.Object)
        {
            return update; // arrays, strings, numbers, null → Unknown
        }

        // Webhook payload shape: {"ok":true,"result":{...}} — unwrap result nếu có (getUpdates gửi element trực tiếp)
        if (element.TryGetProperty("result", out var resultElement)
            && resultElement.ValueKind == JsonValueKind.Object)
        {
            element = resultElement;
        }

        update.UpdateId = TryGetLong(element, "update_id") ?? 0;
        update.EventName = TryGetString(element, "event_name");

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
        update.MessageType = TryGetString(message, "message_type");

        // Docs 22/10/2026: message_type (CHAT_TEXT/CHAT_PHOTO/CHAT_STICKER/CHAT_VOICE) là nguồn phân loại tin cậy nhất;
        // thiếu hoặc giá trị lạ → suy luận theo trường có mặt (tương thích payload cũ).
        var classified = update.MessageType switch
        {
            "CHAT_TEXT" => SetText(update),
            "CHAT_PHOTO" => SetMedia(update, ContentType.Photo, PhotoUrl(message)),
            "CHAT_STICKER" => SetMedia(update, ContentType.Sticker, StickerUrl(message)),
            "CHAT_VOICE" => SetMedia(update, ContentType.Voice, TryGetString(message, "voice_url")),
            _ => false,
        };

        if (!classified)
        {
            if (text is not null && !HasPhoto(message))
            {
                update.ContentType = ContentType.Text;
            }
            else if (HasPhoto(message))
            {
                // Ảnh (kể cả có caption): Text giữ caption, MediaUrl là ảnh — photo_url (docs mới) hoặc photo (cũ).
                SetMedia(update, ContentType.Photo, PhotoUrl(message));
            }
            else if (message.TryGetProperty("voice_url", out var voiceUrl))
            {
                SetMedia(update, ContentType.Voice, ExtractUrl(voiceUrl));
            }
            else if (message.TryGetProperty("sticker", out _))
            {
                SetMedia(update, ContentType.Sticker, StickerUrl(message));
            }
            else if (message.TryGetProperty("action", out var action) && action.ValueKind == JsonValueKind.String)
            {
                update.ContentType = ContentType.ChatAction;
                update.MediaUrl = null;
            }
        }

        update.ChatType = TryGetString(message, "chat", "chat_type");
        update.FromIsBot = message.TryGetProperty("from", out var from) && from.ValueKind == JsonValueKind.Object
            ? TryGetBool(from, "is_bot")
            : null;
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

    /// <summary>Parses a sendMessage/sendPhoto/sendSticker/sendVoice result. Malformed payloads never throw.
    /// RawJson keeps the original result JSON for audit logging.</summary>
    public static Message ParseMessage(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return new Message();
        }

        var message = new Message
        {
            MessageId = TryGetString(element, "message_id"),
            Date = TryGetLong(element, "date"),
        };
        try
        {
            message.RawJson = element.GetRawText();
        }
        catch (InvalidOperationException)
        {
            // element detached from its document — defensive, should not happen for parsed responses
        }

        return message;
    }

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

    /// <summary>Parses a setWebhook result. Tolerates legacy responses where result is
    /// a bare bool or missing (verification stays null — callers must treat null as unknown).</summary>
    public static SetWebhookResult ParseSetWebhookResult(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return new SetWebhookResult(); // ok:true without result / result:true
        }

        var verification = element.TryGetProperty("verification", out var v) && v.ValueKind == JsonValueKind.Object
            ? new WebhookVerification
            {
                Ok = v.TryGetProperty("ok", out var ok) ? ParseBool(ok) : null,
                Url = TryGetString(v, "url"),
                Outcome = TryGetString(v, "outcome"),
                Hint = TryGetString(v, "hint"),
            }
            : null;

        return new SetWebhookResult
        {
            Url = TryGetString(element, "url"),
            UpdatedAt = TryGetLong(element, "updated_at"),
            Verification = verification,
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

    private static bool SetText(Update update)
    {
        update.ContentType = ContentType.Text;
        return true;
    }

    private static bool SetMedia(Update update, ContentType contentType, string? mediaUrl)
    {
        update.ContentType = contentType;
        update.MediaUrl = mediaUrl;
        return true;
    }

    private static bool HasPhoto(JsonElement message)
        => message.TryGetProperty("photo_url", out _) || message.TryGetProperty("photo", out _);

    /// <summary>photo_url (docs 22/10/2026) hoặc photo (payload cũ: string URL hoặc object có url).</summary>
    private static string? PhotoUrl(JsonElement message)
        => TryGetString(message, "photo_url")
            ?? (message.TryGetProperty("photo", out var photo) ? ExtractUrl(photo) : null);

    /// <summary>Docs: sticker là mã, url là ảnh hiển thị — url ưu tiên, mã sticker chỉ là fallback.</summary>
    private static string? StickerUrl(JsonElement message)
        => TryGetString(message, "url")
            ?? (message.TryGetProperty("sticker", out var sticker) ? ExtractUrl(sticker) : null);

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
