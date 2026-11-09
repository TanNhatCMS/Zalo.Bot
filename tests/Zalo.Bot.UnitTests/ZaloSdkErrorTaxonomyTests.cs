namespace Zalo.Bot.UnitTests;

/// <summary>Bảng mã lỗi chính thức của Zalo Bot API → nhóm lỗi và loại exception.</summary>
public class ZaloSdkErrorTaxonomyTests
{
    [TestCase(400, ZaloErrorCategory.NonRetryable)]
    [TestCase(401, ZaloErrorCategory.Token)]
    [TestCase(403, ZaloErrorCategory.ServerRetry)]
    [TestCase(404, ZaloErrorCategory.NonRetryable)]
    [TestCase(408, ZaloErrorCategory.Retryable)]
    [TestCase(429, ZaloErrorCategory.Retryable)]
    [TestCase(500, ZaloErrorCategory.Unknown)]
    [TestCase(0, ZaloErrorCategory.Unknown)]
    [TestCase(-1, ZaloErrorCategory.Unknown)]
    public void CategoryOf_MapsOfficialCodes(int code, ZaloErrorCategory expected)
    {
        ZaloSdkErrorTaxonomy.CategoryOf(code).ShouldBe(expected);
    }

    [TestCase(408, true)]
    [TestCase(429, true)]
    [TestCase(403, false)]
    [TestCase(400, false)]
    [TestCase(401, false)]
    [TestCase(500, false)]
    public void IsRetryable_IsTrueOnlyForTimeoutAndQuota(int code, bool expected)
    {
        ZaloSdkErrorTaxonomy.IsRetryable(code).ShouldBe(expected);
    }

    [TestCase(403, true)]
    [TestCase(408, false)]
    [TestCase(429, false)]
    [TestCase(404, false)]
    public void IsServerRetry_IsTrueOnlyFor403(int code, bool expected)
    {
        ZaloSdkErrorTaxonomy.IsServerRetry(code).ShouldBe(expected);
    }

    [Test]
    public void CreateException_BuildsATokenException_For401()
    {
        var exception = ZaloSdkErrorTaxonomy.CreateException(401, "token expired");

        exception.ShouldBeOfType<ZaloBotTokenException>();
        exception.ErrorCode.ShouldBe(401);
        exception.Message.ShouldContain("token expired");
    }

    [TestCase(408)]
    [TestCase(429)]
    public void CreateException_BuildsARetryableException_ForTimeoutAndQuota(int code)
    {
        var exception = ZaloSdkErrorTaxonomy.CreateException(code, "slow down");

        exception.ShouldBeOfType<ZaloBotRetryableException>();
        exception.ErrorCode.ShouldBe(code);
    }

    [TestCase(400)]
    [TestCase(403)]
    [TestCase(404)]
    [TestCase(500)]
    public void CreateException_BuildsAPlainApiException_ForOtherCodes(int code)
    {
        var exception = ZaloSdkErrorTaxonomy.CreateException(code, "nope");

        exception.GetType().ShouldBe(typeof(ApiRequestException));
        exception.ErrorCode.ShouldBe(code);
    }
}
