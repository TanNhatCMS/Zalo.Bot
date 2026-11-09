# Zalo Bot API — offline docs (scraped)

Bản sao offline toàn bộ tài liệu chính thức tại <https://bot.zapps.me/docs/>
(scraped 2026-09-14; mỗi file ghi URL gốc ở dòng comment đầu — upstream "Cập nhật lần cuối" nằm ngay sau đó trong thân trang).

Dành cho AI + dev khi làm việc với `Zalo.Bot` mà không cần mạng. Nội dung là
verbatim tiếng Việt từ trang chính thức (tiêu đề, bảng tham số, sample request/response,
mã lỗi, mã outcome webhook).

## Mục lục

### Cơ bản

| File | Nội dung |
| ---- | -------- |
| [index.md](index.md) | Giới thiệu Zalo Bot Platform |
| [create-bot.md](create-bot.md) | Tạo bot qua Zalo Bot Creator (OA → Tạo bot → nhận Token qua tin nhắn Zalo) |
| [authorize.md](authorize.md) | Bot Token: dạng `12345689:abc-xyz`, không hết hạn tới khi reset |
| [call-api.md](call-api.md) | Quy ước gọi API: URL `https://bot-api.zaloplatforms.com/bot{token}/{method}`, GET/POST, truyền tham số, wrapper response `{ok, result, description, error_code}` |
| [error-code.md](error-code.md) | Bảng mã lỗi HTTP: 400/401/403/404/408/429 |

### Nhận sự kiện

| File | Nội dung |
| ---- | -------- |
| [webhook.md](webhook.md) | Đặc tả webhook: header `X-Bot-Api-Secret-Token`, payload `{ok, result}`, `event_name`, bảng `message`, sample payload đầy đủ |
| [build-your-bot.md](build-your-bot.md) | Hướng dẫn chatbot cơ bản với getUpdates (polling) — cho dev/local |
| [build-your-bot-with-webhook.md](build-your-bot-with-webhook.md) | Hướng dẫn chatbot với webhook — cho production |
| [build-bot-interaction-with-group.md](build-bot-interaction-with-group.md) | Bot trong nhóm (Beta): mời bot, quote/mention, `chat.id` = id nhóm |
| [build-personal-assistant-with-open-claw.md](build-personal-assistant-with-open-claw.md) | Plugin OpenClaw (trợ lý AI cá nhân) |

### API methods (11)

| File | Method | Ghi chú |
| ---- | ------ | ------- |
| [apis/getMe.md](apis/getMe.md) | `getMe` | Kiểm tra token, trả `{id, account_name, account_type, can_join_groups}` |
| [apis/getUpdates.md](apis/getUpdates.md) | `getUpdates` | Long polling; không chạy khi đã set webhook; `timeout` (s, default 30) |
| [apis/setWebhook.md](apis/setWebhook.md) | `setWebhook` | `url` HTTPS công khai + `secret_token` 8–256 ký tự; response kèm `verification` (outcome như testWebhook) |
| [apis/deleteWebhook.md](apis/deleteWebhook.md) | `deleteWebhook` | Xóa webhook để quay lại getUpdates |
| [apis/getWebhookInfo.md](apis/getWebhookInfo.md) | `getWebhookInfo` | Trạng thái webhook hiện tại |
| [apis/testWebhook.md](apis/testWebhook.md) | `testWebhook` | Tự chẩn đoán webhook; bảng `outcome` (`webhook.ok`, `webhook.http.403`, `webhook.err.*`...); rate-limit mỗi ngày (errorCode 426) |
| [apis/sendMessage.md](apis/sendMessage.md) | `sendMessage` | `chat_id`, `text` 1–2000, `parse_mode` (markdown/html), `text_styles` (style runs, offset UTF-16) |
| [apis/sendPhoto.md](apis/sendPhoto.md) | `sendPhoto` | `photo` URL, `caption` tùy chọn |
| [apis/sendSticker.md](apis/sendSticker.md) | `sendSticker` | `sticker` id từ https://stickers.zaloapp.com/ |
| [apis/sendVoice.md](apis/sendVoice.md) | `sendVoice` | `voice_url` phải `.aac`; **chỉ chat 1-1, không hỗ trợ nhóm** |
| [apis/sendChatAction.md](apis/sendChatAction.md) | `sendChatAction` | `action`: `typing`, `upload_photo` (sắp ra mắt) |

### Khác

| File | Nội dung |
| ---- | -------- |
| [terms.md](terms.md) | Điều khoản sử dụng Zalo Bot Platform (bản full dài, scraped verbatim) |

## Đối chiếu nhanh với SDK

Mỗi API method trên có request type tương ứng trong `Zalo.Bot/Requests/`
(`SendMessageRequest.cs` → `apis/sendMessage.md`, ...) và extension method trong
`ZaloBotClientExtensions.ApiMethods.cs` — bảng mapping đầy đủ ở [../README.md](../README.md#api-methods-all-of-them).
