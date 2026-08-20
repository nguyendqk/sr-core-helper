# CallApi&lt;TResponse&gt;

> Nguon: `FTELSRCore.Shared/Utilizes/CallApiWithHttp.cs` (dong 1466 den 2520)
> Loai: static class (generic, `where TResponse : class`)
> Cap nhat theo commit: `2262829`

## 1. Tong quan

`CallApi<TResponse>` la lop static tien ich goi HTTP outbound cua tang Shared (`FTELSRCore.Utilizes`). Day la bien the **khong co generic `TRequest`** cua `CallApiWithHttp<TRequest, TResponse>` (cung file, dong 16): vi khong co doi tuong request nen lop nay **khong build query string** va **khong serialize body**. Moi method deu deserialize response thanh `TResponse`, dong goi trang thai loi vao `ErrorModel`, do latency bang `Stopwatch` va ghi log tracing trong block `finally`.

> [!WARNING]
> **Cai bay quan trong nhat cua module nay:** 4 method `PostAsJSonAsync`, `PostWithHeadersAsJSonAsync`, `PutAsJSonAsync`, `PatchAsJSonAsync` tao `HttpRequestMessage` **KHONG set thuoc tinh `Content`** — nghia la request duoc gui **KHONG CO BODY**. Chi duy nhat `PostFormDataAsJSonAsync` co set `Content = form` (`CallApiWithHttp.cs:1978`). Chi tiet o muc 2 va muc 3.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Gui GET / POST / PUT / DELETE / PATCH toi `option.Uri` | Gui body JSON — khong method nao serialize object thanh `StringContent`/`JsonContent` |
| Gui body `multipart/form-data` (chi rieng `PostFormDataAsJSonAsync`, `CallApiWithHttp.cs:1978`) | Build query string tu object (khong co `TRequest`; `HttpClientUtilizes.ToQueryString` khong duoc goi trong lop nay) |
| Deserialize response body thanh `TResponse` qua `ResponseResult<TResponse>` (`HttpClientUtilizes.cs:362`) | Nem exception ra ngoai khi request that bai (tru `OperationCanceledException` tu `ThrowIfCancellationRequested` truoc `try`) |
| Dinh kem `Authorization` header tu `option.Token` + `option.AuthType` **khi `option.Token` khac rong** (`HttpClientUtilizes.cs:354-357`) | Retry / circuit breaker / fallback — khong co dong code nao thuc hien |
| — | Xoa/reset `Authorization` khi `option.Token` rong — khong co `Remove`/`= null` o bat ky dau (`HttpClientUtilizes.cs:354-357`, `CallApiWithHttp.cs:1967-1971`) |
| Them header tuy chinh vao `HttpRequestMessage.Headers` (`CallApiWithHttp.cs:1734`) hoac `client.DefaultRequestHeaders` (`CallApiWithHttp.cs:2091`) | Tra ve raw response body khi deserialize that bai (`ReadAsStreamAsync` tra `default`, `HttpClientUtilizes.cs:339`) |
| Tra ve `HttpResponseHeaders` (chi rieng `GetAsJSonAndHeaderAsync`) | Tra ve `HttpResponseHeaders` cho cac method con lai |
| Ap timeout rieng cho tung call qua `cancellationTokenTime` (`CancellationTokenHelper.cs:12`) | Dat `HttpClient.Timeout`; timeout duoc thuc hien bang `CancellationTokenSource.CancelAfter` |
| Canh bao request cham khi `SendAsync` vuot `desiredTime` (`InvokeForHTTP`, `MeasureExecutionTimeExtensions.cs:84-90`) | Nem loi khi HTTP status la 4xx/5xx — `EnsureSuccessOrException` chi gan `ErrorModel`; doan `EnsureSuccessStatusCode()` bi comment (`HttpClientUtilizes.cs:412-415`) va cung chi nham vao `>= 500` |
| Ghi log tracing (uri, statusCode, latency, direction) trong `finally` | Ghi log tracing khi `form is null` o `PostFormDataAsJSonAsync` (return truoc `try`, `CallApiWithHttp.cs:1953-1956`) |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `HttpOptionModel` (`FTELSRCore.Shared/Models/Https/HttpOptionModel.cs`) | Chua `Client`, `BaseAddress`, `Token`, `AuthType` (mac dinh `"Bearer"`), `Uri`, `SystemOwner` (mac dinh `"Service Request"`), `CompletionOption` (mac dinh `HttpCompletionOption.ResponseContentRead`) |
| `ErrorModel` (`FTELSRCore.Shared/Models/Https/ErrorModel.cs`) | Chua `Code` (int), `Message` (string), `Succeeded` (bool) |
| `HttpContentExtensionsUtilizes.ConfigHttpClient` (`HttpClientUtilizes.cs:343`) | Gan `BaseAddress`, `Accept: application/json`, `Authorization` len `option.Client` |
| `HttpContentExtensionsUtilizes.SetHttpVersion` (`HttpClientUtilizes.cs:280`) | Ep `HttpVersionPolicy.RequestVersionOrLower` khi `ASPNETCORE_ENVIRONMENT == "Local"`; nguoc lai giu `versionPolicy` |
| `HttpContentExtensionsUtilizes.ResponseResult<TResponse>` (`HttpClientUtilizes.cs:362`) | Kiem tra `Content-Type`; nem `CustomException` neu rong hoac chua `text/html`; nguoc lai deserialize qua stream |
| `HttpContentExtensionsUtilizes.ReadAsStreamAsync<T>` (`HttpClientUtilizes.cs:317`) | Deserialize bang `System.Text.Json` voi `PropertyNameCaseInsensitive`, `ReferenceHandler.IgnoreCycles`, `AllowReadingFromString` |
| `HttpContentExtensionsUtilizes.EnsureSuccessOrException` (`HttpClientUtilizes.cs:401`) | Gan `Code`/`Message`/`Succeeded` vao `ErrorModel` tu response; **khong nem** khi status la 4xx/5xx |
| `HttpContentExtensionsUtilizes.ErrorException` / `ErrorCanceledException` (`HttpClientUtilizes.cs:377-399`) | Map exception sang `ErrorModel` (500 hoac 408, kem message tieng Viet co `option.SystemOwner`) |
| `CancellationTokenHelper.CreateLinkedTokenWithTimeout` (`CancellationTokenHelper.cs:12`) | Tao `CancellationTokenSource` link voi token ngoai + `CancelAfter(cancellationTokenTime)` khi `> 0` |
| `MeasureExecutionTimeExtensions.InvokeForHTTP` (`MeasureExecutionTimeExtensions.cs:67`) | Do thoi gian `SendAsync`; ghi `logger.Warning` khi vuot `desiredTime` (giay) |
| `HttpClientUtilizes.HasPort` (`HttpClientUtilizes.cs:35`) | Xac dinh `DirectionType` cho log: co port khong-mac-dinh -> `Inbound`, nguoc lai `Outbound` |
| `LoggerExtensions.HttpResultWithTracing` / `HttpErrorResult` (`LoggerExtensions.cs:460`, `:426`, `:443`) | Ghi log ket qua va log loi |
| `CustomException` (`FTELSRCore.Shared/Exceptions/CustomException.cs`) | Exception noi bo mang `Code` (mac dinh 500) |
| `Microsoft.Extensions.Logging.ILogger` | Tham so `logger` (global using tai `FTELSRCore.Shared/GlobalUsing.cs:2`) |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `GetAsJSonAsync` | GET | GET toi `option.Uri`, tra ve `(TResponse, ErrorModel)` |
| `GetAsJSonAndHeaderAsync` | GET | Nhu tren nhung tra ve them `HttpResponseHeaders` |
| `GetAsJSonCustomHeaderAsync` | GET | GET co header tuy chinh, add vao `HttpRequestMessage.Headers` (per-request) |
| `PostAsJSonAsync` | POST | POST **khong body** |
| `PostFormDataAsJSonAsync` | POST | POST voi body `MultipartFormDataContent` do caller cung cap |
| `PostWithHeadersAsJSonAsync` | POST | POST **khong body**, header tuy chinh add vao `client.DefaultRequestHeaders` (shared client) |
| `PutAsJSonAsync` | PUT | PUT **khong body** |
| `DeleteAsJSonAsync` | DELETE | DELETE **khong body**, khong query string |
| `PatchAsJSonAsync` | PATCH | PATCH **khong body** |

