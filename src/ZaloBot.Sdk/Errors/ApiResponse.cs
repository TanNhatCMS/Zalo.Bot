namespace ZaloBot.Sdk.Errors;

/// <summary>Represents a Zalo Bot API response wrapper: {"ok":bool,"result":...,"error_code":int,"description":string?}</summary>
public class ApiResponse
{
    /// <summary>Gets a value indicating whether the request was successful.</summary>
    public bool Ok { get; init; }

    /// <summary>Gets the error message.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the official error code.</summary>
    public int ErrorCode { get; init; }
}

/// <summary>Represents a successful Zalo Bot API response with result</summary>
/// <typeparam name="TResult">Expected type of operation result</typeparam>
public class ApiResponse<TResult> : ApiResponse
{
    /// <summary>Gets the result object.</summary>
    public TResult? Result { get; init; }
}
