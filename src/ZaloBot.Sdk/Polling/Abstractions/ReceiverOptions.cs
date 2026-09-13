namespace ZaloBot.Sdk.Polling;

/// <summary>Options to configure getUpdates requests of the polling loop.
/// (Adapted from Telegram.Bot.ReceiverOptions — Zalo không có AllowedUpdates, UpdateId là long,
/// và long-poll timeout phải nhỏ hơn client TimeoutSeconds để tránh hai đồng hồ đua — xem README.)</summary>
public sealed class ReceiverOptions
{
    private int? _limit = 100;

    /// <summary>Identifier of the first update to be returned (thường là lastUpdateId + 1)</summary>
    public long? Offset { get; set; }

    /// <summary>Limits the number of updates to be retrieved. Values between 1-100 are accepted.
    /// Defaults to 100 when set to <see langword="null"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value doesn't satisfy constraints</exception>
    public int? Limit
    {
        get => _limit;
        set => _limit = value is not (< 1 or > 100) ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, $"'{nameof(Limit)}' can not be less than 1 or greater than 100");
    }

    /// <summary>Long-poll: số giây nền tảng giữ kết nối mỗi vòng getUpdates (docs: bỏ trống → server giữ
    /// mặc định 30s). Mặc định 25; 0 = short polling. Client <c>TimeoutSeconds</c> phải ≥ giá trị này
    /// + 15s buffer, nếu không hai đồng hồ đua nhau → timeout giả.</summary>
    public int TimeoutSeconds { get; set; } = 25;

    /// <summary>Chỉ ra có bỏ qua toàn bộ update đang chờ trước khi bắt đầu poll hay không —
    /// short-poll lấy danh sách rồi advance offset mà KHÔNG dispatch cho handler.</summary>
    public bool DropPendingUpdates { get; set; }
}
