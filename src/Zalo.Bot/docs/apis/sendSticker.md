<!-- Source: https://bot.zapps.me/docs/apis/sendSticker/ (scraped 2026-09-14; upstream "Cập nhật lần cuối" ở đầu trang) -->

Cập nhật lần cuối: 16/12/2025

API cho phép Bot của bạn gửi tin nhắn Sticker đến người dùng hoặc các cuộc trò chuyện.

- URL: https://bot-api.zaloplatforms.com/bot${BOT_TOKEN}/sendSticker
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
| `sticker` | String | true | Truyền vào stricker lấy từ nguồn: [https://stickers.zaloapp.com/](https://stickers.zaloapp.com/). Vui lòng xem video hướng dẫn tại đây: [https://vimeo.com/649330161](https://vimeo.com/649330161) |

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
