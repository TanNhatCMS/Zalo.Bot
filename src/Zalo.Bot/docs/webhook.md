<!-- Source: https://bot.zapps.me/docs/webhook/ (scraped 2026-11-09; upstream "Cập nhật lần cuối" ở đầu trang) -->

Cập nhật lần cuối: 22/10/2026

Zalo sẽ gửi các HTTP Request (phương thức POST) đến Webhook URL bạn đã thiết lập khi có tương tác từ người dùng hoặc các thay đổi liên quan tới Bot.

Tất cả các request sẽ được gửi kèm headers X-Bot-Api-Secret-Token với giá trị là secret_token bạn đã thiết lập trước đó, vui lòng xác thực lại token này trước khi xử lý để đảm bảo yêu cầu hợp lệ.

- URL: https://your-webhookurl.com
- Method: POST
- Headers: X-Bot-Api-Secret-Token
- Request Type: application/json

> **mẹo**
>
> Nên thiết lập Webhook URL với domain sử dụng HTTPS để tăng tính bảo mật cho hệ thống của bạn. Xem hướng dẫn thiết lập tại [setWebhook](apis/setWebhook.md).

> **Không nhận được sự kiện nào?**
>
> Trước khi liên hệ hỗ trợ, hãy gọi [testWebhook](apis/testWebhook.md) để tự kiểm tra Webhook URL của bạn có đang truy cập được không, và xem gợi ý xử lý tương ứng.

#### Sample code

src/backend.ts

```diff
    app.use(express.json());
    const WEBHOOK_SECRET_TOKEN = 'your-secret-token';

+   .post("/webhooks", async (req, res) => {
+     const secretToken = req.headers["x-bot-api-secret-token"];
+     if (secretToken !== WEBHOOK_SECRET_TOKEN) {
+       return res.status(403).json({ message: "Unauthorized" });
+     }
+     let body = req.body;
+     // Handle your logic at here
+     res.json({ message: "Success" });
+   })
    .listen(port, () => {
      console.log(`Server running at http://localhost:${port}`);
    });
```

### Parameters

Body của request là một JSON object mô tả sự kiện, gồm các trường thông tin sau:

| Trường | Kiểu dữ liệu | Bắt buộc | Mô tả |
|---|---|---|---|
| `event_name` | String | true | Tên sự kiện, sẽ nhận một trong các giá trị sau:<br>`message.text.received`: nhận được một tin nhắn văn bản.<br>`message.image.received`: nhận được một tin nhắn dạng hình ảnh.<br>`message.sticker.received`: nhận được một tin nhắn Sticker.<br>`message.voice.received`: nhận được một tin nhắn thoại.<br>`message.unsupported.received`: nhận được một tin nhắn chưa hỗ trợ xử lý. |
| `message` | JSON object | false | Nếu là sự kiện có tin nhắn mới, bạn sẽ nhận được thông tin chi tiết về message. Tùy theo từng loại tin nhắn sẽ có thêm các trường thông tin tương ứng. Tham khảo bảng đặc tả bên dưới |

### Sample request body

Sự kiện `message.text.received`:

```json
{
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
```

Sự kiện `message.image.received`:

```json
{
  "event_name": "message.image.received",
  "message": {
    "message_id": "2d758cb5e222177a4e36",
    "date": 1750316135240,
    "message_type": "CHAT_PHOTO",
    "from": {
      "id": "6ede9afa66b88fe6d6a9",
      "display_name": "Ted",
      "is_bot": false
    },
    "chat": {
      "id": "6ede9afa66b88fe6d6a9",
      "chat_type": "PRIVATE"
    },
    "photo_url": "https://example.com/photo.jpg",
    "caption": "Ảnh sản phẩm"
  }
}
```

#### Message

| Trường | Kiểu dữ liệu | Bắt buộc | Mô tả |
|---|---|---|---|
| `message_id` | String | true | ID của tin nhắn |
| `date` | Long | true | Thời điểm gửi tin nhắn, tính theo mili giây (Unix timestamp) |
| `message_type` | String | false | Loại tin nhắn, nhận một trong các giá trị: `CHAT_TEXT`, `CHAT_PHOTO`, `CHAT_STICKER`, `CHAT_VOICE`. |
| `from` | JSON object | true | Thông tin người gửi tin nhắn, gồm `id`, `display_name` và `is_bot` |
| `chat` | JSON object | true | Thông tin cuộc trò chuyện. Trong đó `chat_type` sẽ là một trong các giá trị:<br>`PRIVATE`: cuộc hội thoại cá nhân.<br>`GROUP`: cuộc hội thoại với nhóm (Beta).<br>Sử dụng `chat.id` để gửi tin nhắn phản hồi tới cuộc trò chuyện. |
| `text` | String | false | Nội dung của tin nhắn văn bản |
| `photo_url` | String | false | Đường dẫn hình ảnh của tin nhắn hình ảnh |
| `caption` | String | false | Nội dung văn bản được gửi kèm tin nhắn hình ảnh |
| `sticker` | String | false | Mã sticker của tin nhắn Sticker |
| `url` | String | false | Đường dẫn hình ảnh của sticker trong tin nhắn Sticker |
| `voice_url` | String | false | Đường dẫn tệp âm thanh của tin nhắn thoại |

> **cảnh báo**
>
> Trường hợp tài khoản người gửi tin nhắn thuộc nhóm đối tượng đặc biệt (bao gồm nhưng không giới hạn: trẻ em, người khuyết tật, người không biết chữ,...), thay vì nhận nội dung tin nhắn, hệ thống của bạn sẽ nhận được sự kiện webhook `message.unsupported.received`, nhằm đảm bảo việc xử lý dữ liệu tuân thủ quy định pháp luật hiện hành.
