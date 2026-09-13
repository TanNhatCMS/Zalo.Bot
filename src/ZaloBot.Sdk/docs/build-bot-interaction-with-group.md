<!-- Source: https://bot.zapps.me/docs/build-bot-interaction-with-group/ (scraped 2026-09-14; upstream "Cập nhật lần cuối" ở đầu trang) -->

Cập nhật lần cuối: 3/6/2026Lưu ý

Tính năng đang trong giai đoạn thử nghiệm nội bộ, sẽ được ra mắt trong thời gian tới.

Dưới đây là hướng dẫn chi tiết về cách đưa Zalo Bot vào hoạt động trong môi trường nhóm chat (Group) và quy tắc tương tác giữa thành viên và Bot.

## Mục tiêu

- Hiểu quy trình mời Bot vào nhóm chat trên Zalo App.
- 

- Nắm được cơ chế Bot nhận tin nhắn trong nhóm
- 

- Xác định được ID của nhóm để gửi phản hồi.
- 

## Bước 1: Chuẩn bị

Trước khi bắt đầu, đảm bảo bạn đã hoàn thành việc tạo Bot và thiết lập môi trường (Webhook hoặc Polling) theo các hướng dẫn trước:

- [Tạo Bot và lấy Token](/docs/create-bot/)
- 

- [Xây dựng Bot với Webhook](/docs/build-your-bot-with-webhook/) hoặc [Polling](/docs/build-your-bot/)
- 

## Bước 2: Thêm Bot vào Nhóm Chat

Bot cần được mời vào nhóm chat để có thể tương tác với các thành viên trong nhóm. Thực hiện theo các bước sau để thêm Bot vào nhóm:

Chi tiết các bước:

- Lấy Link Bot: Mở Mini app [Zalo Bot Creator](https://zalo.me/app/link/zapps/3082563950095582238/) chọn bot cần thêm vào group để vào trang thông tin của Bot
- 

- Chia sẻ vào nhóm: Ở mục "Mời Bot vào nhóm" nhấn nút chia sẻ hoặc gửi link này vào nhóm chat mà bạn muốn thêm Bot.
- 

- Kích hoạt: Tại giao diện chat của nhóm, trưởng nhóm nhấn vào link Bot vừa gửi.
- 

- Xác nhận: Một popup sẽ hiện ra yêu cầu xác nhận "Thêm Bot vào Nhóm", hãy nhấn Xác nhận.
- 

- Hoàn tất: Bot sẽ gửi tin nhắn chào mừng hoặc thông báo đã tham gia nhóm thành công.
- 

## Bước 3: Tương tác với Bot trong Nhóm

Sau khi bot được thêm thành công vào group thì các thành viên có thể tương tác với bot theo cách sau.

- Các tin nhắn trả lời trực tiếp đến tin nhắn của bot (reply message): Bot sẽ nhận được sự kiện khi có người dùng "Trả lời" (Quote) một tin nhắn mà Bot đã gửi trước đó.
- 

- Các tin nhắn mà bot được nhắc đến (mention): Bot sẽ nhận được sự kiện khi người dùng gõ @ và chọn tên của Bot trong tin nhắn.
- 

### Xử lý dữ liệu Webhook

Khi sự kiện xảy ra, Bot sẽ nhận được tin nhắn có cấu trúc dữ liệu JSON gửi về Server. Vui lòng tham khảo cấu trúc mẫu [tại đây](/docs/webhook/#sample-response).

Lưu ý khi xử lý:

- ID của group sẽ được lấy ở trường chat.id trong dữ liệu nhận về.
- 

- Bot có thể sử dụng giá trị chat.id này để gửi API phản hồi tin nhắn vào đúng nhóm chat đó.
-
