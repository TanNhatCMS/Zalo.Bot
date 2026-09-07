namespace ZaloBot.Sdk.Types;

/// <summary>A Zalo bot (result of getMe). Field names follow the Zalo Bot API:
/// id, account_name, account_type, can_join_groups, display_name. Note that id is a STRING.</summary>
public partial class User
{
    /// <summary>Unique identifier of the bot (string)</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Account name of the bot, e.g. "bot.OodbOvGw"</summary>
    public string? AccountName { get; set; }

    /// <summary>Account type of the bot, e.g. "BASIC"</summary>
    public string? AccountType { get; set; }

    /// <summary>Whether the bot can join groups</summary>
    public bool? CanJoinGroups { get; set; }

    /// <summary>Display name of the bot, e.g. "Bot PNH Note"</summary>
    public string? DisplayName { get; set; }

    /// <inheritdoc/>
    public override string ToString() => DisplayName is null ? Id : $"{DisplayName} ({Id})";
}