Tong: **9 public static method**. Khong co overload trung ten trong lop nay.

---

## 2. Chi tiet API

### 2.0 Hanh vi dung chung cho moi method

Cac buoc duoi day xuat hien y nguyen trong than moi method (chi khac ten method trong log va HTTP verb). Cac muc 2.1-2.9 se chi neu diem khac biet.

**Thu tu thuc thi**

1. `cancellationToken.ThrowIfCancellationRequested()` — **ngoai `try`** (vi du `CallApiWithHttp.cs:1487`), nen neu token da huy thi method **nem `OperationCanceledException` ra ngoai**, khong tra ve `ErrorModel`.
2. `long start = Stopwatch.GetTimestamp()` — moc do latency.
3. `ErrorModel errorModel = new()` — khoi tao `Code = 0`, `Message = null`, `Succeeded = false`.
4. `result = (null, errorModel)` — tuple ket qua khoi tao.
5. Trong `try`: lay `HttpClient` (`option.ConfigHttpClient()`, tru `PostFormDataAsJSonAsync`).
6. Tao `CancellationTokenSource` link + timeout `cancellationTokenTime` giay.
7. Tao `HttpRequestMessage` voi `Version = HttpVersion.Version20` (**hardcode**) va `VersionPolicy = SetHttpVersion(versionPolicy)`.
8. Goi `client.SendAsync(request, completionOption: option.CompletionOption, cancellationToken: cancellationTokenSource.Token)` boc trong `MeasureExecutionTimeExtensions.InvokeForHTTP` (vi du `CallApiWithHttp.cs:1508-1517`). `HttpCompletionOption` **khong** duoc hardcode ma lay tu `option.CompletionOption`.
9. `httpResponseMessage.EnsureSuccessOrException(ref errorModel)` — gan status vao `errorModel`, **khong nem** voi 4xx/5xx.
10. `await httpResponseMessage.ResponseResult<TResponse>(logger)` — deserialize.
11. `finally`: `logger.HttpResultWithTracing(...)`.

**Error handling dung chung** (3 block `catch`, thu tu trong code):

| Exception | Ham map | `ErrorModel` sau khi map | Log | Gia tri tra ve |
|---|---|---|---|---|
| `OperationCanceledException` | `ErrorCanceledException` (`HttpClientUtilizes.cs:393`) | `Succeeded = false`, `Code = 408`, `Message = "Hệ thống {SystemOwner} đang xử lý chậm hơn bình thường. Vui lòng thử lại sau ít phút"` | `logger.HttpErrorResult(className, methodName, message)` — **khong truyen exception**, mat stack trace | `(null, errorModel)` |
| `CustomException` | `ErrorException` (`HttpClientUtilizes.cs:385`) | `Succeeded = false`, `Code = exception.Code`, `Message = exception.Message ?? fallback` | `logger.HttpErrorResult(..., e: exception)` | `(null, errorModel)` |
| `Exception` | `ErrorException` (`HttpClientUtilizes.cs:377`) | `Succeeded = false`, `Code = 500`, `Message = "Hệ thống {SystemOwner} đang gặp sự cố tạm thời, vui lòng thử lại sau"` | `logger.HttpErrorResult(..., e: exception)` | `(null, errorModel)` |

Khong co truong hop nao nem lai exception tu trong `try`.

**Side effect dung chung**

| Side effect | Vi tri |
|---|---|
| Mutate `option.Client` (object dung chung): gan `BaseAddress`, `Add` them `Accept: application/json`, ghi de `DefaultRequestHeaders.Authorization` | `HttpClientUtilizes.cs:347-357` |
| Goi API ngoai (`SendAsync`) | vi du `CallApiWithHttp.cs:1510` |
| Ghi log Information tracing moi lan goi (ke ca khi loi) | vi du `CallApiWithHttp.cs:1560` |
| Ghi log Warning khi latency > `desiredTime` | `MeasureExecutionTimeExtensions.cs:86` |
| Ghi log Error khi vao `catch` | vi du `CallApiWithHttp.cs:1529` (`catch` bat dau `:1525`) |
| **Serialize toan bo `option` (bao gom `Token` va `Client`) vao message log** | vi du `CallApiWithHttp.cs:1577` |
| **Serialize toan bo `result` — bao gom `data` (payload response da deserialize) — vao message log** | vi du `CallApiWithHttp.cs:1577` |

> [!CAUTION]
> Message log duoc tao bang `string.Format("{{\"Option\":{0},\"Result\":{1}}}", System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result))`.
> - `HttpOptionModel.Token` la property `public string` (`HttpOptionModel.cs:9`) nen **gia tri token duoc ghi nguyen van vao log**. `option.Client` (`HttpClient`) cung nam trong pham vi serialize.
> - `JsonConvert` la `Newtonsoft.Json` (`CallApiWithHttp.cs:3`) va serialize ca public field, nen `result.data` — **toan bo payload response** — cung bi ghi vao log Information o **moi** lan goi, ke ca khi thanh cong.

---

### 2.1 GetAsJSonAsync

**Signature**

```csharp
public static async Task<(TResponse, ErrorModel)> GetAsJSonAsync(
    HttpOptionModel option, ILogger logger,
    HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
    int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
```

