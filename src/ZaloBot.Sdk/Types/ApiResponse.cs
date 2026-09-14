// Model tham chiếu từ tmp/design/TelegramBots/Telegram.Bot/src/Telegram.Bot/Types/ApiResponse.cs —
// giữ để AI tương lai có cấu trúc gốc khi đọc/chuyển code Telegram.Bot. Trong Zalo SDK,
// ZaloSdkHttpSender parse envelope riêng (không qua ApiResponse<T>) nhưng model này vẫn là tài liệu.

namespace ZaloBot.Sdk.Types;

/// <summary>Represents a Bot API response</summary>
public class ApiResponse
{
    /// <summary>Gets a value indicating whether the request was successful.</summary>
    public bool Ok { get; init; }

    /// <summary>Gets the error message.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the error code.</summary>
    public int ErrorCode { get; init; }
}

/// <summary>Represents a successful Bot API response with result</summary>
/// <typeparam name="TResult">Expected type of operation result</typeparam>
public class ApiResponse<TResult> : ApiResponse
{
    /// <summary>Gets the result object.</summary>
    public TResult? Result { get; init; }
}
