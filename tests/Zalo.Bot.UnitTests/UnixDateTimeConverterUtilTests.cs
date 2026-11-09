using System.Text;
using System.Text.Json;
using Zalo.Bot.Serialization;

namespace Zalo.Bot.UnitTests;

public class UnixDateTimeConverterUtilTests
{
    private static DateTime ReadSeconds(long seconds)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(seconds.ToString()));
        reader.Read();
        return UnixDateTimeConverterUtil.Read(ref reader, typeof(DateTime));
    }

    private static string Write(DateTime value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
            UnixDateTimeConverterUtil.Write(writer, value);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    [Test]
    public void Read_ConvertsUnixSecondsToUtc()
    {
        var value = ReadSeconds(1_700_000_000);

        value.ShouldBe(new DateTime(2023, 11, 14, 22, 13, 20, DateTimeKind.Utc));
        value.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Test]
    public void Read_TreatsZeroAsDefault()
    {
        ReadSeconds(0).ShouldBe(default);
    }

    [Test]
    public void Read_RejectsDatesBeforeTheEpoch()
    {
        Should.Throw<JsonException>(() => ReadSeconds(-1));
    }

    [Test]
    public void Write_ConvertsUtcToUnixSeconds()
    {
        Write(new DateTime(2023, 11, 14, 22, 13, 20, DateTimeKind.Utc)).ShouldBe("1700000000");
    }

    [Test]
    public void Write_ConvertsLocalTimeThroughUtc()
    {
        var local = new DateTime(2023, 11, 14, 22, 13, 20, DateTimeKind.Utc).ToLocalTime();

        Write(local).ShouldBe("1700000000");
    }

    [Test]
    public void Write_WritesZeroForDefault()
    {
        Write(default).ShouldBe("0");
    }

    [Test]
    public void Write_RejectsDatesBeforeTheEpoch()
    {
        Should.Throw<JsonException>(() => Write(new DateTime(1960, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void RoundTrips()
    {
        var original = new DateTime(2026, 10, 30, 8, 30, 0, DateTimeKind.Utc);

        ReadSeconds(long.Parse(Write(original))).ShouldBe(original);
    }
}