**Muc dich** — Gui `HttpMethod.Get` toi `option.Uri` (khong body, khong query string tu object), deserialize response thanh `TResponse`. Than ham: `CallApiWithHttp.cs:1482-1579`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `option` | `HttpOptionModel` | Co | **Khong validate null.** `option.Client` phai khac null (`HttpClientUtilizes.cs:345`). `option.BaseAddress` chi duoc gan khi khac rong (`:347`). `option.Uri` truyen thang vao `HttpRequestMessage` | — |
| `logger` | `ILogger` | Co | **Khong validate null**; duoc dung ca trong `finally` | — |
| `versionPolicy` | `HttpVersionPolicy` | Khong | Bi ghi de thanh `RequestVersionOrLower` khi `ASPNETCORE_ENVIRONMENT == "Local"` (`HttpClientUtilizes.cs:280-287`) | `HttpVersionPolicy.RequestVersionOrLower` |
| `desiredTime` | `int` (giay) | Khong | Khong validate; so sanh `elapsed > desiredTime` (`MeasureExecutionTimeExtensions.cs:84`) | `3` |
| `cancellationTokenTime` | `int` (giay) | Khong | `CancelAfter` chi ap dung khi `> 0` (`CancellationTokenHelper.cs:16`); `<= 0` -> khong timeout | `15` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` truoc `try` (`CallApiWithHttp.cs:1487`) | `default` |

**Output** — `Task<(TResponse, ErrorModel)>`

| Truong hop | Item1 (`data`) | Item2 (`errorModel`) |
|---|---|---|
| HTTP 2xx + body JSON deserialize thanh cong | Object `TResponse` | `Code` = status thuc te, `Message` = `ReasonPhrase`, `Succeeded = true` |
| HTTP 2xx + body rong / khong parse duoc | `null` (`ReadAsStreamAsync` tra `default`, `HttpClientUtilizes.cs:339`) | `Succeeded = true`, `Code` = status 2xx |
| HTTP 4xx/5xx co `Content-Type` JSON | Ket qua deserialize body loi vao `TResponse` — co the khac null | `Succeeded = false`, `Code` = 4xx/5xx, `Message` = `ReasonPhrase` |
| Response khong co `Content-Type` (vi du 204) hoac `text/html` | `null` | `Succeeded = false`, `Code` = status code cua response, `Message` = noi dung body dang string |
| Timeout / bi huy trong luc gui | `null` | `Code = 408`, `Succeeded = false`, message tieng Viet |
| Exception khac | `null` | `Code = 500`, `Succeeded = false`, message tieng Viet |
| `cancellationToken` da huy truoc khi vao `try` | Khong tra ve — **nem `OperationCanceledException`** | — |

**Dieu kien xu ly** — Xem muc 2.0. Khong co guard clause rieng.

**Side effect** — Nhu muc 2.0.

**Error handling** — Nhu bang o muc 2.0.

**Khi nao NEN dung** — Goi GET endpoint tra JSON, khi URL da duoc dung san day du (ke ca query string) trong `option.Uri`, va caller khong can doc response header.

**Khi nao KHONG dung** — Khi can build query string tu object (dung `CallApiWithHttp<TRequest, TResponse>` thay vi lop nay); khi can doc response header (dung `GetAsJSonAndHeaderAsync`); khi can header tuy chinh (dung `GetAsJSonCustomHeaderAsync`); khi khong duoc phep ghi `Token` vao log.

**Gioi han**

- `HttpVersion.Version20` **hardcode** (`CallApiWithHttp.cs:1504`) — khong the yeu cau HTTP/1.1 hay HTTP/3 qua tham so.
- Khong phan biet duoc "2xx nhung body rong" va "2xx nhung JSON sai schema": ca hai deu tra `data = null` voi `Succeeded = true`.
- Non-2xx **khong** ngan buoc deserialize: body loi van bi deserialize vao `TResponse`. Doan `EnsureSuccessStatusCode()` bi comment tai `HttpClientUtilizes.cs:412-415` chi bao phu dieu kien `StatusCode >= HttpStatusCode.InternalServerError` (5xx); ngay ca khi bo comment thi **4xx van khong bao gio nem**.
- `EnsureSuccessOrException` gan `errorModel.Message = httpResponseMessage.ReasonPhrase` (`HttpClientUtilizes.cs:409`). Source code khong bao dam gia tri nay khac null; `Message` tren nhanh thanh cong co the la `null`. Caller khong nen dua vao `Message` o nhanh 2xx.
- Mutate `option.Client` dung chung: neu `HttpClient` da gui request truoc do, viec gan `BaseAddress` (`HttpClientUtilizes.cs:349`) nem `InvalidOperationException` — bi bat boi block `catch (Exception)` va bien thanh `Code = 500`.
- `DefaultRequestHeaders.Accept.Add(...)` duoc goi moi lan (`HttpClientUtilizes.cs:352`) -> danh sach `Accept` tang dan tren client dung chung.
- `Authorization` chi duoc gan khi `option.Token` **khac rong** (`HttpClientUtilizes.cs:354-357`) va **khong bao gio bi xoa**: neu goi voi `Token` rong tren mot `HttpClient` da tung duoc gan `Authorization` truoc do, request van mang token cu -> co the goi API bang credential cua luong/nguoi dung khac.
- Khong an toan khi nhieu luong dung chung mot `HttpClient` instance: `DefaultRequestHeaders` bi ghi/doc dong thoi.

---

### 2.2 GetAsJSonAndHeaderAsync

**Signature**

```csharp
public static async Task<(TResponse, ErrorModel, HttpResponseHeaders)> GetAsJSonAndHeaderAsync(
    HttpOptionModel option, ILogger logger,
    HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
    int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
```

**Muc dich** — Giong `GetAsJSonAsync` nhung tra ve them `httpResponseMessage.Headers`. Than ham: `CallApiWithHttp.cs:1593-1691`.

**Input hop le** — Y het bang o muc 2.1.

**Output** — `Task<(TResponse, ErrorModel, HttpResponseHeaders)>`

| Truong hop | `data` | `errorModel` | `Headers` |
|---|---|---|---|
| Deserialize thanh cong (`CallApiWithHttp.cs:1632`) | `TResponse` | Nhu 2.1 | `httpResponseMessage.Headers` |
| `OperationCanceledException` | `null` | `Code = 408` | `null!` |
| `CustomException` | `null` | `Code = exception.Code` | `null!` |
| `Exception` khac | `null` | `Code = 500` | `null!` |

**Dieu kien xu ly** — Nhu muc 2.0; khong co guard clause rieng.

**Side effect** — Nhu muc 2.0.

**Error handling** — Nhu muc 2.0, nhung tuple tra ve co 3 phan tu, phan tu thu 3 luon `null!`.

**Khi nao NEN dung** — Khi can doc header phan hoi: phan trang (`X-Total-Count`), `ETag`, rate limit, correlation id.

**Khi nao KHONG dung** — Khi can doc **content header** (`Content-Type`, `Content-Length`, `Content-Disposition`): chung nam o `httpResponseMessage.Content.Headers`, **khong** duoc tra ve. Khong dung khi can header trong tinh huong loi (luon `null`).

**Gioi han**

- Chi tra ve `Headers`, **khong** tra ve `Content.Headers`.
- `httpResponseMessage` **khong** duoc `using`/`Dispose` (`CallApiWithHttp.cs:1619`). Day **khong** phai dac thu cua method nay: **ca 9 method** deu khai bao `HttpResponseMessage` khong co `using` (chi `HttpRequestMessage` va `CancellationTokenSource` moi co `using`). Diem rieng cua method nay la `Headers` cua object chua dispose duoc tra ra ngoai; hanh vi truy cap `Headers` sau khi method ket thuc khong duoc bao dam trong source code.
- Tat ca gioi han o muc 2.1 ap dung y nguyen.

---

### 2.3 GetAsJSonCustomHeaderAsync

**Signature**

```csharp
public static async Task<(TResponse, ErrorModel)> GetAsJSonCustomHeaderAsync(
   HttpOptionModel option, IEnumerable<KeyValuePair<string, string>> headers, ILogger logger,
   HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
   int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
```

**Muc dich** — GET co header tuy chinh. Header duoc add vao **`requestMessage.Headers`** — pham vi mot request duy nhat, khong anh huong sang cac call khac dung cung `HttpClient`. Than ham: `CallApiWithHttp.cs:1706-1809`; vong lap add header: `CallApiWithHttp.cs:1732-1735`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `option` | `HttpOptionModel` | Co | Nhu 2.1 | — |
| `headers` | `IEnumerable<KeyValuePair<string, string>>` | Co | **Khong co null check** — `foreach` truc tiep tai `CallApiWithHttp.cs:1732`. Key/value phai hop le voi `HttpRequestHeaders.Add`; content header (`Content-Type`,...) se lam `Add` nem `InvalidOperationException` | — |
| `logger` | `ILogger` | Co | Khong validate null | — |
| `versionPolicy` | `HttpVersionPolicy` | Khong | Nhu 2.1 | `HttpVersionPolicy.RequestVersionOrLower` |
| `desiredTime` | `int` | Khong | Nhu 2.1 | `3` |
| `cancellationTokenTime` | `int` | Khong | Nhu 2.1 | `15` |
| `cancellationToken` | `CancellationToken` | Khong | Nhu 2.1 | `default` |

**Output** — Giong bang o muc 2.1. Them: neu `headers` la `null` -> `NullReferenceException` bi bat boi `catch (Exception)` -> `(null, errorModel)` voi `Code = 500`, khong request nao duoc gui.

**Dieu kien xu ly**

1. `ThrowIfCancellationRequested` (ngoai `try`).
2. `option.ConfigHttpClient()`.
3. Tao `CancellationTokenSource`.
4. Tao `HttpRequestMessage` (`HttpMethod.Get`) — **khong set `Content`**.
5. `foreach (KeyValuePair<string, string> header in headers)` -> `requestMessage.Headers.Add(header.Key, header.Value)` (`CallApiWithHttp.cs:1734`).
6. `SendAsync` -> `EnsureSuccessOrException` -> `ResponseResult<TResponse>`.

**Side effect** — Nhu muc 2.0. **Khong** mutate `client.DefaultRequestHeaders` bang cac header truyen vao (khac han `PostWithHeadersAsJSonAsync`).

**Error handling** — Nhu muc 2.0.

**Khi nao NEN dung** — Khi can header dac thu cho **rieng mot** request (correlation id, `X-Request-Id`, API key cua ben thu ba, header ngon ngu), dac biet khi `HttpClient` la instance dung chung tu `IHttpClientFactory`.

**Khi nao KHONG dung** — Khi `headers` co the null (khong co guard, se thanh loi 500 mo ho). Khi can dat content header — `HttpRequestHeaders.Add` khong nhan header thuoc nhom content va se nem. Khi khong duoc phep ghi gia tri header/token vao log (`option` van bi serialize).

**Gioi han**

- Khong co null check cho `headers` (`CallApiWithHttp.cs:1732`).
- Khong loai bo/ghi de header trung: `Add` cong don gia tri chu khong thay the.
- Header co ky tu khong hop le -> `FormatException`/`InvalidOperationException` -> bien thanh `Code = 500` chung, mat thong tin nguyen nhan trong `ErrorModel` (chi con trong log).
- Cac gioi han o muc 2.1 ap dung y nguyen.

---

### 2.4 PostAsJSonAsync

**Signature**

```csharp
public static async Task<(TResponse, ErrorModel)> PostAsJSonAsync(
    HttpOptionModel option, ILogger logger,
    HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
    int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
```

**Muc dich** — Gui `HttpMethod.Post` toi `option.Uri`. Than ham: `CallApiWithHttp.cs:1827-1924`.

> [!WARNING]
> `HttpRequestMessage` duoc khoi tao tai `CallApiWithHttp.cs:1847-1851` chi voi `Version` va `VersionPolicy`; **khong co dong nao gan `Content`**. Request POST duoc gui **khong co body** va **khong co header `Content-Type`**. Ten method chua chu `AsJSon` nhung khong co JSON nao duoc gui di — `AsJSon` chi mo ta chieu **doc** response.

**Input hop le** — Y het bang o muc 2.1 (khong co tham so nao mang du lieu body).

**Output** — Giong bang o muc 2.1. Luu y them: nhieu server tra `204 No Content` cho POST khong body; khi do `Content-Type` thieu -> `ResponseResult` nem `CustomException` (`HttpClientUtilizes.cs:366-372`) -> ket qua la `(null, errorModel)` voi `Succeeded = false`, `Code = 204`, `Message` = body dang string (thuong la rong).

**Dieu kien xu ly** — Nhu muc 2.0, khong co guard clause rieng.

**Side effect** — Nhu muc 2.0. Ngoai ra: **goi POST len he thong ngoai** nen co the tao/thay doi du lieu ben phia server nhan.

**Error handling** — Nhu muc 2.0.

**Khi nao NEN dung** — Endpoint dang trigger/action khong can body: `POST /jobs/{id}/retry`, `POST /cache/refresh`, `POST /session/logout`, hoac endpoint nhan toan bo tham so qua path/query da nam trong `option.Uri`.

**Khi nao KHONG dung**

- **Khi endpoint yeu cau body JSON** — day la loi thuong gap nhat. Server se tra `400 Bad Request` hoac `415 Unsupported Media Type` vi request khong co body va khong co `Content-Type`. Dung `CallApiWithHttp<TRequest, TResponse>.PostAsJSonAsync` cho truong hop nay.
- Khi can gui file/form -> dung `PostFormDataAsJSonAsync`.
- Khi can header tuy chinh ma khong muon lam ban `HttpClient` dung chung — `PostWithHeadersAsJSonAsync` add vao `DefaultRequestHeaders`, khong co bien the add vao `HttpRequestMessage.Headers` cho POST trong lop nay.
- Khi thao tac khong idempotent va co the timeout: khong co retry, cung khong co idempotency key.

**Gioi han**

- **Khong bao gio gui body** (`CallApiWithHttp.cs:1847-1851`).
- Khong co `Content-Type` request header.
- Server tra 204/khong co `Content-Type` se bi coi la loi (`Succeeded = false`) du thao tac da thanh cong -> **false negative**.
- Cac gioi han o muc 2.1 ap dung y nguyen.

---

### 2.5 PostFormDataAsJSonAsync

**Signature**

```csharp
public static async Task<(TResponse, ErrorModel)> PostFormDataAsJSonAsync(
    HttpOptionModel option, MultipartFormDataContent form, ILogger logger,
    HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
    int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
```

**Muc dich** — Gui POST voi body `multipart/form-data` do **caller tu xay dung san**. Day la **method duy nhat trong `CallApi<TResponse>` co set `Content`** (`CallApiWithHttp.cs:1978`). Than ham: `CallApiWithHttp.cs:1940-2055`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `option` | `HttpOptionModel` | Co | **Khong goi `ConfigHttpClient`**. Thay vao do: `client = option.Client` (`:1960`); gan `BaseAddress` khi khac rong (`:1962-1965`); gan `Authorization` khi `Token` khac rong (`:1967-1971`). **Khong** them header `Accept: application/json` | — |
| `form` | `MultipartFormDataContent` | Co (co guard) | `if (form is null) return result;` tai `CallApiWithHttp.cs:1953-1956` — return **truoc** `try` | — |
| `logger` | `ILogger` | Co | Khong validate null | — |
| `versionPolicy` | `HttpVersionPolicy` | Khong | Nhu 2.1 | `HttpVersionPolicy.RequestVersionOrLower` |
| `desiredTime` | `int` | Khong | Nhu 2.1 | `3` |
| `cancellationTokenTime` | `int` | Khong | Nhu 2.1. **15 giay** thuong qua ngan cho upload file lon | `15` |
| `cancellationToken` | `CancellationToken` | Khong | Nhu 2.1 | `default` |

**Output** — `Task<(TResponse, ErrorModel)>`

| Truong hop | `data` | `errorModel` |
|---|---|---|
| `form is null` | `null` | Instance moi chua bi map: `Code = 0`, `Message = null`, `Succeeded = false`. **Khong co request nao duoc gui, khong co log tracing nao duoc ghi** |
| Upload thanh cong, response JSON | `TResponse` | `Code` = status, `Succeeded = true` |
| Non-2xx | Ket qua deserialize body loi | `Succeeded = false`, `Code` = status |
| Response khong co `Content-Type` / `text/html` | `null` | `Succeeded = false`, `Code` = status, `Message` = body string |
| Timeout | `null` | `Code = 408` |
| Exception khac | `null` | `Code = 500` |

**Dieu kien xu ly**

1. `ThrowIfCancellationRequested` (ngoai `try`).
2. `start`, `errorModel`, `result` khoi tao.
3. **Guard: `if (form is null) return result;`** (`CallApiWithHttp.cs:1953`) — thoat truoc `try`/`finally`.
4. `client = option.Client`.
5. Neu `option.BaseAddress` khac rong -> `client.BaseAddress = new Uri(option.BaseAddress)`.
6. Neu `option.Token` khac rong -> `client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(option.AuthType, option.Token)`.
7. Tao `CancellationTokenSource`.
8. Tao `HttpRequestMessage(HttpMethod.Post, option.Uri)` voi `Content = form`.
9. `SendAsync` -> `EnsureSuccessOrException` -> `ResponseResult<TResponse>`.

**Side effect**

- Mutate `option.Client`: gan `BaseAddress` va `Authorization` (`CallApiWithHttp.cs:1964`, `:1969-1970`).
- **`form` bi dispose**: `httpRequestMessage` duoc khai bao `using` (`CallApiWithHttp.cs:1976`); `HttpRequestMessage.Dispose()` dispose luon `Content`, tuc la `form` cua caller — sau khi method tra ve, `form` khong con dung lai duoc.
- Gui du lieu (co the la file) len he thong ngoai.
- Ghi log tracing/warning/error nhu muc 2.0 (tru truong hop `form is null`).

**Error handling** — Nhu bang o muc 2.0. Rieng nhanh `form is null` **khong** duoc coi la loi: khong log, khong set `ErrorModel.Message`.

**Khi nao NEN dung** — Upload file / gui `multipart/form-data` khi caller da tu tao `MultipartFormDataContent` (them `StreamContent`, `ByteArrayContent`, `StringContent`, dat ten field va filename).

**Khi nao KHONG dung**

- Khi can **tai su dung** `form` cho nhieu lan goi hoac cho retry — `form` bi dispose sau lan goi dau.
- Khi can phan biet "chua truyen form" voi "loi he thong": ca hai deu cho `Succeeded = false`, nhung `form is null` cho `Code = 0` va **khong co log**, rat kho dieu tra.
- Upload file lon / mang cham voi timeout mac dinh 15 giay — phai tang `cancellationTokenTime` mot cach tuong minh.
- Khi server bat buoc header `Accept: application/json`: method nay **khong** them header do (khac voi cac method dung `ConfigHttpClient`).

**Gioi han**

- Khong kiem tra `form` co phan tu nao khong — `MultipartFormDataContent` rong van duoc gui.
- `Authorization` chi duoc gan khi `option.Token` khac rong (`CallApiWithHttp.cs:1967-1971`) va khong bao gio bi xoa; ngoai ra vi khong goi `ConfigHttpClient` nen header `Accept` **khong** duoc them — nhung neu client dung chung da tung di qua `ConfigHttpClient` thi `Accept: application/json` (va `Authorization` cu) **van con** tren client. Hanh vi phu thuoc thu tu cac lan goi truoc do.
- Khong gioi han dung luong, khong dem so file, khong validate content type cua tung part.
- `Code = 0` khi `form is null` khong phai HTTP status hop le; caller kiem tra theo `Code` phai xu ly rieng gia tri 0.
- Trung lap logic cau hinh client thay vi dung `ConfigHttpClient` -> hanh vi lech so voi 8 method con lai (thieu `Accept`).
- Cac gioi han lien quan `HttpClient` dung chung o muc 2.1 ap dung y nguyen.

---

### 2.6 PostWithHeadersAsJSonAsync

**Signature**

```csharp
public static async Task<(TResponse, ErrorModel)> PostWithHeadersAsJSonAsync(
    HttpOptionModel option, Dictionary<string, string> headers, ILogger logger,
    HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
    int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
```

**Muc dich** — Gui POST kem header tuy chinh. Than ham: `CallApiWithHttp.cs:2070-2175`.

> [!WARNING]
> Hai diem can nam ro:
> 1. `HttpRequestMessage` tai `CallApiWithHttp.cs:2098-2102` **khong set `Content`** -> POST **khong co body**.
> 2. Header duoc add vao **`client.DefaultRequestHeaders`** (`CallApiWithHttp.cs:2091`), tuc la len **`HttpClient` dung chung**, khong phai len rieng request nay. Khac han `GetAsJSonCustomHeaderAsync` (add vao `requestMessage.Headers`, `CallApiWithHttp.cs:1734`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `option` | `HttpOptionModel` | Co | Nhu 2.1 (`ConfigHttpClient`) | — |
| `headers` | `Dictionary<string, string>` | Khong | Co null check: `if (headers != null)` (`CallApiWithHttp.cs:2087`) -> null thi bo qua. Source code **khong** kiem tra trung key, **khong** `Remove`/`Clear` truoc khi `Add` (`CallApiWithHttp.cs:2091`): goi lai voi cung key tren cung `HttpClient` se **cong don gia tri** vao header do (voi header tuy chinh) hoac lam `HttpHeaders.Add` nem exception (voi header chuan chi cho phep 1 gia tri). Loai exception cu the do runtime `HttpClient` quyet dinh — **khong xac dinh duoc tu source code** | — |
| `logger` | `ILogger` | Co | Khong validate null | — |
| `versionPolicy` | `HttpVersionPolicy` | Khong | Nhu 2.1 | `HttpVersionPolicy.RequestVersionOrLower` |
| `desiredTime` | `int` | Khong | Nhu 2.1 | `3` |
| `cancellationTokenTime` | `int` | Khong | Nhu 2.1 | `15` |
| `cancellationToken` | `CancellationToken` | Khong | Nhu 2.1 | `default` |

**Output** — Giong bang o muc 2.1.

**Dieu kien xu ly**

1. `ThrowIfCancellationRequested` (ngoai `try`).
2. `option.ConfigHttpClient()`.
3. `if (headers != null)` -> `foreach` -> `client.DefaultRequestHeaders.Add(header.Key, header.Value)` (`CallApiWithHttp.cs:2087-2093`).
4. Tao `CancellationTokenSource`.
5. Tao `HttpRequestMessage(HttpMethod.Post, option.Uri)` — **khong `Content`**.
6. `SendAsync` -> `EnsureSuccessOrException` -> `ResponseResult<TResponse>`.

**Side effect**

- **Mutate vinh vien `option.Client.DefaultRequestHeaders`**: header duoc `Add` va **khong bao gio duoc remove** — khong co `Remove`/`Clear` trong than ham. Header con lai tren client va di theo **moi request sau do** cua cung instance `HttpClient`.
- Cac side effect dung chung o muc 2.0.
- Goi POST len he thong ngoai.

**Error handling** — Nhu bang o muc 2.0. Truong hop dac thu: `client.DefaultRequestHeaders.Add` nam **trong** `try` (`CallApiWithHttp.cs:2085-2093`) nhung **truoc** khi tao `HttpRequestMessage`, nen bat ky exception tu buoc add header (header trung tren header chuan 1-gia-tri, ten/gia tri header khong hop le, content header dung sai cho) deu bi `catch (Exception)` bat -> tra `Code = 500` va **request khong duoc gui**. Voi header tuy chinh trung key thi khong co exception — gia tri chi bi cong don, loi im lang.

**Khi nao NEN dung** — Chi khi `option.Client` la instance dung rieng, dung mot lan (khong lay tu pool/`IHttpClientFactory` dung chung), va endpoint that su **khong can body**.

**Khi nao KHONG dung**

- Khi endpoint can body JSON — method nay khong gui body.
- Khi `option.Client` la instance dung chung (`IHttpClientFactory`, singleton, static): header ro ri sang cac request khac (nguy hiem voi header chua thong tin nhay cam) va lan goi thu hai cung key se lam gia tri header bi cong don (hoac nem exception voi header chuan chi cho phep 1 gia tri).
- Khi co the co nhieu luong goi song song tren cung `HttpClient` — `DefaultRequestHeaders` khong thread-safe.
- Khi can header chi cho mot request: hien tai lop `CallApi<TResponse>` **khong co** method POST nao add header vao `HttpRequestMessage.Headers`.

**Gioi han**

- **Khong gui body** (`CallApiWithHttp.cs:2098-2102`).
- Khong go header sau khi gui (`CallApiWithHttp.cs:2087-2093` khong co `Remove`/`Clear`) -> ro ri va tich luy header.
- Khong xu ly header trung: `Add` **cong don gia tri** chu khong ghi de; voi header chuan chi cho phep 1 gia tri thi `Add` nem exception (loai exception do runtime `HttpClient` quyet dinh, khong xac dinh duoc tu source code).
- Kieu tham so la `Dictionary<string, string>` (khong phai `IEnumerable<KeyValuePair<string, string>>` nhu `GetAsJSonCustomHeaderAsync`) -> API khong nhat quan giua hai method.
- Cac gioi han o muc 2.1 ap dung y nguyen.

---

### 2.7 PutAsJSonAsync

**Signature**

```csharp
public static async Task<(TResponse, ErrorModel)> PutAsJSonAsync(
    HttpOptionModel option, ILogger logger,
    HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
    int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
```

**Muc dich** — Gui `HttpMethod.Put` toi `option.Uri`. Than ham: `CallApiWithHttp.cs:2192-2289`.

> [!WARNING]
> `HttpRequestMessage` tai `CallApiWithHttp.cs:2212-2216` **khong set `Content`** -> PUT duoc gui **khong co body**. Voi semantics HTTP, PUT la thao tac "thay the toan bo resource"; gui PUT khong body la truong hop rat it khi dung.

**Input hop le** — Y het bang o muc 2.1.

**Output** — Giong bang o muc 2.1. PUT thuong tra `204 No Content` -> thieu `Content-Type` -> `ResponseResult` nem `CustomException` -> `Succeeded = false` du server da xu ly xong.

**Dieu kien xu ly** — Nhu muc 2.0, khong co guard clause rieng.

**Side effect** — Nhu muc 2.0; ngoai ra co the thay doi du lieu ben server nhan.

**Error handling** — Nhu muc 2.0.

**Khi nao NEN dung** — Endpoint PUT khong nhan body, toan bo tham so nam trong path/query cua `option.Uri` (vi du `PUT /flags/{key}/enable`).

**Khi nao KHONG dung** — Khi can cap nhat resource bang du lieu JSON (dung `CallApiWithHttp<TRequest, TResponse>.PutAsJSonAsync`). Khi server tra 204 va caller dua vao `Succeeded` de xac dinh thanh cong.

**Gioi han**

- **Khong bao gio gui body**; khong co `Content-Type`.
- 204 bi map thanh loi -> false negative.
- Cac gioi han o muc 2.1 ap dung y nguyen.

---

### 2.8 DeleteAsJSonAsync

**Signature**

```csharp
public static async Task<(TResponse, ErrorModel)> DeleteAsJSonAsync(
    HttpOptionModel option, ILogger logger,
    HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
    int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
```

**Muc dich** — Gui `HttpMethod.Delete` toi `option.Uri`. Than ham: `CallApiWithHttp.cs:2306-2403`. Khong set `Content` (`CallApiWithHttp.cs:2326-2330`) — phu hop voi semantics DELETE thong thuong.

**Input hop le** — Y het bang o muc 2.1. Moi dinh danh resource can xoa phai da nam trong `option.Uri`; lop nay **khong** build query string.

**Output** — Giong bang o muc 2.1. DELETE thuong tra `204 No Content` -> thieu `Content-Type` -> `CustomException` -> `Succeeded = false` du xoa da thanh cong.

**Dieu kien xu ly** — Nhu muc 2.0, khong co guard clause rieng.

**Side effect** — Nhu muc 2.0. **Xoa du lieu ben he thong nhan** — thao tac pha huy, khong the hoan tac tu phia client.

**Error handling** — Nhu muc 2.0.

**Khi nao NEN dung** — Xoa resource theo id da nhung trong `option.Uri`, khi API tra body JSON xac nhan ket qua.

**Khi nao KHONG dung**

- Khi API DELETE tra `204 No Content` va caller dua vao `Succeeded` -> se bao loi sai (xem muc 3, van de #4).
- Khi can DELETE co body (mot so API nhan danh sach id trong body) — method nay khong gui body.
- Khi can bao dam khong xoa trung: khong co retry nen khong co van de xoa lap tu module, nhung cung khong co xac nhan/idempotency check nao.

**Gioi han**

- Khong build query string tu object.
- Khong co co che confirm/dry-run.
- 204 bi map thanh loi.
- Cac gioi han o muc 2.1 ap dung y nguyen.

---

### 2.9 PatchAsJSonAsync

**Signature**

```csharp
public static async Task<(TResponse, ErrorModel)> PatchAsJSonAsync(
    HttpOptionModel option, ILogger logger,
    HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
    int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
```

**Muc dich** — Gui `HttpMethod.Patch` toi `option.Uri`. Than ham: `CallApiWithHttp.cs:2420-2517`.

> [!WARNING]
> `HttpRequestMessage` tai `CallApiWithHttp.cs:2440-2444` **khong set `Content`** -> PATCH duoc gui **khong co body**. PATCH ve ban chat la "gui tap thay doi"; khong co body thi khong co thay doi nao duoc mo ta. Day la method de dung sai nhat trong nhom.

**Input hop le** — Y het bang o muc 2.1.

**Output** — Giong bang o muc 2.1.

**Dieu kien xu ly** — Nhu muc 2.0, khong co guard clause rieng. Khac biet cu phap duy nhat: `using var cancellationTokenSource` (`CallApiWithHttp.cs:2437`) thay vi khai bao kieu tuong minh `using CancellationTokenSource ...` nhu cac method khac — khong lam thay doi hanh vi.

**Side effect** — Nhu muc 2.0; co the thay doi du lieu ben server nhan.

**Error handling** — Nhu muc 2.0.

**Khi nao NEN dung** — Chi khi endpoint PATCH duoc thiet ke nhan toan bo tham so qua path/query va **khong** doc body.

**Khi nao KHONG dung** — Hau het cac tinh huong PATCH thuc te (JSON Patch, JSON Merge Patch, partial update): dung `CallApiWithHttp<TRequest, TResponse>.PatchAsJSonAsync`. Server yeu cau `Content-Type: application/json-patch+json` se tra `415`.

**Gioi han**

- **Khong bao gio gui body**; khong co `Content-Type`.
- Khong ho tro cu phap JSON Patch (`op`/`path`/`value`).
- Cac gioi han o muc 2.1 ap dung y nguyen.

---

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `PostAsJSonAsync`, `PostWithHeadersAsJSonAsync`, `PutAsJSonAsync`, `PatchAsJSonAsync` tao `HttpRequestMessage` khong set `Content` -> gui request **khong co body**, khong co `Content-Type`. Ten method chua `AsJSon` de gay hieu nham la co body JSON | `CallApiWithHttp.cs:1847`, `:2098`, `:2212`, `:2440` | Cao. Dev nham tuong co body -> server tra 400/415 hoac cap nhat rong. Chi `PostFormDataAsJSonAsync` (`:1978`) thuc su co body |
| 2 | Header trong `PostWithHeadersAsJSonAsync` duoc `Add` vao `client.DefaultRequestHeaders` va **khong bao gio duoc remove**; trong khi `GetAsJSonCustomHeaderAsync` add vao `requestMessage.Headers` (dung pham vi request) | `CallApiWithHttp.cs:2091` so voi `:1734` | Cao. Header ro ri sang moi request sau tren cung `HttpClient`; goi lan hai cung key -> gia tri header bi **cong don** (header tuy chinh) hoac `Add` nem exception -> `Code = 500`, request khong duoc gui (header chuan 1-gia-tri); khong thread-safe |
| 3 | `EnsureSuccessOrException` **khong nem** voi bat ky status nao — no chi gan `Code`/`Message`/`Succeeded` — nen luong xu ly tiep tuc deserialize body loi vao `TResponse`. Doan `EnsureSuccessStatusCode()` bi comment chi bao phu `StatusCode >= 500`, tuc la **4xx chua bao gio nem** ke ca truoc khi bi comment | `HttpClientUtilizes.cs:401-416` (code bi comment tai `:412-415`) | Cao. Caller co the nhan `data` khac null trong khi `Succeeded = false`; neu chi kiem tra `data != null` se coi loi la thanh cong |
| 4 | Response khong co `Content-Type` (dien hinh `204 No Content`) bi `ResponseResult` nem `CustomException` -> `Succeeded = false` | `HttpClientUtilizes.cs:364-372` | Cao. POST/PUT/PATCH/DELETE khong body thuong tra 204 -> bao loi sai (false negative) du thao tac da thanh cong |
| 5 | Message log duoc tao boi `System.Text.Json.JsonSerializer.Serialize(option)`, trong khi `HttpOptionModel.Token` la property public | `CallApiWithHttp.cs:1577`, `:1689`, `:1807`, `:1922`, `:2053`, `:2173`, `:2287`, `:2401`, `:2515`; `HttpOptionModel.cs:9` | Cao (bao mat). Token/credential bi ghi vao log o **moi** lan goi, ke ca khi thanh cong |
| 5b | Cung dong log do con `JsonConvert.SerializeObject(result)` — `JsonConvert` la `Newtonsoft.Json` (`CallApiWithHttp.cs:3`) va serialize ca public field cua `ValueTuple`, nen `result.data` (**toan bo payload response da deserialize**) bi ghi vao log Information | `CallApiWithHttp.cs:1577`, `:1689`, `:1807`, `:1922`, `:2053`, `:2173`, `:2287`, `:2401`, `:2515` | Cao (bao mat + dung luong log). Du lieu nghiep vu/PII trong response bi do vao log o moi lan goi thanh cong; khong co co che mask hay gioi han kich thuoc |
| 6 | `ConfigHttpClient` mutate `HttpClient` dung chung moi lan goi: gan lai `BaseAddress`, `Add` them `Accept: application/json`, ghi de `Authorization` | `HttpClientUtilizes.cs:343-360` | Cao. Gan `BaseAddress` sau khi client da gui request -> `InvalidOperationException` -> `Code = 500`; `Accept` tich luy khong gioi han; `Authorization` cua luong khac bi ghi de |
| 6b | `Authorization` chi duoc **gan khi `option.Token` khac rong** va **khong bao gio duoc xoa/reset**. Goi voi `Token` rong tren `HttpClient` da tung mang `Authorization` -> request van gui token cu | `HttpClientUtilizes.cs:354-357`; `CallApiWithHttp.cs:1967-1971` | Cao (bao mat). Token cua luong/nguoi dung truoc bi tai su dung cho request khong dinh danh; khong the goi "anonymous" tren client dung chung |
| 7 | `PostFormDataAsJSonAsync` khong dung `ConfigHttpClient` ma tu cau hinh client, **thieu header `Accept: application/json`** | `CallApiWithHttp.cs:1960-1971` so voi `HttpClientUtilizes.cs:352` | Trung binh. Hanh vi khong nhat quan; server negotiate content type co the tra dinh dang khac JSON -> `ResponseResult` nem `CustomException` |
| 8 | `PostFormDataAsJSonAsync`: guard `form is null` `return` **truoc** `try`/`finally` -> khong ghi log tracing va tra `ErrorModel` chua duoc map (`Code = 0`, `Message = null`) | `CallApiWithHttp.cs:1953-1956` | Trung binh. Loi im lang, khong dau vet trong log; `Code = 0` khong phai HTTP status hop le |
| 9 | `form` bi dispose ngoai y muon: `using var httpRequestMessage` dispose luon `Content` (= `form` cua caller) | `CallApiWithHttp.cs:1976-1981` | Trung binh. Khong the retry hay tai su dung `form`; truy cap sau do -> `ObjectDisposedException` |
| 10 | `GetAsJSonCustomHeaderAsync` khong null check `headers` truoc `foreach` (trong khi `PostWithHeadersAsJSonAsync` co check) | `CallApiWithHttp.cs:1732` so voi `:2087` | Trung binh. `headers = null` -> `NullReferenceException` -> `Code = 500` mo ho, khong phan biet duoc voi loi he thong that |
| 11 | **Ca 9 method** deu khong `Dispose` `HttpResponseMessage` (khong method nao dat `using` cho bien nay; chi `HttpRequestMessage` va `CancellationTokenSource` co `using`). Rieng `GetAsJSonAndHeaderAsync` con tra `Headers` cua object chua dispose ra ngoai | `CallApiWithHttp.cs:1508`, `:1619`, `:1632-1633`, `:1737`, `:1853`, `:1983`, `:2104`, `:2218`, `:2332`, `:2446` | Trung binh. Resource khong duoc giai phong tuong minh; hanh vi truy cap `Headers` sau khi method ket thuc khong duoc bao dam trong source code |
| 12 | Logic `DirectionType`: `HasPort(option.BaseAddress) == true` -> `Inbound`, `false` -> `Outbound`. Ca 9 method deu la loi goi **di ra** ben ngoai, nen nhan `Inbound` theo su co mat cua port la kho giai thich | `CallApiWithHttp.cs:1568`, `:1680`, `:1798`, `:1913`, `:2044`, `:2164`, `:2278`, `:2392`, `:2506`; `HttpClientUtilizes.cs:35-48` | Trung binh. Truong `Direction` trong log/tracing co the bi gan sai, anh huong dashboard va phan tich su co |
| 13 | `HttpVersion.Version20` hardcode trong ca 9 method; chi `VersionPolicy` la tham so hoa | `CallApiWithHttp.cs:1504`, `:1615`, `:1728`, `:1849`, `:1979`, `:2100`, `:2214`, `:2328`, `:2442` | Thap-Trung binh. Khong the chon HTTP/1.1 hay HTTP/3; phu thuoc `VersionPolicy` de downgrade |
| 14 | `catch (OperationCanceledException)` goi `logger.HttpErrorResult(className, methodName, message)` — overload **khong nhan exception** (`LoggerExtensions.cs:426`), khac 2 catch con lai dung overload co `e` (`:443`) | `CallApiWithHttp.cs:1529`, `:1641`, `:1759`, `:1874`, `:2005`, `:2125`, `:2239`, `:2353`, `:2467` | Thap-Trung binh. Mat stack trace khi timeout, kho phan biet timeout do client (`CancelAfter`) hay do caller huy |
| 15 | Toan bo `ErrorModel.Message` trong nhanh loi la cau tieng Viet huong nguoi dung; message ky thuat goc chi con trong log | `HttpClientUtilizes.cs:377-399` | Thap-Trung binh. Caller khong the phan loai loi theo message; `CustomException` la ngoai le vi giu message goc |
| 16 | `cancellationToken.ThrowIfCancellationRequested()` dat **ngoai** `try` nen exception thoat ra khoi method thay vi duoc map thanh `ErrorModel` (Code 408) — khac han khi bi huy trong luc `SendAsync` | `CallApiWithHttp.cs:1487`, `:1598`, `:1711`, `:1832`, `:1945`, `:2075`, `:2197`, `:2311`, `:2425` | Thap-Trung binh. Hai kieu hanh vi khac nhau cho cung nguyen nhan "bi huy"; caller khong boc `try/catch` se bi crash |
| 17 | Khong co retry, circuit breaker, hay fallback trong bat ky method nao | Toan bo `CallApiWithHttp.cs:1466-2520` | Thap-Trung binh. Loi tam thoi cua he thong ngoai truyen thang ve caller |
| 18 | XML doc cua **ca 4 method khong body** deu khai bao ro "không có body", khop voi than ham: `PostAsJSonAsync` (`:1816-1817`), `PostWithHeadersAsJSonAsync` (`:2058-2059`), `PutAsJSonAsync` (`:2182`), `PatchAsJSonAsync` (`:2410`); **tuy nhien** ten method (`...AsJSonAsync`) va tinh nhat quan voi lop `CallApiWithHttp<TRequest, TResponse>` van gay hieu nham | `CallApiWithHttp.cs:1816-1817`, `:2058-2059`, `:2182`, `:2410` | Thap. Khong phai mau thuan comment-vs-code (comment dung), nhung ten API gay nham lan. Khong phat hien mau thuan nao giua comment va than ham trong pham vi lop nay |
| 19 | `logger` khong duoc null check o bat ky method nao, du duoc su dung trong `finally` | vi du `CallApiWithHttp.cs:1560` | Thap. `logger = null` -> `NullReferenceException` tu `finally`, che mat exception goc |
| 20 | `option` khong duoc null check; `option.Client` cung khong | `HttpClientUtilizes.cs:345`; `CallApiWithHttp.cs:1960` | Thap. `NullReferenceException` -> bat boi `catch (Exception)` -> `Code = 500`; nhung neu `option` null thi `finally` cung nem khi doc `option.Uri` |
| 21 | Khoi `finally` **khong** duoc bao ve bang `try/catch`: no doc `option.Uri`/`option.SystemOwner`/`option.BaseAddress`, goi `JsonSerializer.Serialize(option)` (serialize ca `option.Client` la `HttpClient`) va `JsonConvert.SerializeObject(result)` | vi du `CallApiWithHttp.cs:1558-1578` | Trung binh. Bat ky exception phat sinh trong `finally` deu **thay the** gia tri tra ve / exception goc, lam mat ket qua that su cua request |
| 22 | `ReadAsStreamAsync` khi deserialize that bai se doc lai body bang `content.ReadAsStringAsync()` **sau khi** `Stream` da bi dispose boi `await using`. Ket qua chi dung khi content da duoc buffer, tuc khi `option.CompletionOption` la `ResponseContentRead` (mac dinh) | `HttpClientUtilizes.cs:321` so voi `:331`; `HttpOptionModel.cs:17` | Trung binh. Caller dat `option.CompletionOption = HttpCompletionOption.ResponseHeadersRead` thi nhanh xu ly loi nay khong duoc bao dam; exception phat sinh o day thoat ra khoi `ReadAsStreamAsync` va bien thanh `Code = 500` |
| 23 | Latency trong log tracing (`Stopwatch` tu dau method) va nguong canh bao cua `InvokeForHTTP` do **hai khoang khac nhau**: `InvokeForHTTP` chi do `SendAsync`, khong tinh buoc `ResponseResult`/deserialize | `CallApiWithHttp.cs:1489` va `:1574` so voi `MeasureExecutionTimeExtensions.cs:78-90` | Thap. Request cham do deserialize payload lon se **khong** kich hoat canh bao `[PERFORMANCE]`, du `responseTimeMs` trong log tracing van cao |
| 24 | `desiredTime` mac dinh cua `InvokeForHTTP` la `5` giay (`MeasureExecutionTimeExtensions.cs:68`) nhung ca 9 method cua `CallApi<TResponse>` truyen mac dinh `3` giay | `CallApiWithHttp.cs:1484` va cac signature tuong tu | Thap. Nguong canh bao thuc te la 3 giay, khac gia tri mac dinh cua ham do; de doc sai khi tra cuu tung noi |
