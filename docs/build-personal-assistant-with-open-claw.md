<!-- Source: https://bot.zapps.me/docs/build-personal-assistant-with-open-claw/ (scraped 2026-09-14; upstream "Cập nhật lần cuối" ở đầu trang) -->

Cập nhật lần cuối: 3/6/2026

Dưới đây là hướng dẫn xây dựng trợ lý cá nhân My ClawBot kết nối OpenClaw

## Giới thiệu

- @zalo-platforms/openclaw-zaloclawbot là plugin giúp kết nối OpenClaw để tạo ra My ClawBot - trợ lý AI cá nhân hoạt động trực tiếp trên Zalo.
- 

- Chỉ cần cài đặt plugin, quét mã QR bằng Zalo và xác nhận quyền truy cập, My ClawBot sẽ tự động được kết nối với OpenClaw của bạn.
- 

- Sau khi hoàn tất, bạn có thể trò chuyện với My ClawBot ngay trên Zalo như một cuộc trò chuyện thông thường.
- 

## Trước khi bắt đầu, hãy đảm bảo

- Đã cài đặt OpenClaw
- 

- Đã hoàn thành bước openclaw onboard
- 

- Đã cấu hình AI Provider (OpenAI, Claude, Gemini,...)
- 

## Bước 1: Cài đặt Plugin

Cài đặt plugin từ npm:

```
npx -y @zalo-platforms/openclaw-zaloclawbot-cli install

```

## Bước 2: Tạo My ClawBot

- Sau khi cài đặt plugin hoàn tất, màn hình terminal sẽ hiển thị QR Code đăng nhập.
- 

- Mở ứng dụng Zalo để quét mã QR được hiển thị từ plugin.
- 

- Nhấn Tạo My ClawBot để cho phép plugin:

- Tạo My ClawBot cho tài khoản của bạn
- 

- Kết nối My ClawBot với OpenClaw
- 

- Đồng bộ các thiết lập cần thiết
- 

- 

## Bước 3: Chat với My ClawBot

- Sau khi kết nối thành công, My ClawBot sẽ xuất hiện trong danh sách chat Zalo của bạn.
-
