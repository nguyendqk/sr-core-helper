# Cross-cutting Small Utilities (Exception, Timeout, Measure, Lazy)

> Nguon: FTELSRCore.Shared/Exceptions/CustomException.cs; FTELSRCore.Shared/Extensions/EnvironmentExtensions.cs; FTELSRCore.Shared/Extensions/LazyResolverExtensions.cs; FTELSRCore.Shared/Extensions/RunWithTimeoutExtentions.cs; FTELSRCore.Shared/Extensions/MeasureExecutionTimeExtensions.cs; FTELSRCore.Shared/Helpers/CancellationTokenHelper.cs; FTELSRCore.Shared/Helpers/CollectionHelpers.cs; FTELSRCore.Shared/Helpers/ObfuscationHelpers.cs; FTELSRCore.Shared/Utilizes/LazyInstanceUtility.cs
> Loai: class (CustomException) | static class (EnvironmentExtensions, RunWithTimeoutExtensions, MeasureExecutionTimeExtensions, CancellationTokenHelper, CollectionHelpers, ObfuscationHelpers) | class generic mo (LazyResolverExtensions.LazyResolver\<T\>, LazyInstanceUtility\<T\>)
> Cap nhat theo commit: 89c1ce9

## 1. Tong quan

Day la tap hop 9 file "linh tinh" (cross-cutting) nam rai trong `FTELSRCore.Shared`, khong thuoc mot tang nghiep vu cu the: mot exception noi bo dung chung toan repo (`CustomException`), hai wrapper do-thoi-gian-thuc-thi (`MeasureExecutionTimeExtensions`), mot helper tao token huy co timeout (`CancellationTokenHelper`), mot helper chay-voi-timeout kieu "fire-and-forget" (`RunWithTimeoutExtensions`), hai lop bao boc `Lazy<T>` de resolve dependency tre (`LazyResolverExtensions.LazyResolver<T>`, `LazyInstanceUtility<T>`), mot helper kiem tra collection rong (`CollectionHelpers`), mot helper doc/ma hoa chuoi kieu XOR+Base64 (`ObfuscationHelpers`), va mot extension doc bien moi truong (`EnvironmentExtensions`). Tat ca deu la `public` (hoac lop generic public). Da kiem tra truc tiep `FTELSRCore.Shared/GlobalUsing.cs` (11 dong) va toan bo repo (grep `global using`): 8/9 file nam trong cac namespace da duoc `global using` (`FTELSRCore.Exceptions`, `FTELSRCore.Extensions`, `FTELSRCore.Helpers`) nen goi truc tiep khong can `using` rieng. **Ngoai le**: `LazyInstanceUtility<T>` (namespace `FTELSRCore.Utilizes` — `LazyInstanceUtility.cs:3`) **KHONG** nam trong danh sach `global using` cua `GlobalUsing.cs`, va khong co global using nao khac cho `FTELSRCore.Utilizes` trong repo (repo chi co 1 file `.csproj`/`GlobalUsing.cs`; `ImplicitUsings=enable` trong `.csproj` chi bat cac namespace chuan cua SDK, khong anh huong namespace tuy bien nay). Vi vay code ben ngoai namespace `FTELSRCore.Utilizes` muon dung `LazyInstanceUtility<T>` truc tiep (khong fully-qualified) van can khai bao rieng `using FTELSRCore.Utilizes;` — xem chi tiet muc 2.12.

