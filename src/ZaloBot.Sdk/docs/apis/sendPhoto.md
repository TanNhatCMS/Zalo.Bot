<!-- Source: https://bot.zapps.me/docs/apis/sendPhoto/ (scraped 2026-09-14; upstream "Cập nhật lần cuối" ở đầu trang) -->

Cập nhật lần cuối: 16/12/2025

API cho phép Bot của bạn gửi tin nhắn hình ảnh đến người dùng hoặc các cuộc trò chuyện.

- URL: https://bot-api.zaloplatforms.com/bot${BOT_TOKEN}/sendPhoto
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
| `photo` | String | true | Đường dẫn hình ảnh sẽ được gửi |
| `caption` | String | false | Nội dung văn bản của tin nhắn sẽ được gửi kèm, với độ dài từ 1 đến 2000 ký tự |

### Sample response

```
{
  "ok": true,
  "result": {
    "message_id": "82599fa32f56d00e8941",
    "date": 1749632637199
  }
}

```
