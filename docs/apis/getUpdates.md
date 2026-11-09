<!-- Source: https://bot.zapps.me/docs/apis/getUpdates/ (scraped 2026-11-09; upstream "Cập nhật lần cuối" ở đầu trang) -->

Cập nhật lần cuối: 22/10/2026

Hệ thống Zalo hỗ trợ 2 cách **độc lập và loại trừ lẫn nhau** để bot của bạn nhận được các tin nhắn mới:

- Sử dụng phương thức getUpdates, dựa trên cơ chế long polling.

- Sử dụng [Webhook](/docs/webhook/).

Lưu ý

Phương thức getUpdates sẽ không hoạt động nếu bạn đã thiết lập Webhook trước đó (khi đó, vui lòng sử dụng phương thức [deleteWebhook](/docs/apis/deleteWebhook/) để xóa cấu hình Webhook trước khi sử dụng API này).
Chỉ nên sử dụng API này để chạy local, trong môi trường development, thử nghiệm, với môi trường production, bạn nên thiết lập Webhook để tránh bỏ lỡ event.

- URL: https://bot-api.zaloplatforms.com/bot${BOT_TOKEN}/getUpdates

- Method: POST

- Response Type: application/json

#### Sample code

##### Nodejs
```
const axios = require("axios");

const entrypoint = `https://bot-api.zaloplatforms.com/bot${BOT_TOKEN}/getUpdates`;
const response = await axios.post(entrypoint, {
  timeout: 30
});
```
##### curl
```
curl -X POST "https://bot-api.zaloplatforms.com/bot<BOT_TOKEN>/getUpdates" \
  -H "Content-Type: application/json" \
  -d '{ "timeout": 30 }'
```

### Parameters

| Trường | Kiểu dữ liệu | Bắt buộc | Mô tả |
|---|---|---|---|
| `timeout` | String | false | Thời gian timeout của HTTP Request tính theo giây. Mặc định hệ thống sẽ lấy thời gian timeout là 30 giây. |

### Sample response

Trường `result` có cấu trúc giống body sự kiện [Webhook](../webhook.md).
```
{
  "ok": true,
  "result": {
    "event_name": "message.text.received",
    "message": {
      "message_id": "2d758cb5e222177a4e35",
      "date": 1750316131602,
      "message_type": "CHAT_TEXT",
      "from": {
        "id": "6ede9afa66b88fe6d6a9",
        "display_name": "Ted",
        "is_bot": false
      },
      "chat": {
        "id": "6ede9afa66b88fe6d6a9",
        "chat_type": "PRIVATE"
      },
      "text": "Xin chào"
    }
  }
}
```
mẹo

Xem hướng dẫn xây dựng Chatbot cơ bản với cơ chế getUpdates [tại đây](/docs/build-your-bot/).
