<!-- Source: https://bot.zapps.me/docs/apis/sendChatAction/ (scraped 2026-09-14; upstream "Cập nhật lần cuối" ở đầu trang) -->

Cập nhật lần cuối: 16/12/2025

API cho phép Bot hiển thị một trạng thái tạm thời trong cuộc trò chuyện, chẳng hạn như đang soạn tin nhắn hoặc đang gửi ảnh.

- URL: https://bot-api.zaloplatforms.com/bot${BOT_TOKEN}/sendChatAction
- 

- Method: POST
- 

- Response Type: application/json
- 

#### Sample code

### Parameters

| Trường | Kiểu dữ liệu | Bắt buộc | Mô tả |
|---|---|---|---|
| `chat_id` | String | true | ID của người nhận hoặc cuộc trò chuyện |
| `action` | String | true | Loại hành động mà bot sẽ phát đi. Các loại hành động có sẵn bao gồm: `typing`: Cho tin nhắn văn bản.`upload_photo`: Cho ảnh (Sắp ra mắt). |

### Sample response

```
{
  "ok": true
}

```
