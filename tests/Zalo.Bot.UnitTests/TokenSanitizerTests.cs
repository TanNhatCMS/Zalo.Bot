using Zalo.Bot.Internal;

namespace Zalo.Bot.UnitTests;

/// <summary>FR-009 — access token không bao giờ xuất hiện trong thông báo lỗi hay trích đoạn body.</summary>
public class TokenSanitizerTests
{
    private const string Token = "123456:SECRET-TOKEN";

    [TestCase(null)]
    [TestCase("")]
    public void Sanitize_ReturnsNull_ForEmptyText(string? text)
    {
        TokenSanitizer.Sanitize(text, Token).ShouldBeNull();
    }

    [Test]
    public void Sanitize_MasksEveryOccurrenceOfTheToken()
    {
        var result = TokenSanitizer.Sanitize($"GET /bot{Token}/getMe failed for {Token}", Token);

        result.ShouldBe("GET /bot***/getMe failed for ***");
    }

    [Test]
    public void Sanitize_LeavesTextUntouched_WhenTokenIsEmpty()
    {
        TokenSanitizer.Sanitize("some body", string.Empty).ShouldBe("some body");
    }

    [Test]
    public void Sanitize_IsCaseSensitive()
    {
        TokenSanitizer.Sanitize("123456:secret-token", Token).ShouldBe("123456:secret-token");
    }

    [Test]
    public void Sanitize_TruncatesLongTextToTheExcerptLimit()
    {
        var result = TokenSanitizer.Sanitize(new string('x', 500), Token)!;

        result.ShouldBe(new string('x', TokenSanitizer.MaxExcerptLength) + "…");
    }

    [Test]
    public void Truncate_KeepsShortTextAndCutsLongText()
    {
        TokenSanitizer.Truncate("abc", 3).ShouldBe("abc");
        TokenSanitizer.Truncate("abcd", 3).ShouldBe("abc…");
        TokenSanitizer.Truncate(string.Empty, 3).ShouldBe(string.Empty);
    }

    [Test]
    public void ContainsToken_DetectsTheToken()
    {
        TokenSanitizer.ContainsToken($"url={Token}", Token).ShouldBeTrue();
        TokenSanitizer.ContainsToken("clean", Token).ShouldBeFalse();
    }

    [TestCase(null, "tok")]
    [TestCase("", "tok")]
    [TestCase("anything", "")]
    public void ContainsToken_IsFalse_ForEmptyTextOrToken(string? text, string token)
    {
        TokenSanitizer.ContainsToken(text, token).ShouldBeFalse();
    }

    [Test]
    public void BuildSafeMessage_MasksTheTokenInMessageAndExcerpt()
    {
        var message = TokenSanitizer.BuildSafeMessage($"call to {Token} failed", Token, $"body mentions {Token}");

        message.ShouldBe("call to *** failed Body excerpt: body mentions ***");
        message.ShouldNotContain(Token);
    }

    [Test]
    public void BuildSafeMessage_OmitsTheExcerpt_WhenThereIsNone()
    {
        TokenSanitizer.BuildSafeMessage("plain failure", Token).ShouldBe("plain failure");
        TokenSanitizer.BuildSafeMessage("plain failure", Token, string.Empty).ShouldBe("plain failure");
    }

    [Test]
    public void BuildSafeMessage_FallsBackToTheMessage_WhenTheMessageIsEmpty()
    {
        TokenSanitizer.BuildSafeMessage(string.Empty, Token).ShouldBe(string.Empty);
    }
}
