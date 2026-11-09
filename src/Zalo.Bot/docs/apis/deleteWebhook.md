<!-- Source: https://bot.zapps.me/docs/apis/deleteWebhook/ (scraped 2026-09-14; upstream "Cập nhật lần cuối" ở đầu trang) -->

Cập nhật lần cuối: 16/12/2025

Sử dụng phương thức này để gỡ bỏ thiết lập webhook nếu bạn quyết định chuyển lại sang getUpdates. Phương thức này sẽ trả về True khi xử lý thành công.

- URL: https://bot-api.zaloplatforms.com/bot${BOT_TOKEN}/deleteWebhook
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
   "url": "",
   "updated_at": 1749538250568
  }
}

```
