<!-- Source: https://bot.zapps.me/docs/build-your-bot/ (scraped 2026-09-14; upstream "Cập nhật lần cuối" ở đầu trang) -->

Cập nhật lần cuối: 3/6/2026

Dưới đây là hướng dẫn xây dựng Zalo Bot cơ bản sử dụng chế độ Polling, phù hợp cho người mới bắt đầu và có thể dễ dàng chạy trên máy local.

## Bước 1: Hiểu sơ lược về Zalo Bot

Zalo Bot là một tài khoản tự động (bot) hoạt động trên nền tảng Zalo, cho phép tương tác với người dùng thông qua tin nhắn. Bot có thể giúp bạn:

- Trả lời tin nhắn theo từ khóa, yêu cầu...
- 

- Gửi thông tin cảnh báo
- 

- Tự động phản hồi đơn hàng, hỗ trợ khách hàng, khảo sát, v.v.
- 

## Bước 2: Tạo Bot

Để tạo Zalo Bot, vui lòng làm theo hướng dẫn [tại đây](/docs/create-bot/). Sau khi tạo Bot, bạn sẽ nhận được thông tin Bot Token để tiến hành tích hợp API.

## Bước 3: Lập trình Bot

Tham khảo code mẫu bên dưới để lập trình Bot đơn giản sử dụng cơ chế [getUpdates](/docs/apis/getUpdates/) và Zalo Bot SDK, phù hợp với môi trường Development, nhu cầu chạy thử nghiệm từ local trong quá trình tích hợp.

- Python: Tham khảo thêm tài liệu tại [python-zalo-bot](https://pypi.org/project/python-zalo-bot/).
- 

- Nodejs: Tham khảo thêm tài liệu tại [node-zalo-bot](https://www.npmjs.com/package/node-zalo-bot).
-
