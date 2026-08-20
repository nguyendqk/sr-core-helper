# CallApiWithHttp&lt;TRequest, TResponse&gt;

> Nguon: `FTELSRCore.Shared/Utilizes/CallApiWithHttp.cs` (dong 16 - 1464, chi class `CallApiWithHttp<TRequest, TResponse>`)
> Loai: static class (generic, `where TResponse : class`)
> Cap nhat theo commit: `2262829`

## 1. Tong quan

`CallApiWithHttp<TRequest, TResponse>` la lop tien ich (utility layer) goi HTTP API ra ben ngoai, dung chung cho toan bo cac service trong SR Core. Lop nay bao boc `HttpClient.SendAsync` va gop cac viec sau vao mot lan goi:

- dung URL query string tu model generic `TRequest` — **chi 4 method** (`GetAsJSonAsync`, `GetAsJSonAndHeaderAsync`, `GetAsJSonCustomHeaderAsync`, `DeleteAsJSonAsync`); cac method POST/PUT/PATCH dung `option.Value` lam **body**, khong build query string;
- gan `Authorization` len `HttpClient` (moi method) va `Accept: application/json` (moi method **tru** `PostFormUrlEncodedAsync`, xem muc 3, van de #22);
- ap timeout bang `CancellationTokenSource` (`CancelAfter`) khi `cancellationTokenTime > 0`;
- deserialize response thanh `TResponse` bang `System.Text.Json`;
- chuyen **cac exception phat sinh trong block `try`** thanh cap `(TResponse, ErrorModel)`. Day **khong** phai hop dong "khong bao gio nem": phan build URL/body va `ThrowIfCancellationRequested()` nam **ngoai** `try`, exception tu do van nem thang ra caller (xem muc 3, van de #7).

Lop nam o tang Shared/Utilizes va khong chua business logic. Trong repo `sr-core-helper` **khong co call site nao** cua lop nay (day la thu vien dung chung, `grep "CallApiWithHttp<"` chi tra ve chinh file dinh nghia), nen tang nao goi lop nay **khong xac dinh duoc tu source code cua repo**. Moi method deu ghi mot dong log tracing trong block `finally` (`logger.HttpResultWithTracing`) va mot log canh bao neu request cham hon `desiredTime` (`MeasureExecutionTimeExtensions.InvokeForHTTP`).

Diem quan trong nhat khi doc tai lieu nay: **cac method KHONG dong nhat ve cach build query string, cach gan header, va cach doi xu voi HTTP status code loi**. Nhung khac biet do duoc lam ro o tung muc.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Goi GET / POST / PUT / DELETE / PATCH ra API ngoai qua `HttpClient` duoc caller cung cap (`option.Client`) | Khong tu tao / tu quan ly `HttpClient`, khong dung `IHttpClientFactory` ben trong |
| Build query string tu model `TRequest` bang reflection — **hai co che khac nhau**: `ParseModelToQueryString` (`CallApiWithHttp.cs:1429`) cho `GetAsJSonAsync`/`GetAsJSonAndHeaderAsync`/`DeleteAsJSonAsync`, va `HttpClientUtilizes.ToQueryString` (`HttpClientUtilizes.cs:111`) cho `GetAsJSonCustomHeaderAsync` | Khong ho tro property dang collection/array trong query string (chi goi `value.ToString()`, `CallApiWithHttp.cs:1450` va `HttpClientUtilizes.cs:119`) |
| Body JSON qua `System.Text.Json` (`CallApiWithHttp.cs:527`, `967`, `1094`, `1331`) | Khong cho phep caller thay doi `JsonSerializerOptions` khi serialize body (goi `Serialize(option.Value)` khong truyen options) |
| Body `application/x-www-form-urlencoded` (`PostFormUrlEncodedAsync`, `CallApiWithHttp.cs:425`) | Khong co method GET/PUT/PATCH/DELETE nao gui body form-urlencoded |
| Body `multipart/form-data` co upload `IFormFile` (`PostAsFileAsync`, `PostAsFileV2Async`) | Khong ho tro upload file cho PUT/PATCH |
| Them header tuy chinh cho GET (`CallApiWithHttp.cs:296`) va POST JSON (`CallApiWithHttp.cs:979`) | Khong co overload them header tuy chinh cho PUT / PATCH / DELETE / form-urlencoded / multipart |
| Gan `Authorization` theo `option.AuthType` + `option.Token` (`HttpClientUtilizes.cs:354-357`) | Khong tu refresh token, khong kiem tra token het han (viec do o `TokenExpirationHelperUtilizes`, khong duoc goi trong lop nay) |
| Ap timeout cho toan bo `SendAsync` bang `cancellationTokenTime` (`CancellationTokenHelper.cs:12-22`) | **Khong co retry noi tai** — trong lop nay khong co vong lap retry, khong co Polly pipeline nao duoc goi |
| Doc `HttpResponseHeaders` cua response (`GetAsJSonAndHeaderAsync`) | Khong tra ve raw body string / stream cho caller; chi tra ve `TResponse` da deserialize |
| Tra ve `ErrorModel` mang HTTP status code + message tieng Viet | Khong nem exception ra ngoai cho loi HTTP/network (tru cac truong hop tai muc 3) |
| Ghi log tracing (`className`, `methodName`, `uri`, `statusCode`, `responseTimeMs`, `direction`) trong `finally` | Khong mask/ẩn `Token` va payload trong log tracing (xem muc 3, van de #1) |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `HttpOptionModel<T>` (`FTELSRCore.Shared/Models/Https/HttpOptionModel.cs`) | `record HttpOptionModel<T> : HttpOptionModel where T : notnull`. Base `HttpOptionModel` mang `Client`, `BaseAddress`, `Token`, `AuthType` (default `"Bearer"`), `Uri`, `SystemOwner` (default `"Service Request"`), `CompletionOption` (default `ResponseContentRead`) — tat ca `{ get; set; }`. Lop generic bo sung `Value { get; init; }` (**init-only**, khong gan lai duoc sau khi khoi tao) |
| `ErrorModel` (`FTELSRCore.Shared/Models/Https/ErrorModel.cs`) | `record` ket qua loi tra ve: `Code` (int), `Message` (string), `Succeeded` (bool), kem `ErrorDeConstruct(out message, out statusCode)` |
| `HttpContentExtensionsUtilizes.ConfigHttpClient` (`HttpClientUtilizes.cs:343-360`) | Set `client.BaseAddress`, `Add` header `Accept: application/json`, set `client.DefaultRequestHeaders.Authorization` |
| `HttpContentExtensionsUtilizes.ResponseResult<TResponse>` (`HttpClientUtilizes.cs:362-375`) | Kiem tra `Content-Type`; nem `CustomException` neu media type rong hoac chua `text/html`; con lai deserialize qua `ReadAsStreamAsync<T>` |
| `HttpContentExtensionsUtilizes.ReadAsStreamAsync<T>` (`HttpClientUtilizes.cs:317-341`) | Deserialize body bang `System.Text.Json` voi options `PropertyNameCaseInsensitive = true`, `ReferenceHandler.IgnoreCycles`, `NumberHandling.AllowReadingFromString`; loi thi log va tra `default` (khong nem) |
| `HttpContentExtensionsUtilizes.EnsureSuccessOrException` (`HttpClientUtilizes.cs:401-416`) | Gan `errorModel.Code = (int)StatusCode`, `Message = ReasonPhrase`, `Succeeded = IsSuccessStatusCode`. **Chi nem khi `httpResponseMessage is null`** — doan `EnsureSuccessStatusCode()` da bi comment (`HttpClientUtilizes.cs:412-415`) |
| `HttpContentExtensionsUtilizes.ErrorException` (2 overload, `HttpClientUtilizes.cs:377-391`) | Map exception -> `ErrorModel`, ca hai overload deu set `Succeeded = false`. Overload `Exception`: `Code = 500`, message `"Hệ thống {SystemOwner} đang gặp sự cố tạm thời, vui lòng thử lại sau"` — **noi dung exception goc bi bo hoan toan** (tham so ten `_`). Overload `CustomException`: `Code = exception.Code`, `Message = exception.Message ?? "Hệ thống {SystemOwner} đang gặp sự cố tạm thời, vui lòng thử lại sau"` (dong 390) |
| `HttpContentExtensionsUtilizes.ErrorCanceledException` (`HttpClientUtilizes.cs:393-399`) | `Succeeded = false`, `Code = 408 (RequestTimeout)`, message `"Hệ thống {SystemOwner} đang xử lý chậm hơn bình thường. Vui lòng thử lại sau ít phút"`. **Khong phan biet** huy do caller (`cancellationToken`) voi timeout noi tai — ca hai deu ra `408` |
| `HttpContentExtensionsUtilizes.SetHttpVersion` (`HttpClientUtilizes.cs:280-287`) | Neu environment la `Local` thi ep `HttpVersionPolicy.RequestVersionOrLower`, nguoc lai giu `versionPolicy` caller truyen |
| `HttpClientUtilizes.ToQueryString` (`HttpClientUtilizes.cs:111-132`) | Build query string **co san dau `?` o dau**, dung ten property (khong doc `JsonPropertyNameAttribute`), escape ca ten va gia tri bang `Uri.EscapeDataString`, doc property qua `GetProperties(BindingFlags.Public \| BindingFlags.Instance)` (dong 113). Escape **truoc** khi kiem tra rong (dong 119-124) nen gia tri chi gom khoang trang tro thanh `%20` va **van duoc ghi vao URL**. Chi `GetAsJSonCustomHeaderAsync` dung ham nay |
| `HttpClientUtilizes.HasPort` (`HttpClientUtilizes.cs:35-48`) | `false` neu url rong / khong parse duoc absolute URI / dung port mac dinh (`uri.IsDefaultPort`); `true` neu URI co port khac mac dinh. Dung de chon `DirectionType` khi ghi log |
| `CancellationTokenHelper.CreateLinkedTokenWithTimeout` (`FTELSRCore.Shared/Helpers/CancellationTokenHelper.cs:12-22`) | Tao `CancellationTokenSource` link voi token ngoai; goi `CancelAfter(TimeSpan.FromSeconds(timeoutSeconds))` chi khi `timeoutSeconds > 0` |
| `MeasureExecutionTimeExtensions.InvokeForHTTP` (`FTELSRCore.Shared/Extensions/MeasureExecutionTimeExtensions.cs:67-94`) | `Func<ValueTask<TOut>> func, ILogger logger, string measureByKey, int desiredTime = 5, CancellationToken cancellationToken = default` (default `desiredTime` cua ham la `5`, nhung moi call site trong lop nay deu truyen tuong minh gia tri `desiredTime` cua method - **mac dinh `3` giay** o ca 11 signature, vi du `CallApiWithHttp.cs:35`; nguong canh bao thuc te vi vay la 3 giay, khong phai 5). Goi `cancellationToken.ThrowIfCancellationRequested()` **2 lan** (dong 70 va 76) truoc khi chay `func`. Do thoi gian `func`; neu `elapsed > desiredTime` (giay) thi `logger.Warning(...)` voi thong diep `[PERFORMANCE] Long Running Request [{measureByKey}] took {elapsed} seconds.`. **Khong retry, khong huy, khong doi ket qua** |
| `LoggerExtensions.HttpResultWithTracing` (`FTELSRCore.Shared/Extensions/Loggers/LoggerExtensions.cs:460-489`) | Log `LogLevel.Information` mot dong tracing gom `HttpMethod`, `Endpoint`, `EndpointWithQuery`, `SystemOwner`, `Direction`, `HttpStatusCode`, `Latency` (`ResponseTimeMs`), `LatencyRating`, `ErrorCategory`, `Message`. **`EndpointWithQuery` luon rong**: no lay tu tham so `uriWithQuery` (default `""`, dong 468) va **khong call site nao trong `CallApiWithHttp.cs` truyen tham so nay**. `direction` co default `DirectionType.Inbound` (dong 469) |
| `LoggerExtensions.HttpErrorResult` (`LoggerExtensions.cs:426-458`) | Log `LogLevel.Error` khi vao nhanh `catch` |
| `CustomException` (`FTELSRCore.Shared/Exceptions/CustomException.cs`) | Exception noi bo mang `Code` (default `500`) va `Messages` |
| `SRKafkaLogFormatter.DirectionType` (`.../Formatters/SRKafkaLogFormatter.cs:303-307`) | Enum `Outbound = 0`, `Inbound = 1` |
| `Newtonsoft.Json.JsonConvert` | Chi dung trong `finally` de serialize `result` khi ghi log tracing |
| `Microsoft.AspNetCore.Http.IFormFile` | Kieu file dau vao cua `PostAsFileAsync` / `PostAsFileV2Async` |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `GetAsJSonAsync` | GET | GET, query string tu `ParseModelToQueryString`, tra ve `(TResponse, ErrorModel)` |
| `GetAsJSonAndHeaderAsync` | GET | Giong `GetAsJSonAsync` nhung tra ve them `HttpResponseHeaders` |
| `GetAsJSonCustomHeaderAsync` | GET | GET, query string tu `HttpClientUtilizes.ToQueryString`, them header vao `HttpRequestMessage.Headers` |
| `PostFormUrlEncodedAsync` | POST | POST body `application/x-www-form-urlencoded` tu `Dictionary<string, string>` |
| `PostAsJSonAsync` | POST | POST body JSON `application/json` |
| `PostAsFileAsync` | POST | POST `multipart/form-data`, doc file vao `byte[]` roi gui `ByteArrayContent` |
| `PostAsFileV2Async` | POST | POST `multipart/form-data`, stream file qua `StreamContent`, ep `HttpVersion.Version10` |
| `PostWithHeadersAsJSonAsync` | POST | POST body JSON, them header vao `client.DefaultRequestHeaders` |
| `PutAsJSonAsync` | PUT | PUT body JSON |
| `DeleteAsJSonAsync` | DELETE | DELETE, query string tu `ParseModelToQueryString`, khong co body |
| `PatchAsJSonAsync` | PATCH | PATCH body JSON |
| `ParseModelToQueryString` | Private helper | Build query string tu `TRequest` bang reflection (khong public) |

---

## 2. Chi tiet API

### 2.0 Khung xu ly dung chung (doc truoc de khong lap lai)

Toan bo 11 public method deu theo dung khung sau — cac muc 2.1 tro di chi ghi phan **khac biet**:

1. `cancellationToken.ThrowIfCancellationRequested();` — **nam NGOAI `try`** (`CallApiWithHttp.cs:38`, `154`, `272`, `396`, `519`, `639`, `801`, `959`, `1086`, `1205`, `1323`). Neu token da bi huy, method **nem `OperationCanceledException` ra ngoai**, khong tra `ErrorModel`.
2. `long start = Stopwatch.GetTimestamp();` — moc do latency cho log tracing.
3. Chuan bi URL / body — mot so method lam viec nay **ngoai `try`** (chi tiet tung muc).
4. `ErrorModel errorModel = new();` va `result = (null, errorModel);` — `errorModel` la **cung mot instance** duoc chia se giua bien `result` va cac ham `ErrorException` / `ErrorCanceledException` (nhung ham nay chi mutate property, khong gan lai object), nen log trong `finally` luon thay status code sau cung.
5. Trong `try`: lay `HttpClient` -> chuan bi `Content` (neu co) -> tao `CancellationTokenSource` co timeout -> tao `HttpRequestMessage` (`Version = HttpVersion.Version20`, tru `PostAsFileV2Async` dung `Version10`; `VersionPolicy = SetHttpVersion(versionPolicy)`) -> goi `SendAsync` boc trong `MeasureExecutionTimeExtensions.InvokeForHTTP` -> `EnsureSuccessOrException(ref errorModel)` -> `ResponseResult<TResponse>`. **Thu tu nay khong dong nhat**: `GetAsJSonCustomHeaderAsync` tao `HttpRequestMessage` + gan header **truoc** khi tao `CancellationTokenSource` (dong 288-300); `PostAsFileAsync` / `PostAsFileV2Async` xay dung `MultipartFormDataContent` (doc/mo file) **truoc** khi tao `CancellationTokenSource` (dong 652-703 va 814-860), va hai method nay khoi tao `CancellationTokenSource` + `HttpRequestMessage` **khong co `using`** (xem muc 3, van de #10 va #26).
6. Ba nhanh `catch` theo dung thu tu: `OperationCanceledException` -> `CustomException` -> `Exception`. Moi nhanh map `ErrorModel`, goi `logger.HttpErrorResult(...)`, roi `return (null, errorModel)`. **Khong nhanh nao nem lai exception.**
7. `finally`: goi `logger.HttpResultWithTracing(...)` voi:
   - `statusCode: result.errorModel?.Code.ToString()`
   - `direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch { true => DirectionType.Inbound, false => DirectionType.Outbound }`
   - `responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency`
   - `message`: chuoi JSON **do `string.Format` ghep tay**, trong do `option` duoc serialize bang **`System.Text.Json.JsonSerializer`** con `result` duoc serialize bang **`Newtonsoft.Json.JsonConvert`** (hai serializer khac nhau trong cung mot dong log).

> [!WARNING]
> `EnsureSuccessOrException` **khong nem exception khi HTTP status la 4xx/5xx** — doan `EnsureSuccessStatusCode()` da bi comment tai `HttpClientUtilizes.cs:412-415`. Vi vay voi response `400`/`404`/`500`, luong code **van chay tiep** sang `ResponseResult<TResponse>` va co gang deserialize body loi thanh `TResponse`. Ket qua co the la `(data != null, errorModel.Succeeded == false)`. Dieu nay **mau thuan voi XML doc** cua tat ca method (`"data null nếu lỗi"`) — xem muc 3, van de #2.

> [!IMPORTANT]
> **Khong co retry noi tai.** Trong `CallApiWithHttp.cs` khong co vong lap retry va khong goi Polly. Trong repo, Polly chi duoc dung cho SQL va MongoDB (`FTELSRCore.Shared/Data/SQL/Helpers/Policies/SqlResiliencePolicyFactory.cs`, `.../MongoDB/Helpers/Policies/MongoResiliencePolicyFactory.cs`); khong tim thay `AddPolicyHandler` nao cho `HttpClient` trong repo. Tuy vay, `cancellationTokenTime` duoc ap bang `CancelAfter` len `CancellationTokenSource` **truoc khi** goi `SendAsync`, nen neu tang `IHttpClientFactory` ben ngoai co gan pipeline retry qua `AddPolicyHandler`, timeout nay se **bao trum toan bo cac lan retry** chu khong reset cho tung lan.

---

### 2.1 GetAsJSonAsync

**Signature**

```csharp
public static async Task<(TResponse, ErrorModel)> GetAsJSonAsync(
    HttpOptionModel<TRequest> option, ILogger logger,
    HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
    int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
```

**Muc dich** — Gui `GET` toi `option.Uri`, query string duoc build tu `option.Value` bang `ParseModelToQueryString` (private helper cua chinh class nay), deserialize body thanh `TResponse`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `option` | `HttpOptionModel<TRequest>` | Co | Khong co null-check. `option.Value` duoc kiem tra `is null` (`CallApiWithHttp.cs:42`). `option.BaseAddress` chi set len client khi khong rong (`HttpClientUtilizes.cs:347`). `option.Token` chi set `Authorization` khi khong rong (`HttpClientUtilizes.cs:354`) | — |
| `logger` | `ILogger` (`Microsoft.Extensions.Logging`, qua `GlobalUsing.cs:2`) | Co | Khong null-check; `finally` luon goi `logger.HttpResultWithTracing` nen `logger == null` lam `finally` **nem exception**. `HttpResultWithTracing` uy quyen cho extension `Microsoft.Extensions.Logging.LoggerExtensions.Log` (`LoggerExtensions.cs:471`) — extension nay co guard null cho `logger`, nen loai exception thuc te la `ArgumentNullException` chu **khong** phai `NullReferenceException` (chi tiet nam trong BCL, khong xac dinh duoc tu source code cua repo) | — |
| `versionPolicy` | `HttpVersionPolicy` | Khong | Duoc dua qua `SetHttpVersion`; environment `Local` se ghi de thanh `RequestVersionOrLower` | `RequestVersionOrLower` |
| `desiredTime` | `int` | Khong | Nguong giay de ghi log `Warning`; khong anh huong hanh vi request | `3` |
| `cancellationTokenTime` | `int` | Khong | Don vi giay. `<= 0` thi **khong ap timeout** (`CancellationTokenHelper.cs:16`) | `15` |
| `cancellationToken` | `CancellationToken` | Khong | Kiem tra ngay dau method, ngoai `try` | `default` |

**Output** — `Task<(TResponse, ErrorModel)>`

| Truong hop | `TResponse` | `ErrorModel` |
|---|---|---|
| HTTP 2xx, body JSON parse duoc | Object da deserialize | `Code` = status code, `Message` = `ReasonPhrase`, `Succeeded = true` |
| HTTP 2xx nhung body khong parse duoc | `null` (`ReadAsStreamAsync` tra `default` va log error, `HttpClientUtilizes.cs:329-339`) | `Succeeded = true`, `Code` = 2xx |
| HTTP 4xx/5xx co `Content-Type` JSON | **Co the khac `null`** (deserialize body loi thanh `TResponse`) | `Code` = status code that, `Message` = `ReasonPhrase`, `Succeeded = false` |
| `Content-Type` rong hoac `text/html` | `null` | `Code` = status code that, `Message` = **nguyen van body** (`CustomException` tu `HttpClientUtilizes.cs:366-371`), `Succeeded = false` |
| Timeout / bi huy | `null` | `Code = 408`, `Message = "Hệ thống {SystemOwner} đang xử lý chậm hơn bình thường. Vui lòng thử lại sau ít phút"`, `Succeeded = false` |
| Exception khac (DNS, socket, TLS, `ArgumentException`...) | `null` | `Code = 500`, `Message = "Hệ thống {SystemOwner} đang gặp sự cố tạm thời, vui lòng thử lại sau"`, `Succeeded = false` |

> [!NOTE]
> Cac o `Message = ReasonPhrase` o tren la **nguyen van** gia tri `httpResponseMessage.ReasonPhrase` (`HttpClientUtilizes.cs:409`). Vi `Version` duoc hardcode `HttpVersion.Version20`, khi ket noi that su duoc thuong luong len HTTP/2 thi giao thuc **khong co reason phrase** va `ErrorModel.Message` co the la `null`. Gia tri cuoi cung phu thuoc protocol thuc te — **khong xac dinh duoc tu source code cua repo**.

**Dieu kien xu ly** (theo thu tu thuc thi)

1. `cancellationToken.ThrowIfCancellationRequested()` — `CallApiWithHttp.cs:38`, ngoai `try`.
2. `option.Value is null` ? dung `option.Uri` : `$"{option.Uri}?{ParseModelToQueryString(option.Value)}"` — `CallApiWithHttp.cs:42-44`, **ngoai `try`**.
3. `option.ConfigHttpClient()` — `CallApiWithHttp.cs:52`: set `BaseAddress` (neu co), `Add` `Accept: application/json`, set `Authorization` (neu co `Token`).
4. `CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime)` — `CallApiWithHttp.cs:54-55`.
5. Tao `HttpRequestMessage(HttpMethod.Get, urlQueryString)` voi `Version = HttpVersion.Version20` — `CallApiWithHttp.cs:57-61`.
6. `InvokeForHTTP(...)` boc `client.SendAsync(request, option.CompletionOption, cts.Token)`; `measureByKey = urlQueryString` — `CallApiWithHttp.cs:63-73`.
7. `EnsureSuccessOrException(ref errorModel)` — `CallApiWithHttp.cs:75`. Chi nem khi response `null`.
8. `ResponseResult<TResponse>(logger)` — `CallApiWithHttp.cs:77`. Nem `CustomException` neu media type rong / `text/html`.
9. Ba nhanh `catch` (dong 81, 92, 103), roi `finally` (dong 114-134).

**Side effect**

- **Mutate `option.Client`** (object dung chung): gan `client.BaseAddress`, `Add` vao `client.DefaultRequestHeaders.Accept`, gan `client.DefaultRequestHeaders.Authorization` (`HttpClientUtilizes.cs:345-357`).
- Goi API ngoai qua `SendAsync`.
- Ghi log: 1 dong `Information` tracing trong `finally`; 1 dong `Warning` neu vuot `desiredTime`; 1 dong `Error` neu vao `catch`; co the them 1 dong `Error` tu `ReadAsStreamAsync` khi deserialize that bai.
- **Khong mutate `option.Value`.**

**Error handling** — Bat `OperationCanceledException`, `CustomException`, `Exception`. Moi nhanh map `ErrorModel`, ghi `logger.HttpErrorResult`, tra `(null, errorModel)`. **Khong nem lai.** Ngoai le: `ThrowIfCancellationRequested()` (dong 38) va `ParseModelToQueryString` (dong 44) nam ngoai `try` — exception tu 2 cho nay **nem thang ra caller**.

**Khi nao NEN dung** — GET API JSON co tham so query la mot POCO phang (string/so/bool/DateTime), khi caller khong can doc response header, va khi model query co the dung `JsonPropertyNameAttribute` de doi ten tham so.

**Khi nao KHONG dung** — Khi can doc response header (dung `GetAsJSonAndHeaderAsync`); khi can header request tuy chinh (dung `GetAsJSonCustomHeaderAsync`); khi model query co property dang list/array (se sinh query sai, xem muc 2.12); khi can phan biet ro rang "404 khong tim thay" voi "loi he thong" ma khong muon dua vao viec doc `ErrorModel.Code`.

**Gioi han**

- `HttpVersion.Version20` **hardcode** tai dong 59 — caller chi dieu khien duoc `VersionPolicy`, khong doi duoc version.
- `ParseModelToQueryString` chi `HttpUtility.UrlEncode` cho gia tri kieu `string` (`CallApiWithHttp.cs:1449`); cac kieu khac (`DateTime`, `enum`, `decimal`...) khong duoc encode.
- Neu `option.Value` khac `null` nhung moi property deu bi bo qua, `ParseModelToQueryString` tra chuoi rong, URL cuoi cung ket thuc bang dau `?` du (dong 44).
- `measureByKey = urlQueryString` nen log `Warning` khi cham se chua **nguyen ca query string** (co the co du lieu ca nhan).
- `ConfigHttpClient` goi `Accept.Add(...)` moi lan; voi `HttpClient` dung lai nhieu lan, header `Accept` tich luy trung lap (xem muc 3, van de #3).
- `client.BaseAddress` bi gan lai moi lan — `HttpClient` nem `InvalidOperationException` neu instance da gui request truoc do (xem muc 3, van de #4).
- Khong co retry noi tai.

---

### 2.2 GetAsJSonAndHeaderAsync

**Signature**

```csharp
public static async Task<(TResponse, ErrorModel, HttpResponseHeaders)> GetAsJSonAndHeaderAsync(
    HttpOptionModel<TRequest> option, ILogger logger,
    HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
    int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
```

**Muc dich** — Giong `GetAsJSonAsync`, bo sung phan tu thu ba trong tuple la `httpResponseMessage.Headers`.

**Input hop le** — Y het bang o muc 2.1 (cung ten, cung kieu, cung gia tri mac dinh).

**Output** — `Task<(TResponse, ErrorModel, HttpResponseHeaders)>`

| Truong hop | Ket qua |
|---|---|
| Khong nem exception (ke ca HTTP 4xx/5xx co body JSON) | `(data, errorModel, httpResponseMessage.Headers)` — `CallApiWithHttp.cs:193` |
| Bat ky nhanh `catch` nao | `(null, errorModel, null)` — `CallApiWithHttp.cs:206`, `217`, `228` |

Luu y: `Headers` tra ve la **response headers**, khong bao gom `Content-*` headers (nhung header do nam trong `HttpResponseMessage.Content.Headers`, khong duoc tra ve).

**Dieu kien xu ly** — Trung hoan toan voi muc 2.1 (query string qua `ParseModelToQueryString` tai dong 158-160; `ConfigHttpClient` dong 168; `HttpVersion.Version20` dong 175; `measureByKey = urlQueryString` dong 188; `EnsureSuccessOrException` dong 191), khac o dong 193 khi gan `httpResponseMessage.Headers` vao tuple.

**Side effect** — Trung muc 2.1. Them mot rui ro: `HttpResponseMessage` **khong duoc dispose** trong ca hai method GET (khong co `using`), nhung tuple cua method nay giu tham chieu `Headers` ra ngoai pham vi method.

**Error handling** — Trung muc 2.1; nhanh `catch` tra tuple 3 phan tu voi `Headers = null`.

**Khi nao NEN dung** — Khi API tra thong tin can thiet o header: phan trang (`X-Total-Count`), `ETag`, `Location`, correlation id, rate-limit.

**Khi nao KHONG dung** — Khi khong can header (dung `GetAsJSonAsync` de tuple gon hon); khi can doc `Content-Type` / `Content-Length` (khong nam trong `HttpResponseHeaders` tra ve); khi can header ngay ca luc loi (nhanh `catch` luon tra `null`).

**Gioi han**

- Khi co exception, caller **khong bao gio** doc duoc header — ke ca truong hop `ResponseResult` nem `CustomException` du response da ve.
- `HttpResponseMessage` khong dispose; truy cap `Headers` sau khi response bi GC/dispose la khong an toan.
- Cac gioi han con lai giong muc 2.1 (`HttpVersion.Version20` hardcode dong 175, mutate shared client, khong retry).

---

### 2.3 GetAsJSonCustomHeaderAsync

**Signature**

```csharp
public static async Task<(TResponse, ErrorModel)> GetAsJSonCustomHeaderAsync(
    HttpOptionModel<TRequest> option, IEnumerable<KeyValuePair<string, string>> headers, ILogger logger,
    HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
    int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
```

**Muc dich** — Gui `GET` co header tuy chinh. Query string build bang `HttpClientUtilizes.ToQueryString` (**KHAC** hai method GET tren, von dung `ParseModelToQueryString`). Header duoc `Add` vao `HttpRequestMessage.Headers` — **chi anh huong dung request nay**.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `option` | `HttpOptionModel<TRequest>` | Co | `option.Value is null` duoc kiem tra (`CallApiWithHttp.cs:280`) | — |
| `headers` | `IEnumerable<KeyValuePair<string, string>>` | Co | **Khong null-check** — `foreach` tai dong 294 se nem `NullReferenceException` neu truyen `null`; exception nay nam trong `try` nen bien thanh `ErrorModel` `Code = 500` | — |
| `logger` | `ILogger` | Co | Khong null-check | — |
| `versionPolicy` | `HttpVersionPolicy` | Khong | Qua `SetHttpVersion` | `RequestVersionOrLower` |
| `desiredTime` | `int` | Khong | Chi de log `Warning` | `3` |
| `cancellationTokenTime` | `int` | Khong | Giay; `<= 0` khong ap timeout | `15` |
| `cancellationToken` | `CancellationToken` | Khong | Kiem tra ngoai `try` | `default` |

**Output** — Giong bang Output muc 2.1 (`(TResponse, ErrorModel)`), them truong hop: `headers` chua header khong hop le (ten sai chuan, hoac la content header nhu `Content-Type`) -> `HttpRequestMessage.Headers.Add` nem `InvalidOperationException`/`FormatException` -> nhanh `catch (Exception)` -> `Code = 500`, message chung.

**Dieu kien xu ly**

1. `cancellationToken.ThrowIfCancellationRequested()` — dong 272 (ngoai `try`).
2. `option.Value is null` ? `option.Uri` : `$"{option.Uri}{HttpClientUtilizes.ToQueryString(option.Value)}"` — dong 280-282, **ngoai `try`**. **Khong co dau `?` trong chuoi noi** vi `ToQueryString` da tu them `?` khi co it nhat 1 tham so (`HttpClientUtilizes.cs:126`).
3. `option.ConfigHttpClient()` — dong 286.
4. Tao `HttpRequestMessage(HttpMethod.Get, urlQueryString)`, `Version = HttpVersion.Version20` — dong 288-292.
5. `foreach (var header in headers) requestMessage.Headers.Add(header.Key, header.Value);` — dong 294-297.
6. `CreateLinkedTokenWithTimeout(...)` — dong 299-300 (**tao SAU khi da gan header**, khac cac method khac).
7. `InvokeForHTTP` + `SendAsync`, `measureByKey = option.Uri` (**khong phai** `urlQueryString`) — dong 302-311.
8. `EnsureSuccessOrException` (dong 313) -> `ResponseResult<TResponse>` (dong 315).
9. `catch` x3 (dong 319, 330, 341), `finally` (dong 352-372).

**Side effect** — Nhu muc 2.1 (mutate `option.Client` qua `ConfigHttpClient`), **cong them** header tuy chinh chi ton tai tren `HttpRequestMessage` — **khong** ro ri sang `client.DefaultRequestHeaders`.

**Error handling** — Giong muc 2.1. Diem khac: loi do `headers == null` hoac header sai format se bien thanh `Code = 500` voi message chung `"Hệ thống ... đang gặp sự cố tạm thời"` — caller khong phan biet duoc voi loi mang.

**Khi nao NEN dung** — GET can header rieng cho tung request (`X-Request-Id`, `X-Correlation-Id`, `X-Api-Key`, `X-Tenant`), dac biet khi `HttpClient` la instance dung chung: trong cac method co nhan `headers`, day la method duy nhat **khong** ghi header tuy chinh vao `client.DefaultRequestHeaders`. Luu y: method nay **van** goi `ConfigHttpClient()` (dong 286) nen `BaseAddress`, `Accept` va `Authorization` **van bi mutate tren client dung chung** — no chi an toan doi voi **header tuy chinh**, khong phai an toan tuyet doi.

**Khi nao KHONG dung** — Khi model query dua vao `JsonPropertyNameAttribute` de doi ten tham so: `ToQueryString` **bo qua hoan toan** attribute nay va luon dung ten property (`HttpClientUtilizes.cs:118`), nen URL sinh ra khac `GetAsJSonAsync`. Cung khong dung khi can them `Content-*` header (GET khong co body, `HttpRequestMessage.Headers` khong nhan content header).

**Gioi han**

- **Khac biet query string so voi `GetAsJSonAsync`**: `ToQueryString` escape ca **ten** property bang `Uri.EscapeDataString` (`HttpClientUtilizes.cs:118`), bo qua `JsonPropertyNameAttribute`, va escape gia tri bang `Uri.EscapeDataString` thay vi `HttpUtility.UrlEncode` (khac nhau o cach ma hoa dau cach: `%20` vs `+`).
- `ToQueryString` goi `obj.GetType()` khong null-check (`HttpClientUtilizes.cs:113`) — an toan o day chi vi caller da kiem tra `option.Value is null`.
- `ToQueryString` khong ho tro collection: `property.GetValue(obj)?.ToString()` cho ra ten kieu (vi du `System.Collections.Generic.List\`1[System.String]`).
- **Khac biet ve loc gia tri rong**: `ToQueryString` escape **truoc** roi moi kiem tra `string.IsNullOrWhiteSpace(value)` (`HttpClientUtilizes.cs:119-124`), nen gia tri chi gom khoang trang bien thanh `%20` (khong con la whitespace) va **van duoc ghi vao URL**; `ParseModelToQueryString` thi kiem tra `string.IsNullOrWhiteSpace(value.ToString())` **truoc khi** encode (`CallApiWithHttp.cs:1439`) nen bo qua. Nguoc lai, `ParseModelToQueryString` bo qua rieng gia tri chuoi `"null"` (`value.Equals("null")`), con `ToQueryString` thi **khong**.
- **Khac biet ve `BindingFlags`**: `ToQueryString` dung `GetProperties(BindingFlags.Public | BindingFlags.Instance)` (`HttpClientUtilizes.cs:113`) — chi property instance; `ParseModelToQueryString` dung `type.GetProperties()` khong tham so (`CallApiWithHttp.cs:1435`) nen bao gom ca **public static property**.
- Log tracing cua method nay **khong co truong `URL`** (dong 369-371), khac `GetAsJSonAsync` — kho dieu tra query string tu log.
- `headers` khong duoc validate; khong co xu ly trung key.
- `HttpVersion.Version20` hardcode (dong 290). Khong co retry noi tai.

---

### 2.4 PostFormUrlEncodedAsync

**Signature**

```csharp
public static async Task<(TResponse, ErrorModel)> PostFormUrlEncodedAsync(
    HttpOptionModel<Dictionary<string, string>> option, ILogger logger,
    HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
    int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
```

**Muc dich** — `POST` body `application/x-www-form-urlencoded` tu `Dictionary<string, string>`. Thuong dung cho endpoint kieu OAuth token.

> [!NOTE]
> Tham so `option` co kieu `HttpOptionModel<Dictionary<string, string>>`, **khong phai** `HttpOptionModel<TRequest>`. Type parameter `TRequest` cua class khong duoc dung trong method nay, nhung caller **van phai** khai bao no khi goi (vi day la static class generic).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `option` | `HttpOptionModel<Dictionary<string, string>>` | Co | **`option.Value` khong duoc kiem tra null** — `new FormUrlEncodedContent(option.Value)` (dong 425) nem `ArgumentNullException` neu `null`; exception nam trong `try` nen thanh `Code = 500` | — |
| `logger` | `ILogger` | Co | Khong null-check | — |
| `versionPolicy` | `HttpVersionPolicy` | Khong | Qua `SetHttpVersion` | `RequestVersionOrLower` |
| `desiredTime` | `int` | Khong | Chi de log `Warning` | `3` |
| `cancellationTokenTime` | `int` | Khong | Giay; `<= 0` khong ap timeout | `15` |
| `cancellationToken` | `CancellationToken` | Khong | Kiem tra ngoai `try` | `default` |

**Output** — Giong bang Output muc 2.1.

**Dieu kien xu ly**

1. `cancellationToken.ThrowIfCancellationRequested()` — dong 396.
2. `HttpClient client = option.Client;` — dong 406. **Khong goi `ConfigHttpClient`** — day la method duy nhat lam cau hinh client bang code inline.
3. `if (!string.IsNullOrEmpty(option.BaseAddress)) client.BaseAddress = new Uri(option.BaseAddress);` — dong 408-411.
4. `if (!string.IsNullOrEmpty(option.Token)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(option.AuthType, option.Token);` — dong 413-417.
5. `CreateLinkedTokenWithTimeout(...)` — dong 419-420.
6. `HttpRequestMessage(HttpMethod.Post, option.Uri)` voi `Content = new FormUrlEncodedContent(option.Value)`, `Version = HttpVersion.Version20` — dong 422-427.
7. `InvokeForHTTP` + `SendAsync`, `measureByKey = option.Uri` — dong 429-438.
8. `EnsureSuccessOrException` (dong 440) -> `ResponseResult<TResponse>` (dong 442).
9. `catch` x3 (dong 446, 457, 468), `finally` (dong 479-499).

**Side effect** — Mutate `option.Client`: gan `BaseAddress` va `Authorization`. **Khong** them header `Accept: application/json` (vi khong goi `ConfigHttpClient`). Goi API ngoai. Ghi log nhu muc 2.1.

**Error handling** — Giong muc 2.1 (3 nhanh `catch`, khong nem lai).

**Khi nao NEN dung** — Endpoint yeu cau body form-urlencoded, dien hinh la lay OAuth access token (`grant_type`, `client_id`, `client_secret`).

**Khi nao KHONG dung** — Khi endpoint can `Accept: application/json` de tra JSON (method nay khong set `Accept`; neu server tra `text/html` thi `ResponseResult` se nem `CustomException` va `ErrorModel.Message` chua nguyen trang HTML). Khong dung khi body co field khong phai `string` (kieu `Dictionary<string, string>` co dinh) hoac can nested object.

**Gioi han**

- Khong set `Accept` header — khac biet hanh vi ro rang so voi cac method dung `ConfigHttpClient`.
- Body chi la `new FormUrlEncodedContent(option.Value)` (dong 425): **khong co doan code nao trong repo** xu ly gia tri `null` trong `Dictionary`, khong chunk/stream payload, khong gioi han kich thuoc. Hanh vi cu the voi gia tri `null` hoac payload rat lon **khong xac dinh duoc tu source code cua repo** (thuoc implementation `FormUrlEncodedContent` cua .NET).
- `client.BaseAddress` bi gan lai moi lan goi (xem muc 3, van de #4).
- Doan cau hinh client duoc **copy tay** (dong 406-417) thay vi dung `ConfigHttpClient` — moi thay doi trong `ConfigHttpClient` se **khong** ap dung cho method nay (rui ro trang thai code phan ky).
- `HttpVersion.Version20` hardcode (dong 424). Khong co retry noi tai. Khong ho tro header tuy chinh.

---

### 2.5 PostAsJSonAsync

**Signature**

```csharp
public static async Task<(TResponse, ErrorModel)> PostAsJSonAsync(
    HttpOptionModel<TRequest> option, ILogger logger,
    HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
    int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
```

**Muc dich** — `POST` body JSON: serialize `option.Value` bang `System.Text.Json.JsonSerializer` roi gui `StringContent` voi `Encoding.UTF8` va media type `MediaTypeNames.Application.Json` (`application/json`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `option` | `HttpOptionModel<TRequest>` | Co | Khong null-check `option`. `option.Value` **khong** duoc kiem tra null — `JsonSerializer.Serialize(null)` sinh chuoi `"null"` va body request se la literal `null` | — |
| `logger` | `ILogger` | Co | Khong null-check | — |
| `versionPolicy` | `HttpVersionPolicy` | Khong | Qua `SetHttpVersion` | `RequestVersionOrLower` |
| `desiredTime` | `int` | Khong | Chi de log `Warning` | `3` |
| `cancellationTokenTime` | `int` | Khong | Giay; `<= 0` khong ap timeout | `15` |
| `cancellationToken` | `CancellationToken` | Khong | Kiem tra ngoai `try` | `default` |

**Output** — Giong bang Output muc 2.1.

**Dieu kien xu ly**

1. `cancellationToken.ThrowIfCancellationRequested()` — dong 519.
2. `string json = System.Text.Json.JsonSerializer.Serialize(option.Value);` — **dong 527, NGOAI `try`**. Neu serialize nem (vi du object co vong tham chieu, hoac converter loi), exception **thoat ra caller** thay vi thanh `ErrorModel`.
3. `option.ConfigHttpClient()` — dong 531.
4. `StringContent content = new(json, Encoding.UTF8, MediaTypeNames.Application.Json);` — dong 533.
5. `CreateLinkedTokenWithTimeout(...)` — dong 535-536.
6. `HttpRequestMessage(HttpMethod.Post, option.Uri)`, `Version = HttpVersion.Version20` — dong 538-544.
7. `InvokeForHTTP` + `SendAsync`, `measureByKey = option.Uri` — dong 546-555.
8. `EnsureSuccessOrException` (dong 557) -> `ResponseResult<TResponse>` (dong 559).
9. `catch` x3 (dong 563, 574, 585), `finally` (dong 596-616).

**Side effect** — Mutate `option.Client` (`BaseAddress`, `Accept`, `Authorization`). Goi API ngoai. Ghi log nhu muc 2.1 (tracing message co `Option` + `Result`, **khong** co `URL`).

**Error handling** — Giong muc 2.1. Chu y: loi serialize body **khong** duoc bat.

**Khi nao NEN dung** — POST JSON tieu chuan khi khong can header tuy chinh va khong can upload file. Day la method POST duoc dung pho bien nhat.

**Khi nao KHONG dung** — Khi can header tuy chinh (nhung `PostWithHeadersAsJSonAsync` lai co rui ro ro ri header, xem muc 2.8); khi can `JsonSerializerOptions` rieng (vi du `camelCase`, bo qua `null`) — method nay serialize bang options **mac dinh** cua `System.Text.Json`, tuc `PascalCase` theo ten property tru khi model co `JsonPropertyNameAttribute`; khi body la file/multipart.

**Gioi han**

- **Bat doi xung serializer**: body request serialize bang `System.Text.Json` **options mac dinh**, con response deserialize bang `System.Text.Json` voi `PropertyNameCaseInsensitive = true` (`HttpClientUtilizes.cs:272-278`). Request khong duoc noi long ve casing.
- `option.Value == null` tao body `"null"` chu khong phai bo trong body.
- `JsonSerializer.Serialize` nam ngoai `try` -> exception khong duoc chuyen thanh `ErrorModel`.
- `HttpVersion.Version20` hardcode (dong 542). Khong co retry noi tai. Mutate shared client.

---

### 2.6 PostAsFileAsync

**Signature**

```csharp
public static async Task<(TResponse, ErrorModel)> PostAsFileAsync(
    HttpOptionModel<TRequest> option, IEnumerable<IFormFile> files, string fileParameterName, ILogger logger,
    HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
    int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
```

**Muc dich** — `POST` `multipart/form-data`: doc **toan bo** noi dung tung `IFormFile` vao `byte[]` roi add lam `ByteArrayContent`; sau do duyet reflection cac property cua `TRequest` de add lam field form dang `StringContent`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `option` | `HttpOptionModel<TRequest>` | Co | `option.Value != null` duoc kiem tra truoc khi duyet reflection (dong 676) | — |
| `files` | `IEnumerable<IFormFile>` | Khong (chap nhan `null`) | `files ?? []` (dong 655). Moi file co `Length == 0` bi `continue` (dong 657-658) | — |
| `fileParameterName` | `string` | Co | Khong validate rong/null; duoc dung lam `name` khi `content.Add(fileContent, fileParameterName, file.FileName)` (dong 672) | — |
| `logger` | `ILogger` | Co | Khong null-check | — |
| `versionPolicy` | `HttpVersionPolicy` | Khong | Qua `SetHttpVersion` | `RequestVersionOrLower` |
| `desiredTime` | `int` | Khong | Chi de log `Warning` | `3` |
| `cancellationTokenTime` | `int` | Khong | Giay; `<= 0` khong ap timeout | `15` |
| `cancellationToken` | `CancellationToken` | Khong | Kiem tra ngoai `try`; **duoc dung truc tiep cho `fileStream.ReadAsync`** (dong 665-666), khong phai token da co timeout | `default` |

**Output** — Giong bang Output muc 2.1.

**Dieu kien xu ly**

1. `cancellationToken.ThrowIfCancellationRequested()` — dong 639.
2. `option.ConfigHttpClient()` — dong 649.
3. `MultipartFormDataContent content = [];` — dong 652.
4. Voi moi `file` trong `files ?? []` (dong 655):
   - `file.Length == 0` -> `continue` (dong 657-658).
   - `await using var fileStream = file.OpenReadStream();` (dong 661).
   - `byte[] fileBytes = new byte[fileStream.Length];` (dong 663).
   - `_ = await fileStream.ReadAsync(fileBytes.AsMemory(0, (int)fileStream.Length), cancellationToken)` (dong 665-666) — **gia tri tra ve bi bo qua**.
   - `fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType)` (dong 670) — `file.ContentType` **khong** co fallback (khac `PostAsFileV2Async`).
   - `content.Add(fileContent, fileParameterName, file.FileName)` (dong 672).
5. Neu `option.Value != null` (dong 676): duyet `typeof(TRequest).GetProperties()` (dong 678):
   - `value == null` -> `continue` (dong 682-683).
   - `case IEnumerable<string> stringList when property.PropertyType != typeof(string)` -> add tung item lam `StringContent` cung ten `property.Name` (dong 685-694). Day la `switch` statement tren `value`, khac `PostAsFileV2Async` dung chuoi `if/else`.
   - `default` -> `content.Add(new StringContent(value.ToString()!), property.Name)` (dong 695-697). **Khong co nhanh bo qua `IFormFile`** — khac `PostAsFileV2Async`.
6. `var cancellationTokenSource = CreateLinkedTokenWithTimeout(...)` — dong 702-703, **khong co `using`** -> khong dispose.
7. `InvokeForHTTP` + `SendAsync` voi `HttpRequestMessage` khoi tao **inline, khong `using`** (dong 708-713); `Version = HttpVersion.Version20`; `measureByKey = $"{option.BaseAddress}{option.Uri}"` (dong 715).
8. `EnsureSuccessOrException` (dong 719) -> `ResponseResult<TResponse>` (dong 721).
9. `catch` x3 (dong 725, 736, 747), `finally` (dong 758-778).

**Side effect** — Mutate `option.Client`. Doc toan bo noi dung file vao **bo nho heap** (`byte[]` moi cho tung file). Goi API ngoai. Ghi log nhu muc 2.1. **Khong** mutate `option.Value`. `MultipartFormDataContent`, `HttpRequestMessage`, `CancellationTokenSource`, `HttpResponseMessage` deu **khong duoc dispose**.

**Error handling** — Giong muc 2.1. `NullReferenceException` khi `file.ContentType == null` (dong 670) nam trong `try` -> `Code = 500`.

**Khi nao NEN dung** — Upload mot vai file **nho** kem theo vai field text phang, khi API dich yeu cau tat ca part dung chung mot `name` (`fileParameterName`).

**Khi nao KHONG dung**

- **File lon**: toan bo file duoc nap vao `byte[]` -> ap luc bo nho va Large Object Heap. Dung `PostAsFileV2Async` (stream) cho truong hop nay.
- Khi `TRequest` co property kieu `IFormFile`: property do se bi add nhu `StringContent(value.ToString())`, gui len ten kieu .NET thay vi noi dung file (`PostAsFileV2Async` co nhanh `value is IFormFile -> continue`, dong 842).
- Khi can dat `name` khac nhau cho tung file — `fileParameterName` la mot gia tri duy nhat cho moi file.

**Gioi han**

- `(int)fileStream.Length` (dong 663, 665) — **tran so** voi file > 2 GB (`int` overflow).
- Mot lan `ReadAsync` duy nhat va **bo qua so byte doc duoc** (`_ =`, dong 665): theo hop dong cua `Stream`, `ReadAsync` co the tra ve it hon so byte yeu cau -> **noi dung file gui len co the bi thieu/lech** voi cac stream khong doc mot lan het duoc.
- Khong dispose `MultipartFormDataContent` / `HttpRequestMessage` / `CancellationTokenSource` / `HttpResponseMessage`.
- Reflection duyet **tat ca** public property cua `TRequest`, ke ca property khong mong muon (khong co attribute de loai tru). Khong doc `JsonPropertyNameAttribute` -> ten field form luon la `property.Name`, **khac** quy tac dat ten cua `ParseModelToQueryString`.
- Dung `typeof(TRequest).GetProperties()` (**kieu compile-time**, dong 678), khac `ParseModelToQueryString` dung `data.GetType()` (kieu runtime, dong 1433). Voi doi tuong lop con truyen vao `TRequest` la lop cha, property rieng cua lop con **bi bo qua** o day nhung **duoc dua vao** query string o cac method GET/DELETE — khong nhat quan.
- Property dang collection khong phai `IEnumerable<string>` (vi du `List<int>`) roi vao nhanh mac dinh -> `value.ToString()` cho ra ten kieu.
- **`cancellationTokenTime` KHONG bao trum giai doan doc file**: `CancellationTokenSource` co timeout chi duoc tao o dong 702-703, tuc **sau khi** toan bo vong lap doc file vao `byte[]` (dong 655-673) da chay xong. Vong lap do dung truc tiep `cancellationToken` cua caller (dong 666), khong co timeout. Tuong tu, `desiredTime` chi do phan `SendAsync`, khong tinh thoi gian doc file.
- `HttpVersion.Version20` hardcode (dong 711). Khong co retry noi tai.
- `SendAsync` trong lambda o dong 707-715 **khong goi `.ConfigureAwait(false)`** (khac cac method GET/POST-JSON) — khac biet nho ve context capture, khong nhat quan trong cung mot class.

---

### 2.7 PostAsFileV2Async

**Signature**

```csharp
public static async Task<(TResponse, ErrorModel)> PostAsFileV2Async(
    HttpOptionModel<TRequest> option, IEnumerable<IFormFile> files, string fileParameterName, ILogger logger,
    HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
    int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
```

**Muc dich** — Bien the cua `PostAsFileAsync` dung `StreamContent` de stream truc tiep tu `file.OpenReadStream()` (khong nap vao `byte[]`), tu set `ContentDisposition` cho tung part, va **dat `Version = HttpVersion.Version10`**.

**Input hop le** — Cung bang tham so nhu muc 2.6, voi cac khac biet:

| Tham so | Khac biet so voi `PostAsFileAsync` |
|---|---|
| `files` | Tuong tu (`files ?? []` dong 817, bo qua `file.Length == 0` dong 819-820) |
| `cancellationToken` | **Khong** duoc dung de doc file (khong co `ReadAsync` thu cong); chi dung de tao linked token |
| `option.Value` | Nhanh reflection **bo qua property kieu `IFormFile`** (dong 842) |

**Output** — Giong bang Output muc 2.1.

**Dieu kien xu ly**

1. `cancellationToken.ThrowIfCancellationRequested()` — dong 801.
2. `option.ConfigHttpClient()` — dong 811.
3. `MultipartFormDataContent content = [];` — dong 814.
4. Voi moi `file` trong `files ?? []` (dong 817):
   - `file.Length == 0` -> `continue` (dong 819-820).
   - `var stream = file.OpenReadStream();` (dong 821) — **khong co `using`/`await using`**.
   - `new StreamContent(stream)` (dong 822).
   - `ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream")` (dong 823-824) — **co fallback**, khac muc 2.6.
   - Set thu cong `ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = $"\"{fileParameterName}\"", FileName = $"\"{file.FileName}\"", FileNameStar = file.FileName }` (dong 826-831) — **cac gia tri `Name`/`FileName` duoc boc them dau ngoac kep tuong minh trong code**.
   - `content.Add(streamContent, fileParameterName, file.FileName)` (dong 833) — goi `Add` overload co `name`/`fileName`. Ket qua cuoi cung cua header `Content-Disposition` phu thuoc implementation cua `MultipartFormDataContent.Add` trong .NET runtime; **khong xac dinh duoc tu source code cua repo nay**.
5. Neu `option.Value != null` (dong 837): duyet `typeof(TRequest).GetProperties()` (dong 839):
   - `value == null || value is IFormFile` -> `continue` (dong 842).
   - `value is IEnumerable<string> stringList && property.PropertyType != typeof(string)` -> add tung item (dong 844-851).
   - Con lai -> `content.Add(new StringContent(value.ToString()!), property.Name)` (dong 852-855).
6. `var cancellationTokenSource = CreateLinkedTokenWithTimeout(...)` — dong 859-860, **khong `using`**.
7. `InvokeForHTTP` + `SendAsync` voi `HttpRequestMessage` inline, **khong `using`**; `Version = HttpVersion.Version10` (dong 868); `measureByKey = $"{option.BaseAddress}{option.Uri}"` (dong 874).
8. `EnsureSuccessOrException` (dong 878) -> `ResponseResult<TResponse>` (dong 880).
9. `catch` x3 (dong 884, 895, 906), `finally` (dong 917-937).

**Side effect** — Mutate `option.Client`. Mo stream tu `IFormFile` va **giu mo** cho toi khi `SendAsync` doc xong (khong dispose tuong minh). Goi API ngoai. Ghi log nhu muc 2.1. `MultipartFormDataContent`, `HttpRequestMessage`, `CancellationTokenSource`, `HttpResponseMessage` khong duoc dispose.

**Error handling** — Giong muc 2.1.

**Khi nao NEN dung** — Upload file **lon** (giam ap luc bo nho vi khong tao `byte[]` toan bo file); khi `TRequest` co ca property `IFormFile` lan field text (method nay bo qua dung property `IFormFile`); khi server dich chi chap nhan HTTP/1.0 hoac gap loi voi HTTP/2 multipart.

**Khi nao KHONG dung**

- Khi ban can HTTP/2: `Version = HttpVersion.Version10` **hardcode** (dong 868) — khong the nang version bang tham so (`versionPolicy` chi doi `VersionPolicy`, khong doi `Version`).
- Khi ten file co ky tu non-ASCII va ban can header `Content-Disposition` chinh xac: code chen dau ngoac kep vao gia tri `Name`/`FileName` (dong 828-829), co the sinh header bi ngoac kep hai lan.
- Khi caller dispose `IFormFile`/`HttpRequest` cua ASP.NET truoc khi `SendAsync` doc xong stream — stream se bi dong giua duong.

**Gioi han**

- **HTTP/1.0 hardcode** (dong 868) — HTTP/1.0 khong ho tro chunked transfer encoding, nen mot so proxy/server co the tu choi hoac buoc phai biet `Content-Length` truoc.
- `stream` khong dispose tuong minh (dong 821); phu thuoc `StreamContent` bi dispose theo `MultipartFormDataContent` — nhung `content` cung khong duoc dispose.
- Set `ContentDisposition` thu cong roi lai goi `content.Add(..., name, fileName)` — **hai co che chong nhau**; ket qua cuoi phu thuoc runtime, khong the ket luan tu source code repo.
- Cung cac han che reflection nhu muc 2.6 (collection khong phai `IEnumerable<string>` -> `ToString()` sai; duyet het public property; dung `typeof(TRequest)` kieu compile-time o dong 839 nen bo qua property cua lop con).
- **`cancellationTokenTime` KHONG bao trum giai doan mo stream**: `CancellationTokenSource` co timeout chi duoc tao o dong 859-860, sau khi vong lap `OpenReadStream()` (dong 817-834) da chay xong.
- `SendAsync` trong lambda o dong 864-872 **khong goi `.ConfigureAwait(false)`** — giong `PostAsFileAsync`, khac cac method con lai.
- `fileParameterName` dung chung cho moi file. Khong co retry noi tai.

---

### 2.8 PostWithHeadersAsJSonAsync

**Signature**

```csharp
public static async Task<(TResponse, ErrorModel)> PostWithHeadersAsJSonAsync(
    HttpOptionModel<TRequest> option, Dictionary<string, string> headers, ILogger logger,
    HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
    int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
```

**Muc dich** — `POST` body JSON co header tuy chinh. Header duoc `Add` vao **`client.DefaultRequestHeaders`** (dong 979) — **khong** phai `HttpRequestMessage.Headers`.

> [!CAUTION]
> Day la khac biet quan trong nhat cua method nay so voi `GetAsJSonCustomHeaderAsync`. `client.DefaultRequestHeaders.Add(...)` gan header len **doi tuong `HttpClient` dung chung**, nen header con nguyen sau khi method ket thuc va **ap dung cho moi request tiep theo** di qua cung instance client do — ke ca request cua luong/nguoi dung khac neu client duoc lay tu `IHttpClientFactory` hoac DI singleton. Ngoai ra `HttpHeaders.Add` **nem `InvalidOperationException`** khi them mot key da ton tai va key do khong phai loai cho phep nhieu gia tri, nen lan goi thu hai voi cung bo header rat de that bai (loi nay bi bat va tra ve `Code = 500`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `option` | `HttpOptionModel<TRequest>` | Co | `option.Value` khong null-check; `Serialize(null)` -> body `"null"` | — |
| `headers` | `Dictionary<string, string>` | Khong (chap nhan `null`) | **Co null-check**: `if (headers != null)` (dong 975) — khac `GetAsJSonCustomHeaderAsync` | — |
| `logger` | `ILogger` | Co | Khong null-check | — |
| `versionPolicy` | `HttpVersionPolicy` | Khong | Qua `SetHttpVersion` | `RequestVersionOrLower` |
| `desiredTime` | `int` | Khong | Chi de log `Warning` | `3` |
| `cancellationTokenTime` | `int` | Khong | Giay; `<= 0` khong ap timeout | `15` |
| `cancellationToken` | `CancellationToken` | Khong | Kiem tra ngoai `try` | `default` |

**Output** — Giong bang Output muc 2.1, them: `headers` chua key da ton tai tren client -> `InvalidOperationException` -> `Code = 500`, message chung.

**Dieu kien xu ly**

1. `cancellationToken.ThrowIfCancellationRequested()` — dong 959.
2. `string json = System.Text.Json.JsonSerializer.Serialize(option.Value);` — **dong 967, NGOAI `try`**.
3. `option.ConfigHttpClient()` — dong 971.
4. `StringContent content = new(json, Encoding.UTF8, MediaTypeNames.Application.Json);` — dong 973.
5. `if (headers != null)` (dong 975) roi `foreach (...) client.DefaultRequestHeaders.Add(header.Key, header.Value);` (dong 977-980) — toan block 975-981.
6. `CreateLinkedTokenWithTimeout(...)` — dong 983-984.
7. `HttpRequestMessage(HttpMethod.Post, option.Uri)`, `Version = HttpVersion.Version20` — dong 986-991.
8. `InvokeForHTTP` + `SendAsync`, `measureByKey = option.Uri` — dong 993-1002.
9. `EnsureSuccessOrException` (dong 1004) -> `ResponseResult<TResponse>` (dong 1006).
10. `catch` x3 (dong 1010, 1021, 1032), `finally` (dong 1043-1063).

**Side effect**

- **Mutate `option.Client` tich luy**: gan `BaseAddress`, `Accept`, `Authorization` (`ConfigHttpClient`) **va them toan bo `headers` vao `DefaultRequestHeaders`** — khong bao gio duoc xoa. Day la side effect co pham vi rong nhat trong ca class.
- Goi API ngoai. Ghi log nhu muc 2.1.

**Error handling** — Giong muc 2.1. Loi `DefaultRequestHeaders.Add` nam trong `try` -> `Code = 500`. Loi serialize body (dong 967) **khong** duoc bat.

**Khi nao NEN dung** — Chi khi `option.Client` la **instance rieng, dung mot lan** cho endpoint nay (vi du `HttpClient` moi tao, khong chia se), va endpoint yeu cau header co dinh cho moi request.

**Khi nao KHONG dung**

- Khi `option.Client` la client dung chung (`IHttpClientFactory`, DI singleton, static): header se ro ri sang request khac va lan goi thu hai co the nem `InvalidOperationException` do trung key. Trong tinh huong nay hay **tu tao `HttpRequestMessage` o tang goi** hoac dung mot method khong ghi vao `DefaultRequestHeaders`.
- Khi can header **khac nhau theo tung request** trong moi truong da luong.
- Khi can header dang `Content-*` (vi du `Content-MD5`): `DefaultRequestHeaders` khong nhan content header.

**Gioi han**

- Ghi header vao `DefaultRequestHeaders` -> khong idempotent, khong thread-safe voi client dung chung (van de #5 muc 3).
- Khong xoa header sau khi dung (khong co `Remove` trong `finally`).
- `JsonSerializer.Serialize` ngoai `try`.
- `HttpVersion.Version20` hardcode (dong 989). Khong co retry noi tai.

---

### 2.9 PutAsJSonAsync

**Signature**

```csharp
public static async Task<(TResponse, ErrorModel)> PutAsJSonAsync(
    HttpOptionModel<TRequest> option, ILogger logger,
    HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
    int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
```

**Muc dich** — `PUT` body JSON. Cau truc than ham trung khop `PostAsJSonAsync`, chi doi `HttpMethod.Put` (dong 1105) va `httpMethod: HttpMethod.Put.Method` khi log (dong 1165).

**Input hop le** — Y het bang o muc 2.5.

**Output** — Giong bang Output muc 2.1.

**Dieu kien xu ly**

1. `cancellationToken.ThrowIfCancellationRequested()` — dong 1086.
2. `string json = System.Text.Json.JsonSerializer.Serialize(option.Value);` — **dong 1094, NGOAI `try`**.
3. `option.ConfigHttpClient()` — dong 1098.
4. `StringContent content = new(json, Encoding.UTF8, MediaTypeNames.Application.Json);` — dong 1100.
5. `CreateLinkedTokenWithTimeout(...)` — dong 1102-1103.
6. `HttpRequestMessage(HttpMethod.Put, option.Uri)`, `Version = HttpVersion.Version20` — dong 1105-1110.
7. `InvokeForHTTP` + `SendAsync`, `measureByKey = option.Uri` — dong 1111-1121.
8. `EnsureSuccessOrException` (dong 1123) -> `ResponseResult<TResponse>` (dong 1125).
9. `catch` x3 (dong 1129, 1140, 1151), `finally` (dong 1162-1182).

**Side effect** — Mutate `option.Client` (`BaseAddress`, `Accept`, `Authorization`). Goi API ngoai. Ghi log nhu muc 2.1.

**Error handling** — Giong muc 2.1. Loi serialize body **khong** duoc bat.

**Khi nao NEN dung** — Cap nhat toan bo resource (`PUT` semantics) voi body JSON, khong can header tuy chinh.

**Khi nao KHONG dung** — Khi can header tuy chinh (khong co overload `PutWithHeaders...`); khi can cap nhat mot phan (dung `PatchAsJSonAsync`); khi can query string kem theo (method nay chi dung `option.Uri`, **khong build query string** — caller phai tu ghep vao `option.Uri`).

**Gioi han**

- **Khong build query string**: `option.Value` chi duoc dung lam body. Neu API can ca query lan body, caller phai tu ghep query vao `option.Uri`.
- `JsonSerializer.Serialize` ngoai `try`; `option.Value == null` -> body `"null"`.
- `HttpVersion.Version20` hardcode (dong 1108). Khong co retry noi tai. Mutate shared client.

---

### 2.10 DeleteAsJSonAsync

**Signature**

```csharp
public static async Task<(TResponse, ErrorModel)> DeleteAsJSonAsync(
    HttpOptionModel<TRequest> option, ILogger logger,
    HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
    int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
```

**Muc dich** — `DELETE` toi `option.Uri`, query string build tu `option.Value` bang `ParseModelToQueryString` (giong `GetAsJSonAsync`). **Khong gui body.**

**Input hop le** — Y het bang o muc 2.1.

**Output** — Giong bang Output muc 2.1. Luu y: DELETE thanh cong thuong tra `204 No Content` — luc do `Content-Type` rong, `ResponseResult` nem `CustomException` (`HttpClientUtilizes.cs:366-371`) va ket qua se la `(null, ErrorModel { Code = 204, Message = "" (body rong), Succeeded = false })`. Xem muc 3, van de #6.

**Dieu kien xu ly**

1. `cancellationToken.ThrowIfCancellationRequested()` — dong 1205.
2. `option.Value is null` ? `option.Uri` : `$"{option.Uri}?{ParseModelToQueryString(option.Value)}"` — dong 1213-1215, **ngoai `try`**.
3. `option.ConfigHttpClient()` — dong 1219.
4. `CreateLinkedTokenWithTimeout(...)` — dong 1221-1222.
5. `HttpRequestMessage(HttpMethod.Delete, urlQueryString)`, `Version = HttpVersion.Version20`, **khong set `Content`** — dong 1224-1228.
6. `InvokeForHTTP` + `SendAsync`, `measureByKey = option.Uri` (**khong** phai `urlQueryString`) — dong 1230-1239.
7. `EnsureSuccessOrException` (dong 1241) -> `ResponseResult<TResponse>` (dong 1243).
8. `catch` x3 (dong 1247, 1258, 1269), `finally` (dong 1280-1300). Log co truong `URL` = `urlQueryString` (dong 1298).

**Side effect** — Mutate `option.Client` (`BaseAddress`, `Accept`, `Authorization`). Goi API ngoai. Ghi log nhu muc 2.1.

**Error handling** — Giong muc 2.1. Exception tu `ParseModelToQueryString` (dong 1215) **nem thang ra caller** vi nam ngoai `try`.

**Khi nao NEN dung** — Xoa resource khi tham so nam o query string / da nam trong `option.Uri`, va API tra JSON body cho ket qua xoa.

**Khi nao KHONG dung**

- Khi API DELETE tra `204 No Content` hoac body rong: se bi coi la loi (xem Output o tren).
- Khi DELETE can body (`request body` cho DELETE): method nay **khong** ho tro.
- Khi model query co property collection (query sinh sai, xem muc 2.12).

**Gioi han**

- Khong ho tro body cho DELETE.
- Neu `option.Value` khac `null` nhung khong sinh tham so nao, URL ket thuc bang dau `?` du (dong 1215).
- `HttpVersion.Version20` hardcode (dong 1226). Khong co retry noi tai. Mutate shared client.

---

### 2.11 PatchAsJSonAsync

**Signature**

```csharp
public static async Task<(TResponse, ErrorModel)> PatchAsJSonAsync(
    HttpOptionModel<TRequest> option, ILogger logger,
    HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
    int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
```

**Muc dich** — `PATCH` body JSON. Than ham trung khop `PostAsJSonAsync` / `PutAsJSonAsync`, chi doi `HttpMethod.Patch` (dong 1342) va `httpMethod: HttpMethod.Patch.Method` khi log (dong 1402).

**Input hop le** — Y het bang o muc 2.5.

**Output** — Giong bang Output muc 2.1.

**Dieu kien xu ly**

1. `cancellationToken.ThrowIfCancellationRequested()` — dong 1323.
2. `string json = System.Text.Json.JsonSerializer.Serialize(option.Value);` — **dong 1331, NGOAI `try`**.
3. `option.ConfigHttpClient()` — dong 1335.
4. `StringContent content = new(json, Encoding.UTF8, MediaTypeNames.Application.Json);` — dong 1337 (`Content-Type: application/json`, **khong** phai `application/merge-patch+json` hay `application/json-patch+json`).
5. `CreateLinkedTokenWithTimeout(...)` — dong 1339-1340.
6. `HttpRequestMessage(HttpMethod.Patch, option.Uri)`, `Version = HttpVersion.Version20` — dong 1342-1347.
7. `InvokeForHTTP` + `SendAsync`, `measureByKey = option.Uri` — dong 1348-1358.
8. `EnsureSuccessOrException` (dong 1360) -> `ResponseResult<TResponse>` (dong 1362).
9. `catch` x3 (dong 1366, 1377, 1388), `finally` (dong 1399-1419).

**Side effect** — Mutate `option.Client`. Goi API ngoai. Ghi log nhu muc 2.1.

**Error handling** — Giong muc 2.1. Loi serialize body **khong** duoc bat.

**Khi nao NEN dung** — Cap nhat mot phan resource khi server nhan `PATCH` voi `Content-Type: application/json`.

**Khi nao KHONG dung** — Khi server yeu cau `application/json-patch+json` (RFC 6902) hoac `application/merge-patch+json` (RFC 7396): media type **hardcode** `application/json` (dong 1337), khong the doi. Khong dung khi can header tuy chinh hoac query string (khong ho tro).

**Gioi han**

- Media type body hardcode `MediaTypeNames.Application.Json`.
- `JsonSerializer.Serialize` ngoai `try`; `option.Value == null` -> body `"null"`.
- Khong build query string. `HttpVersion.Version20` hardcode (dong 1345). Khong co retry noi tai.

---

### 2.12 ParseModelToQueryString (private)

**Signature**

```csharp
private static string ParseModelToQueryString(TRequest data)
```

**Muc dich** — Build query string tu `data` bang reflection. Duoc `GetAsJSonAsync` (dong 44), `GetAsJSonAndHeaderAsync` (dong 160) va `DeleteAsJSonAsync` (dong 1215) su dung. **Khong public** — khong the goi tu ben ngoai.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `data` | `TRequest` | Co | **Khong null-check** — `data.GetType()` (dong 1433) nem `NullReferenceException` neu `null`. Caller da chan bang `option.Value is null` truoc do | — |

**Output** — `string` query string **khong co dau `?` o dau** va **khong co `&` o cuoi**:

| Truong hop | Ket qua |
|---|---|
| Co it nhat 1 property duoc lay | `"key1=value1&key2=value2"` (da cat `&` cuoi, dong 1453-1462) |
| Tat ca property bi bo qua | Chuoi rong `""` (dong 1455 tra ve `result.ToString()` khi khong ket thuc bang `&`) |

**Dieu kien xu ly**

1. `Type type = data.GetType();` — dong 1433 (dung **runtime type**, khong phai `typeof(TRequest)`).
2. Voi moi `PropertyInfo` trong `type.GetProperties()` (dong 1435):
   - Lay `value = item.GetValue(data, index: null)` (dong 1437).
   - Bo qua neu `value is null || value.Equals("null") || string.IsNullOrWhiteSpace(value.ToString())` (dong 1439-1442) — luu y dieu kien `value.Equals("null")` chi dung voi gia tri chuoi `"null"`.
   - Lay `JsonPropertyNameAttribute` (dong 1444); `key` = `jsonAttr.Name.Trim()` neu co, nguoc lai `item.Name.Trim()` (dong 1446).
   - Neu `value is string` -> `result.Append($"{key}={HttpUtility.UrlEncode(value.ToString())}&")`; nguoc lai -> `result.Append($"{key}={value.ToString()}&")` (dong 1448-1450). **Chi encode gia tri kieu `string`.**
3. Neu chuoi khong ket thuc bang `&` -> tra ve nguyen chuoi (dong 1453-1456).
4. Nguoc lai, tao `StringBuilder` moi, copy `result` bo ky tu cuoi, tra ve (dong 1458-1462).

**Side effect** — Khong co (ham thuan, chi doc `data` qua reflection).

**Error handling** — **Khong co `try/catch`**. Moi exception (`NullReferenceException` khi `data == null`, `TargetInvocationException` khi getter cua property nem loi, `TargetParameterCountException` voi indexer property) **thoat ra ngoai**. Vi cac caller goi ham nay **ngoai block `try`**, exception se noi thang toi caller cua `GetAsJSonAsync` / `GetAsJSonAndHeaderAsync` / `DeleteAsJSonAsync` va **khong** duoc chuyen thanh `ErrorModel`.

**Khi nao NEN dung** — Khong ap dung truc tiep (private). Chi anh huong den lua chon method: neu model query dung `JsonPropertyNameAttribute` de doi ten tham so thi phai chon `GetAsJSonAsync` / `GetAsJSonAndHeaderAsync` / `DeleteAsJSonAsync`.

**Khi nao KHONG dung** — Khong ap dung.

**Gioi han**

- **Khong ho tro property dang collection**: `List<string>`, `int[]`... roi vao nhanh `value.ToString()` -> sinh chuoi nhu `System.Collections.Generic.List\`1[System.String]`.
- **Chi encode gia tri `string`**: `DateTime` (chua dau cach va `:`), `enum`, `decimal` khong duoc encode -> URL co the khong hop le hoac bi hieu sai.
- **Bo qua gia tri `false` cua `bool`?** Khong — `false.ToString()` = `"False"`, khong rong, nen van duoc ghi. Nhung gia tri `string` la `"null"` (chuoi) bi bo qua (dong 1439).
- Dung `data.GetType()` (runtime type) nen voi doi tuong ke thua, property cua lop con cung duoc dua vao query string du `TRequest` la lop cha. **Khong nhat quan** voi `PostAsFileAsync` / `PostAsFileV2Async` — hai method do dung `typeof(TRequest)` (compile-time type, dong 678 va 839).
- `type.GetProperties()` goi **khong tham so** (dong 1435) nen dung `BindingFlags` mac dinh `Public | Instance | Static` — **public static property cung bi dua vao query string**. `HttpClientUtilizes.ToQueryString` thi chi lay `Public | Instance` (`HttpClientUtilizes.cs:113`).
- Property co indexer se lam `GetValue(data, null)` nem exception — khong duoc bat.
- Dung `HttpUtility.UrlEncode` (`System.Web`) ma hoa dau cach thanh `+`, khac `Uri.EscapeDataString` (`%20`) ma `HttpClientUtilizes.ToQueryString` dung.

---

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | Log tracing trong `finally` serialize **toan bo `option`** bang `System.Text.Json`, bao gom ca `Token` (JWT/API key) va `Client`; `result` (toan bo response da deserialize) duoc serialize bang `Newtonsoft.Json`. Hai serializer khac nhau trong cung mot dong log | `CallApiWithHttp.cs:131-133`, `247-249`, `369-371`, `496-498`, `613-615`, `775-777`, `934-936`, `1060-1062`, `1179-1181`, `1297-1299`, `1416-1418` | **Ro ri bi mat (`Token`) va du lieu ca nhan (payload response) vao log**; log phinh to; chi phi CPU serialize cho **moi** request ke ca khi thanh cong; neu serialize nem exception thi exception phat sinh trong `finally` se **thay the** ket qua/exception goc |
| 2 | `EnsureSuccessOrException` **khong nem** khi HTTP status la 4xx/5xx (doan `EnsureSuccessStatusCode()` bi comment). Luong code van chay tiep va deserialize body loi thanh `TResponse` | `HttpClientUtilizes.cs:401-416`, dac biet `412-415`; goi tai `CallApiWithHttp.cs:75`, `191`, `313`, `440`, `557`, `719`, `878`, `1004`, `1123`, `1241`, `1360` | **Mau thuan voi XML doc** (`"data null nếu lỗi"`) o moi method. Caller **khong the** dua vao `data != null` de biet thanh cong — **bat buoc kiem tra `ErrorModel.Succeeded`**. Truong hop xau: body loi cua server co truong trung ten voi `TResponse` -> `data` mang du lieu sai lech nhung khong `null` |
| 3 | `ConfigHttpClient` goi `client.DefaultRequestHeaders.Accept.Add(...)` **moi lan** ma khong kiem tra da ton tai | `HttpClientUtilizes.cs:352` | Voi `HttpClient` dung lai (`IHttpClientFactory`/singleton), header `Accept` **tich luy trung lap** vo han: `Accept: application/json, application/json, application/json, ...`. Tang kich thuoc request theo so lan goi, co the vuot gioi han header cua server/proxy |
| 4 | `ConfigHttpClient` (va `PostFormUrlEncodedAsync` inline) gan `client.BaseAddress` moi lan goi | `HttpClientUtilizes.cs:347-350`; `CallApiWithHttp.cs:408-411` | `HttpClient` nem `InvalidOperationException` khi doi `BaseAddress` sau khi instance da gui request. Loi nay bi bat boi `catch (Exception)` va **bien thanh `Code = 500` voi message chung**, rat kho chan doan tu log |
| 5 | `PostWithHeadersAsJSonAsync` ghi header vao `client.DefaultRequestHeaders` thay vi `HttpRequestMessage.Headers`, va **khong xoa** sau khi dung | `CallApiWithHttp.cs:975-982` | Header **ro ri sang moi request khac** dung cung `HttpClient` (kha nang lot header xac thuc/tenant giua cac request). Goi lan hai voi cung key -> `InvalidOperationException` (duplicate) -> `Code = 500`. Khong thread-safe. Doi lap voi `GetAsJSonCustomHeaderAsync` (dong 296) lam **dung** cach |
| 6 | `ResponseResult` nem `CustomException` khi `Content-Type` rong hoac chua `text/html` | `HttpClientUtilizes.cs:364-372`; anh huong manh nhat cho `DeleteAsJSonAsync` | Response `204 No Content` hoac `200` voi body rong (khong co `Content-Type`) bi coi la **loi**: `data = null`, `Succeeded = false`, `Message` = nguyen van body. Ngoai ra khi proxy/WAF tra trang HTML loi, `ErrorModel.Message` **chua toan bo HTML** va co the bi tra thang ve client |
| 7 | `cancellationToken.ThrowIfCancellationRequested()` va viec build URL/body nam **ngoai `try`** | `CallApiWithHttp.cs:38`+`44`, `154`+`160`, `272`+`282`, `396`, `519`+`527`, `639`, `801`, `959`+`967`, `1086`+`1094`, `1205`+`1215`, `1323`+`1331` | Pha vo hop dong "khong bao gio nem": `OperationCanceledException`, loi serialize JSON, loi reflection query string deu **nem thang ra caller** thay vi tra `ErrorModel`. Caller nao chi kiem tra `ErrorModel` se bi exception khong mong doi |
| 8 | `PostAsFileAsync` goi `ReadAsync` **mot lan** va bo qua so byte doc duoc; ep `(int)fileStream.Length` | `CallApiWithHttp.cs:663`, `665-666` | Noi dung file gui len **co the bi thieu** neu stream khong tra du byte trong mot lan doc. File > 2 GB gay tran `int`. Ngoai ra toan bo file nam trong `byte[]` -> ap luc bo nho / Large Object Heap |
| 9 | `PostAsFileAsync` **khong bo qua** property kieu `IFormFile` khi duyet reflection, con `PostAsFileV2Async` co bo qua | `CallApiWithHttp.cs:678-697` so voi `842` | Neu `TRequest` co property `IFormFile`, `PostAsFileAsync` gui len mot form field chua **ten kieu .NET** (`Microsoft.AspNetCore.Http.Internal.FormFile`) thay vi noi dung file — du lieu rac toi server |
| 10 | `PostAsFileAsync` / `PostAsFileV2Async` khong dung `using` cho `CancellationTokenSource`, `HttpRequestMessage`, `MultipartFormDataContent`; `PostAsFileV2Async` khong dispose `stream` mo tu `OpenReadStream()`. Moi method deu khong dispose `HttpResponseMessage` | `CallApiWithHttp.cs:702-703`, `708-713`, `859-860`, `865-870`, `821` | Ro ri handle/timer cho tai khi GC don. `CancellationTokenSource` co `CancelAfter` giu timer trong `TimerQueue` -> ap luc GC va rui ro can kiet resource duoi tai cao |
| 11 | `PostAsFileV2Async` set `ContentDisposition` thu cong (co **chen dau ngoac kep** vao `Name`/`FileName`) roi lai goi `content.Add(streamContent, fileParameterName, file.FileName)` | `CallApiWithHttp.cs:826-833` | Hai co che dat `Content-Disposition` chong nhau; ket qua cuoi cung phu thuoc implementation `MultipartFormDataContent.Add` cua .NET runtime — **khong xac dinh duoc tu source code repo**. Rui ro header bi ngoac kep hai lan, server parse sai ten file |
| 12 | `PostAsFileV2Async` ep `Version = HttpVersion.Version10` (khong the thay doi qua tham so) | `CallApiWithHttp.cs:868` | HTTP/1.0 khong ho tro chunked transfer encoding va keep-alive mac dinh; upload lon co the bi proxy/server tu choi. XML doc (dong 784) co ghi nhan dieu nay, nen day la co y — nhung caller **khong duoc phep** override |
| 13 | Query string duoc build bang **hai co che khac nhau** giua cac method: `ParseModelToQueryString` (`GetAsJSonAsync`, `GetAsJSonAndHeaderAsync`, `DeleteAsJSonAsync`) vs `HttpClientUtilizes.ToQueryString` (`GetAsJSonCustomHeaderAsync`) | `CallApiWithHttp.cs:44`, `160`, `1215` so voi `282`; `CallApiWithHttp.cs:1429-1463` so voi `HttpClientUtilizes.cs:111-132` | **Cung mot model sinh ra hai URL khac nhau**: `ParseModelToQueryString` doc `JsonPropertyNameAttribute`, encode bang `HttpUtility.UrlEncode` (dau cach -> `+`), duyet `Public\|Instance\|Static`, bo qua gia tri whitespace va chuoi `"null"`; `ToQueryString` **bo qua** attribute, encode ca ten property, dung `Uri.EscapeDataString` (dau cach -> `%20`), chi duyet `Public\|Instance`, va vi escape **truoc** khi kiem tra rong nen **van gui** gia tri chi gom khoang trang (thanh `%20`). Doi method la co the vo API |
| 14 | Khi `option.Value != null` nhung khong sinh duoc tham so nao, URL bi them dau `?` du | `CallApiWithHttp.cs:44`, `160`, `1215` | URL ket thuc bang `?`. Thuong vo hai nhung co the lam sai cache key, lam sai signature/HMAC neu API xac thuc theo URL |
| 15 | `GetAsJSonCustomHeaderAsync` **khong null-check** `headers`, trong khi `PostWithHeadersAsJSonAsync` co check | `CallApiWithHttp.cs:294` so voi `975` | Truyen `headers = null` cho GET -> `NullReferenceException` -> `Code = 500` voi message `"Hệ thống ... đang gặp sự cố tạm thời"`, che mat nguyen nhan that (loi lap trinh) |
| 16 | `measureByKey` va noi dung log tracing khong nhat quan giua cac method; tham so `uriWithQuery` cua `HttpResultWithTracing` **khong bao gio duoc truyen** | `CallApiWithHttp.cs:72`/`188` (`urlQueryString`), `309`/`436`/`553`/`1001`/`1120`/`1237`/`1357` (`option.Uri`), `715`/`874` (`$"{BaseAddress}{Uri}"`); truong `URL` co o dong 132, 248, 1298 nhung thieu o dong 370, 497, 614, 776, 935, 1061, 1180, 1417; `LoggerExtensions.cs:468` | Log `Warning` cua `GetAsJSonAsync` va `GetAsJSonAndHeaderAsync` chua **nguyen ca query string** (rui ro PII). Nguoc lai, phan lon log tracing **thieu truong `URL`**, gay kho khi dieu tra su co theo endpoint cu the. Ngoai ra truong `EndpointWithQuery` trong template log **luon rong** vi khong call site nao truyen `uriWithQuery` — log ra dang `Endpoint:{uri}.` (co dau `.` du) |
| 17 | `direction` duoc suy ra tu `HasPort(option.BaseAddress)`: co port khac mac dinh -> `Inbound`, nguoc lai -> `Outbound` | `CallApiWithHttp.cs:124-129` (va cac vi tri tuong tu); `HttpClientUtilizes.cs:35-48` | Moi lan goi trong lop nay ban chat deu la **outbound** (client goi ra ngoai). Viec gan nhan `Inbound` chi vi URL co port (vi du `http://internal-svc:8080`) co the lam **sai lech dashboard/alert** dua tren `Direction`. Khong xac dinh duoc tu source code lieu day la co y (phan biet internal/external) hay khong |
| 18 | `desiredTime` chi sinh log `Warning`, khong co tac dung dieu khien | `MeasureExecutionTimeExtensions.cs:84-90` | De hieu sai la nguong timeout. Timeout that su chi den tu `cancellationTokenTime` (`CancellationTokenHelper.cs:16-19`) va `HttpClient.Timeout` do caller cau hinh ben ngoai |
| 19 | `cancellationTokenTime <= 0` khien **khong co timeout nao** duoc ap | `CancellationTokenHelper.cs:16` | Truyen `0` (hoac so am) de "tat timeout" se khien request phu thuoc hoan toan vao `HttpClient.Timeout` (mac dinh 100 giay) — de gay treo request lau hon mong doi |
| 20 | Khong co retry noi tai; `cancellationTokenTime` duoc `CancelAfter` **truoc** `SendAsync` | `CallApiWithHttp.cs:54-73` (va tuong tu o cac method khac); `CancellationTokenHelper.cs:12-22` | Loi tam thoi (transient) khong duoc thu lai. Neu tang ngoai gan pipeline retry qua `AddPolicyHandler`, timeout `cancellationTokenTime` **bao trum toan bo cac lan retry** -> retry that su khong co thoi gian de chay. Trong repo nay khong tim thay `AddPolicyHandler` cho `HttpClient` (Polly chi dung cho SQL va MongoDB) |
| 21 | `logger` khong duoc null-check nhung `finally` luon goi `logger.HttpResultWithTracing` | `CallApiWithHttp.cs:116`, `232`, `354`, `481`, `598`, `760`, `919`, `1045`, `1164`, `1282`, `1401`; `LoggerExtensions.cs:471` | `logger == null` lam `finally` **nem exception**, thay the ket qua tra ve va khong bi bat boi bat ky `catch` nao trong method. Loai exception la `ArgumentNullException` (guard cua `Microsoft.Extensions.Logging.LoggerExtensions.Log`), **khong** phai `NullReferenceException`; chi tiet nam trong BCL nen khong xac dinh duoc tu source code cua repo |
| 22 | `PostFormUrlEncodedAsync` khong goi `ConfigHttpClient` nen **khong set header `Accept: application/json`** | `CallApiWithHttp.cs:406-417` so voi `HttpClientUtilizes.cs:343-360` | Server co the tra `text/html` -> `ResponseResult` nem `CustomException` -> `Succeeded = false` du request thanh cong ve mat nghiep vu. Hanh vi khong dong nhat voi cac method con lai |
| 23 | `ReadAsStreamAsync` **khong nem** khi deserialize that bai — chi log `Error` va tra `default` | `HttpClientUtilizes.cs:317-341` | Co the xay ra `(data == null, Succeeded == true)`: HTTP 200 nhung body sai schema. Caller kiem tra `Succeeded` roi truy cap `data` -> `NullReferenceException`. **Bat buoc kiem tra ca `Succeeded` va `data != null`** |
| 24 | XML doc cua `GetAsJSonCustomHeaderAsync` (dong 253-256) va `PostWithHeadersAsJSonAsync` (dong 940-944) mo ta dung khac biet `HttpRequestMessage.Headers` vs `DefaultRequestHeaders`, nhung XML doc `"data null nếu lỗi"` o **tat ca** method thi **khong dung** voi hanh vi that | So sanh XML doc dong 31, 147, 265, 389, 512, 632, 794, 952, 1079, 1199, 1316 voi `HttpClientUtilizes.cs:412-415` | Tin **than ham**, khong tin XML doc ve diem nay. Xem van de #2 |
| 25 | `ErrorModel.Message` lay tu `httpResponseMessage.ReasonPhrase` trong khi `Version` bi hardcode `HttpVersion.Version20` | `HttpClientUtilizes.cs:409`; `CallApiWithHttp.cs:59`, `175`, `290`, `424`, `542`, `711`, `989`, `1108`, `1226`, `1345` | Voi ket noi thuc su chay HTTP/2, giao thuc khong co reason phrase nen `ErrorModel.Message` co the la `null` ngay ca khi `Succeeded = true`. Caller in `Message` ra UI/log co the nhan `null`. Gia tri cuoi cung phu thuoc protocol thuong luong — **khong xac dinh duoc tu source code cua repo** |
| 26 | `cancellationTokenTime` **khong bao trum** giai doan chuan bi noi dung file trong hai method upload: `CancellationTokenSource` chi duoc tao **sau** khi doc/mo file xong | `CallApiWithHttp.cs:655-673` roi `702-703` (`PostAsFileAsync`); `817-834` roi `859-860` (`PostAsFileV2Async`) | Doc file lon vao `byte[]` (hoac mo stream) co the treo vo han neu caller khong tu truyen `cancellationToken` co timeout. Timeout duoc quang cao "cho toan bo request" tren XML doc thuc te chi ap cho `SendAsync` |
| 27 | Reflection dung **hai kieu type** khac nhau: `data.GetType()` (runtime) trong `ParseModelToQueryString` vs `typeof(TRequest)` (compile-time) trong hai method upload | `CallApiWithHttp.cs:1433` so voi `678` va `839` | Cung mot doi tuong lop con truyen qua `TRequest` la lop cha: query string **co** property cua lop con, con form field multipart **khong co**. Kho phat hien khi debug |
| 28 | Viec doc `JsonPropertyNameAttribute` khong nhat quan giua cac duong dan trong cung mot class: **co doc** o `ParseModelToQueryString` (`CallApiWithHttp.cs:1444-1446`) va o body JSON (`System.Text.Json` mac dinh, `527`/`967`/`1094`/`1331`); **khong doc** o `HttpClientUtilizes.ToQueryString` (`HttpClientUtilizes.cs:118`) va o nhanh multipart (`CallApiWithHttp.cs:690`/`696`, `849`/`854`) | Cung mot model co `JsonPropertyNameAttribute` se sinh **hai bo ten field khac nhau** tuy method: GET/DELETE thuong + body JSON dung ten trong attribute, con `GetAsJSonCustomHeaderAsync` + `PostAsFileAsync`/`PostAsFileV2Async` dung ten property .NET. API dich se nhan sai ten tham so o nhom thu hai |
| 29 | `statusCode: result.errorModel?.Code.ToString()` dung null-conditional tren mot gia tri **khong bao gio null** | `CallApiWithHttp.cs:123`, `239`, `361`, `488`, `605`, `767`, `926`, `1052`, `1171`, `1289`, `1408` | `errorModel` duoc khoi tao `new()` truoc khi gan vao `result` va **khong bao gio** bi gan lai `null`, nen `?.` la dead code. Che di viec neu tinh huong nao do lam `errorModel` null thi `statusCode` se la `null` (log mat status code) thay vi loi ro rang |
