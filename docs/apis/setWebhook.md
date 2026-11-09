<!-- Source: https://bot.zapps.me/docs/apis/setWebhook/ (scraped 2026-09-14; upstream "Cập nhật lần cuối" ở đầu trang) -->

Cập nhật lần cuối: 7/9/2026

API cho phép cấu hình Webhook URL cho Bot của bạn.

- URL: https://bot-api.zaloplatforms.com/bot${BOT_TOKEN}/setWebhook
- 

- Method: POST
- 

- Response Type: application/json
- 

cảnh báo

Webhook URL phải là một địa chỉ có thể truy cập được từ Internet công khai. Các địa chỉ như localhost, 127.0.0.1, hoặc IP nội bộ (192.168.x.x, 10.x.x.x,...) sẽ bị từ chối, vì Zalo Bot Platform không thể gửi request đến các địa chỉ này.

Nếu bạn đang phát triển ở local, hãy dùng một dịch vụ tunnel như [ngrok](https://ngrok.com/) hoặc Cloudflare Tunnel để có một URL công khai, sau đó đăng ký URL đó.

Các URL thuộc tên miền nội bộ của Zalo cũng sẽ bị từ chối. Hãy đăng ký một URL trên tên miền do bạn quản lý.

#### Sample code

### Parameters

| Trường | Kiểu dữ liệu | Bắt buộc | Mô tả |
|---|---|---|---|
| `url` | String | true | URL nhận thông báo dạng HTTPS. |
| `secret_token` | String | true | Một khóa bí mật từ 8 tới 256 ký tự, để xác thực yêu cầu từ Zalo gọi về hệ thống của bạn. Token sẽ được đính kèm trong header "X-Bot-Api-Secret-Token" trong tất cả các yêu cầu từ Zalo gọi tới hệ thống của bạn. |

### Sample response

Sau khi lưu, hệ thống sẽ tự động gửi thử một request tới Webhook URL của bạn và trả về kết quả trong trường verification, giúp bạn biết ngay Webhook đã hoạt động hay chưa mà không cần gọi thêm [testWebhook](/docs/apis/testWebhook/):

```
{
 "ok": true,
 "result": {
   "url": "https://your-webhookurl.com",
   "updated_at": 1749538250568,
   "verification": {
     "ok": true,
     "url": "https://your-webhookurl.com",
     "outcome": "webhook.ok",
     "hint": "Your endpoint responded successfully."
   }
  }
}

```

ghi chú

Webhook URL vẫn được lưu lại dù kết quả verification là thành công hay thất bại — vì vậy bạn có thể đăng ký Webhook URL trước khi triển khai xong server của mình. Xem chi tiết các trường và mã outcome tại [testWebhook](/docs/apis/testWebhook/), và có thể gọi lại API đó bất cứ lúc nào để kiểm tra lại.
