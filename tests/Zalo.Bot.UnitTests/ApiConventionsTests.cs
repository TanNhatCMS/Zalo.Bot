using Shouldly;
using Zalo.Bot.Requests;

namespace Zalo.Bot.UnitTests;

/// <summary>Chốt quy ước gọi API theo docs bot.zapps.me/docs/call-api:
/// method name trên URL phân biệt HOA THƯỜNG (từng tên dưới đây đã được verify HTTP 200 tại
/// bot.zapps.me/docs/apis/&lt;name&gt;/, sai casing trả lỗi), GET cho API đọc, POST cho API ghi.</summary>
public class ApiConventionsTests
{
    [Test]
    public void Should_UseDocumentedMethodNames_When_RequestsCreated()
    {
        // Case-sensitive per docs/call-api — đổi casing nào cũng phải đối chiếu lại docs/apis/<name>/
        new GetMeRequest().MethodName.ShouldBe("getMe");
        new GetUpdatesRequest().MethodName.ShouldBe("getUpdates");
        new SendMessageRequest { ChatId = "1", Text = "hi" }.MethodName.ShouldBe("sendMessage");
        new SendPhotoRequest { ChatId = "1", Photo = "u" }.MethodName.ShouldBe("sendPhoto");
        new SendStickerRequest { ChatId = "1", Sticker = "s" }.MethodName.ShouldBe("sendSticker");
        new SendVoiceRequest { ChatId = "1", VoiceUrl = "u" }.MethodName.ShouldBe("sendVoice");
        new SendChatActionRequest { ChatId = "1", Action = ChatAction.Typing }.MethodName.ShouldBe("sendChatAction");
        new SetWebhookRequest { Url = "https://app.example.com/hook" }.MethodName.ShouldBe("setWebhook");
        new TestWebhookRequest().MethodName.ShouldBe("testWebhook");
        new DeleteWebhookRequest().MethodName.ShouldBe("deleteWebhook");
        new GetWebhookInfoRequest().MethodName.ShouldBe("getWebhookInfo");
    }

    [Test]
    public void Should_UseGetForReadOnlyApis_AndPostForTheRest()
    {
        // docs/call-api: hỗ trợ cả GET+POST cho mọi API, khuyến nghị GET khi truy xuất dữ liệu
        new GetMeRequest().HttpMethod.ShouldBe(HttpMethod.Get);
        new GetWebhookInfoRequest().HttpMethod.ShouldBe(HttpMethod.Get);
        new GetMeRequest().ToHttpContent().ShouldBeNull();
        new GetWebhookInfoRequest().ToHttpContent().ShouldBeNull();

        // getUpdates (long-poll, trang docs ghi Method: POST) và các API ghi giữ POST
        new GetUpdatesRequest().HttpMethod.ShouldBe(HttpMethod.Post);
        new SendMessageRequest { ChatId = "1", Text = "hi" }.HttpMethod.ShouldBe(HttpMethod.Post);
        new SendPhotoRequest { ChatId = "1", Photo = "u" }.HttpMethod.ShouldBe(HttpMethod.Post);
        new SendStickerRequest { ChatId = "1", Sticker = "s" }.HttpMethod.ShouldBe(HttpMethod.Post);
        new SendVoiceRequest { ChatId = "1", VoiceUrl = "u" }.HttpMethod.ShouldBe(HttpMethod.Post);
        new SendChatActionRequest { ChatId = "1", Action = ChatAction.Typing }.HttpMethod.ShouldBe(HttpMethod.Post);
        new SetWebhookRequest { Url = "https://app.example.com/hook" }.HttpMethod.ShouldBe(HttpMethod.Post);
        new TestWebhookRequest().HttpMethod.ShouldBe(HttpMethod.Post);
        new DeleteWebhookRequest().HttpMethod.ShouldBe(HttpMethod.Post);
    }
}
