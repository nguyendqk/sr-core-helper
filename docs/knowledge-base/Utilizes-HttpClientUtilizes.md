# HttpClientUtilizes

> Nguon: FTELSRCore.Shared/Utilizes/HttpClientUtilizes.cs
> Loai: static class (nhieu class trong cung 1 file: `HttpClientUtilizes`, `TokenExpirationHelperUtilizes`, `HttpContentExtensionsUtilizes`) + 2 abstract class (`FormatOptions`, `FormatOptions<TPath>`)
> Cap nhat theo commit: 89c1ce9

## 1. Tong quan

File nay nam o tang `FTELSRCore.Shared/Utilizes` va tap hop cac helper ky thuat dung boi lop goi HTTP `CallApiWithHttp.cs`: cau hinh `HttpClient`, doc/parse body response thanh JSON, map exception sang `ErrorModel`, va vai helper doc lap (build query string, kiem tra JWT het han, kiem tra URL co port). File **khong chua business logic nghiep vu** — day la tang ha tang (infrastructure) dung chung cho moi loi goi API ra ngoai trong repo.

File gom 4 nhom noi dung khac nhau, khong lien quan truc tiep ve mat logic:
1. `FormatOptions` / `FormatOptions<TPath>` — abstract class cau hinh dia chi dich (host/scheme/port...).
2. `HttpClientUtilizes` (static, **public**) — `HasPort`, `GetUri` (2 overload, extension cua `FormatOptions`), `ToQueryString`, `LogHttpResult`.
3. `TokenExpirationHelperUtilizes` (static, **public**) — kiem tra/tinh thoi gian song cua JWT.
4. `HttpContentExtensionsUtilizes` (static, **internal**) — cau hinh `HttpClient`, doc/parse body, map loi. Day la nhom duoc `CallApiWithHttp.cs` goi truc tiep o hau het method.

**Quan trong**: `logger.HttpResult`, `logger.HttpErrorResult` (dung trong `LogHttpResult`, `ReadAsJSonAsync`, `ReadAsStreamAsync`) va `logger.HttpResultWithTracing` (dung boi `CallApiWithHttp.cs`, **khong** dung trong file nay) **KHONG duoc dinh nghia trong file nay** — chung la extension method cua `ILogger` dinh nghia tai `FTELSRCore.Shared/Extensions/Loggers/LoggerExtensions.cs` (`HttpResult` dong 412, `HttpErrorResult` dong 426/443, `HttpResultWithTracing` dong 460). File nay chi **goi** chung, khong so huu chung.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Cau hinh `HttpClient` dung chung: `BaseAddress`, header `Accept: application/json`, header `Authorization` (`ConfigHttpClient`) | Khong tu xoa/reset header cu tren `HttpClient` — moi lan goi `ConfigHttpClient` deu `Add` them `Accept`, khong kiem tra da ton tai |
| Doc body response va deserialize JSON qua `System.Text.Json`, co log loi khi that bai (`ReadAsJSonAsync`, `ReadAsStreamAsync`) | Khong nem exception khi deserialize that bai — chi log va tra `default` (khong phai `throw`) |
| Phan biet response "co JSON hop le" voi "khong phai JSON" dua vao `Content-Type` (`ResponseResult`) | Khong kiem tra `Content-Type` co dung la `application/json` hay khong — chi loai tru truong hop rong hoac `text/html`, moi media type khac (vi du `text/plain`, `application/xml`) van duoc coi la "hop le" va dua vao deserialize JSON |
| Gan `Code`/`Message`/`Succeeded` cua `ErrorModel` tu `HttpResponseMessage` (`EnsureSuccessOrException`) | **Khong nem exception khi HTTP status la 4xx/5xx** — doan `EnsureSuccessStatusCode()` da bi comment (dong 412-415) |
| Map 3 loai exception (`Exception`, `CustomException`, `OperationCanceledException`) sang `ErrorModel` voi message tieng Viet co san (`ErrorException` x2, `ErrorCanceledException`) | Khong giu lai noi dung exception goc cho overload `Exception` — tham so dat ten `_` (discard), message goc **bi bo hoan toan** |
| Build query string tu object bang reflection (`ToQueryString`) | Khong doc `JsonPropertyNameAttribute`, khong ho tro property kieu collection/array |
| Kiem tra URL co port khac mac dinh hay khong (`HasPort`) — dung de chon `DirectionType` khi log tracing o `CallApiWithHttp.cs` | Khong tu suy luan Inbound/Outbound dung nghia nghiep vu — chi dua thuan tuy vao viec URL co khai bao port |
| Ghi log ket qua HTTP theo co (`LogHttpResult`) | **Khong co call site nao trong repo goi `LogHttpResult`** — xem muc 3 |
| Kiem tra JWT con han hay khong, tinh thoi gian song con lai (`TokenExpirationHelperUtilizes`) | Khong validate signature/issuer cua JWT — chi doc payload base64 de lay claim `exp` |
| Tao `HttpOptionModel`/`HttpOptionModel<TValue>` tu `FormatOptions` (`GetUri` 2 overload) | **Khong co call site nao trong repo goi `GetUri`** — xem muc 3 |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `FTELSRCore.Models.Https.HttpOptionModel` / `HttpOptionModel<T>` (`FTELSRCore.Shared/Models/Https/HttpOptionModel.cs`) | `record` mang `Client`, `BaseAddress`, `Token`, `AuthType` (default `"Bearer"`), `Uri`, `SystemOwner` (default `"Service Request"`), `CompletionOption`; ban generic co them `Value { get; init; }` |
| `FTELSRCore.Models.Https.ErrorModel` (`FTELSRCore.Shared/Models/Https/ErrorModel.cs`) | `record` ket qua loi: `Code` (int), `Message` (string), `Succeeded` (bool) |
| `FTELSRCore.Exceptions.CustomException` (`FTELSRCore.Shared/Exceptions/CustomException.cs`) | Exception tuy bien mang them `Code` (int, default 500) |
| `FTELSRCore.Constants.DelimiterConstant.CHAR_APOSTROPHE`, `CHAR_DOT` (`FTELSRCore.Shared/Constants/DelimiterConstant.cs`) | Ky tu phan cach dung trong `GetUri` va `TokenExpirationHelperUtilizes` |
| `FTELSRCore.Constants.CommonBaseConstant.System`, `DateTimeUtc`, `ConfigLoggerExceptionByConsole` (`FTELSRCore.Shared/Constants/CommonBaseConstant.cs`) | Gia tri `SystemOwner` mac dinh, lay thoi gian UTC, log loi ra console khi catch exception |
| `FTELSRCore.Constants.CachedBaseConstant.RandomTimeCache` (`FTELSRCore.Shared/Constants/CachedBaseConstant.cs`) | Gia tri fallback (co jitter) khi khong doc duoc thoi gian song token |
| `FTELSRCore.Extensions.EnvironmentExtensions.GetEnvironment`, `ELocal` (`FTELSRCore.Shared/Extensions/EnvironmentExtensions.cs`) | Xac dinh moi truong hien tai de ep `HttpVersionPolicy` khi chay Local |
| `FTELSRCore.Helpers.JSonParseHelpers.JSonTryParse<T>` (`FTELSRCore.Shared/Helpers/JSonParseHelpers.cs`) | Parse JSON string sang `T`, dung trong `ReadAsJSonAsync` |
| `Microsoft.Extensions.Logging.ILogger` + `FTELSRCore.Extensions.Loggers.LoggerExtensions.HttpResult`/`HttpErrorResult` (`FTELSRCore.Shared/Extensions/Loggers/LoggerExtensions.cs:412,426,443`) | Ghi log ket qua thanh cong/loi — **dinh nghia o file khac**, file nay chi goi |
| `System.Text.Json.JsonSerializer` | Deserialize body response (`ReadAsStreamAsync`) voi option `PropertyNameCaseInsensitive = true`, `ReferenceHandler.IgnoreCycles`, `NumberHandling.AllowReadingFromString` |
| `System.Reflection.BindingFlags` | Duyet public instance property cua object trong `ToQueryString` |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `HttpClientUtilizes.HasPort(string url)` | public static method | URL co khai bao port khac mac dinh khong |
| `FormatOptions.GetUri(...)` | public static extension (non-generic) | Build `HttpOptionModel` tu `FormatOptions` |
| `FormatOptions.GetUri<TValue>(...)` | public static extension (generic) | Build `HttpOptionModel<TValue>` tu `FormatOptions`, kem `Value` |
| `HttpClientUtilizes.ToQueryString(object obj)` | public static method | Build query string tu object bang reflection |
| `ILogger.LogHttpResult(...)` | public static extension | Ghi log HTTP thanh cong/loi theo co `isSucceeded` |
| `TokenExpirationHelperUtilizes.IsExpiration(string jwt)` | public static method | JWT con han hay het han (bien phong 3 phut) |
| `TokenExpirationHelperUtilizes.GetExpirationTime(string jwt)` | public static method | So phut con lai truoc khi JWT het han |
| `HttpContentExtensionsUtilizes.SetHttpVersion(...)` | internal static method | Chon `HttpVersionPolicy` theo moi truong |
| `HttpContentExtensionsUtilizes.ReadAsJSonAsync<T>(...)` | internal static extension | Doc body thanh string roi parse JSON bang `JSonTryParse` |
| `HttpContentExtensionsUtilizes.ReadAsStreamAsync<T>(...)` | internal static extension | Doc body qua stream roi `JsonSerializer.DeserializeAsync` |
| `HttpContentExtensionsUtilizes.ConfigHttpClient(...)` | internal static extension | Gan `BaseAddress`, `Accept`, `Authorization` len `HttpClient` |
| `HttpContentExtensionsUtilizes.ResponseResult<TResponse>(...)` | internal static extension | Kiem tra `Content-Type`, nem loi hoac deserialize |
| `HttpContentExtensionsUtilizes.ErrorException(ref ErrorModel, HttpOptionModel, Exception)` | internal static method | Map `Exception` thuong sang `ErrorModel` |
| `HttpContentExtensionsUtilizes.ErrorException(ref ErrorModel, HttpOptionModel, CustomException)` | internal static method | Map `CustomException` sang `ErrorModel` |
| `HttpContentExtensionsUtilizes.ErrorCanceledException(...)` | internal static method | Map `OperationCanceledException` sang `ErrorModel` |
| `HttpContentExtensionsUtilizes.EnsureSuccessOrException(...)` | internal static extension | Gan `Code`/`Message`/`Succeeded` tu `HttpResponseMessage` |

