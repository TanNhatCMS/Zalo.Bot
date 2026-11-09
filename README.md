# .NET Client for Zalo Bot API

[![NuGet](https://img.shields.io/nuget/vpre/ZaloBot.Sdk.svg?style=flat-square&label=ZaloBot.Sdk&color=0068ff)](https://www.nuget.org/packages/ZaloBot.Sdk)
[![Downloads](https://img.shields.io/nuget/dt/ZaloBot.Sdk.svg?style=flat-square&label=Downloads)](https://www.nuget.org/packages/ZaloBot.Sdk)
[![Zalo Bot API](https://img.shields.io/badge/Zalo_Bot_API-11_methods-0068ff.svg?style=flat-square)](https://bot.zapps.me/docs)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512bd4.svg?style=flat-square)](https://dotnet.microsoft.com/)
[![Build](https://img.shields.io/github/actions/workflow/status/TanNhatCMS/Zalo.Bot/nuget.yml?branch=main&style=flat-square&label=build)](https://github.com/TanNhatCMS/Zalo.Bot/actions/workflows/nuget.yml)
[![License](https://img.shields.io/github/license/TanNhatCMS/Zalo.Bot?style=flat-square&label=License)](LICENSE)
[![Coverage](https://img.shields.io/github/actions/workflow/status/TanNhatCMS/Zalo.Bot/coverage.yml?branch=main&style=flat-square&label=coverage)](https://github.com/TanNhatCMS/Zalo.Bot/actions/workflows/coverage.yml)

**ZaloBot.Sdk** là thư viện .NET để gọi [Zalo Bot API] — giao diện HTTP cho lập trình viên xây dựng chatbot trên [Zalo].

Xem 👉 [_Giới thiệu Zalo Bot_] 👈 để hiểu Zalo Bot là gì và làm được những gì.

Thư viện được chuyển thể từ [Telegram.Bot] (MIT): giữ lại phong cách API quen thuộc (`client.SendMessageAsync(...)`,
`StartReceiving`, `IUpdateHandler`), rút gọn về đúng **11 method** mà Zalo Bot API cung cấp, và bổ sung những phần
cần cho production: phân loại mã lỗi chính thức, retry/backoff, và **access token không bao giờ lọt ra exception hay log**.

| Package | Tài liệu SDK | Tài liệu Zalo Bot API | Báo lỗi |
|:-------:|:------------:|:---------------------:|:-------:|
| [nuget.org][NuGet package] | [src/ZaloBot.Sdk/README.md] | [bot.zapps.me/docs][Zalo Bot API] | [Issues] |
| Feed chính thức (bản `.nupkg` cũng có trên [GitHub Releases]) | Wire contract, polling, lỗi & retry | Tài liệu chính thức (bản offline trong [`docs/`][offline docs]) | Báo lỗi, đề xuất tính năng |

## 🔨 Bắt đầu

### Cài đặt

```bash
dotnet add package ZaloBot.Sdk
```

Mỗi phiên bản cũng có `.nupkg`/`.snupkg` đính kèm trên [GitHub Releases] — dùng khi cần feed cục bộ
(thêm `<add key="local" value="packages" />` vào `nuget.config`).

### Gửi tin nhắn đầu tiên

```csharp
using ZaloBot.Sdk;

var bot = new ZaloBotClient("YOUR_ACCESS_TOKEN");

var me = await bot.GetMeAsync();
Console.WriteLine($"Xin chào, mình là {me.DisplayName}");

await bot.SendMessageAsync(chatId: "5128471675483426707", text: "Xin chào từ .NET!");
```

### Echo bot (long polling)

```csharp
using ZaloBot.Sdk;
using ZaloBot.Sdk.Types;

using var cts = new CancellationTokenSource();
var bot = new ZaloBotClient("YOUR_ACCESS_TOKEN");

// Đăng ký OnUpdate tự khởi động vòng getUpdates; huỷ đăng ký cuối cùng sẽ dừng nó.
bot.OnUpdate += async update =>
{
    if (update is { ContentType: ContentType.Text, ChatId: { } chatId, Text: { } text })
        await bot.SendMessageAsync(chatId, $"Bạn vừa nói: {text}");
};
bot.OnError += (exception, source) =>
{
    Console.WriteLine($"[{source}] {exception.Message}");
    return Task.CompletedTask;
};

Console.WriteLine("Bot đang chạy, nhấn Enter để dừng...");
Console.ReadLine();
```

> Webhook và getUpdates **loại trừ nhau** trên Zalo: gọi `DeleteWebhookAsync()` trước khi polling.
> Production nên dùng webhook — parse body JSON của webhook bằng `ZaloBotClient.ParseWebhookUpdate(JsonElement)`.

## ✨ Tính năng

- **Đủ 11 method** của Zalo Bot API: `getMe`, `getUpdates`, `sendMessage` (kèm `parse_mode` và `text_styles`),
  `sendPhoto`, `sendSticker`, `sendVoice`, `sendChatAction`, `setWebhook`, `testWebhook`, `deleteWebhook`, `getWebhookInfo`.
- **Polling dựng sẵn** theo mô hình Telegram.Bot: `StartReceiving`, `ReceiveAsync`, `IUpdateHandler`,
  `DropPendingUpdatesAsync`, hoặc sự kiện `OnUpdate`/`OnError`.
- **Phân loại lỗi chính thức**: 401 → `ZaloBotTokenException`, 408/429 → retry có backoff (`ZaloBotRetryableException`),
  403 → retry đúng một lần, lỗi mạng → `ZaloBotNetworkException`.
- **Vệ sinh token**: token chỉ nằm trong URL request; mọi message exception và trích đoạn body đều đi qua `TokenSanitizer`.
- **Parse phòng thủ**: payload lạ không bao giờ ném lỗi — update không nhận ra thành `ContentType.Unknown`.
- **Không phụ thuộc NuGet nào**: chỉ dùng `System.Text.Json` có sẵn trong shared framework.

## 🚧 Nền tảng hỗ trợ

Thư viện target **.NET 10** (`net10.0`).

## ✅ Độ chính xác & kiểm thử

Mọi method được kiểm thử bằng unit test (NUnit + Shouldly) với `FakeHttpMessageHandler` — kiểm tra URL, verb,
JSON body, ánh xạ kết quả và từng nhánh của bảng mã lỗi. Tên method API được ghim theo tài liệu chính thức
(`ApiConventionsTests`), nên test cũng là ví dụ dùng từng method.

Workflow [Test Coverage] chạy trên mỗi push/PR và đăng báo cáo vào Job Summary.

```bash
dotnet build ZaloBot.slnx -c Release
dotnet test ZaloBot.slnx -c Release --no-build
```

## 📦 Phát hành

Workflow [`nuget.yml`][Build & Pack] build, test và pack trên mỗi push/PR (`X.Y.Z-ci.<run>`, artifact `nuget`).
Push tag `vX.Y.Z` sẽ pack phiên bản `X.Y.Z`, tạo GitHub Release kèm `.nupkg`/`.snupkg`, rồi publish lên nuget.org
bằng [Trusted Publishing] — GitHub cấp OIDC token, nuget.org đổi lấy API key tạm 1 giờ, **không lưu API key nào trong repo**.

```bash
git tag v1.0.1 && git push origin v1.0.1
```

Thiết lập một lần:

1. **nuget.org** → tên tài khoản → **Trusted Publishing** → thêm policy:
   Repository Owner `TanNhatCMS`, Repository `Zalo.Bot`, Workflow File `nuget.yml`, Environment `nuget`.
2. **GitHub** → Settings → Secrets and variables → Actions → **Variables** → `NUGET_USER` = tên profile nuget.org
   (không phải email). Thiếu biến này thì job `publish-nuget` được bỏ qua.
3. (Tuỳ chọn) Settings → Environments → `nuget`: thêm *Required reviewers* để duyệt tay trước mỗi lần publish.

## 🗂 Tham khảo

- [Tài liệu SDK chi tiết][src/ZaloBot.Sdk/README.md] — wire contract, rich text, polling, lỗi & retry, cách thêm method mới
- [Zalo Bot API] — tài liệu chính thức ([bản offline][offline docs])
- [Telegram.Bot] — thư viện gốc mà SDK được chuyển thể từ đó

<br/>

> [!NOTE]
> Phát hành theo giấy phép [MIT](LICENSE). Một phần mã nguồn chuyển thể từ [Telegram.Bot] theo giấy phép MIT — xem [`LICENSE-Telegram.Bot.txt`](src/ZaloBot.Sdk/LICENSE-Telegram.Bot.txt).

<!-- ---- -->

[Zalo Bot API]: https://bot.zapps.me/docs
[Zalo]: https://zalo.me/
[_Giới thiệu Zalo Bot_]: https://bot.zapps.me/docs
[Telegram.Bot]: https://github.com/TelegramBots/Telegram.Bot
[GitHub Releases]: https://github.com/TanNhatCMS/Zalo.Bot/releases
[NuGet package]: https://www.nuget.org/packages/ZaloBot.Sdk
[Trusted Publishing]: https://learn.microsoft.com/nuget/nuget-org/trusted-publishing
[Issues]: https://github.com/TanNhatCMS/Zalo.Bot/issues
[src/ZaloBot.Sdk/README.md]: src/ZaloBot.Sdk/README.md
[offline docs]: src/ZaloBot.Sdk/docs
[Test Coverage]: https://github.com/TanNhatCMS/Zalo.Bot/actions/workflows/coverage.yml
[Build & Pack]: .github/workflows/nuget.yml
