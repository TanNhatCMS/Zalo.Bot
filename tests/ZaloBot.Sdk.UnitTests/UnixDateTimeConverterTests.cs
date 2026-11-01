using System.Text.Json;
using ZaloBot.Sdk.Serialization;

namespace ZaloBot.Sdk.UnitTests;

public class UnixDateTimeConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new UnixDateTimeConverter() },
    };

    [Test]
    public void Deserialize_ReadsUnixSecondsAsUtc()
    {
        var value = JsonSerializer.Deserialize<DateTime>("1700000000", Options);

        value.ShouldBe(new DateTime(2023, 11, 14, 22, 13, 20, DateTimeKind.Utc));
    }

    [Test]
    public void Serialize_WritesUnixSeconds()
    {
        JsonSerializer.Serialize(new DateTime(2023, 11, 14, 22, 13, 20, DateTimeKind.Utc), Options)
            .ShouldBe("1700000000");
    }

    [Test]
    public void RoundTrips_InsideAnObject()
    {
        var original = new Holder { At = new DateTime(2026, 10, 30, 8, 30, 0, DateTimeKind.Utc) };

        var json = JsonSerializer.Serialize(original, Options);
        var back = JsonSerializer.Deserialize<Holder>(json, Options);

        back!.At.ShouldBe(original.At);
    }

    [Test]
    public void Deserialize_RejectsNegativeSeconds()
    {
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<DateTime>("-5", Options));
    }

    private sealed class Holder
    {
        public DateTime At { get; set; }
    }
}