## 2. Chi tiet API

### 2.1 HasPort

**Signature**
```csharp
public static bool HasPort(string url)
```
**Muc dich** — Xac dinh mot URL co khai bao port khac port mac dinh cua scheme hay khong (dong 35-48).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `url` | `string` | Co | Neu rong/whitespace hoac khong parse duoc thanh `Uri` tuyet doi (`Uri.TryCreate(url, UriKind.Absolute, ...)`) thi coi la khong co port | — |

**Output** — `bool`: `true` neu `Uri.TryCreate` thanh cong VA `uri.IsDefaultPort == false` (co port rieng, vi du `http://host:8080`); `false` trong moi truong hop con lai (rong, khong parse duoc, hoac dung port mac dinh cua scheme nhu `80`/`443`).

**Dieu kien xu ly**
1. `string.IsNullOrWhiteSpace(url)` -> `false`.
2. `!Uri.TryCreate(url, UriKind.Absolute, out var uri)` -> `false` (URL tuong doi hoac sai format cung roi vao day).
3. Con lai: tra `!uri.IsDefaultPort`.

**Side effect** — Khong co.

**Error handling** — Khong throw; `Uri.TryCreate` tu no khong nem exception khi that bai (tra `false` qua `out` param).

**Khi nao NEN dung** — Kiem tra nhanh mot URL co chi dinh port tuong minh, dung lam tieu chi phan loai (vi du `CallApiWithHttp.cs` dung de chon `DirectionType.Inbound`/`Outbound` khi log tracing).

**Khi nao KHONG dung** — Khong dung de xac dinh URL nay la noi bo (Inbound) hay ben ngoai (Outbound) mot cach chinh xac — viec co port hay khong **khong lien quan** ve mat ngu nghia den huong goi (xem muc 3).

**Gioi han** — Ten ham "HasPort" khong phan anh day du dieu kien: URL rong hoac sai format cung tra `false` giong nhu "khong co port", nen `false` mang 3 y nghia khac nhau (rong / sai format / dung port mac dinh) ma caller khong phan biet duoc.

---

### 2.2 GetUri (non-generic)

**Signature**
```csharp
public static HttpOptionModel GetUri(this FormatOptions option, string requestUri, HttpClient httpClient, string token = "")
```
**Muc dich** — Extension method cua `FormatOptions`, dung `UriBuilder` de ghep `Host`/`Scheme`/`Port` thanh `BaseAddress`, ghep `SubDirectory` (neu co) vao truoc `requestUri`, roi tra ve `HttpOptionModel` san sang truyen cho cac method trong `CallApiWithHttp.cs` (dong 50-78).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `option` | `FormatOptions` | Co | Khong null-check; `option.Host`/`option.Scheme` duoc dua thang vao `UriBuilder` | — |
| `requestUri` | `string` | Co | Neu `option.SubDirectory` khac rong, bi noi lai thanh `option.SubDirectory + '/' + requestUri` (dung `DelimiterConstant.CHAR_APOSTROPHE`, gia tri thuc te la `'/'` — xem muc 3) | — |
| `httpClient` | `HttpClient` | Co | Khong null-check, gan thang vao `HttpOptionModel.Client` | — |
| `token` | `string` | Khong | — | `""` |

**Output** — `HttpOptionModel` voi `Token`, `Uri` (da noi `SubDirectory` neu co), `Client`, `BaseAddress` (chuoi tu `UriBuilder`, da xoa ky tu `[` va `]`), `SystemOwner` (lay tu `option.SystemOwner`).

**Dieu kien xu ly**
1. Tao `UriBuilder` voi `Host`, `Scheme` tu `option`.
2. Neu `option.Port > 0` -> gan `uriBuilder.Port`.
3. Neu `option.SubDirectory` khac rong -> noi vao truoc `requestUri` bang ky tu `DelimiterConstant.CHAR_APOSTROPHE` (thuc te la `/`).
4. `baseAddress = uriBuilder.ToString().Replace("[", "").Replace("]", "")` — loai bo dau `[` `]` (thuong xuat hien khi `Host` la dia chi IPv6 khong co port).
5. Tra `HttpOptionModel` moi.

**Side effect** — Khong mutate `httpClient` (chi gan reference vao `Client` cua object tra ve — viec `ConfigHttpClient` mutate client thuc su xay ra o buoc goi sau, khong phai trong ham nay).

