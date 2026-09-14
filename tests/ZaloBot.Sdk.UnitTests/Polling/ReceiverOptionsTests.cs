using Shouldly;
using ZaloBot.Sdk.Polling;
using ZaloBot.Sdk.UnitTests;

namespace ZaloBot.Sdk.UnitTests.Polling;

/// <summary>Validation của ReceiverOptions (giới hạn Limit 1–100 như reference ReceiverOptions.cs).</summary>
public class ReceiverOptionsTests
{
    [Test]
    public void Should_ThrowLocally_When_LimitOutOfRange()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new ReceiverOptions { Limit = 0 });
        Should.Throw<ArgumentOutOfRangeException>(() => new ReceiverOptions { Limit = 101 });
    }

    [Test]
    public void Should_AcceptBoundaryLimits_When_LimitIs1Or100()
    {
        new ReceiverOptions { Limit = 1 }.Limit.ShouldBe(1);
        new ReceiverOptions { Limit = 100 }.Limit.ShouldBe(100);
    }

    [Test]
    public void Should_DefaultToLongPoll25_AndLimit100_When_OptionsFresh()
    {
        var options = new ReceiverOptions();

        options.Limit.ShouldBe(100);
        options.TimeoutSeconds.ShouldBe(25); // long-poll mặc định, 0 = short polling
        options.DropPendingUpdates.ShouldBeFalse();
        options.Offset.ShouldBeNull();
    }
}
