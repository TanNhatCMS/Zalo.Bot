<!-- Source: https://bot.zapps.me/docs/build-your-bot-with-webhook/ (scraped 2026-09-14; upstream "Cập nhật lần cuối" ở đầu trang) -->

Cập nhật lần cuối: 3/6/2026

Dưới đây là hướng dẫn xây dựng Zalo Bot sử dụng cơ chế Webhook dành cho người mới bắt đầu:

## Mục tiêu

- Tạo một bot Zalo sử dụng cơ chế Webhook để nhận sự kiện từ người dùng.
- 

- Xử lý các sự kiện như nhận tin nhắn, gửi phản hồi, gửi ảnh,...
- 

- Hiện thực bằng NodeJS hoặc Python sử dụng các SDK có sẵn.
- 

## Bước 1: Tạo Bot

Để tạo Zalo Bot, vui lòng làm theo hướng dẫn [tại đây](/docs/create-bot/). Sau khi tạo Bot, bạn sẽ có thông tin Bot Token để tích hợp API.

## Bước 2: Thiết lập Webhook

Bạn cần thiết lập Server với domain HTTPS để đăng ký Webhook nhận sự kiện. Bạn có thể dùng:

- Ngrok (dành cho dev local): ngrok http 3000
- 

- Render, Railway, Vercel,... (có hỗ trợ HTTPS)
- 

Sau đó sử dụng API [setWebhook](/docs/apis/setWebhook/) để thiết lập Webhook cho Zalo Bot của bạn.

## Bước 3: Lập trình Bot

Sử dụng Zalo Bot SDK theo code mẫu bên dưới để hiện thực logic cho Bot của bạn.

- Python: Tham khảo thêm tài liệu tại [python-zalo-bot](https://pypi.org/project/python-zalo-bot/).
- 

- Nodejs: Tham khảo thêm tài liệu tại [node-zalo-bot](https://www.npmjs.com/package/node-zalo-bot).
-