**Error handling** — Khong co try/catch; loi tu `UriBuilder` (vi du `Host` null) se nem thang ra ngoai.

**Khi nao NEN dung** — Khi co san mot object ke thua `FormatOptions` (host/scheme/port cau hinh rieng cho tung API dich) va can build `HttpOptionModel` truoc khi goi `CallApiWithHttp.cs`.

**Khi nao KHONG dung** — Khong dung khi da co `BaseAddress` day du duoi dang string — ham nay luon build lai tu `Host`/`Scheme`/`Port`.

**Gioi han** — **Khong co call site nao trong repo hien tai goi `GetUri`** (ca 2 overload) — xem muc 3. Khong validate `option.Host` rong/null truoc khi dua vao `UriBuilder`.

---

### 2.3 GetUri<TValue> (generic)

**Signature**
```csharp
public static HttpOptionModel<TValue> GetUri<TValue>(this FormatOptions option, string requestUri, TValue value, HttpClient httpClient, string token = "")
```
**Muc dich** — Giong 2.2 nhung tra ve `HttpOptionModel<TValue>` co them `Value = value` (dong 80-109), dung khi API dich can body/query object.

**Input hop le** — Giong 2.2, cong them:

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `value` | `TValue` | Co | Khong validate, gan thang vao `HttpOptionModel<TValue>.Value` (property `init`) | — |

**Output** — `HttpOptionModel<TValue>` (them `Value`), cac truong con lai giong 2.2.

**Dieu kien xu ly** — Hoan toan trung voi 2.2 (build `UriBuilder`, ghep `SubDirectory`, xoa `[`/`]`).

**Side effect** — Khong co (giong 2.2).

**Error handling** — Khong co try/catch, giong 2.2.

**Khi nao NEN dung / KHONG dung** — Giong 2.2, chon overload nay khi can mang kem `TValue` (body hoac tham so query).

**Gioi han** — **Khong co call site nao trong repo hien tai goi ham nay** — xem muc 3. Trung code voi overload 2.2 (2 block giong nhau ~90%, khong tan dung lai logic chung).

---

### 2.4 ToQueryString

