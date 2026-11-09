<!-- Source: https://bot.zapps.me/docs/apis/sendMessage/ (scraped 2026-09-14; upstream "Cập nhật lần cuối" ở đầu trang) -->

Cập nhật lần cuối: 10/6/2026

API cho phép Bot của bạn gửi tin nhắn văn bản đến người dùng hoặc các cuộc trò chuyện.

- URL: https://bot-api.zaloplatforms.com/bot${BOT_TOKEN}/sendMessage
- 

- Method: POST
- 

- Response Type: application/json
- 

#### Sample code

### Parameters

| Trường | Kiểu dữ liệu | Bắt buộc | Mô tả |
|---|---|---|---|
| `chat_id` | String | true | ID của người nhận hoặc cuộc trò chuyện |
| `text` | String | true | Nội dung văn bản của tin nhắn sẽ được gửi, với độ dài từ 1 đến 2000 ký tự |
| `parse_mode` | String | false | Cho phép server tự suy ra định dạng từ `text`. Nhận một trong hai giá trị: `markdown` hoặc `html`. Khi dùng `parse_mode`, các ký tự đánh dấu (markup) sẽ được loại bỏ khỏi nội dung hiển thị |
| `text_styles` | Array | false | Danh sách các đoạn định dạng (style run) áp dụng trực tiếp lên `text` ở dạng thô. Xem chi tiết bên dưới |

### Định dạng văn bản (Rich Text)

Bot có thể gửi tin nhắn có định dạng (in đậm, in nghiêng, màu sắc, danh sách...) bằng một trong hai cách: dùng parse_mode để server tự phân tích, hoặc dùng text_styles để chỉ định trực tiếp các đoạn định dạng.

Thứ tự ưu tiên

Nếu cả parse_mode và text_styles cùng được gửi, parse_mode sẽ được ưu tiên và text_styles sẽ bị bỏ qua. Hai trường này dùng hệ tọa độ offset khác nhau (parse_mode tính trên văn bản đã loại bỏ markup, còn text_styles tính trên văn bản thô) nên không thể kết hợp đồng thời. Cả hai trường đều tùy chọn và tương thích ngược — nếu không gửi, tin nhắn sẽ được gửi dưới dạng văn bản thuần.

#### Cách 1: dùng parse_mode

Đặt parse_mode là markdown hoặc html và viết nội dung định dạng trực tiếp trong trường text. Server sẽ phân tích, loại bỏ phần markup và tự sinh định dạng tương ứng.

Cú pháp Markdown được hỗ trợ:

| Cú pháp | Kết quả |
|---|---|
| `**đậm**`, `__đậm__` | In đậm |
| `*nghiêng*`, `_nghiêng_` | In nghiêng |
| `***đậm nghiêng***` | In đậm + in nghiêng |
| `~~gạch ngang~~` | Gạch ngang |
| ``code`` | Giữ nguyên nội dung (không áp định dạng) |
| `# Tiêu đề` … `#### Tiêu đề` | In đậm (riêng `#` còn tăng cỡ chữ) |
| `- mục`, `* mục`, `+ mục` | Danh sách không thứ tự |
| `1. mục` | Danh sách có thứ tự |
| `> trích dẫn` | Thụt lề (mỗi cấp `>` là một mức) |
| khoảng trắng đầu dòng | Thụt lề (2 khoảng trắng = 1 mức) |
| `{red}…{/red}`, `{orange}…{/orange}`, `{yellow}…{/yellow}`, `{green}…{/green}` | Màu chữ |
| `{big}…{/big}` | Cỡ chữ lớn |
| `{underline}…{/underline}` | Gạch chân |
| `\*`, `\_`, … | Ký tự thoát (escape) để hiển thị nguyên ký tự đánh dấu |

Thẻ HTML được hỗ trợ:

| Thẻ | Kết quả |
|---|---|
| `<b>`, `<strong>` | In đậm |
| `<i>`, `<em>` | In nghiêng |
| `<u>` | Gạch chân |
| `<s>`, `<del>`, `<strike>` | Gạch ngang |
| `<h1>`–`<h6>` | In đậm (kèm tăng cỡ chữ với `h1`–`h4`) |
| `<ul>`, `<ol>`, `<li>` | Danh sách không thứ tự / có thứ tự |
| `<p>`, `<div>` | Khối văn bản (xuống dòng) |
| `style="..."` | `font-size`, `font-weight`, `font-style`, `text-decoration`, `color` (chỉ nhận các màu trong bảng màu được hỗ trợ) |

Các thẻ HTML không nằm trong danh sách trên sẽ bị loại bỏ nhưng phần nội dung văn bản bên trong vẫn được giữ lại.

#### Cách 2: dùng text_styles

Gửi danh sách các đoạn định dạng áp trực tiếp lên text. Mỗi phần tử gồm:

| Trường | Kiểu dữ liệu | Mô tả |
|---|---|---|
| `start` | Integer | Vị trí bắt đầu của đoạn, tính theo UTF-16 code unit (chỉ số chuỗi như trong JavaScript/Java) |
| `len` | Integer | Độ dài của đoạn (số UTF-16 code unit) |
| `st` | Array<String> | Danh sách các mã định dạng áp cho đoạn, ví dụ `["b", "c_db342e"]` |

```
curl -X POST "https://bot-api.zaloplatforms.com/bot<BOT_TOKEN>/sendMessage" \
  -H "Content-Type: application/json" \
  -d '{
    "chat_id": "abc.xyz",
    "text": "Xin chào bạn",
    "text_styles": [
      { "start": 0, "len": 7, "st": ["b", "c_db342e"] }
    ]
  }'

```

Bảng mã định dạng (st) được hỗ trợ:

| Mã | Ý nghĩa |
|---|---|
| `b` | In đậm |
| `i` | In nghiêng |
| `u` | Gạch chân |
| `s` | Gạch ngang |
| `f_13`, `f_15`, `f_18`, `f_20` | Cỡ chữ (nhỏ / thường / lớn / rất lớn) |
| `c_050a19` | Màu mặc định |
| `c_15a85f` | Màu xanh lá |
| `c_f7b503` | Màu vàng |
| `c_f27806` | Màu cam |
| `c_db342e` | Màu đỏ |
| `lst_1` | Danh sách không thứ tự |
| `lst_2` | Danh sách có thứ tự |
| `ind_1` … `ind_5` | Mức thụt lề (1 đến 5) |

Các mã không nằm trong danh sách trên sẽ bị bỏ qua, vì client chỉ hiển thị được tập mã đóng này. Các offset vượt ngoài phạm vi của text sẽ được tự điều chỉnh hoặc loại bỏ.

### Sample response

```
{
  "ok": true,
  "result": {
    "message_id": "82599fa32f56d00e8941",
    "date": 1749632637199
  }
}

```
