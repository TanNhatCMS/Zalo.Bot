namespace Zalo.Bot.Requests;

/// <summary>Use this method to receive incoming updates using long polling.<para>Returns: An Array of <see cref="Update"/> objects.</para></summary>
public class GetUpdatesRequest() : RequestBase<Update[]>("getUpdates")
{
    /// <summary>Identifier of the first update to be returned. Must be greater by one than the highest among the identifiers of previously received updates.</summary>
    [JsonPropertyName("offset")]
    public long? Offset { get; set; }

    /// <summary>Limits the number of updates to be retrieved. Defaults to 100.</summary>
    public int? Limit { get; set; }

    /// <summary>Timeout in seconds for long polling. Defaults to 0, i.e. usual short polling.</summary>
    public int? Timeout { get; set; }
}
