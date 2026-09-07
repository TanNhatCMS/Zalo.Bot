using System.Globalization;

namespace ZaloBot.Sdk.Types;

/// <summary>Represents a ChatId. Zalo chat identifiers are strings (user/group ids);
/// numeric values are kept as long for convenience but always serialized as strings.</summary>
[JsonConverter(typeof(ChatIdConverter))]
public class ChatId : IEquatable<ChatId>
{
    /// <summary>Unique identifier for the chat</summary>
    public long? Identifier { get; }

    /// <summary>Raw chat identifier as a string (non-numeric Zalo chat ids, or @-handles)</summary>
    public string? Username { get; }

    /// <summary>Create a <see cref="ChatId"/> using unique identifier for the chat</summary>
    /// <param name="identifier">Unique identifier for the chat</param>
    public ChatId(long identifier) => Identifier = identifier;

    /// <summary>Create a <see cref="ChatId"/> using a chat identifier string (numeric id, @handle, or any raw Zalo chat id).</summary>
    /// <param name="id">Chat identifier string</param>
    /// <exception cref="ArgumentNullException">Thrown when string value is <see langword="null"/></exception>
    public ChatId(string id)
    {
        if (id is null) { throw new ArgumentNullException(nameof(id)); }
        if (long.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var identifier))
            Identifier = identifier;
        else
            Username = id; // @handles and any other raw Zalo chat id — passed through as-is
    }

    /// <summary>Determines whether the specified object is equal to the current object.</summary>
    /// <param name="obj">The object to compare with the current object.</param>
    public override bool Equals(object? obj) => obj is ChatId chatId && this == chatId;

    /// <inheritdoc/>
    public bool Equals(ChatId? other) => this == other;

    /// <summary>Gets the hash code of this object</summary>
    public override int GetHashCode() => StringComparer.InvariantCulture.GetHashCode(ToString());

    /// <summary>Create a string out of a <see cref="ChatId"/></summary>
    public override string ToString() => (Username ?? Identifier?.ToString(CultureInfo.InvariantCulture))!;

    /// <summary>Create a <see cref="ChatId"/> using unique identifier for the chat</summary>
    /// <param name="identifier">Unique identifier for the chat</param>
    public static implicit operator ChatId(long identifier) => new(identifier);

    /// <summary>Create a <see cref="ChatId"/> using a chat identifier string</summary>
    /// <param name="id">Chat identifier string</param>
    public static implicit operator ChatId(string id) => new(id);

    /// <summary>Convert a <see cref="User"/> object to a <see cref="ChatId"/></summary>
    [return: NotNullIfNotNull(nameof(user))]
    public static implicit operator ChatId?(User? user) => user is null ? null : new(user.Id);

    /// <summary>Compares two ChatId objects</summary>
    public static bool operator ==(ChatId? obj1, ChatId? obj2)
    {
        if (ReferenceEquals(obj1, obj2)) return true;
        if (obj1 is null || obj2 is null) return false;
        return obj1.Identifier == obj2.Identifier && string.Equals(obj1.Username, obj2.Username, StringComparison.Ordinal);
    }

    /// <summary>Compares two ChatId objects</summary>
    public static bool operator !=(ChatId obj1, ChatId obj2) => !(obj1 == obj2);
}
