<!-- Source: https://bot.zapps.me/docs/apis/getMe/ (scraped 2026-09-14; upstream "Cập nhật lần cuối" ở đầu trang) -->

Cập nhật lần cuối: 16/12/2025

Sử dụng phương thức này để kiểm tra Bot Token, nếu token hợp lệ sẽ trả về các thông tin cơ bản về Bot của bạn.

- URL: https://bot-api.zaloplatforms.com/bot${BOT_TOKEN}/getMe
- 

- Method: POST
- 

- Response Type: application/json
- 

#### Sample code

### Parameters

Không yêu cầu tham số đi kèm.

### Sample response

```
{
  "ok": true,
  "result": {
    "id": "1459232241454765289",
    "account_name": "bot.VDKyGxQvc",
    "account_type": "BASIC",
    "can_join_groups": false
  }
}

```
