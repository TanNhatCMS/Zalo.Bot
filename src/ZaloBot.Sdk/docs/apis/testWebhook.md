<!-- Source: https://bot.zapps.me/docs/apis/testWebhook/ (scraped 2026-09-14; upstream "Cập nhật lần cuối" ở đầu trang) -->

Cập nhật lần cuối: 7/9/2026

API cho phép kiểm tra ngay lập tức Webhook URL hiện tại của Bot có nhận được request từ Zalo hay không, giúp bạn tự chẩn đoán lỗi mà không cần liên hệ hỗ trợ.

- URL: https://bot-api.zaloplatforms.com/bot${BOT_TOKEN}/testWebhook
- 

- Method: POST
- 

- Response Type: application/json
- 

mẹo

Nên gọi API này sau khi [setWebhook](/docs/apis/setWebhook/) hoặc bất cứ khi nào nghi ngờ Bot không nhận được sự kiện, trước khi liên hệ hỗ trợ.

#### Sample code

### Parameters

Không yêu cầu tham số đi kèm.

### Sample response

Khi Webhook URL của bạn phản hồi thành công:

```
{
  "ok": true,
  "result": {
    "ok": true,
    "url": "https://your-webhookurl.com",
    "outcome": "webhook.ok",
    "hint": "Your endpoint responded successfully."
  }
}

```

Khi Webhook URL của bạn từ chối request (ví dụ WAF/CDN chặn với mã 403):

```
{
  "ok": true,
  "result": {
    "ok": false,
    "url": "https://your-webhookurl.com",
    "outcome": "webhook.http.403",
    "hint": "Your server or CDN rejected the request with 403. Check WAF / Cloudflare rules, any IP allowlist, and that the User-Agent \"Java/<version>\" is permitted."
  }
}

```

ghi chú

Trường ok ở ngoài cùng cho biết yêu cầu gọi API có thành công hay không. Trường result.ok mới cho biết Webhook URL của bạn có phản hồi hợp lệ (2xx) hay không — đây là trường bạn cần kiểm tra để biết Webhook đã hoạt động đúng chưa.

#### Result

| Trường | Kiểu dữ liệu | Mô tả |
|---|---|---|
| `ok` | Boolean | `true` nếu Webhook URL phản hồi mã 2xx. |
| `url` | String | Webhook URL hiện đang được kiểm tra. |
| `outcome` | String | Mã phân loại kết quả, xem bảng bên dưới. |
| `hint` | String | Gợi ý cách xử lý, tương ứng với `outcome`. |

#### Outcome

| `outcome` | Ý nghĩa |
|---|---|
| `webhook.ok` | Webhook URL phản hồi thành công (2xx). |
| `webhook.http.403` | Webhook URL từ chối request với mã 403. Thường do WAF/CDN, IP allowlist, hoặc chặn theo `User-Agent`. |
| `webhook.http.404` | Webhook URL trả về 404. Endpoint chưa được deploy hoặc không nhận method POST. |
| `webhook.http.5xx` | Server của bạn gặp lỗi khi xử lý request. |
| `webhook.http.other` | Server trả về mã khác không phải 2xx (ví dụ redirect 3xx — Zalo Bot Platform không tự động theo redirect, vui lòng đăng ký URL cuối cùng). |
| `webhook.err.tls` | Lỗi bắt tay TLS. Chuỗi chứng chỉ (certificate chain) không đầy đủ, hết hạn, hoặc do CA nội bộ cấp. |
| `webhook.err.dns` | Không thể phân giải được hostname từ Internet công khai. |
| `webhook.err.timeout` | Webhook URL không phản hồi trong thời gian cho phép. Hãy trả về 2xx ngay và xử lý logic bất đồng bộ. |
| `webhook.err.conn` / `webhook.err.proxy` | Không thể kết nối tới Webhook URL từ hệ thống Zalo Bot Platform. |
| `webhook.err.blocked` | Webhook URL không trỏ tới một địa chỉ được phép: địa chỉ không truy cập được từ Internet công khai (ví dụ `localhost`, IP nội bộ), hoặc tên miền nội bộ của Zalo. Xem chi tiết tại [setWebhook](/docs/apis/setWebhook/). |
| `webhook.err.other` | Lỗi không xác định. |

cảnh báo

Để tránh làm quá tải Webhook URL của bạn, API này bị giới hạn số lần gọi mỗi ngày cho mỗi Bot. Khi vượt quá giới hạn, response sẽ trả về "ok": false kèm "errorCode": 426.
