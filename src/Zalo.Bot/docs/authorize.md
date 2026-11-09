<!-- Source: https://bot.zapps.me/docs/authorize/ (scraped 2026-09-14; upstream "Cập nhật lần cuối" ở đầu trang) -->

Cập nhật lần cuối: 16/12/2025

Zalo sử dụng mô hình Bot Token xác thực và cho phép bot sử dụng API.

### Bot Token

Được cung cấp sau khi tạo bot thành công, Token này sẽ không hết hạn cho tới khi bạn chủ động reset. Token sẽ có dạng 12345689:abc-xyz và được dùng để gọi tất cả các API với phương thức như sau:

```
 https://bot-api.zaloplatforms.com/bot${BOT_TOKEN}/functionName

```

mẹo

Để cài đặt lại Bot Token, vui lòng truy cập Zalo Bot Creator, chọn thiết lập và làm theo hướng dẫn. Khi thay đổi thành công, hệ thống sẽ gửi Token mới cho bạn qua tin nhắn Zalo.