Diem quan trong nhat can luu y: `CustomException` va `MeasureExecutionTimeExtensions.InvokeForHTTP`/`InvokeForMediaR` la hai diem duoc goi/bat (`catch`) day dac trong toan bo tang goi HTTP (`CallApiWithHttp<,>`, `CallApi<TResponse>`) va tang CQRS (`LoggingBehavior<,>`); `CancellationTokenHelper.CreateLinkedTokenWithTimeout` cung duoc goi trong toan bo `CallApiWithHttp.cs`.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| `CustomException`: mang `Code` (HTTP status, default `500`) va `Messages` (danh sach 1 message) kem theo `Exception.Message` chuan | Khong cung cap constructor nhan danh sach nhieu message; `Messages` luon duoc khoi tao voi dung 1 phan tu `[message]` |
| `EnvironmentExtensions.GetEnvironment()`: doc bien moi truong `ASPNETCORE_ENVIRONMENT`, tra `string.Empty` neu khong co | Khong cache gia tri, moi lan goi la mot lan doc `Environment.GetEnvironmentVariable` moi |
| `EnvironmentExtensions.GetPrefixEnvironment()`: map moi truong sang prefix chuoi (`stag-`, rong, hoac `dev-`) | Khong co nhanh rieng cho `Local`/`Development`; ca hai gia tri nay (va bat ky gia tri la khac `Staging`/`Production`) deu roi vao nhanh `default` -> `dev-` |
| `RunWithTimeoutExtensions.RunWithTimeoutAsync<T>`: cho mot `Task<T>` chay, nhuong lai control sau `timeoutMs` neu qua han, nhung KHONG huy task goc — task van tiep tuc chay ngam | Khong bao gio bao cho caller biet la task da timeout hay da hoan tat qua tuple tra ve (xem muc 3, van de #1) |
| `MeasureExecutionTimeExtensions.InvokeForHTTP` / `InvokeForMediaR`: do thoi gian thuc thi mot `Func<ValueTask<TOut>>`, ghi log `Warning` neu vuot `desiredTime` (giay) | Khong huy request khi vuot `desiredTime`; day khong phai la timeout — chi la canh bao log |
| `CancellationTokenHelper.CreateLinkedTokenWithTimeout`: tao `CancellationTokenSource` link voi token ngoai, tuy chon `CancelAfter` | Khong tu dong `Dispose()` — caller phai `using`/goi `Dispose()` |
| `CollectionHelpers.IsNullOrEmpty<T>`: kiem tra null-or-empty toi uu theo loai collection (`ICollection`, `IReadOnlyCollection`, `List`, fallback `.Any()`) | Khong phan biet duoc "null" voi "rong" (ca hai deu tra `true`) |
| `ObfuscationHelpers.DecodeDataFromSR<T>` / `EncodeDataFromSR<T>`: ma hoa/giai ma chuoi bang XOR key + Base64, kem serialize/deserialize JSON | Khong phai ma hoa an toan (XOR don gian, key dang plaintext) — chi la "obfuscation" nhu ten ham, khong phai encryption thuc su |
| `LazyResolverExtensions.LazyResolver<T>` / `LazyInstanceUtility<T>`: bao boc `Lazy<T>` de resolve `T` tu `IServiceProvider` tai lan truy cap `.Value` dau tien | Khong tu dong dispose `T` neu `T` la `IDisposable`; khong co logic retry neu `GetRequiredService<T>` nem loi |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `Microsoft.Extensions.Logging.ILogger` (global using tu `GlobalUsing.cs:2`) | Tham so `logger` trong `InvokeForHTTP`, `InvokeForMediaR`, `DecodeDataFromSR`, `EncodeDataFromSR`. Day la `ILogger` chuan cua Microsoft, KHONG phai `Serilog.ILogger` (khong co `using Serilog` global nao trong file) |
| `FTELSRCore.Extensions.Loggers.LoggerExtensions` (extension methods `Warning`, `MediaRResult`, `ErrorException`) | Ghi log co cau truc (`className`, `methodName`, `message`) tu `MeasureExecutionTimeExtensions` va `ObfuscationHelpers` |
| `Microsoft.Extensions.DependencyInjection` (`IServiceProvider.GetRequiredService<T>`) | Duoc dung boi `LazyResolverExtensions.LazyResolver<T>` va `LazyInstanceUtility<T>` |
| `FTELSRCore.Helpers.JSonParseHelpers` (`ToJSon`, `JSonTryParse`) | `ObfuscationHelpers` dung de serialize/deserialize JSON truoc/sau khi XOR |
| `FTELSRCore.Constants.CommonBaseConstant.ConfigLoggerExceptionByConsole` | Duong log fallback khi `ObfuscationHelpers` duoc goi voi `logger = null` |
| `Newtonsoft.Json.JsonConvert.SerializeObject` | Dung trong message loi cua `EncodeDataFromSR` de log lai du lieu goc khi that bai |
| `System.Diagnostics.Stopwatch` | `MeasureExecutionTimeExtensions` dung `Stopwatch.GetTimestamp()`/`Stopwatch.Frequency` de do thoi gian (khong dung `Stopwatch` instance) |
| `System.Net.HttpStatusCode` | `CustomException` dung `(int)HttpStatusCode.InternalServerError` (500) lam gia tri mac dinh cho `statusCode` |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `CustomException(string, int)` / `CustomException(int, Exception)` | Exception | Exception noi bo mang `Code` va `Messages` |
| `EnvironmentExtensions.GetEnvironment()` | Environment | Doc `ASPNETCORE_ENVIRONMENT` |
| `EnvironmentExtensions.GetPrefixEnvironment()` | Environment | Map moi truong -> prefix chuoi |
| `RunWithTimeoutExtensions.RunWithTimeoutAsync<T>` | Timeout | Cho task chay toi da `timeoutMs`, khong huy task goc |
| `MeasureExecutionTimeExtensions.InvokeForMediaR<TOut>` | Measure | Do thoi gian + log Warning + log `MediaRResult` (dung cho pipeline MediatR) |
| `MeasureExecutionTimeExtensions.InvokeForHTTP<TOut>` | Measure | Do thoi gian + log Warning (dung cho goi HTTP) |
| `CancellationTokenHelper.CreateLinkedTokenWithTimeout` | Cancellation | Tao `CancellationTokenSource` lien ket + timeout tuy chon |
| `CollectionHelpers.IsNullOrEmpty<T>` | Collection | Kiem tra null-or-empty toi uu theo kieu cu the |
| `ObfuscationHelpers.DecodeDataFromSR<T>` | Obfuscation | Base64 -> XOR -> JSON -> `T` |
| `ObfuscationHelpers.EncodeDataFromSR<T>` | Obfuscation | `T` -> JSON -> XOR -> Base64 |
| `LazyResolverExtensions.LazyResolver<T>` | Lazy DI | `Lazy<T>` resolve qua `IServiceProvider` |
| `LazyInstanceUtility<T>` | Lazy DI | Ban sao chuc nang cua `LazyResolver<T>`, khac namespace |

## 2. Chi tiet API

### 2.1 CustomException

**Signature**
```csharp
public class CustomException(string message, int statusCode = (int)HttpStatusCode.InternalServerError) : Exception(message)
{
    public int Code { get; set; } = statusCode;

    public IEnumerable<string> Messages { get; set; } = [message];

    public CustomException(int statusCode, Exception inner) : this(inner?.Message?.ToString(), statusCode)
    {
    }
}
```
`FTELSRCore.Shared/Exceptions/CustomException.cs:5-14`.

**Muc dich** — Exception dung chung cho toan repo de mang theo ma loi HTTP (`Code`) cung voi noi dung loi, thay cho `Exception` chuan khong co `Code`. Duoc dung o hau het tang goi HTTP/data de bao loi nghiep vu co kem status code.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `message` | `string` | Co (constructor chinh) | Khong validate null/rong — truyen thang vao `Exception(message)` va `Messages = [message]` | Khong co |
| `statusCode` | `int` | Khong | Khong validate (khong kiem tra co phai HTTP status code hop le) | `(int)HttpStatusCode.InternalServerError` = `500` |
| `inner` (constructor phu) | `Exception` | Co | Khong validate null; neu `inner` la `null` thi `inner?.Message?.ToString()` tra `null`, `message` cua exception moi se la `null` | Khong co |

**Output** — Day la constructor, khong co gia tri tra ve; sau khi tao, doc thuoc tinh:
- `Code`: `int`, bang `statusCode` da truyen (hoac 500 neu dung mac dinh).
- `Messages`: `IEnumerable<string>` chi chua **dung 1 phan tu** — chinh `message` da truyen vao. Khong co logic nao them nhieu message.
- `Message` (ke thua tu `Exception`): bang `message` da truyen (hoac message cua `inner` neu dung constructor phu).

**Dieu kien xu ly** — Khong co nhanh re, guard clause hay validate nao trong constructor. Constructor phu chi ganh chuyen tiep (`this(...)`) sang constructor chinh voi `inner?.Message?.ToString()` lam `message`.

**Side effect** — Khong co. Day la mot kieu du lieu thuan (POCO ke thua `Exception`), khong ghi log, khong goi ngoai, khong mutate state ben ngoai.

**Error handling** — Khong ap dung (day chinh la mot lop Exception, khong bat exception nao ben trong no).

**Khi nao NEN dung** — Khi can nem/tra ve mot loi noi bo co kem ma trang thai HTTP ro rang (vi du chuyen tiep status code tu response ben ngoai), va khi tang goi (`CallApi`, `CallApiWithHttp`) can bat rieng loai loi nay de xu ly khac voi `Exception` chung (xem `Utilizes-CallApi.md`, `Utilizes-CallApiWithHttp.md`).

**Khi nao KHONG dung** — Khi can mot `InnerException` chuan de giu nguyen stack trace goc: constructor phu **khong** goi `base(message, innerException)` — no chi lay `inner.Message` lam chuoi, hoan toan **khong gan `inner` lam `InnerException`** cua exception moi (xem muc 3, van de #1). Neu can giu nguyen stack trace goc, `CustomException` khong dap ung duoc.

**Gioi han**
- Khong co validate `message`/`statusCode`; co the tao `CustomException(null)` hoac voi `statusCode` am/khong hop le ma khong bi chan.
- `Messages` luon chi co 1 phan tu bang chinh `message` — thuoc tinh nay co ve du thua vi trung lap voi `Exception.Message`, nhung mot so noi trong repo (theo cac KB da co, vi du `HttpClientUtilizes.ErrorException`) co the doc `Message` (khong phai `Messages`) khi map sang `ErrorModel`.
- `Code`/`Messages` co `set` cong khai — co the bi mutate sau khi tao, khong immutable.
- Constructor phu `CustomException(int statusCode, Exception inner)` **danh mat thong tin goc** cua `inner` (stack trace, type, `InnerException` cua no) — chi giu lai `Message` dang chuoi.

### 2.2 EnvironmentExtensions.GetEnvironment

**Signature**
```csharp
public static string GetEnvironment() =>
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;
```
`FTELSRCore.Shared/Extensions/EnvironmentExtensions.cs:18-19`.

**Muc dich** — Doc gia tri bien moi truong `ASPNETCORE_ENVIRONMENT` cua tien trinh dang chay, dung lam co so de xac dinh moi truong (Local/Dev/Staging/Production).

**Input hop le** — Khong co tham so.

**Output** — `string`: gia tri thuc te cua bien moi truong `ASPNETCORE_ENVIRONMENT` neu co set; neu bien khong ton tai (`null`), tra ve `string.Empty` (khong bao gio tra `null`).

**Dieu kien xu ly** — Chi mot bieu thuc `??`: neu `Environment.GetEnvironmentVariable(...)` tra `null` thi dung `string.Empty`.

**Side effect** — Khong co (chi doc, khong ghi).

**Error handling** — Khong co try/catch; `Environment.GetEnvironmentVariable` theo tai lieu .NET khong nem exception cho truong hop bien khong ton tai (tra `null`), nen khong can bat loi.

**Khi nao NEN dung** — Khi can biet ten moi truong hien tai de re nhanh logic (vi du: bat/tat Swagger, chon connection string...).

**Khi nao KHONG dung** — Khi can gia tri moi truong duoc cache/khong doi trong doi song ung dung — ham nay doc lai bien moi truong o **moi lan goi**, khong cache.

**Gioi han** — Phu thuoc hoan toan vao bien moi truong `ASPNETCORE_ENVIRONMENT` duoc thiet lap dung tu ben ngoai (docker/appsettings/launchSettings); ham khong co fallback nao khac (vi du doc tu `IWebHostEnvironment`).

### 2.3 EnvironmentExtensions.GetPrefixEnvironment

**Signature**
```csharp
public static string GetPrefixEnvironment()
{
    return GetEnvironment() switch
    {
        EStag => "stag-",
        EProd => string.Empty,
        _ => "dev-"
    };
}
```
`FTELSRCore.Shared/Extensions/EnvironmentExtensions.cs:26-34`.

**Muc dich** — Tra ve mot prefix chuoi tuong ung voi moi truong hien tai, thuong dung de dat ten resource theo moi truong (topic Kafka, cache key, ten queue...).

**Input hop le** — Khong co tham so; phu thuoc gian tiep vao `GetEnvironment()`.

**Output** — `string`:
- `"stag-"` neu `GetEnvironment()` tra dung chuoi `EStag` = `"Staging"`.
- `string.Empty` neu tra dung `EProd` = `"Production"`.
- `"dev-"` cho **tat ca truong hop con lai**, bao gom `EDev` (`"Development"`), `ELocal` (`"Local"`), chuoi rong, hoac bat ky gia tri khac.

**Dieu kien xu ly** — `switch` pattern-matching tren gia tri chuoi tra ve tu `GetEnvironment()`, chi co 2 nhanh cu the (`EStag`, `EProd`) va 1 nhanh `_` (default).

**Side effect** — Khong co.

**Error handling** — Khong co; khong the throw vi `switch` co nhanh `_` bao phu moi truong hop.

**Khi nao NEN dung** — Khi can prefix on dinh cho moi truong Staging/Production theo dung quy uoc cua he thong; moi truong khac (Local/Dev/khong xac dinh) deu dung chung prefix `dev-`.

**Khi nao KHONG dung** — Khi can phan biet ro Local voi Development bang prefix rieng — ham nay gop ca hai vao cung `"dev-"`.

**Gioi han** — Cac hang so `ELocal`, `EDev` duoc dinh nghia (`EnvironmentExtensions.cs:5,7`) nhung **khong co nhanh `switch` rieng** nao dung den chung trong `GetPrefixEnvironment` — ca hai deu roi vao `default`. Xem muc 3, van de #2.

**Bang hang so lien quan**

| Ten hang so | Gia tri thuc | Y nghia | Dong |
|---|---|---|---|
| `ELocal` | `"Local"` | Gia tri ky vong cua `ASPNETCORE_ENVIRONMENT` khi chay local | `EnvironmentExtensions.cs:5` |
| `EDev` | `"Development"` | Gia tri ky vong khi chay moi truong Development | `EnvironmentExtensions.cs:7` |
| `EStag` | `"Staging"` | Gia tri ky vong khi chay moi truong Staging | `EnvironmentExtensions.cs:9` |
| `EProd` | `"Production"` | Gia tri ky vong khi chay moi truong Production | `EnvironmentExtensions.cs:11` |

### 2.4 RunWithTimeoutExtensions.RunWithTimeoutAsync\<T\>

**Signature**
```csharp
public static async Task<(T Data, bool Result)> RunWithTimeoutAsync<T>(Func<Task<T>> action, int timeoutMs, CancellationToken cancellationToken = default)
```
`FTELSRCore.Shared/Extensions/RunWithTimeoutExtentions.cs:14`. (Ten file co loi chinh ta — xem muc 3, van de #4.)

**Muc dich** — Theo dung comment XML doc trong code (`RunWithTimeoutExtentions.cs:6`): "Ham cho phep thuc thi voi thoi gian cho mong muon neu qua thi timeout nhung van chay tien trinh hoan tat" — nghia la khong huy `action`, chi ngung cho doi no sau `timeoutMs`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `action` | `Func<Task<T>>` | Co | Khong validate null; goi ngay `action()` truoc khi vao `switch` — neu `action` la `null` se nem `NullReferenceException` ngay tai dong 16 | Khong co |
| `timeoutMs` | `int` | Co | Khong validate am; chi kiem tra `timeoutMs is 0` de re nhanh | Khong co |
| `cancellationToken` | `CancellationToken` | Khong | Chi duoc truyen vao `Task.Delay`, khong duoc kiem tra (`ThrowIfCancellationRequested`) o dau ham | `default` |

**Output** — `Task<(T Data, bool Result)>`:
- Neu `timeoutMs == 0`: `(Data: await workTask, Result: true)` — cho `action` chay xong hoan toan, khong co gioi han thoi gian.
- Neu `timeoutMs != 0` va `Task.Delay` hoan tat truoc `workTask` (tuc la het thoi gian cho): `(Data: default, Result: true)` — `Data` la `default(T)` (null cho reference type).
- Neu `timeoutMs != 0` va `workTask` hoan tat truoc: `(Data: await workTask, Result: true)`.

**QUAN TRONG**: trong **ca 3 nhanh**, `Result` **luon la `true`** — khong co bat ky duong dan nao gan `Result = false`. Xem muc 3, van de #1 (day la mau thuan giua "ten field goi y co bao loi" va hanh vi thuc te trong code).

**Dieu kien xu ly**
1. Goi `action()` ngay lap tuc de lay `workTask` (dong 16) — **truoc** ca khi kiem tra `timeoutMs`.
2. `switch (timeoutMs is 0)`:
   - `case true`: `await workTask` truc tiep, khong co `Task.Delay` nao duoc tao.
   - `case false`: tao `Task.Delay(timeoutMs, cancellationToken)`, dung `Task.WhenAny(workTask, timeoutTask)`; neu ket qua la `timeoutTask` (het gio truoc) tra `(default, true)` **ngay**, con `workTask` **van tiep tuc chay ngam ben duoi** (khong bi huy, khong duoc `await` tiep, ket qua bi bo qua hoan toan).

**Side effect** — Neu timeout xay ra, `workTask` (goi tu `action`) **van tiep tuc thuc thi ngam trong background** sau khi ham da return — day la side effect quan trong nhat can luu y: tien trinh ben trong `action` khong bi huy va co the hoan tat/gay side effect (ghi DB, goi API...) sau khi caller da nhan ket qua timeout.

**Error handling** — Khong co try/catch trong ham. Neu `action()` nem exception dong bo ngay khi goi (dong 16), exception nay se nem thang ra caller, khong duoc bat. Neu `workTask` fault sau khi da bi "bo roi" (do timeout xay ra truoc), exception cua `workTask` se khong duoc quan sat boi ham nay (unobserved task exception).

**Khi nao NEN dung** — Khi muon "cho toi da X ms roi tiep tuc, khong quan tam task nen xong hay chua", va viec task chay tiep ngam KHONG gay hai (vi du: warm-up cache, prefetch khong quan trong).

**Khi nao KHONG dung** — Khi can biet chinh xac task co hoan tat dung han hay bi timeout (field `Result` khong dung duoc cho muc dich nay); khi can huy thuc su task ben trong khi timeout (ham nay khong huy `action`, chi ngung `await`); khi `action` co side effect nguy hiem neu chay "mo cong" ngoai kiem soat (vi du transaction, ghi tien).

**Gioi han**
- `Result` luon `true` — ten field gay hieu nham nghiem trong, khong the dung de phan biet thanh cong/timeout.
- Khong co co che huy `workTask` khi timeout — khac han "timeout" theo nghia thong thuong (nem `TimeoutException`/huy request).
- Khong xu ly truong hop `action` nem exception dong bo ngay khi goi ham `action()`.
- File co ten sai chinh ta ("Extentions") nhung class ben trong dat dung ("Extensions") — xem muc 3, van de #4.

### 2.5 MeasureExecutionTimeExtensions.InvokeForMediaR\<TOut\>

**Signature**
```csharp
public static ValueTask<TOut> InvokeForMediaR<TOut>(
    Func<ValueTask<TOut>> func, ILogger logger, string measureByKey, int desiredTime = 5, CancellationToken cancellationToken = default) where TOut : notnull
```
`FTELSRCore.Shared/Extensions/MeasureExecutionTimeExtensions.cs:18-19`.

**Muc dich** — Theo dung XML doc (`MeasureExecutionTimeExtensions.cs:8`): "Thuc thi ham bat dong bo, do thoi gian va ghi log neu vuot nguong thoi gian mong muon." Dung cho pipeline MediatR (`LoggingBehavior<,>` — `CQRS/Behaviors/LoggingBehavior.cs:22-29`) de do latency xu ly request/handler va ghi log ket qua qua `logger.MediaRResult`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `func` | `Func<ValueTask<TOut>>` | Co | Khong validate null | Khong co |
| `logger` | `ILogger` (Microsoft.Extensions.Logging) | Co | Khong validate null — neu `null`, nem `NullReferenceException` khi vao nhanh `Warning`/`MediaRResult` | Khong co |
| `measureByKey` | `string` | Co | Khong validate | Khong co |
| `desiredTime` | `int` | Khong | Khong validate; don vi la **giay**, so sanh `elapseds > desiredTime` | `5` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` duoc goi **2 lan**: truoc khi tra ve `ValueTask` (dong 21) va lai mot lan nua o dau local function `ExecuteAsync` (dong 27) | `default` |

**Output** — `ValueTask<TOut>`: ket qua tra ve tu `func()` sau khi da do thoi gian va ghi log. Khong bao boc/doi kieu ket qua.

**Dieu kien xu ly**
1. `cancellationToken.ThrowIfCancellationRequested()` truoc khi goi `ExecuteAsync` (dong 21).
2. Ben trong `ExecuteAsync` (local function async): kiem tra lai `ThrowIfCancellationRequested()` (dong 27) — **lap** voi buoc 1 do tham so `cancellationToken` cua local function co gia tri mac dinh rieng (`= default`) nhung thuc te duoc goi voi `cancellationToken: cancellationToken` tu ngoai truyen vao (dong 23), nen hai lan kiem tra nay dung cung mot token.
3. Do thoi gian bang `Stopwatch.GetTimestamp()` truoc/sau `await func()`.
4. Tinh `elapsedMs` (long, mili-giay) va `elapseds` (double, giay = `elapsedMs / 1000.0`).
5. Neu `elapseds > desiredTime`: goi `logger.Warning(...)` voi message dang `"[PERFORMANCE] Long Running Request [{measureByKey}] took {elapseds} seconds."`.
6. Luon goi `logger.MediaRResult(...)` (khong dieu kien) voi `latency = elapsedMs`, `message = measureByKey` — **khac voi `InvokeForHTTP` KHONG co buoc nay**.
7. Tra ve `result`.

**Side effect** — Ghi log: 0 hoac 1 dong `Warning` (tuy latency), va **luon** 1 dong qua `logger.MediaRResult` (du co vuot nguong hay khong).

**Error handling** — Khong co try/catch trong ham nay. Neu `func()` nem exception, exception se lan thang ra caller (khong duoc bat/log boi ham nay); cac dong log Warning/MediaRResult **se khong duoc thuc thi** vi nam sau `await func()`.

**Khi nao NEN dung** — Khi can do va log latency cua mot buoc xu ly MediatR handler/pipeline, kem canh bao khi cham.

**Khi nao KHONG dung** — Khi can gioi han thoi gian thuc su (timeout) cho `func` — ham nay khong huy `func`, chi canh bao qua log.

**Gioi han**
- `desiredTime` khong phai nguong timeout, chi la nguong ghi log `Warning`.
- Khong bat exception tu `func()` — neu can log loi thi caller phai tu lam.
- `logger.MediaRResult` luon duoc goi ke ca khi thoi gian rat ngan — co the tao nhieu log neu goi voi tan suat cao.

### 2.6 MeasureExecutionTimeExtensions.InvokeForHTTP\<TOut\>

**Signature**
```csharp
public static ValueTask<TOut> InvokeForHTTP<TOut>(
    Func<ValueTask<TOut>> func, ILogger logger, string measureByKey, int desiredTime = 5, CancellationToken cancellationToken = default) where TOut : notnull
```
`FTELSRCore.Shared/Extensions/MeasureExecutionTimeExtensions.cs:67-68`. XML doc phia tren hoan toan rong (chi co tag trong, khong co `<summary>` — dong 56-66), nen phan mo ta duoi day duoc suy ra tu than ham thuc te, khong phai tu comment.

**Muc dich** — Do thoi gian thuc thi mot `Func<ValueTask<TOut>>` (thuong la `client.SendAsync(...)` boc trong lambda — xem `CallApiWithHttp.cs` moi method GET/POST/PUT/DELETE) va **chi ghi log `Warning`** khi thoi gian vuot `desiredTime` giay. **Day la diem doi chieu voi cac KB cu (`Utilizes-CallApi.md`, `Utilizes-CallApiWithHttp.md`) — ca hai KB cu deu mo ta dung: `desiredTime` khong phai nguong timeout, chi sinh log canh bao (vi du `Utilizes-CallApiWithHttp.md:820`, `Utilizes-CallApi.md:26,42,103`). Kiem tra lai tren source that (dong 84-90 duoi day) xac nhan cac KB cu mo ta CHINH XAC — khong co mau thuan can sua.**

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `func` | `Func<ValueTask<TOut>>` | Co | Khong validate null | Khong co |
| `logger` | `ILogger` | Co | Khong validate null | Khong co |
| `measureByKey` | `string` | Co | Khong validate; dung lam nhan dinh danh trong message log | Khong co |
| `desiredTime` | `int` | Khong | Khong validate; don vi giay | `5` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` goi 2 lan (dong 70 va 76), tuong tu `InvokeForMediaR` | `default` |

**Output** — `ValueTask<TOut>`: ket qua nguyen ban tu `func()`.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 70) truoc khi goi `ExecuteAsync`.
2. Trong `ExecuteAsync`: `ThrowIfCancellationRequested()` lai (dong 76).
3. `start = Stopwatch.GetTimestamp()` (dong 78).
4. `result = await func()` (dong 80) — **day la diem quan trong**: neu `func` la `client.SendAsync(...)` voi mot `cancellationTokenSource.Token` da duoc set `CancelAfter` boi `CancellationTokenHelper.CreateLinkedTokenWithTimeout` (xem 2.7 va `CallApiWithHttp.cs`), thi **viec huy request thuc su** (nem `OperationCanceledException`/`TaskCanceledException`) la do token do gay ra, **khong lien quan gi den `desiredTime` cua ham nay**.
5. Tinh `elapsed` (double, giay) = `(Stopwatch.GetTimestamp() - start) / (double)Stopwatch.Frequency` — luu y: khac cach tinh cua `InvokeForMediaR` (tinh qua `elapsedMs` roi chia 1000.0); ca hai deu ra ket qua tuong duong ve mat gia tri nhung duong tinh khac nhau va **`InvokeForHTTP` khong co bien `elapsedMs` (long, milli-giay) rieng**.
6. Neu `elapsed > desiredTime`: goi `logger.Warning(...)` voi message `"[PERFORMANCE] Long Running Request [{measureByKey}] took {elapsed} seconds."`.
7. **Khong co buoc log ket qua nao khac** (khac `InvokeForMediaR` co them `logger.MediaRResult` khong dieu kien).
8. Tra `result`.

**Side effect** — Ghi 0 hoac 1 dong log `Warning`. **Khong huy `func()` bat ky luc nao** — `desiredTime` bi vuot chi duoc phat hien **sau khi** `func()` da hoan tat (`await func()` da xong o dong 80 truoc khi tinh `elapsed`), nen viec ghi log Warning luon xay ra **sau khi request thuc su ket thuc**, khong phai canh bao "dang cham" theo thoi gian thuc.

**Error handling** — Khong co try/catch. Moi exception tu `func()` (bao gom `OperationCanceledException` do timeout tu `CancellationTokenHelper`) deu **nem thang qua ham nay ra caller**; cac catch-block xu ly thuc te nam o tang goi (vi du `CallApiWithHttp<,>.GetAsJSonAsync` — xem `Utilizes-CallApiWithHttp.md`).

**Khi nao NEN dung** — Khi can do va canh bao (qua log) cac request HTTP cham, ma khong can/khong the huy request giua duong chi vi cham.

**Khi nao KHONG dung** — Khi ky vong `desiredTime` se **thuc su huy** request qua thoi gian — dieu nay **khong dung**. Timeout thuc su (huy request) chi den tu tham so `cancellationTokenTime` duoc dua vao `CancellationTokenHelper.CreateLinkedTokenWithTimeout` o tang goi ben ngoai, hoan toan doc lap voi `desiredTime` cua ham nay.

**Gioi han**
- `desiredTime` chi anh huong log, khong anh huong hanh vi thuc thi — de nham la "timeout param" khi doc signature.
- Khong log ket qua thanh cong (khong co dong tuong duong `MediaRResult`), chi log khi cham — kho theo doi latency binh thuong qua ham nay (phai dua vao log tracing rieng cua tang goi, vi du `logger.HttpResultWithTracing` trong `CallApiWithHttp.cs`).
- Cach tinh `elapsed` (chia `Stopwatch.Frequency` truc tiep, ra `double` giay) khac `InvokeForMediaR` (tinh `elapsedMs` roi chia `1000.0`) — hai ham co logic do gio khong dong nhat trong cung 1 file.

### 2.7 CancellationTokenHelper.CreateLinkedTokenWithTimeout

**Signature**
```csharp
public static CancellationTokenSource CreateLinkedTokenWithTimeout(CancellationToken cancellationToken, int timeoutSeconds = 0)
```
`FTELSRCore.Shared/Helpers/CancellationTokenHelper.cs:12`.

**Muc dich** — Theo XML doc (dong 5-10): "Tao CancellationTokenSource lien ket voi token ben ngoai va timeout neu co." Duoc goi trong **toan bo** cac method cua `CallApiWithHttp<TRequest,TResponse>` (moi method GET/POST/PUT/DELETE deu goi truoc khi `SendAsync`) de vua ton trong huy tu caller (token ngoai) vua tu dong huy sau `timeoutSeconds`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `cancellationToken` | `CancellationToken` | Co | Khong validate | Khong co |
| `timeoutSeconds` | `int` | Khong | Chi kiem tra `timeoutSeconds > 0` de quyet dinh co goi `CancelAfter` hay khong; gia tri am hoac `0` deu **khong** cau hinh timeout | `0` |

**Output** — `CancellationTokenSource`: mot instance moi, duoc tao qua `CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)`, tuc token cua no (`.Token`) se bi huy khi **either** (a) `cancellationToken` (token ngoai/caller) bi huy, **hoac** (b) — neu `timeoutSeconds > 0` — sau khi het `TimeSpan.FromSeconds(timeoutSeconds)` do `CancelAfter` kich hoat.

**Dieu kien xu ly**
1. Tao `cancellationSet = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)` — day la buoc "link" voi token ngoai.
2. Neu `timeoutSeconds > 0`: goi `cancellationSet.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds))` — dat lich tu-huy.
3. Tra `cancellationSet`.

**Side effect** — Tao mot `CancellationTokenSource` moi (co the co timer noi bo tu `CancelAfter`) — day la **unmanaged-like resource can duoc `Dispose()`**.

**Error handling** — Khong co try/catch; khong throw ngoai cac exception ma `CancellationTokenSource.CreateLinkedTokenSource`/`CancelAfter` tu ban than .NET co the nem (vi du `ObjectDisposedException` neu `cancellationToken` da gan voi mot source da dispose — truong hop hiem).

**Khi nao NEN dung** — Khi can mot token vua chiu su huy tu caller ben ngoai, vua co gioi han thoi gian rieng (per-call timeout) — dung tot cho cac loi goi HTTP ra ngoai co gioi han thoi gian.

**Khi nao KHONG dung** — Khong co han che ro ret khi dung, nhung **caller PHAI tu dispose** ket qua tra ve (xem Gioi han).

**Gioi han**
- **Khong tu dispose**: ham tra ve `CancellationTokenSource` — trach nhiem `Dispose()` thuoc ve caller. Trong `CallApiWithHttp.cs`, hau het cac method dung `using CancellationTokenSource cancellationTokenSource = CreateLinkedTokenWithTimeout(...)` (dispose dung), NHUNG rieng `PostAsFileAsync` va `PostAsFileV2Async` khai bao **khong co `using`** (`var cancellationTokenSource = CreateLinkedTokenWithTimeout(...)` — `CallApiWithHttp.cs:702-703` va `:859-860`), nghia la trong 2 method nay, `CancellationTokenSource` (va timer `CancelAfter` ben trong, neu co) **khong duoc dispose ro rang**, co the ro ri handle/timer neu goi voi tan suat cao. (Diem nay da duoc `Utilizes-CallApiWithHttp.md` ghi nhan dung — xem `Utilizes-CallApiWithHttp.md:441,511`; khong co mau thuan can sua.)
- Khi timeout kich hoat (`CancelAfter`), token bi huy **giong het** truong hop caller tu huy `cancellationToken` ngoai — ca hai deu bieu hien qua `OperationCanceledException` khi `SendAsync` dang cho; ham **khong cung cap cach nao de phan biet** "huy do timeout" voi "huy do caller" tu ket qua tra ve cua ham nay (phai tu kiem tra rieng, vi du so sanh `cancellationToken.IsCancellationRequested` cua token ngoai truoc khi ket luan).
- `timeoutSeconds` la **giay** (khong phai milli-giay) — de nham voi cac tham so `desiredTime`/timeout khac trong repo dung don vi giay tuong tu nhung co ham (nhu `RunWithTimeoutAsync`) dung milli-giay.

### 2.8 CollectionHelpers.IsNullOrEmpty\<T\>

**Signature**
```csharp
public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
```
`FTELSRCore.Shared/Helpers/CollectionHelpers.cs:14`.

**Muc dich** — Theo XML doc (dong 5-12): kiem tra mot `IEnumerable<T>` co `null` hoac khong co phan tu nao, tra `true` cho ca hai truong hop, `false` neu co it nhat 1 phan tu.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `enumerable` | `IEnumerable<T>` (extension - `this`) | Co (nhung ho tro goi tren instance `null`) | Khong throw khi `null` — day chinh la truong hop dau tien duoc xu ly | Khong co |

**Output** — `bool`:
- `true` neu `enumerable is null`.
- `true` neu la `ICollection<T>`/`IReadOnlyCollection<T>`/`List<T>` va `Count is 0`.
- `false` neu la cac kieu tren va `Count > 0`.
- Fallback: `!enumerable.Any()` cho cac `IEnumerable<T>` khac (vi du ket qua tu LINQ chua duoc materialize) — `true` neu khong co phan tu nao khi enumerate.

**Dieu kien xu ly** (theo dung thu tu trong code)
1. `enumerable is null` -> `return true`.
2. `enumerable is ICollection<T> collection` -> `return collection.Count is 0`.
3. `enumerable is IReadOnlyCollection<T> readOnlyCollection` -> `return readOnlyCollection.Count is 0`.
4. `enumerable is List<T> list` -> `return list.Count is 0`.
5. Fallback: `return !enumerable.Any()`.

**Side effect** — Khong co, ngoai tru **enumerate mot lan** khi roi vao nhanh fallback (`.Any()`) — neu `enumerable` la mot `IEnumerable<T>` "lazy" (chua materialize, vi du ket qua truc tiep tu mot cau LINQ `Select`/`Where` chua `.ToList()`), goi `IsNullOrEmpty` se **kich hoat thuc thi 1 phan tu dau** cua chuoi LINQ do (side effect tiem an neu nguon du lieu co side effect, vi du query DB).

**Error handling** — Khong co try/catch; khong nem exception trong dieu kien binh thuong.

**Khi nao NEN dung** — Guard clause pho bien truoc khi xu ly danh sach (`if (list.IsNullOrEmpty()) return;`), toi uu hon `list == null || !list.Any()` viet tay vi tranh duoc enumerate toan bo khi da biet `Count` truc tiep.

**Khi nao KHONG dung** — Khi can phan biet "null" va "rong" nhu hai truong hop khac nhau (ham nay coi ca hai la mot).

**Gioi han**
- Nhanh `enumerable is List<T> list` (buoc 4) la **code khong bao gio duoc thuc thi (dead branch)**: `List<T>` da tu implement `ICollection<T>`, nen bat ky instance `List<T>` deu da khop voi pattern o buoc 2 (`is ICollection<T>`) va return truoc khi den buoc 4. Xem muc 3, van de #3.
- Voi kieu `IReadOnlyCollection<T>` khong dong thoi la `ICollection<T>` (hiem trong thuc te .NET base types, nhung co the xay ra voi kieu custom), thu tu kiem tra van dung logic — khong co van de o day, chi la buoc 4 vinh vien khong dat toi.

### 2.9 ObfuscationHelpers.DecodeDataFromSR\<T\>

**Signature**
```csharp
public static bool DecodeDataFromSR<T>(
    this string data, out T result, string key, ILogger logger = null)
```
`FTELSRCore.Shared/Helpers/ObfuscationHelpers.cs:18-19`.

**Muc dich** — Giai ma mot chuoi da duoc ma hoa (Base64 cua du lieu XOR voi `key`) tro lai thanh doi tuong `T`, qua buoc trung gian la chuoi JSON.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `data` | `string` (extension - `this`) | Co | Tra `false` som (khong throw) neu: `key` rong/whitespace, HOAC `data` rong/whitespace, HOAC `data.Trim()` (khong phan biet hoa/thuong) bang `"null"`, HOAC `data.Trim()` bang `"{}"` hoac `"[]"` | Khong co |
| `result` | `out T` | Co (out) | Luon duoc gan — `default` neu khong the giai ma, gia tri thuc te neu thanh cong | Khong co |
| `key` | `string` | Co | Nhu tren; con dung lam key XOR (UTF8 bytes), lap lai theo modulo do dai key | Khong co |
| `logger` | `ILogger` | Khong | Neu `null`, dung `CommonBaseConstant.ConfigLoggerExceptionByConsole` de log ra console thay vi qua `ILogger` | `null` |

**Output** — `bool`:
- `false` + `result = default` neu roi vao 1 trong 4 dieu kien guard (key/data rong, data la `"null"`/`"{}"`/`"[]"`).
- `false` + `result = default` neu qua trinh giai ma (`Convert.FromBase64String`, XOR, `Encoding.UTF8.GetString`) hoac `JSonTryParse` nem exception (bat trong `catch (Exception exception)`).
- `false` + `result` theo gia tri `JSonTryParse` tra ve (co the la `default` neu JSON khong parse duoc dung `T`) — **ket qua cuoi cung phu thuoc hoan toan vao `plainJson.JSonTryParse(out result, logger)`** (dong 53); ham nay khong tu kiem tra `result != null` truoc khi tra.
- `true` neu `JSonTryParse` tra `true`.

**Dieu kien xu ly**
1. Guard 4 dieu kien (key/data rong hoac `data` la mot trong 3 gia tri "rong ve nghia" — `null`/`{}`/`[]`) -> tra `false` ngay, **khong log gi ca** (khong phai loi, chi la khong co du lieu de decode).
2. `try`: goi local function `XorDecodeFromBase64` (Base64 decode -> XOR voi key (lap key theo `i % keyBytes.Length`) -> UTF8 decode ra chuoi JSON) roi goi `plainJson.JSonTryParse(out result, logger)` va tra ve ket qua cua no.
3. `catch (Exception exception)`: gan `result = default`, log loi (qua `logger.ErrorException` neu co `logger`, hoac console fallback neu khong), tra `false`.

**Side effect** — Ghi log loi (qua `ILogger` hoac console) **chi khi co exception**, khong ghi log gi trong nhanh guard (buoc 1) hay nhanh thanh cong.

**Error handling** — Bat `Exception` (chung, khong loc loai cu the) quanh toan bo qua trinh giai ma + parse JSON; khong nem lai (`false` duoc tra ve, khong throw ra caller).

**Khi nao NEN dung** — Khi can giai ma nguoc lai du lieu da duoc `EncodeDataFromSR` ma hoa truoc do (cung `key`), thuong dung de truyen du lieu "obfuscated" qua tham so/URL/token noi bo giua cac service SR.

**Khi nao KHONG dung** — Khong dung nhu mot co che bao mat/encryption thuc su — XOR + Base64 la ky thuat che dau don gian (obfuscation), khong chong duoc phan tich mat ma neu `key` bi lo hoac bi doan (XOR key ngan lap lai la de bi tan cong biet-ro-plaintext).

**Gioi han**
- Neu `data`/`key` co do dai khac nhau, XOR lap key theo modulo — khong co van de ky thuat, nhung neu `key` rat ngan so voi `data`, do "che dau" thap.
- Khong validate `data` co dung la Base64 hop le truoc khi goi `Convert.FromBase64String` — loi format se roi vao `catch` chung, tra `false` kem log loi.
- Message log loi ghi ca `data` goc (chuoi da ma hoa) vao message (`$"... fail:" + data`) — **khong log `plainJson` da giai ma** nen it rui ro lo du lieu nhay cam qua log so voi `EncodeDataFromSR` (xem 2.10).

### 2.10 ObfuscationHelpers.EncodeDataFromSR\<T\>

**Signature**
```csharp
public static bool EncodeDataFromSR<T>(
    this T data, out string result, string key, ILogger logger = null)
```
`FTELSRCore.Shared/Helpers/ObfuscationHelpers.cs:93-94`.

**Muc dich** — Ma hoa mot doi tuong `T` thanh chuoi (JSON -> XOR voi `key` -> Base64) de truyen di duoi dang chuoi "che dau".

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `data` | `T` (extension - `this`) | Co | Tra `false` som neu `key` rong/whitespace HOAC `data is null` | Khong co |
| `result` | `out string` | Co (out) | Luon duoc gan — `default` (`null`) neu loi, chuoi Base64 neu thanh cong | Khong co |
| `key` | `string` | Co | Nhu tren; dung lam key XOR | Khong co |
| `logger` | `ILogger` | Khong | Nhu `DecodeDataFromSR` | `null` |

**Output** — `bool`:
- `false` + `result = default` neu `key` rong/whitespace hoac `data is null`.
- `false` + `result = default` neu chuoi Base64 sau khi ma hoa la rong/whitespace (kiem tra o dong 128) — truong hop nay hiem xay ra thuc te vi Base64 cua chuoi rong van la chuoi rong (`Convert.ToBase64String([])` = `""`), nen day co the xay ra khi `data.ToJSon()` tra ve chuoi rong.
- `false` + `result = default` neu co exception trong qua trinh serialize/XOR/encode (bat trong `catch`).
- `true` + `result` = chuoi Base64 da ma hoa, neu thanh cong.

**Dieu kien xu ly**
1. Guard: `key` rong/whitespace hoac `data is null` -> tra `false` ngay, khong log.
2. `try`: goi local function `XorDecodeFromBase64` (ten function **gay hieu nham**: chuc nang thuc te la MA HOA/encode, khong phai decode — xem muc 3, van de #5) — chuyen `data` sang JSON qua `data.ToJSon()` (rong neu `data` serialize ra rong), UTF8 encode, XOR voi `key` (lap theo modulo), roi Base64-encode.
3. Neu `value` (ket qua Base64) rong/whitespace -> `result = default`, tra `false`.
4. Nguoc lai: `result = value`, tra `true`.
5. `catch (Exception exception)`: `result = default`, log loi, tra `false`.

**Side effect** — Ghi log loi khi co exception. **Diem dang chu y**: message log loi trong nhanh `catch` goi `Newtonsoft.Json.JsonConvert.SerializeObject(data)` de dua **toan bo `data` goc (chua ma hoa)** vao chuoi message loi (dong 141-142) — nghia la neu qua trinh ma hoa loi, du lieu goc (thu ma ham nay dang co gang "che dau") **bi ghi thang vao log dang plaintext JSON**. Xem muc 3, van de #6.

**Error handling** — Bat `Exception` chung; khong nem lai; tra `false`.

**Khi nao NEN dung** — Khi can tao mot chuoi "che dau" tu mot object de nhung vao URL/tham so noi bo, sau do dung `DecodeDataFromSR<T>` (cung `key`) de giai ma lai o dau nhan.

**Khi nao KHONG dung** — Tuong tu 2.9, khong dung cho muc dich bao mat that su; **dac biet khong nen dung khi `logger` duoc cau hinh ghi log ra he thong luu tru chung (co the bi doc boi nguoi khong duoc phep) va `data` chua thong tin nhay cam**, vi nhanh loi se log nguyen van `data` (xem van de #6).

**Gioi han**
- Local function ten `XorDecodeFromBase64` duoc dung ca trong `EncodeDataFromSR` (thuc te la encode) — sai ten, de nham khi doc code doc lap voi tai lieu nay.
- Kiem tra `value` rong sau khi encode (buoc 3) chi bat duoc truong hop `data.ToJSon()` tra ve rong; khong co kiem tra nao dam bao `T` co the serialize hop le truoc khi thu.
- Rui ro log lo du lieu nhay cam qua nhanh `catch` (xem van de #6) — day la van de nghiem trong nhat cua ham nay xet theo dung ten "Obfuscation".

### 2.11 LazyResolverExtensions.LazyResolver\<T\>

**Signature**
```csharp
public class LazyResolverExtensions
{
    public class LazyResolver<T>(IServiceProvider provider) : Lazy<T>(provider.GetRequiredService<T>)
    { }
}
```
`FTELSRCore.Shared/Extensions/LazyResolverExtensions.cs:5-9`.

**Muc dich** — Cung cap mot `Lazy<T>` ma factory chinh la `provider.GetRequiredService<T>` — cho phep dang ky `LazyResolver<T>` trong DI container va resolve `T` chi khi thuc su can (truy cap `.Value`), tranh resolve som/circular dependency khi `T` nang hoac chua san sang luc khoi tao.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `provider` (constructor cua `LazyResolver<T>`) | `IServiceProvider` | Co | Khong validate null trong code nay; neu `null`, se nem `NullReferenceException` **chi khi** `.Value` duoc truy cap lan dau (do `Lazy<T>` chi goi factory luc do) | Khong co |

**Output** — Khong ap dung truc tiep (day la mot class, khong phai method); `LazyResolver<T>` ke thua toan bo API cua `Lazy<T>` (`.Value`, `.IsValueCreated`). Khi truy cap `.Value` lan dau, gia tri tra ve la `T` duoc resolve tu `provider.GetRequiredService<T>()`.

**Dieu kien xu ly** — Khong co logic rieng ngoai viec ke thua `Lazy<T>` voi factory delegate co san (`provider.GetRequiredService<T>` — day la mot method group duoc truyen truc tiep nhu `Func<T>` cho constructor cua `Lazy<T>`).

**Side effect** — Lan dau `.Value` duoc goi: kich hoat `IServiceProvider.GetRequiredService<T>()`, co the tao instance moi (transient/scoped) hoac tra instance da co (singleton) tuy vong doi cua `T` trong DI container.

**Error handling** — Khong co try/catch. Neu `T` khong duoc dang ky trong container, `GetRequiredService<T>()` nem `InvalidOperationException` — exception nay se duoc `Lazy<T>` bao (theo mac dinh cua `LazyThreadSafetyMode`) va co the nem lai (rethrow) o CAC LAN truy cap `.Value` tiep theo tuy che do thread-safety mac dinh cua `Lazy<T>` — **khong co logic rieng nao trong `LazyResolver<T>` de bat/xu ly loi nay**.

**Khi nao NEN dung** — Khi mot dependency `T` "nang" (khoi tao ton chi phi) hoac co the gay circular reference neu resolve ngay trong constructor, va muon triri hoan resolve den khi thuc su dung.

**Khi nao KHONG dung** — Khi `T` la `IDisposable` va can quan ly vong doi ro rang qua DI container (Lazy khong tu dispose instance da tao).

**Gioi han**
- **Khong tim thay call site nao** trong repo nay dang ky hoac su dung `LazyResolver<T>` (grep toan repo chi tra ve chinh dinh nghia class) — khong xac dinh duoc tu source code liet nay co dang duoc dung thuc te trong tang nao khong.
- Lop bao ngoai `LazyResolverExtensions` khong co thanh vien nao khac ngoai nested class `LazyResolver<T>` — ten "Extensions" (goi y extension methods) nhung thuc te khong chua extension method nao. Xem muc 3, van de #7.
- Trung lap hoan toan ve chuc nang voi `LazyInstanceUtility<T>` (2.12) — xem muc 3, van de #8.

### 2.12 LazyInstanceUtility\<T\>

**Signature**
```csharp
public class LazyInstanceUtility<T>(IServiceProvider serviceProvider) : Lazy<T>(serviceProvider.GetRequiredService<T>)
{ }
```
`FTELSRCore.Shared/Utilizes/LazyInstanceUtility.cs:5-6`.

**Muc dich, Input, Output, Dieu kien xu ly, Side effect, Error handling** — Giong hoan toan `LazyResolverExtensions.LazyResolver<T>` (muc 2.11): cung ke thua `Lazy<T>` voi factory `serviceProvider.GetRequiredService<T>`, chi khac ten tham so constructor (`serviceProvider` thay vi `provider`) va namespace (`FTELSRCore.Utilizes` thay vi long trong `FTELSRCore.Extensions.LazyResolverExtensions`).

**Khi nao NEN dung / KHONG dung** — Nhu 2.11.

**Gioi han**
- Cung khong tim thay call site nao trong repo (grep chi tra ve chinh dinh nghia).
- Day la **ban sao chuc nang 100%** cua `LazyResolverExtensions.LazyResolver<T>` — hai lop nam o hai namespace/file khac nhau (`FTELSRCore.Extensions` vs `FTELSRCore.Utilizes`) nhung code sinh ra giong nhau tuyet doi. Xem muc 3, van de #8.
- **Khac voi 8 file con lai trong module nay**: namespace `FTELSRCore.Utilizes` cua lop nay **khong** co trong `global using` cua `FTELSRCore.Shared/GlobalUsing.cs` (da kiem tra truc tiep file, 11 dong, khong dong nao la `FTELSRCore.Utilizes`) va khong co global using nao khac trong repo bao phu namespace nay. Do do, khac voi phan con lai cua module (nam trong `FTELSRCore.Exceptions`/`FTELSRCore.Extensions`/`FTELSRCore.Helpers` — deu da co global using), code o ngoai namespace `FTELSRCore.Utilizes` muon dung `LazyInstanceUtility<T>` truc tiep (khong fully-qualified) se can them `using FTELSRCore.Utilizes;` tuong minh, khac phat bieu chung "khong can `using`" da neu (va truoc day bi ghi sai) o muc 1.

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `RunWithTimeoutAsync<T>` tra ve tuple `(T Data, bool Result)` nhung `Result` **luon la `true`** trong ca 3 nhanh (timeout=0, timeout xay ra, hoan tat truoc timeout) — khong co duong dan nao gan `false` | `RunWithTimeoutExtentions.cs:22,31,34` | Cao. Caller khong the dung `Result` de phan biet "task hoan tat thanh cong" voi "da timeout, task van chay ngam" — ten field gay hieu nham nghiem trong so voi hanh vi thuc |
| 2 | `EnvironmentExtensions.GetPrefixEnvironment()` dinh nghia 4 hang so moi truong (`ELocal`, `EDev`, `EStag`, `EProd`) nhung `switch` chi co nhanh rieng cho `EStag` va `EProd`; `ELocal` va `EDev` deu roi vao `default` ("dev-") ma khong co nhanh rieng nao tham chieu den chung | `EnvironmentExtensions.cs:5,7,28-33` | Trung binh. Khong sai ve logic (ket qua van la "dev-" cho ca hai, co le la chu y) nhung khong the xac dinh tu source la co chu dinh gop Local+Dev hay la thieu nhanh — ghi nhan la "khong xac dinh duoc tu source code" ve chu dich thiet ke |
| 3 | Nhanh `enumerable is List<T> list` trong `IsNullOrEmpty<T>` la **dead code**: `List<T>` da implement `ICollection<T>` nen luon khop nhanh truoc (`is ICollection<T>`) va return som | `CollectionHelpers.cs:21-23,31-34` | Thap. Khong gay sai logic (ket qua dung) nhung la code khong bao gio thuc thi, gay nham hieu khi doc |
| 4 | Ten file `RunWithTimeoutExtentions.cs` sai chinh ta ("Extentions" thay vi "Extensions") trong khi ten class ben trong (`RunWithTimeoutExtensions`) viet dung | `FTELSRCore.Shared/Extensions/RunWithTimeoutExtentions.cs` (ten file) | Thap. Chi anh huong kha nang tim kiem file, khong anh huong runtime |
| 5 | Local function ten `XorDecodeFromBase64` duoc dung lai trong CA HAI ham `DecodeDataFromSR` (dung, chuc nang la decode) VA `EncodeDataFromSR` (SAI — chuc nang thuc te la ENCODE, khong phai decode) | `ObfuscationHelpers.cs:96` (dinh nghia trong `EncodeDataFromSR`) | Trung binh. Khong anh huong hanh vi (chi la ten local function noi bo), nhung de gay nham cho nguoi doc/maintain code sau nay |
| 6 | Nhanh `catch` cua `EncodeDataFromSR` ghi **toan bo du lieu goc `data` (chua ma hoa)** vao message log qua `Newtonsoft.Json.JsonConvert.SerializeObject(data)` | `ObfuscationHelpers.cs:141-142` | Cao (neu `data` chua thong tin nhay cam). Muc dich cua ham la "obfuscate" du lieu, nhung khi loi xay ra, chinh du lieu can che dau lai bi log ra plaintext — mau thuan truc tiep voi muc dich thiet ke cua module |
| 7 | Class `LazyResolverExtensions` (ten goi y chua cac extension method) thuc te chi chua 1 nested class `LazyResolver<T>`, khong co extension method nao | `LazyResolverExtensions.cs:5-9` | Thap. Gay nham ten so voi noi dung thuc te |
| 8 | `LazyResolverExtensions.LazyResolver<T>` va `LazyInstanceUtility<T>` la hai lop **trung lap hoan toan ve chuc nang** (cung ke thua `Lazy<T>` voi factory `IServiceProvider.GetRequiredService<T>`), khac nhau chi ve namespace/ten tham so; ca hai deu **khong tim thay call site nao** trong repo nay | `LazyResolverExtensions.cs:7`, `LazyInstanceUtility.cs:5` | Trung binh. Trung lap ma khong ro ly do (co the mot trong hai la ban cu con lai sau refactor); ca hai co the la dead code trong pham vi repo nay — khong xac dinh duoc tu source code neu co project khac (sr-hub-api, sr-request-api...) dang dung qua file `.dll` duoc copy (xem `CopyToOtherLibs` trong `.csproj`) |
| 9 | Constructor phu `CustomException(int statusCode, Exception inner)` chi lay `inner.Message` lam chuoi cho `Exception.Message` cua doi tuong moi, **khong** gan `inner` lam `InnerException` (khong goi `base(message, innerException)`) | `CustomException.cs:11-13` | Trung binh. Mat thong tin stack trace/loai exception goc; nguoi doc code (hoac cong cu debug dua vao `InnerException`) se khong thay duoc nguyen nhan goc |
| 10 | Doi chieu voi KB cu: kiem tra cho thay `Utilizes-CallApi.md` va `Utilizes-CallApiWithHttp.md` mo ta **dung** hanh vi cua `CustomException`, `MeasureExecutionTimeExtensions.InvokeForHTTP` (desiredTime chi log Warning, khong phai timeout) va `CancellationTokenHelper.CreateLinkedTokenWithTimeout` (bao gom ca 2 diem thieu `using` trong `PostAsFileAsync`/`PostAsFileV2Async`) — **khong phat hien mo ta sai/thieu nao can canh bao** o hai file KB do lien quan truc tiep den module nay | `Utilizes-CallApi.md`, `Utilizes-CallApiWithHttp.md` (toan bo cac dong da doi chieu, xem qua trinh doi chieu trong muc 2.6 va 2.7) | Thong tin. Ghi nhan de tuan thu yeu cau doi chieu nguoc; khong can hanh dong sua |
