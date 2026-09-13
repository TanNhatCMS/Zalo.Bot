<!-- Source: https://bot.zapps.me/docs/apis/getWebhookInfo/ (scraped 2026-09-14; upstream "Cập nhật lần cuối" ở đầu trang) -->

Cập nhật lần cuối: 16/12/2025

API cho phép lấy trạng thái cấu hình hiện tại của webhook

- URL: https://bot-api.zaloplatforms.com/bot${BOT_TOKEN}/getWebhookInfo
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
    "url": "https://your-webhookurl.com",
    "updated_at": 1749633372026
  }
}

```
