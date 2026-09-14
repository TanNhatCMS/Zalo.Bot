// GENERATED FILE - DO NOT MODIFY MANUALLY (model phản hức chính của nền tảng, tham chiếu từ
// tmp/design/TelegramBots/Telegram.Bot/src/Telegram.Bot/Types/ApiResponse.cs).
// Trong Zalo SDK, ZaloSdkHttpSender parse envelope riêng (không qua ApiResponse<T>) nhưng model này
// được giữ cho AI tương lai có tham chiếu cấu trúc gốc khi đọc/chuyển code Telegram.

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
