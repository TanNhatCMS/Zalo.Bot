# Zalo.Bot

Independent .NET 10 client library for the **Zalo Bot API** (docs: <https://bot.zapps.me/docs>;
offline copy in [docs/](https://github.com/TanNhatCMS/Zalo.Bot/tree/main/docs) — scraped 2026-09-14, all 21 pages incl. 11 API references).
Adapted from [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) (MIT — see
`LICENSE-Telegram.Bot.txt`), pruned to the **11 methods** the Zalo Bot API exposes, and extended
with an official-error-code taxonomy, a retry/backoff pipeline and access-token hygiene.

**Leaf project**: references no other solution project and no NuGet packages (`System.Text.Json`
is part of the shared framework). Nothing in the app layers may be referenced from here.

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

## QR login (get a bot token without storing one)

`ZaloBotLoginClient` ports the QR login flow from openclaw-zaloclawbot
(`packages/plugin/src/auth/login-qr.ts`). It is **not** one of the 11 Bot API methods: it talks
to the Zalo session service at `https://bot.zaloplatforms.com/agent/*` and needs no bot token —
that is why it is a separate client, not a method on `ZaloBotClient`.

How it works:

![QR login flow: RequestLoginAsync returns LoginUrl and Zbsk, the frontend renders the QR from LoginUrl, the user scans it with the Zalo app, WaitForLoginAsync polls get-login-status every 1.5 s until the scan is confirmed, expired sessions are refreshed up to 3 times, then a ZaloBotClient is created from the returned bot token.](docs/qr-login-flow.svg)

1. `RequestLoginAsync()` — `GET {sessionService}/agent/request-login` returns a
   `ZaloLoginSession`: `LoginUrl` (the URL to render as a QR code) + `Zbsk` (the session key).
   The session — and its QR — expires after 5 minutes.
2. The **frontend renders the QR code from `LoginUrl`**; the SDK never renders QR codes and
   adds no QR dependency. The user scans it with the Zalo app.
3. `WaitForLoginAsync(session)` polls `GET {sessionService}/agent/get-login-status?zbsk=...`
   every 1.5 s until the scan is confirmed, the session expires, or the timeout elapses.
   A confirmed result carries `BotToken` (+ `BotId`/`OwnerId`/`OaId` when reported) — store it
   securely yourself and create a `ZaloBotClient` from it. The SDK never stores credentials.
   Optionally verify it with `GetMeAsync()` (not built into login, so the flow stays simple).

```csharp
var login = new ZaloBotLoginClient(); // defaults: 1.5 s poll, 5 min session, 3 attempts

// 1. Ask for a session and show its URL as a QR code (frontend renders it).
ZaloLoginSession session = await login.RequestLoginAsync();
ShowQrCode(session.LoginUrl); // your UI: render the QR from this link

// 2. Wait for the user to scan it.
ZaloLoginResult result = await login.WaitForLoginAsync(session);
if (!result.IsConnected)
{
    Console.WriteLine($"Login did not complete: {result.Status} — {result.Message}");
    return; // session expired/timed out: request a fresh one (step 1) for a new QR
}

// 3. Use the token like any other bot token.
var bot = new ZaloBotClient(result.BotToken!);
User me = await bot.GetMeAsync(); // optional verification
await bot.SendMessageAsync("5128471675483426707", "Xin chào từ .NET!");
```

`LoginAsync(onSessionReady)` is the same flow as a convenience: it requests a session, invokes
your callback with it (render the QR there), waits, and — when a session expires or times out —
requests a **fresh** session (up to `MaxLoginAttempts`, default 3) and invokes the callback again
with the new URL, so the frontend always sees every new QR and can re-render it. It returns the
first connected result, or the last expired/timed-out one.

```csharp
ZaloLoginResult result = await login.LoginAsync(session =>
{
    ShowQrCode(session.LoginUrl); // called again for every fresh session
    return Task.CompletedTask;
});
```

**No callback for login.** `request-login` is a parameterless GET — it registers no callback or
`redirect_url` anywhere, and the login flow has no callback endpoint. The scan confirmation is
learned **only by polling** `get-login-status`: the login URL is a Zalo page, Zalo marks the
session on their server, and this side finds out by polling every 1.5 s. Do not confuse this
with **webhooks** (`setWebhook`): a webhook is a callback too, but for **message updates after
login** — Zalo pushes updates to your `webhookBaseUrl` — not for reporting that the login
finished. The port therefore keeps the polling design and adds no login-callback endpoint.

Configuration (`ZaloBotLoginOptions`): `SessionServiceUrl` (default
`https://bot.zaloplatforms.com`, overridable for a compatible server), `PollInterval` (default
1.5 s), `SessionTimeout` (default 5 min), `MaxLoginAttempts` (default 3, `LoginAsync` only),
`RequestTimeout` (per-request HTTP timeout, default 30 s). All methods take a
`CancellationToken`; cancelling the wait throws `OperationCanceledException`. Network errors
while polling are retried until the deadline; HTTP 498 (or `code`/`error_code` 498) means the
session expired. The `zbsk` session key and the bot token never appear in exception messages.

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
  updates map to `ContentType.Unknown`, malformed results to empty models. `getUpdates` results
  and webhook bodies both parse via `ParseUpdate`.

## Update payload (docs 22/10/2026)

- **getUpdates**: `result` is **one event object** `{event_name, message}` (same shape as the webhook
  body). `GetUpdatesAsync` returns it as a 1-element `Update[]`; an array `result` (older/Telegram-style
  payloads) is still accepted; `{}`/`null` → empty array. The documented parameter is only `timeout`;
  the SDK also sends `offset`/`limit` for compatibility (the platform ignores unknown fields) and the
  docs define no `update_id`, so `UpdateId` is `0` unless the platform sends one.
- **Webhook body**: `{event_name, message}` at the top level (no `{ok, result}` wrapper any more —
  the wrapper is still unwrapped when present).
- **Classification**: `message.message_type` wins (`CHAT_TEXT`/`CHAT_PHOTO`/`CHAT_STICKER`/`CHAT_VOICE`);
  without it the type is inferred from the fields present. Photos read `photo_url` (falls back to the
  legacy `photo`); a photo caption goes to `Text` while `ContentType` stays `Photo`; stickers prefer
  `url` over the sticker code; voice reads `voice_url`.
- **Extra fields on `Update`**: `MessageType`, `ChatType` (`PRIVATE`/`GROUP`), `FromIsBot`.

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

## Polling (adapted from Telegram.Bot's polling — reference tmp/design/TelegramBots)

`namespace Zalo.Bot.Polling` + `ZaloBotClientExtensions.Polling.cs` — vòng lặp getUpdates
built into the SDK, mirroring the Telegram.Bot API surface:

- `ReceiverOptions { Offset (long?), Limit (1–100, default 100), TimeoutSeconds (default 25), DropPendingUpdates }`.
  **Zalo-specific deviations**: no `AllowedUpdates` (Zalo getUpdates has no such parameter);
  `UpdateId` is long; long-poll hold is set explicitly via `TimeoutSeconds` instead of reusing
  `client.Timeout` (the reference's approach races — client and server would cancel at the same
  moment). Rule: client `TimeoutSeconds` ≥ receiver `TimeoutSeconds` + 15. `TimeoutSeconds = 0`
  means short polling (tight loop — pace it yourself).
- `IUpdateHandler { HandleUpdateAsync, HandleErrorAsync }` + enum `HandleErrorSource`
  (`PollingError` — getUpdates failed, loop continues; `HandleUpdateError` — handler threw, loop
  continues; `FatalError` — loop stopped) + `DefaultUpdateHandler` delegate wrapper.
- Extensions on `IZaloBotClient`: `StartReceiving(...)` (fire-and-forget background loop, several
  delegate/IUpdateHandler overloads), `ReceiveAsync(...)` (blocking variant), `DropPendingUpdatesAsync()`
  (short-poll + advance offset, returns the next offset). Loop behavior: dispatch each update,
  advance `offset = lastUpdateId + 1`, errors go to the error handler and the loop continues,
  `OperationCanceledException` stops it.
- Webhook vs polling are mutually exclusive on the platform (docs/apis/getUpdates) — delete the
  webhook before using the receiver.

**Event sugar on `ZaloBotClient`** (adapted from the reference's TelegramBotClient): subscribing
`client.OnUpdate += handler` automatically **starts** the polling loop; removing the last
`OnUpdate` subscriber automatically **stops** it. `client.OnError += handler` receives polling
errors/handler exceptions (without it, errors go to `Trace.WriteLine`). Zalo has no `UpdateType`,
so there is no `OnMessage` event — dispatch on `Update.ContentType`/`Update.Text` in your handler.

The app currently does NOT use the built-in receiver: `Infrastructure/Services/ZaloBotPollingService`
implements its own multi-bot loop (bot-list cache, per-bot DB offsets, settings, counters). The SDK
receiver is for single-token scenarios (tools, scripts, one bot per process).

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
Zalo.Bot (this project)                        ← HTTP, retry, taxonomy, hygiene
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

`tests/Zalo.Bot.UnitTests` — NUnit + Shouldly, naming `Should{Behavior}_When{Condition}`,
AAA pattern. HTTP is faked with `FakeHttpMessageHandler` (enqueue JSON responses, inspect request
URLs/bodies) via `ZaloBotClientTestBase`. Run:

```bash
dotnet test Zalo.Bot.slnx -c Release
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
4. Tests in `tests/Zalo.Bot.UnitTests` with `FakeHttpMessageHandler` (assert URL, JSON body,
   mapping, error taxonomy paths).
5. Update the tables above and the docs links (<https://bot.zapps.me/docs/apis/{method}/>).
