<!-- Source: https://bot.zapps.me/docs/call-api/ (scraped 2026-09-14; upstream "Cập nhật lần cuối" ở đầu trang) -->

Cập nhật lần cuối: 16/12/2025

Để gửi yêu cầu đến hệ thống Open APIs của Zalo Bot, bạn sử dụng Bot Token đã được cấp và làm theo hướng dẫn bên dưới.

### Định dạng URL

Tất cả các truy vấn đến Zalo Bot API phải được thực hiện qua giao thức HTTPS và có định dạng như sau:

```
https://bot-api.zaloplatforms.com/bot<BOT_TOKEN>/<functionName>

```

Ví dụ:

```
https://bot-api.zaloplatforms.com/bot123456789:abc123xyz/getMe

```

---

### Phương thức HTTP hỗ trợ

- GET
- 

- POST
- 

---

### Cách truyền tham số

Zalo Bot hỗ trợ cả 2 phương thức HTTP GET và POST cho tất cả các API, với các cách để truyền tham số như sau:

- 

Chuỗi truy vấn URL (query string)

Ví dụ:  ...?chat_id=123456&text=Hello

- 

- 

application/x-www-form-urlencoded

Dạng form tiêu chuẩn (dùng với POST đơn giản)

- 

- 

application/json

Gửi payload dạng JSON-object

- 

- 

multipart/form-data

Dùng khi cần tải lên file như ảnh, tài liệu,...

- 

Tuy nhiên, bạn nên cân nhắc sử dụng phương thức HTTP GET cho các API dùng để truy xuất dữ liệu và POST cho các API dùng để thay đổi (ghi/cập nhật) thông tin dữ liệu.

---

### Phản hồi từ API

Phản hồi từ Zalo Bot API luôn là dạng JSON-object, gồm các trường thông tin chính sau:

| Trường | Ý nghĩa |
|---|---|
| `ok` | `true` nếu thành công, `false` nếu có lỗi |
| `result` | Dữ liệu trả về nếu thành công |
| `description` | Mô tả lỗi ngắn gọn (nếu có) |
| `error_code` | Mã lỗi hệ thống |

---

### Lưu ý

- Tất cả truy vấn gửi đến Zalo Bot API phải sử dụng encoding UTF-8.
- 

- Các tên API (method name) có phân biệt chữ hoa và chữ thường.
-