**Signature**
```csharp
public static string ToQueryString(object obj)
```
**Muc dich** — Build query string tu cac public property cua `obj` bang reflection (dong 111-132). Duoc `CallApiWithHttp.cs` dung rieng cho `GetAsJSonCustomHeaderAsync` (theo mo ta trong `Utilizes-CallApiWithHttp.md`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `obj` | `object` | Co | **Khong null-check** — `obj.GetType()` se nem `NullReferenceException` neu `obj` la `null` | — |

**Output** — `string`: chuoi query da co dau `?` o dau (neu co it nhat 1 tham so hop le) hoac `string.Empty` (neu khong co property nao hoac tat ca gia tri deu rong sau khi escape).

**Dieu kien xu ly**
1. Lay danh sach property qua `obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)` — chi property **instance**, khong lay property **static**.
2. Voi tung property: `name = Uri.EscapeDataString(property.Name)` (escape ca ten), `value = Uri.EscapeDataString(property.GetValue(obj)?.ToString() ?? string.Empty)`.
3. **Escape truoc, kiem tra rong sau**: neu `value` (da escape) la `IsNullOrWhiteSpace` thi `continue` (bo qua property nay). Vi da escape, gia tri chi gom ky tu trang se thanh `%20...` (khong con la whitespace theo `char.IsWhiteSpace`) — nhung vi kiem tra dien ra **sau khi gan `value`**, van dung tren gia tri da escape nen truong hop toan whitespace van bi loai (vi `Uri.EscapeDataString(" ")` = `"%20"`, khong rong va khong whitespace theo ham `IsNullOrWhiteSpace` — **can luu y**: dieu nay nghia la gia tri toan khoang trang **khong** bi loai, van duoc ghi vao URL dang `%20`).
4. Noi vao `StringBuilder`: ky tu dau tien la `?`, cac lan sau la `&`, dang `name=value`.
5. Tra `queryString.ToString()`.

**Side effect** — Khong co.

**Error handling** — Khong co try/catch; `NullReferenceException` khi `obj` la `null` se nem thang ra caller.

**Khi nao NEN dung** — Build query string tu mot object don gian (property la kieu scalar: string/số/enum/DateTime) khi khong can doc `JsonPropertyNameAttribute`.

**Khi nao KHONG dung** — Khi model co property kieu collection/array (`property.GetValue(obj)?.ToString()` cho ket qua la ten kieu .NET, vi du `System.Collections.Generic.List\`1[System.String]`, khong phai danh sach gia tri); khi can giu dung ten tham so theo `JsonPropertyNameAttribute` (ham nay luon dung `property.Name`).

**Gioi han**
- Khong ho tro property dang collection/array.
- Khong doc `JsonPropertyNameAttribute` — ten tham so URL luon la ten property C#.
- Chi lay property **Public + Instance** (`BindingFlags.Public | BindingFlags.Instance`) — khac co che `ParseModelToQueryString` trong `CallApiWithHttp.cs` (dung `GetProperties()` khong tham so, bao gom ca static) theo mo ta trong `Utilizes-CallApiWithHttp.md:276`.
- Gia tri toan khoang trang (`"   "`) van duoc ghi vao URL dang `%20` do thu tu escape-truoc-kiem-tra-sau.

---

### 2.5 LogHttpResult

**Signature**
```csharp
public static void LogHttpResult(
    this ILogger logger, string className, string methodName, object message, bool isSucceeded = true)
```
**Muc dich** — Wrapper chon nhanh log thanh cong hay log loi dua vao co `isSucceeded` (dong 134-151).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `logger` | `ILogger` | Co | Khong null-check — `NullReferenceException` neu `logger` la `null` | — |
| `className` | `string` | Co | Khong validate, chuyen thang cho `logger.HttpResult`/`HttpErrorResult` | — |
| `methodName` | `string` | Co | Khong validate | — |
| `message` | `object` | Co | Khong validate | — |
| `isSucceeded` | `bool` | Khong | Dieu khien nhanh `switch` | `true` |

**Output** — `void`.

**Dieu kien xu ly** — `switch (isSucceeded)`: `case true` -> goi `logger.HttpResult(className, methodName, message)`; `case false` -> goi `logger.HttpErrorResult(className, methodName, message)`. Hai method nay **dinh nghia tai `LoggerExtensions.cs`**, khong phai trong file nay.

**Side effect** — Ghi log (qua `ILogger`) — muc do va noi dung log cu the phu thuoc cai dat cua `HttpResult`/`HttpErrorResult` trong `LoggerExtensions.cs`, khong xac dinh duoc them tu file nay.

**Error handling** — Khong co try/catch trong ham nay.

**Khi nao NEN dung** — Khi muon log ket qua mot thao tac HTTP voi 1 dong goi duy nhat, chon nhanh dua vao 1 co boolean, thay vi tu goi `HttpResult`/`HttpErrorResult` truc tiep.

**Khi nao KHONG dung** — Khi can truyen `Exception` kem theo log loi — `LogHttpResult` **khong co tham so `Exception`**, trong khi `HttpErrorResult` (dinh nghia o `LoggerExtensions.cs`) co overload nhan `Exception`. Dung `logger.HttpErrorResult(..., e: exception)` truc tiep trong truong hop nay.

**Gioi han** — **Khong co call site nao trong repo hien tai goi `LogHttpResult`** (xac nhan bang grep toan repo, chi co dinh nghia tai `HttpClientUtilizes.cs:134`) — xem muc 3.

---

### 2.6 TokenExpirationHelperUtilizes.IsExpiration

**Signature**
```csharp
public static bool IsExpiration(string jwt)
```
**Muc dich** — Kiem tra JWT da het han (hoac sap het han trong vong 3 phut) hay chua (dong 199-227). Co XML doc `/// <summary>` san co, noi dung khop voi code.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `jwt` | `string` | Co | Rong/`null`, hoac khong co du 2 phan ngan cach boi `.` (`DelimiterConstant.CHAR_DOT`) -> coi nhu "khong hop le" | — |

**Output** — `bool`: `true` neu JWT rong/khong hop le/khong doc duoc claim `exp`/con duoi 3 phut nua het han/co exception xay ra; `false` neu con han **it nhat 3 phut**.

**Dieu kien xu ly** (helper private `IsCheckValidToken` dong 170-191, goi truoc)
1. `IsCheckValidToken(jwt)`: neu `jwt` rong -> `(false, "")`. Neu `jwt.Split('.')` co it hon 2 phan -> `(false, "")`. Nguoc lai decode Base64 (khong padding) phan payload (`jwtSplit[1]`), deserialize thanh `Dictionary<string, object>`, tim key `"exp"`; khong co -> `(false, "")`, co -> `(true, exp.ToString())`.
2. Trong `IsExpiration`: neu `checkTokenResult is false` -> tra `true` (coi nhu **da het han** / khong hop le).
3. Cat phan sau dau `.` trong `exp` (phong truong hop `exp` la so thap phan/milliseconds) -> `exp2`.
4. `expTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(exp2.Replace(".", "")))` — **sua**: ham thuc te dung la `Convert.ToInt64`, khong phai `long.Parse` (2 ham cho ket qua tuong duong voi input hop le, nhung la API khac nhau; `exp2.Replace(".", "")` gan nhu vo tac dung vi `exp2` da la phan truoc dau `.` dau tien tu buoc 3, thuong khong con dau `.` nao de xoa).
5. `timeUtc = CommonBaseConstant.DateTimeUtc(0)` — **goi voi `addHour = 0`**, tra UTC thuc su (khac gia tri mac dinh `addHour = 7` cua ham nay).
6. `diff = expTime - timeUtc`.
7. Tra `diff.TotalMinutes < 3` (con duoi 3 phut hoac da am -> `true`, tuc "het han/sap het han").

**Side effect** — Khong co (ngoai log console khi loi, xem Error handling).

**Error handling** — Toan bo logic trong `try`; bat `Exception` chung, goi `CommonBaseConstant.ConfigLoggerExceptionByConsole(...)` (log ra Console, khong dung `ILogger`/DI), roi tra `false` — **luu y**: nhanh loi tra `false` (nghia la "con han"), **khac huong** voi nhanh "khong hop le" ben tren tra `true` ("het han") — xem muc 3.

**Khi nao NEN dung** — Kiem tra nhanh mot JWT (thuong la token cache) con dung duoc hay can refresh, truoc khi dung de goi API.

**Khi nao KHONG dung** — Khong dung de xac thuc tinh hop le/chu quyen cua token (khong kiem tra signature, issuer, audience) — chi doc claim `exp` tren payload chua giai ma chu ky.

**Gioi han** — Nhanh `catch` va nhanh "token khong hop le" tra ve **gia tri boolean nguoc nhau** cho cung mot y nghia loi ("khong xu ly duoc"): token rong/thieu `exp` -> `true` (het han); loi exception (vi du JSON deserialize that bai) -> `false` (con han). Hanh vi khong nhat quan, co the che giau loi thuc su.

---

### 2.7 TokenExpirationHelperUtilizes.GetExpirationTime

**Signature**
```csharp
public static double GetExpirationTime(string jwt)
```
**Muc dich** — Tinh so phut con lai truoc khi JWT het han, tru bien phong 2 phut, dung lam thoi gian cache token (dong 235-263). Co XML doc san, khop voi code.

**Input hop le** — Giong 2.6 (`jwt`, cung dung `IsCheckValidToken`).

**Output** — `double`:
- Neu token khong hop le (`checkTokenResult is false`) -> `CachedBaseConstant.RandomTimeCache(5)` (~4.5-5 phut, co jitter ngau nhien toi 10%).
- Neu hop le -> `Math.Round(diff.TotalMinutes - 2, 0)` (so phut con lai, da tru 2 phut bien phong, lam tron ve so nguyen gan nhat — **co the am** neu token da het han qua 2 phut).
- Neu exception -> `CachedBaseConstant.RandomTimeCache(5)` (giong nhanh khong hop le).

**Dieu kien xu ly**
1. `IsCheckValidToken(jwt)`.
2. Khong hop le -> tra `RandomTimeCache(5)`.
3. Hop le -> tinh `exp2`, `timeUtc = CommonBaseConstant.DateTimeUtc(0)`, `expTime`, `diff = expTime - timeUtc`.
4. Tra `Math.Round(diff.TotalMinutes - 2, 0)`.

**Side effect** — Log console khi loi (giong 2.6).

**Error handling** — `try/catch(Exception)`; catch thi log console va tra `RandomTimeCache(5)` — **cung gia tri fallback** voi nhanh "token khong hop le" (khac 2.6, o day khong bi nguoc huong vi ca 2 nhanh deu tra cung 1 loai gia tri fallback).

**Khi nao NEN dung** — Tinh thoi gian TTL (time-to-live) cho cache token, de luu token cung voi thoi gian song hop ly (co tru bien phong) — dung lam tham so cho `IMemoryCache`/`IDistributedCache`.

**Khi nao KHONG dung** — Khong dung ket qua nay de quyet dinh token con "hop le" ve mat bao mat — chi la ước tinh thoi gian cho muc dich cache.

**Gioi han** — Ket qua co the la **so am** (token da het han hon 2 phut) — caller dung gia tri nay lam thoi gian cache (vi du giay/phut TTL) can tu kiem tra `> 0` truoc khi dung, ham nay khong tu clamp ve 0.

---

### 2.8 HttpContentExtensionsUtilizes.SetHttpVersion

**Signature**
```csharp
internal static HttpVersionPolicy SetHttpVersion(HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower)
```
**Muc dich** — Ep `HttpVersionPolicy` thanh `RequestVersionOrLower` khi dang chay o moi truong Local, giu nguyen `versionPolicy` do caller truyen trong cac moi truong khac (dong 280-287).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `versionPolicy` | `HttpVersionPolicy` | Khong | Khong validate | `HttpVersionPolicy.RequestVersionOrLower` |

**Output** — `HttpVersionPolicy`: `RequestVersionOrLower` neu `EnvironmentExtensions.GetEnvironment() == EnvironmentExtensions.ELocal` ("Local"); nguoc lai tra chinh `versionPolicy` da nhan vao.

**Dieu kien xu ly** — `switch` expression: `EnvironmentExtensions.ELocal => HttpVersionPolicy.RequestVersionOrLower`, `_ => versionPolicy`.

**Side effect** — Khong co (doc bien moi truong `ASPNETCORE_ENVIRONMENT` qua `EnvironmentExtensions.GetEnvironment()`, khong mutate gi).

**Error handling** — Khong co try/catch; `GetEnvironment()` tra `string.Empty` neu bien moi truong khong ton tai (khong nem exception).

**Khi nao NEN dung** — Dung noi bo (internal) truoc khi gan `HttpRequestMessage.VersionPolicy`, de dam bao moi truong Local (thuong khong co HTTP/2 day du) khong bi loi negotiate protocol.

**Khi nao KHONG dung** — Khong danh cho code ben ngoai project (dau hieu `internal`).

**Gioi han** — Chi xu ly rieng truong hop `ELocal`; cac gia tri moi truong khac (`EDev`, `EStag`, `EProd`) deu roi vao nhanh `_` va giu nguyen `versionPolicy` truyen vao — khong co logic rieng cho tung moi truong con lai.

---

### 2.9 HttpContentExtensionsUtilizes.ReadAsJSonAsync<T>

**Signature**
```csharp
internal static async Task<T> ReadAsJSonAsync<T>(this HttpContent content, ILogger logger)
```
**Muc dich** — Doc `HttpContent` thanh chuoi, parse sang `T` bang `JSonTryParse` (dong 289-315).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `content` | `HttpContent` | Co | Khong null-check | — |
| `logger` | `ILogger` | Co | Dung de log khi loi | — |

**Output** — `Task<T>`: ket qua parse thanh cong, hoac `default` (`null` voi reference type) neu `json` rong hoac `JSonTryParse` tra `false`, hoac co exception.

**Dieu kien xu ly**
1. `json = await content.ReadAsStringAsync()`.
2. Neu `json` rong HOAC `!json.JSonTryParse(out T result)` -> tra `default`.
3. Nguoc lai tra `result`.

**Side effect** — Ghi log loi (`logger.HttpErrorResult`) khi vao nhanh `catch`.

**Error handling** — `try/catch(Exception)`: catch thi ghi log `logger.HttpErrorResult(e: exception, methodName: nameof(ReadAsJSonAsync), className: nameof(HttpContentExtensionsUtilizes), message: $"{nameof(ReadAsJSonAsync)}: {json}")`, roi tra `default` — **khong nem lai**.

**Khi nao NEN dung** — Khi da co san chuoi/`HttpContent` va muon parse JSON co log loi tu dong, chap nhan ket qua `default` khi that bai (khong throw).

**Khi nao KHONG dung** — Khong dung khi caller can phan biet "parse thanh cong ra gia tri default cua T" voi "parse that bai" — ca hai deu tra ve cung `default(T)`.

**Gioi han** — Khong throw khi parse loi — loi "am tham" tro thanh `default`, caller phai tu kiem tra thay vi dua vao exception. **Xac nhan**: grep toan repo `sr-core-helper` (`grep -rn "ReadAsJSonAsync"`) chi tra ve chinh dinh nghia va cac tham chieu noi bo (`nameof`) trong `HttpClientUtilizes.cs` — **khong co call site nao** goi `ReadAsJSonAsync` o bat ky file `.cs` nao khac trong repo (luong chinh dung `ReadAsStreamAsync`, xem 2.10). Day la ma "chet" trong pham vi repo nay, tuong tu `LogHttpResult`/`GetUri` (xem muc 3, van de #4) — trong pham vi thu vien dung chung nay co the con consumer ben ngoai repo khong xac dinh duoc.

---

### 2.10 HttpContentExtensionsUtilizes.ReadAsStreamAsync<T>

**Signature**
```csharp
internal static async Task<T> ReadAsStreamAsync<T>(this HttpContent content, ILogger logger)
```
**Muc dich** — Doc `HttpContent` qua `Stream` va deserialize truc tiep bang `JsonSerializer.DeserializeAsync` (hieu qua hon doc string roi parse) (dong 317-341). Day la duong doc chinh duoc `ResponseResult` (2.13) su dung.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `content` | `HttpContent` | Co | Khong null-check | — |
| `logger` | `ILogger` | Co | Dung de log khi loi | — |

**Output** — `Task<T>`: ket qua deserialize thanh cong, hoac `default` neu co exception (body khong dung schema JSON cua `T`, stream rong, v.v.).

**Dieu kien xu ly**
1. `await using Stream readAsStream = await content.ReadAsStreamAsync()`.
2. `result = await JsonSerializer.DeserializeAsync<T>(readAsStream, options: _jsonSerializerOptions)` voi `_jsonSerializerOptions` (`PropertyNameCaseInsensitive = true`, `ReferenceHandler.IgnoreCycles`, `NumberHandling.AllowReadingFromString`).
3. Tra `result`.

**Side effect** — Khi loi: doc lai toan bo noi dung bang `content.ReadAsStringAsync()` (doc content **lan 2** chi de phuc vu log) va ghi log loi.

**Error handling** — `try/catch(Exception)`: catch thi `readAsString = await content.ReadAsStringAsync()`, ghi `logger.HttpErrorResult(className: nameof(HttpContentExtensionsUtilizes), methodName: nameof(ReadAsStreamAsync), e: exception, message: $"ReadAsStreamAsync: {readAsString}")`, tra `default` — **khong nem lai**.

**Khi nao NEN dung** — Duong doc mac dinh cho body response JSON trong toan bo `CallApiWithHttp.cs` (qua `ResponseResult`).

**Khi nao KHONG dung** — Khong dung cho body khong phai JSON (XML, text thuan, binary) — se luon roi vao `catch` va tra `default`.

**Gioi han** — Khong nem exception khi deserialize that bai — HTTP 200 nhung body sai schema se cho ket qua `(Succeeded = true, data = default)`; caller **phai** tu kiem tra `data != null` thay vi chi dua vao `ErrorModel.Succeeded`. Viec `ReadAsStreamAsync()` bi goi **hai lan** khi loi (mot lan qua `DeserializeAsync`, mot lan qua `ReadAsStringAsync` de log) co the nem `InvalidOperationException` ("stream da duoc doc") tuy theo trien khai `HttpContent` cu the — **khong xac dinh duoc chac chan tu source code nay** hanh vi cua moi loai `HttpContent` co the gap trong runtime.

---

### 2.11 HttpContentExtensionsUtilizes.ConfigHttpClient

**Signature**
```csharp
internal static HttpClient ConfigHttpClient(this HttpOptionModel option)
```
**Muc dich** — Cau hinh mot `HttpClient` co san (tu `option.Client`) truoc khi goi request: gan `BaseAddress`, them header `Accept: application/json`, gan header `Authorization` neu co token (dong 343-360). Duoc goi boi hau het method trong `CallApiWithHttp.cs`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `option` | `HttpOptionModel` | Co | Khong null-check tren `option`; `option.BaseAddress` chi ap dung khi khong rong (`!string.IsNullOrEmpty`); `option.Token` chi gan `Authorization` khi khong rong | — |

**Output** — `HttpClient`: chinh instance `option.Client` da bi **mutate tai cho** (khong tao instance moi).

**Dieu kien xu ly**
1. `client = option.Client`.
2. Neu `option.BaseAddress` khac rong -> `client.BaseAddress = new Uri(option.BaseAddress)`.
3. Luon `client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"))` — **khong kiem tra da co header nay hay chua**.
4. Neu `option.Token` khac rong -> `client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(option.AuthType, option.Token)` (`option.AuthType` mac dinh `"Bearer"` theo `HttpOptionModel`).
5. Tra `client`.

**Side effect** — **Mutate truc tiep instance `HttpClient` dung chung** (`BaseAddress`, `DefaultRequestHeaders.Accept`, `DefaultRequestHeaders.Authorization`). Neu `HttpClient` la instance tai su dung nhieu lan (vi du tu `IHttpClientFactory` singleton), cac thay doi nay **tich luy** qua tung lan goi.

**Error handling** — Khong co try/catch trong ham nay; `new Uri(option.BaseAddress)` co the nem `UriFormatException` neu `BaseAddress` sai dinh dang — exception nay se nem thang ra caller (thuong nam trong `try` cua `CallApiWithHttp.cs` nen se bi bat va bien thanh `ErrorModel Code = 500`).

**Khi nao NEN dung** — Truoc moi lan goi request trong cac method chuan cua `CallApiWithHttp.cs` can `Accept: application/json` va `Authorization` tu `option.Token`.

**Khi nao KHONG dung** — Khong dung khi `HttpClient` da tung goi request thanh cong va can **doi `BaseAddress`** — `HttpClient` nem `InvalidOperationException` khi thay `BaseAddress` sau khi da gui request tren instance do. Khong dung khi endpoint yeu cau header `Accept` khac `application/json`.

**Gioi han**
- Goi `Accept.Add(...)` **moi lan invoke ham nay**, khong kiem tra trung lap -> voi `HttpClient` dung lai nhieu lan, header `Accept` tich luy khong gioi han (`application/json, application/json, ...`).
- Gan lai `BaseAddress` moi lan -> rui ro `InvalidOperationException` voi `HttpClient` da gui request.
- Khong co co che xoa `Authorization` cu -> token cua luong goi truoc co the anh huong luong sau neu code goi sai thu tu (dua vao gia thiet `ConfigHttpClient` duoc goi dung truoc moi request).

---

### 2.12 HttpContentExtensionsUtilizes.ErrorException (Exception)

**Signature**
```csharp
internal static void ErrorException(
    ref ErrorModel errorModel, HttpOptionModel option, Exception _)
```
**Muc dich** — Map mot `Exception` thong thuong (khong phai `CustomException`) sang `ErrorModel` voi thong bao co dinh, **khong** dua bat ky thong tin cua exception goc vao `Message` (dong 377-383).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `errorModel` | `ref ErrorModel` | Co | Truyen theo `ref`, bi mutate tai cho (3 property) | — |
| `option` | `HttpOptionModel` | Co | Khong null-check; `option.SystemOwner` duoc chen vao message | — |
| `_` (Exception) | `Exception` | Co | **Ten tham so la `_` (discard)** — noi dung khong duoc doc/su dung trong than ham | — |

**Output** — `void` (ket qua tra qua tham so `ref errorModel`).

**Dieu kien xu ly** — Luon thuc hien ca 3 buoc, khong co nhanh re:
1. `errorModel.Succeeded = false`.
2. `errorModel.Code = (int)HttpStatusCode.InternalServerError` (500).
3. `errorModel.Message = $"Hệ thống {option.SystemOwner} đang gặp sự cố tạm thời, vui lòng thử lại sau"` — **luon la cau tieng Viet nay, khong co truong hop khac** cho overload nay.

**Side effect** — Mutate `errorModel` (truyen vao boi tham chieu `ref` tu caller).

**Error handling** — Khong co try/catch (ham nay chinh la noi xu ly exception da bat o noi khac; no khong nem exception).

**Khi nao NEN dung** — Nhanh `catch (Exception exception)` chung, khi khong can phan loai chi tiet ma chi can thong bao chung cho nguoi dung cuoi.

**Khi nao KHONG dung** — Khi can giu lai chi tiet ky thuat cua exception de debug/log co cau truc — thong tin nay **bi mat hoan toan** khoi `ErrorModel` (van con trong tham so `exception` ma caller tu log rieng qua `logger.HttpErrorResult(e: exception, ...)`, nhung khong nam trong `ErrorModel.Message`).

**Gioi han** — `Code` luon co dinh `500` bat ke loai `Exception` thuc te la gi (vi du `TimeoutException`, `SocketException`, `JsonException` deu bi gop chung thanh 500 voi cung 1 cau thong bao).

---

### 2.13 HttpContentExtensionsUtilizes.ErrorException (CustomException)

**Signature**
```csharp
internal static void ErrorException(
    ref ErrorModel errorModel, HttpOptionModel option, CustomException exception)
```
**Muc dich** — Overload rieng cho `CustomException`, giu lai `Code` va `Message` cua exception goc thay vi luon co dinh (dong 385-391).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `errorModel` | `ref ErrorModel` | Co | Bi mutate tai cho | — |
| `option` | `HttpOptionModel` | Co | `option.SystemOwner` dung khi `exception.Message` la `null` | — |
| `exception` | `CustomException` | Co | Khong null-check tren `exception` — `NullReferenceException` neu truyen `null` | — |

**Output** — `void` (qua `ref errorModel`).

**Dieu kien xu ly**
1. `errorModel.Succeeded = false`.
2. `errorModel.Code = exception.Code` (lay nguyen `Code` cua `CustomException`, khong ep ve 500).
3. `errorModel.Message = exception.Message ?? $"Hệ thống {option.SystemOwner} đang gặp sự cố tạm thời, vui lòng thử lại sau"` — **CO 2 truong hop**: neu `exception.Message` khac `null` thi dung nguyen van (co the la bat ky chuoi nao caller da dat khi tao `CustomException`, khong nhat thiet la tieng Viet); chi khi `Message` la `null` moi fallback ve cau tieng Viet co dinh.

**Side effect** — Mutate `errorModel`.

**Error handling** — Khong co try/catch trong chinh ham nay.

**Khi nao NEN dung** — Nhanh `catch (CustomException exception)`, khi exception da duoc nem co chu dich voi `Code`/`Message` xac dinh tu truoc (vi du tu `ResponseResult` khi content-type khong hop le, hoac tu `EnsureSuccessOrException` khi response `null`).

**Khi nao KHONG dung** — Khong dung cho `Exception` thuong (dung overload 2.12).

**Gioi han** — `exception.Message` mac dinh khong bao gio `null` trong constructor cua `CustomException` (`Exception(message)` — `Message` cua `.NET Exception` tra ve chinh `message` truyen vao constructor, hoac chuoi mac dinh cua runtime neu `message` la `null`) — **sua**: `ErrorDeConstruct(out message, out statusCode)` la method dinh nghia tren `ErrorModel` (`FTELSRCore.Shared/Models/Https/ErrorModel.cs`), **khong phai** tren `CustomException`; rieng `CustomException` co property `Messages` (`IEnumerable<string>`, khoi tao `= [message]`, dinh nghia tai `FTELSRCore.Shared/Exceptions/CustomException.cs`) va property nay cung khong lien quan truc tiep den `Exception.Message`. **Khong xac dinh chac chan tu source code file nay** truong hop `exception.Message` thuc su la `null` co xay ra tren thuc te hay khong (phu thuoc runtime .NET, khong phai logic cua repo).

---

### 2.14 HttpContentExtensionsUtilizes.ErrorCanceledException

**Signature**
```csharp
internal static void ErrorCanceledException(
    ref ErrorModel errorModel, HttpOptionModel option, OperationCanceledException _)
```
**Muc dich** — Map `OperationCanceledException` (thuong do timeout tu `CancellationTokenSource`) sang `ErrorModel` voi status `408 Request Timeout` (dong 393-399).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `errorModel` | `ref ErrorModel` | Co | Bi mutate | — |
| `option` | `HttpOptionModel` | Co | `option.SystemOwner` chen vao message | — |
| `_` (OperationCanceledException) | `OperationCanceledException` | Co | Ten tham so `_`, khong doc noi dung | — |

**Output** — `void` (qua `ref errorModel`).

**Dieu kien xu ly** — Luon 3 buoc co dinh:
1. `errorModel.Succeeded = false`.
2. `errorModel.Code = (int)HttpStatusCode.RequestTimeout` (408).
3. `errorModel.Message = $"Hệ thống {option.SystemOwner} đang xử lý chậm hơn bình thường. Vui lòng thử lại sau ít phút"` — cau tieng Viet **khac** voi 2 ham `ErrorException` (noi dung rieng cho tinh huong timeout/huy).

**Side effect** — Mutate `errorModel`.

**Error handling** — Khong co try/catch.

**Khi nao NEN dung** — Nhanh `catch (OperationCanceledException)` khi request vuot thoi gian cho (`CancellationTokenSource` timeout) hoac bi huy chu dong.

**Khi nao KHONG dung** — Khong dung cho cac loai exception khac.

**Gioi han** — Khong phan biet duoc "timeout do cau hinh thoi gian cho" voi "bi huy chu dong boi caller" (`CancellationToken.Cancel()` boi ly do khac) — ca hai deu la `OperationCanceledException` va nhan cung message/Code 408.

---

### 2.15 HttpContentExtensionsUtilizes.EnsureSuccessOrException

**Signature**
```csharp
internal static void EnsureSuccessOrException(this HttpResponseMessage httpResponseMessage, ref ErrorModel errorModel)
```
**Muc dich** — Gan `Code`/`Message`/`Succeeded` cua `ErrorModel` tu `HttpResponseMessage`; ten ham goi y "dam bao thanh cong hoac nem exception" nhung hanh vi thuc te **khong nem exception cho bat ky HTTP status code nao** (dong 401-416).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `httpResponseMessage` | `HttpResponseMessage` | Co | **Co kiem tra `null`**: neu `null` -> `throw new CustomException(message: nameof(EnsureSuccessOrException))` | — |
| `errorModel` | `ref ErrorModel` | Co | Bi mutate 3 property | — |

**Output** — `void` (qua `ref errorModel`); hoac nem `CustomException` neu `httpResponseMessage` la `null`.

**Dieu kien xu ly**
1. Neu `httpResponseMessage is null` -> `throw new CustomException(message: nameof(EnsureSuccessOrException))` (message la chinh chuoi `"EnsureSuccessOrException"`, khong co status code tuong minh -> `CustomException.Code` nhan gia tri mac dinh cua constructor la `(int)HttpStatusCode.InternalServerError` = 500).
2. `errorModel.Code = (int)httpResponseMessage.StatusCode`.
3. `errorModel.Message = httpResponseMessage.ReasonPhrase` (co the la `null`, dac biet voi HTTP/2 khong co reason phrase).
4. `errorModel.Succeeded = httpResponseMessage.IsSuccessStatusCode` — day la **property co san cua .NET** (`HttpResponseMessage.IsSuccessStatusCode`), tra `true` khi `StatusCode` trong khoang **200-299**; **KHONG co logic tuy bien nao khac** trong ham nay dinh nghia them "thanh cong" la gi.
5. Doan code `if (httpResponseMessage is { StatusCode: >= HttpStatusCode.InternalServerError }) { httpResponseMessage.EnsureSuccessStatusCode(); }` **da bi comment (dong 412-415)** — tuc la du co bat comment lai, no cung **chi nham vao status >= 500**, khong bao gio nham vao 4xx. O trang thai hien tai (bi comment), ham **khong nem gi ca** cho bat ky status ma HTTP tra ve (2xx/3xx/4xx/5xx) — chi nem khi tham so dau vao la `null`.

**Side effect** — Mutate `errorModel` (tham chieu tu caller).

**Error handling** — Khong co try/catch; chi 1 `throw` tuong minh cho truong hop `null`.

**Khi nao NEN dung** — Buoc chuan hoa `ErrorModel` tu `HttpResponseMessage` **truoc khi** doc/deserialize body — luon phai goi truoc `ResponseResult` de `errorModel.Succeeded` phan anh dung status code HTTP.

**Khi nao KHONG dung** — Khong dua vao ham nay de **chan** (short-circuit) xu ly tiep khi gap 4xx/5xx — no khong nem, code sau van chay tiep sang buoc doc body.

**Gioi han**
- **Sai lech ro rang voi ten ham**: "EnsureSuccessOrException" ngu y se nem exception khi khong thanh cong, nhung thuc te chi nem khi response la `null`; moi HTTP status loi (400/404/500...) van duoc xu ly "binh thuong" (khong throw), chi khac o gia tri `errorModel.Succeeded = false`.
- `errorModel.Message = ReasonPhrase` co the la `null` (khong bao dam khac null) — caller khong nen gia dinh `Message` luon co gia tri o nhanh thanh cong.
- Day la diem duy nhat trong file quyet dinh "thanh cong" theo dung chuan HTTP (2xx), **khong co ngoai le** nao (khong co whitelist/blacklist status code rieng).

---

### 2.16 HttpContentExtensionsUtilizes.ResponseResult<TResponse>

**Signature**
```csharp
internal static async Task<TResponse> ResponseResult<TResponse>(this HttpResponseMessage httpResponseMessage, ILogger logger)
```
**Muc dich** — Quyet dinh body co the deserialize duoc hay khong dua vao `Content-Type`, roi deserialize hoac nem loi (dong 362-375).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `httpResponseMessage` | `HttpResponseMessage` | Co | Khong null-check tren chinh tham so nay (khac `EnsureSuccessOrException` co check `null`) — se nem `NullReferenceException` neu truyen `null` | — |
| `logger` | `ILogger` | Co | Truyen tiep cho `ReadAsStreamAsync` | — |

**Output** — `Task<TResponse>`: ket qua deserialize (co the la `default` neu `ReadAsStreamAsync` gap loi noi bo — xem 2.10); hoac nem `CustomException` neu dieu kien Content-Type khong dat.

**Dieu kien xu ly**
1. `mediaType = httpResponseMessage.Content.Headers.ContentType?.MediaType ?? string.Empty`.
2. **Neu `mediaType` rong/whitespace HOAC `mediaType.Contains("text/html")`**: doc body thanh string (`ReadAsStringAsync`) va `throw new CustomException(message: readAsString, statusCode: (int)httpResponseMessage.StatusCode)`.
3. **Nguoc lai** (bao gom `application/json`, nhung cung bao gom **bat ky media type khac** khong phai rong/html, vi du `text/plain`, `application/xml`, `application/octet-stream`...): goi `httpResponseMessage.Content.ReadAsStreamAsync<TResponse>(logger)` (xem 2.10) — **khong kiem tra** dung la JSON hay khong truoc khi deserialize.

**Side effect** — Khong ghi log truc tiep trong ham nay (log loi xay ra ben trong `ReadAsStreamAsync` neu deserialize that bai).

**Error handling** — Khong co try/catch cua chinh ham nay; nem `CustomException` tuong minh khi dieu kien (2) dung, de caller (`CallApiWithHttp.cs`) bat va map qua `ErrorException(CustomException)` (2.13).

**Khi nao NEN dung** — Buoc cuoi trong luong goi API sau khi da `EnsureSuccessOrException`, khi API dich tra JSON binh thuong.

**Khi nao KHONG dung** — Khong dung cho API tra ve dinh dang khong phai JSON ma khong khai bao `text/html` — nhung media type khac (XML, plain text) se **khong** bi chan boi dieu kien nay, roi vao `ReadAsStreamAsync<TResponse>` co gang parse JSON va that bai tham lang (tra `default`, khong throw) — de gay hieu nham "thanh cong nhung du lieu rong".

**Gioi han**
- **Khong kiem tra media type "phai la application/json"** — chi loai tru 2 truong hop (rong, `text/html`). Day la diem quan trong can luu y so voi cach dien giai thong thuong "kiem tra content-type JSON": logic thuc te la **danh sach loai tru** (blacklist), khong phai **danh sach cho phep** (whitelist).
- Response `204 No Content` (khong co `Content-Type`) se roi vao nhanh (2), bi coi la loi (`CustomException` voi body rong) — khop voi phat hien da co trong `Utilizes-CallApiWithHttp.md` va `Utilizes-CallApi.md` (xem muc 3, doi chieu).
- Khi proxy/WAF tra trang loi HTML, toan bo noi dung HTML se nam trong `ErrorModel.Message` (qua `CustomException.Message`) sau khi duoc `ErrorException` xu ly.

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `EnsureSuccessOrException` **khong nem exception cho bat ky HTTP status loi nao** (4xx/5xx) — chi nem khi `httpResponseMessage` la `null`; doan `EnsureSuccessStatusCode()` bi comment va du bat lai cung chi nham status `>= 500` | `HttpClientUtilizes.cs:401-416`, dac biet `:412-415` | Ten ham gay hieu nham. Trung khop voi phat hien da co trong 2 file KB cu (`Utilizes-CallApiWithHttp.md:99,804`; `Utilizes-CallApi.md:580`) — xac nhan lai tu chinh than ham, khong phat hien sai lech. |
| 2 | `ResponseResult` xac dinh "hop le de deserialize JSON" bang **danh sach loai tru** (`mediaType` rong hoac chua `text/html` -> loi), khong phai danh sach cho phep — moi media type khac `application/json` (vi du `text/plain`, `application/xml`) van duoc dua vao `ReadAsStreamAsync<TResponse>` nhu the la JSON | `HttpClientUtilizes.cs:362-375` | Response `204 No Content` hoac body rong khong co `Content-Type` bi coi la loi (`CustomException`). Nguoc lai, mot API tra `text/plain` hop le van duoc thu parse JSON va am tham that bai (`default`, khong throw) do `ReadAsStreamAsync` (2.10) khong nem. Day la nhan manh chi tiet hon so voi cach mo ta ngan gon "kiem tra Content-Type" trong 2 file KB cu — **khong mau thuan**, chi bo sung ro rang co che loai tru vs cho phep. |
| 3 | `ErrorException(Exception)` luon gan `Message` la cau tieng Viet co dinh `"Hệ thống {SystemOwner} đang gặp sự cố tạm thời, vui lòng thử lại sau"`, **khong co truong hop khac**; nhung `ErrorException(CustomException)` co 2 truong hop: dung nguyen `exception.Message` (co the la bat ky chuoi nao, khong nhat thiet tieng Viet) neu khac `null`, chi fallback ve cau tieng Viet khi `Message` la `null` | `HttpClientUtilizes.cs:377-391` | Cau tra loi cho yeu cau doi chieu: "Message co phai luon la cau tieng Viet co dinh hay khong" — **KHONG luon dung**, phu thuoc overload. Ca 2 file KB cu (`Utilizes-CallApiWithHttp.md:47`, `Utilizes-CallApi.md:91-92`) da mo ta dung diem nay (`Message = exception.Message ?? fallback` cho `CustomException`) — khong phat hien sai lech. |
| 4 | **Khong co call site nao trong repo** goi `HttpClientUtilizes.LogHttpResult`, `FormatOptions.GetUri`/`GetUri<TValue>`, va **`HttpContentExtensionsUtilizes.ReadAsJSonAsync`** — chi co dinh nghia, khong thay noi nao trong `.cs` khac (bao gom `CallApiWithHttp.cs`) goi den | `HttpClientUtilizes.cs:35-152` (dinh nghia `LogHttpResult`/`GetUri`), `:289-315` (dinh nghia `ReadAsJSonAsync`); xac nhan bang `grep -rn "LogHttpResult\|\.GetUri(\|ReadAsJSonAsync"` toan repo chi tra ve chinh file dinh nghia | Ca 4 API nay co the la ma "chet" (dead code) trong pham vi repo `sr-core-helper`, hoac danh cho consumer ben ngoai repo (thu vien dung chung) — tuong tu nhan xet da co trong `Utilizes-CallApiWithHttp.md:17` ve lop `CallApiWithHttp<>` generic. |
| 5 | Hang so `DelimiterConstant.CHAR_APOSTROPHE` co **gia tri thuc te la `/` (slash)**, khong phai apostrophe (`'`) nhu ten goi y | `FTELSRCore.Shared/Constants/DelimiterConstant.cs:8`; dung tai `HttpClientUtilizes.cs:65,95` de noi `SubDirectory` voi `requestUri` | Ten hang so gay hieu nham khi doc code (`GetUri`, dong 65/95 dung dung muc dich noi duong dan URL bang `/`, hanh vi **dung**, nhung ten hang so **sai ngu nghia**) — de dan den nham lan neu co ai dung lai hang so nay cho muc dich khac dua theo ten. |
| 6 | `TokenExpirationHelperUtilizes.IsExpiration`: nhanh "token khong hop le" (rong/thieu `exp`) tra `true` (het han), nhung nhanh `catch (Exception)` (loi deserialize JSON, parse so...) lai tra `false` (con han) — **2 tinh huong loi deu la "khong xu ly duoc" nhung tra ve gia tri nguoc nhau** | `HttpClientUtilizes.cs:205-208` (tra `true`) so voi `221-226` (tra `false`) | Co the che giau loi thuc su: neu payload JWT bi hong theo cach gay exception (thay vi thieu field `exp`), ham se bao "con han" (`false`) trong khi thuc chat khong doc duoc token — rui ro dung token khong xac dinh duoc trang thai. |
| 7 | `ToQueryString` va `ParseModelToQueryString` (trong `CallApiWithHttp.cs`, da ghi trong `Utilizes-CallApiWithHttp.md` muc 2.12 va van de #13) la **hai co che build query string khac nhau hoan toan** cho cung mot kieu du lieu dau vao — khac `BindingFlags`, khac thu tu escape/kiem tra rong, khac cach doc `JsonPropertyNameAttribute` | `HttpClientUtilizes.cs:111-132` so voi `CallApiWithHttp.cs:1429-1463` (theo `Utilizes-CallApiWithHttp.md:815`) | Xac nhan lai phat hien da co trong KB cu, doc truc tiep tu than ham `ToQueryString` trong file nay: **khong phat hien sai lech**, chi xac nhan cac chi tiet (`BindingFlags.Public \| Instance`, escape-truoc-kiem-rong-sau, khong doc `JsonPropertyNameAttribute`) dung voi `HttpClientUtilizes.cs:111-132`. |
| 8 | `GetUri` (2 overload) khong validate `option.Host` la `null`/rong truoc khi dua vao `UriBuilder`; `ConfigHttpClient`, `ErrorException(CustomException)` khong null-check tham so chinh (`option`, `exception`) | `HttpClientUtilizes.cs:50-109`, `343-360`, `385-391` | Neu goi voi tham so `null`/thieu cau hinh, se nem `NullReferenceException`/`UriFormatException` chu khong co thong bao loi ro rang — nhung vi 2 API nay (GetUri) khong co call site trong repo (van de #4), rui ro thuc te hien tai la thap. |
