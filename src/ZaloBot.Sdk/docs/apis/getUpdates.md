<!-- Source: https://bot.zapps.me/docs/apis/getUpdates/ (scraped 2026-09-14; upstream "Cập nhật lần cuối" ở đầu trang) -->

Cập nhật lần cuối: 16/12/2025

Hệ thống Zalo hỗ trợ 2 cách độc lập và loại trừ lẫn nhau để bot của bạn nhận được các tin nhắn mới:

- Sử dụng phương thức getUpdates, dựa trên cơ chế long polling.
- 

- Sử dụng [Webhook](/docs/webhook/).
- 

Lưu ý

Phương thức getUpdates sẽ không hoạt động nếu bạn đã thiết lập Webhook trước đó (khi đó, vui lòng sử dụng phương thức [deleteWebhook](/docs/apis/deleteWebhook/) để xóa cấu hình Webhook trước khi sử dụng API này).
Chỉ nên sử dụng API này để chạy local, trong môi trường development, thử nghiệm, với môi trường production, bạn nên thiết lập Webhook để tránh bỏ lỡ event.

- URL: https://bot-api.zaloplatforms.com/bot${BOT_TOKEN}/getUpdates
- 

- Method: POST
- 

- Response Type: application/json
- 

#### Sample code

### Parameters

| Trường | Kiểu dữ liệu | Bắt buộc | Mô tả |
|---|---|---|---|
| `timeout` | String | false | Thời gian timeout của HTTP Request tính theo giây. Mặc định hệ thống sẽ lấy thời gian timeout là 30 giây. |

### Sample response

Dữ liệu tin nhắn nhận được sẽ là dạng JSON object, tham khảo dữ liệu mẫu tương tự tại [Webhook](/docs/webhook/).

mẹo

Xem hướng dẫn xây dựng Chatbot cơ bản với cơ chế getUpdates [tại đây](/docs/build-your-bot/).
