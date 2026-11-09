<!-- Source: https://bot.zapps.me/docs/create-bot/ (scraped 2026-09-14; upstream "Cập nhật lần cuối" ở đầu trang) -->

Cập nhật lần cuối: 17/12/2025

Để tạo Zalo Bot, vui lòng thực hiện theo hướng dẫn sau:

### Bước 1: Truy cập Zalo OA

- Mở ứng dụng Zalo
- 

- Tìm kiếm OA Zalo Bot Manager
- 

- Chọn Tạo bot trong menu cửa sổ chat để truy cập ứng dụng Zalo Bot Creator
- 

### Bước 2: Thiết lập thông tin Bot

- Nhập tên Bot (bắt buộc bắt đầu bằng tiền tố Bot, ví dụ: Bot MyShop) và các thông tin cần thiết.
- 

- Nhấn Tạo Bot để xác nhận
- 

- Sau khi tạo thành công, hệ thống sẽ gửi:

- Thông tin Bot
- 

- Bot Token qua tin nhắn cho tài khoản Zalo của bạn.
- 

- 

### Bước 3: Lập trình Bot

- Sử dụng Node.js, Python hoặc nền tảng không cần code (No-code) để tùy biến theo nhu cầu của bạn.
- 

- Zalo Bot hỗ trợ 2 cơ chế giao tiếp để cập nhật thông tin:

- Long polling: gửi yêu cầu định kỳ để lấy tin nhắn mới. Để bắt đầu chạy thử và phát triển Bot, hãy sử dụng API [getUpdates](/docs/apis/getUpdates/) ở máy local, sau đó nhắn tin đến Bot của bạn. Bạn sẽ nhận được tin nhắn và có thể [sendMessage](/docs/apis/sendMessage/) ngược lại cho người dùng.
- 

- Webhook: hệ thống Zalo sẽ gửi tin nhắn đến Webhook URL bạn đã thiết lập, tham khảo API [setWebhook](/docs/apis/setWebhook/).
- 

-
