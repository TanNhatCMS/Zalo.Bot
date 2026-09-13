# ZaloBot.Sdk

Independent .NET 10 client library for the **Zalo Bot API** (docs: <https://bot.zapps.me/docs>).
Adapted from [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) (MIT — see
`LICENSE-Telegram.Bot.txt`), pruned to the **11 methods** the Zalo Bot API exposes, and extended
with an official-error-code taxonomy, a retry/backoff pipeline and access-token hygiene.

**Leaf project**: references no other solution project and no NuGet packages (`System.Text.Json`
is part of the shared framework). Nothing in the app layers may be referenced from here.

> Naming gotcha: the root namespace `ZaloBot.Sdk` shadows the app entity `ZaloBot`
> (`QuanLyCongViec.Domain.Entities.ZaloBot`). App code that uses the SDK aliases the entity
> and the app-side interface — see `Infrastructure/External/ZaloBotClient.cs`.

## Quick start

```csharp
var options = new ZaloBotClientOptions(accessToken);            // token required, base URL optional
options.TimeoutSeconds = 40;                                     // per-request timeout (default 30)
var client   = new ZaloBotClient(options, httpClientFactory);    // HttpClient from IHttpClientFactory
Message sent = await client.SendMessageAsync("5128471675483426707", "Hello bot");
```

- `ZaloBotClientOptions.Token` — bot access token. It is used **only** to build the request URL
  (`{BaseUrl}/bot{Token}/{method}`); it never appears in headers, bodies, logs or exceptions.
- `ZaloBotClientOptions.DefaultBaseUrl` = `https://bot-api.zaloplatforms.com` (official server).
- Events `OnMakingApiRequest` / `OnApiResponseReceived` (`AsyncEventHandler<...>`) allow
  observation of every request/response — used by the app adapter for logging.

## API methods (all of them)

Extension methods live in `ZaloBotClientExtensions.ApiMethods.cs`; request objects in `Requests/`;
result types in `Types/`. HTTP verb per docs/call-api: **GET for read APIs, POST for writes** (the
platform supports both verbs for every API — see "Call API conventions" below).

| API method       | Verb | Extension                                 | Returns            |
| ---------------- | ---- | ----------------------------------------- | ------------------ |
| `getMe`          | GET  | `GetMeAsync()`                            | `User`             |
| `getUpdates`     | POST | `GetUpdatesAsync(offset, timeoutSeconds, limit)` | `Update[]`  |
| `sendMessage`    | POST | `SendMessageAsync(chatId, text, parseMode?, textStyles?, ct)` | `Message` |
| `sendPhoto`      | POST | `SendPhotoAsync(chatId, photo)`           | `Message`          |
| `sendSticker`    | POST | `SendStickerAsync(chatId, sticker)`       | `Message`          |
| `sendVoice`      | POST | `SendVoiceAsync(chatId, voice)`           | `Message`          |
| `sendChatAction` | POST | `SendChatActionAsync(chatId, action)`     | `bool`             |
| `setWebhook`     | POST | `SetWebhookAsync(url, secretToken?)`      | `SetWebhookResult` |
| `testWebhook`    | POST | `TestWebhookAsync()`                      | `bool`             |
| `deleteWebhook`  | POST | `DeleteWebhookAsync()`                    | `bool`             |
| `getWebhookInfo` | GET  | `GetWebhookInfoAsync()`                   | `WebhookInfo?`     |

`Message` (result of `send*` calls) carries what the platform guarantees — `MessageId` (**string**)
and `Date` (epoch **milliseconds**) — plus audit helpers `RawJson` (platform result JSON) and
`RequestJson` (the body the SDK sent), both token-free. Extra/unknown fields from the platform are
ignored, never throw.

## Call API conventions (docs bot.zapps.me/docs/call-api)

- **URL format**: `https://bot-api.zaloplatforms.com/bot{token}/{methodName}` — token sits directly
  after `/bot` (no separator). HTTPS only. `ZaloBotClientOptions` enforces this shape.
- **Method names are case-sensitive** — `sendMessage` ≠ `sendmessage`. The 11 names above are
  pinned by `ApiConventionsTests` (each verified against `bot.zapps.me/docs/apis/<name>/`).
- **HTTP verbs**: the platform accepts GET **and** POST for every API, and recommends GET when
  retrieving data. SDK mapping: pure parameterless reads (`getMe`, `getWebhookInfo`) use **GET**
  (base class `ParameterlessGetRequest<TResult>`); everything else — including `getUpdates`
  (long-poll, its docs page specifies POST) — uses **POST**. The sender honors `IRequest.HttpMethod`.
- **Parameter passing**: the platform accepts query string, form-urlencoded, JSON and
  multipart/form-data (uploads). The SDK always sends **`application/json; charset=utf-8`**
  (UTF-8 is mandatory) — none of the 11 methods need file upload (media is referenced by URL/id).
- **Response envelope**: `{ok, result, error_code, description}` — parsing rules in "Wire contract"
  below.
- **getUpdates vs webhook are mutually exclusive** (docs/apis/getUpdates): if a webhook is
  registered, getUpdates returns nothing until `deleteWebhook` is called. Docs recommend webhooks
  for production, polling for local testing.

## Wire contract

- URL: `{BaseUrl}/bot{Token}/{methodName}` — GET for read APIs (`getMe`, `getWebhookInfo`), POST
  for the rest (see "Call API conventions"); `Content-Type: application/json; charset=utf-8`.
- Serialization: `JsonBotAPI.Options` — `SnakeCaseLower` naming policy, `WhenWritingDefault`
  ignore condition (null/0/false fields are omitted), `AllowReadingFromString` for numbers.
- `ChatId` always serializes as a **string** (`ChatIdConverter`) — required by the platform even
  for numeric ids. Accepts numeric ids, `@handles` and any raw Zalo chat id.
- Response wrapper: `{"ok":bool,"result":...,"error_code":int,"description":string}`.
  Parsing rules (`ZaloSdkHttpSender.ParseWrapper`):
  - `ok:true` → success. `result` may be absent → `bool` calls get `true`, `SetWebhookResult`
    calls get an empty result, others get `null`/empty.
  - `ok:false` (or `error_code`/`description` present) → failure → taxonomy below.
  - Non-object body or object with none of `ok`/`error_code`/`description` → `InvalidJsonException`
    with a **token-sanitized** body excerpt.
- Defensive mapping (`Internal/ZaloSdkPayloadMapper`): unknown shapes never throw — unrecognized
  updates map to `ContentType.Unknown`, malformed results to empty models. `getUpdates` elements
  and webhook payloads (which arrive wrapped as `{ok, result:{...}}`) both parse via `ParseUpdate`.

## sendMessage rich text

Per docs <https://bot.zapps.me/docs/apis/sendMessage>:

| Parameter    | Type                  | Required | Notes                                                                                          |
| ------------ | --------------------- | -------- | ---------------------------------------------------------------------------------------------- |
| `chat_id`    | String                | yes      | Recipient / conversation id                                                                    |
| `text`       | String                | yes      | 1–2000 chars; validated **locally** before any network call (`MaxMessageTextLength = 2000`)     |
| `parse_mode` | String                | no       | `"markdown"` or `"html"`                                                                        |
| `text_styles`| Array of style runs   | no       | Explicit styling; **ignored by the platform when `parse_mode` is also sent** (parse_mode wins)  |

`TextStyle` = `{ Start, Length, Styles }` → JSON `{"start":..,"len":..,"st":[..]}`. Offsets are
**UTF-16 code units** — the same unit as .NET `string` indexing, so `text.IndexOf(...)` /
`string.Length` values apply directly. Style codes are constants on `TextStyleCode`:

| Code                                   | Meaning                                    |
| -------------------------------------- | ------------------------------------------ |
| `b` `i` `u` `s`                        | bold / italic / underline / strikethrough  |
| `f_13` `f_15` `f_18` `f_20`            | font small / normal / large / very large   |
| `c_050a19` `c_15a85f` `c_f7b503` `c_f27806` `c_db342e` | default / green / yellow / orange / red |
| `lst_1` `lst_2`                        | unordered / ordered list                   |
| `ind_1` … `ind_5`                      | indentation levels 1–5                     |

Unknown codes are ignored by the platform; ranges outside the text are auto-adjusted or dropped.

```csharp
await client.SendMessageAsync("5128471675483426707", "Xin chào bạn", textStyles:
[
    TextStyle.Of(0, 7, TextStyleCode.Bold, TextStyleCode.ColorRed),
]);
```

## Errors, retry and token hygiene

Taxonomy (`Errors/ZaloSdkErrorTaxonomy.cs`) maps the official codes:

| Code | Category        | Behavior                                        | Exception thrown           |
| ---- | --------------- | ----------------------------------------------- | -------------------------- |
| 400  | NonRetryable    | fail fast                                       | `ApiRequestException`      |
| 401  | Token           | fail fast                                       | `ZaloBotTokenException`    |
| 403  | ServerRetry     | retried **exactly once**                        | `ApiRequestException`      |
| 404  | NonRetryable    | fail fast                                       | `ApiRequestException`      |
| 408  | Retryable       | retried with backoff until `RetryMaxAttempts`   | `ZaloBotRetryableException`|
| 429  | Retryable       | retried with backoff until `RetryMaxAttempts`   | `ZaloBotRetryableException`|
| other| Unknown         | fail fast                                       | `ApiRequestException`      |

- Retry config on `ZaloBotClientOptions`: `RetryMaxAttempts` (default 3, total attempts),
  `RetryBaseBackoffSeconds` (default 2), `RetryBackoffMultiplier` (default 2.0 — delay before
  retry *i* is `base * mult^(i-1)`). After retries are exhausted the **last** attempt's
  code/description is thrown.
- Network / timeout failures throw `ZaloBotNetworkException` and are **not** retried inside the
  sender (callers decide).
- Every exception message (and the `InvalidJsonException` body excerpt) is sanitized through
  `TokenSanitizer` — the access token never leaks through message, `ToString()` or inner exceptions.
- Never log the request URL or raw bodies in app code: the token lives in the URL only.

## Long-polling gotcha (getUpdates)

With long polling, the server holds the connection up to `timeoutSeconds`. If the SDK per-request
timeout is not strictly **longer** than the long-poll timeout, the client cancels the request just
as the server is about to answer → spurious timeouts. The app therefore sends an explicit
`timeoutSeconds` (adapter constant `LongPollTimeoutSeconds = 25`) plus a 15 s buffer as
the per-request timeout (40 s). Hierarchy to preserve: **server long-poll < SDK per-request timeout**.
Guard tests: `ZaloBotClientLongPollTests`.

## App integration map

```
ZaloBot.Sdk (this project)                        ← HTTP, retry, taxonomy, hygiene
  ▲
  │ thin adapter: Infrastructure/External/ZaloBotClient.cs
  │   (maps SDK types → app DTOs, translates exceptions, per-request timeouts)
  ▼
Application.Common.Interfaces.IZaloBotClient      ← app-side interface (NOT the SDK one)
```

The adapter implements the app interface `QuanLyCongViec.Application.Common.Interfaces.IZaloBotClient`
using the SDK `Sdk.ZaloBotClient` created per call with the bot's access token
(`Infrastructure/DependencyInjection.cs` registers it). Bot entity: `Domain/Entities/ZaloBot`.

## Tests

`tests/ZaloBot.Sdk.UnitTests` — NUnit + Shouldly, naming `Should{Behavior}_When{Condition}`,
AAA pattern. HTTP is faked with `FakeHttpMessageHandler` (enqueue JSON responses, inspect request
URLs/bodies) via `ZaloBotClientTestBase`. Run:

```powershell
cd platform/server
dotnet test --filter "FullyQualifiedName~ZaloBot.Sdk.UnitTests"
```

## Adding a new API method (recipe)

1. Request type in `Requests/` extending `RequestBase<TResponse>` (pure parameterless reads →
   `ParameterlessGetRequest<TResult>` = GET; otherwise POST), with `"methodName"` = the documented
   API name copied **exactly** (camelCase, case-sensitive) and JSON properties via
   `[JsonPropertyName]` or the snake_case policy.
2. Extension method in `ZaloBotClientExtensions.ApiMethods.cs` (local validation of constraints
   such as text length goes here, before any network call).
3. New result type? Add a `Types/` model and a defensive parser in `Internal/ZaloSdkPayloadMapper`
   + a branch in `ZaloSdkHttpSender.MapResult`.
4. Tests in `tests/ZaloBot.Sdk.UnitTests` with `FakeHttpMessageHandler` (assert URL, JSON body,
   mapping, error taxonomy paths).
5. Update the tables above and the docs links (<https://bot.zapps.me/docs/apis/{method}/>).
