# Infrastructure - MiddleWares

> Nguon: FTELSRCore.Shared/Infrastructure/MiddleWares/CorrelationIdMiddleWare.cs; FTELSRCore.Shared/Infrastructure/MiddleWares/ExceptionHandlerMiddleWare.cs; FTELSRCore.Shared/Infrastructure/MiddleWares/MeasureExecutionTimeMiddleWare.cs; FTELSRCore.Shared/Infrastructure/MiddleWares/ResponseWrapperMidleWare.cs; FTELSRCore.Shared/Infrastructure/MiddleWares/SerilogHandlerMiddleWare.cs; FTELSRCore.Shared/Infrastructure/MiddleWares/Helpers/BuildMetaHelper.cs; FTELSRCore.Shared/Infrastructure/MiddleWares/Helpers/ReadRequestBodyHelper.cs
> Loai: class (`CorrelationIdMiddleWare`, `ExceptionHandlerMiddleWare`, `MeasureExecutionTimeMiddleWare`, `SerilogHandlerMiddleWare`, `BuildMetaHelper`, `ReadRequestBodyHelper`) | record (`ExceptionHandlerMiddleWareModel`, `ResponseFTELCoreWrapperModel`) | class cau hinh don gian (`SerilogHandlerMiddleWareModel`) | sealed class IAsyncResultFilter (`ResponseFTELCoreWrapperFilter`)
> Cap nhat theo commit: 89c1ce9

## 1. Tong quan

Module nay tap hop cac thanh phan xu ly cat ngang (cross-cutting) chay tren tung HTTP request/response cua cac service dung `FTELSRCore.Shared`: sinh/lan truyen Correlation-Id, bat va chuan hoa exception thanh response JSON, do thoi gian thuc thi request, gan thong tin dinh danh nguoi dung/IP/user-agent vao Serilog log context, va bo sung truong `Meta`/`System` vao response MVC theo chuan `IResult`. Hai file trong `Helpers/` (`BuildMetaHelper`, `ReadRequestBodyHelper`) la ha tang dung chung, khong tu dung doc lap ma duoc cac middleware khac goi lai. Ve kien truc, day la tang **Infrastructure** — nam giua Kestrel/pipeline ASP.NET Core va tang xu ly nghiep vu (Controller/MediatR).

**Luu y quan trong ve phan loai**: mac du thu muc ten la `MiddleWares`, `ResponseFTELCoreWrapperFilter` (trong `ResponseWrapperMidleWare.cs`) **khong phai** la ASP.NET Core Middleware (khong co constructor nhan `RequestDelegate`, khong co method `Invoke`/`InvokeAsync`) ma la mot **MVC `IAsyncResultFilter`** — chay trong pipeline filter cua MVC (`ResultExecutingContext`/`ResultExecutionDelegate`), khong phai trong pipeline middleware tho cua Kestrel (`ResponseWrapperMidleWare.cs:7,9`).

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| `CorrelationIdMiddleWare`: lay `X-Correlation-Id` tu request header, hoac sinh moi tu `Activity.Current?.TraceId`/GUID; day id vao Serilog `LogContext` va (khi response chua bat dau ghi) gan lai vao response header qua `OnStarting` | Khong xac thuc/khu ky tu nguy hiem trong gia tri `X-Correlation-Id` do client gui len — gia tri nay duoc tin tuong tuyet doi va echo thang vao response header + log |
| `ExceptionHandlerMiddleWare`: bat moi `Exception` tu pipeline phia sau, log kem method/path/query/body, map 3 loai exception cu the (`UnauthorizedAccessException`, `KeyNotFoundException`, `CustomException`) + fallback sang HTTP 500, ghi response JSON theo `Result` | Khong rethrow exception trong bat ky truong hop nao (ke ca khi `httpContext.Response.HasStarted`) — luon swallow sau khi log |
| `MeasureExecutionTimeMiddleWare`: do latency bang `Stopwatch`, log `Warning` khi loi (status >= 400) hoac chay cham (>= 10s), luon log `Response` voi latency cho moi request | Khong do rieng thoi gian cua tung middleware/handler con — chi do tong thoi gian cua toan bo `_next` phia sau no trong pipeline; khong tu bat exception (exception van bay len tren sau khi `finally` log xong) |
| `SerilogHandlerMiddleWare`: doc user-agent/IP/role/username tu request, day 4 `PropertyEnricher` vao Serilog `LogContext` cho toan bo request | Khong doc claim `SRPermissions` (doan code lien quan da bi comment — `SerilogHandlerMiddleWare.cs:43-50`) |
| `ResponseFTELCoreWrapperFilter`: gan `Meta` (request id/trace id/timestamp) va `System` vao `ObjectResult.Value` neu do la `IResult` va `Meta` hien dang `null` | Khong ap dung cho ket qua khong phai `ObjectResult` (vi du Minimal API `Results.Ok(...)` tuy cach cau hinh, `NoContentResult`, …) va khong ghi de neu `Meta` da duoc gan san |
| `BuildMetaHelper.Build`: dung `ResultFTelCoreMetadataModel` (Request_Id, Trace_Id, Timestamp) dung chung cho ca `ExceptionHandlerMiddleWare` va `ResponseFTELCoreWrapperFilter` | Khong tu doc `Activity.Current` — `Trace_Id` chi lay tu header `X-Correlation-Id` cua **request**, khong phai W3C trace context thuc su |
| `ReadRequestBodyHelper.ReadAsync`: doc lai body request (gioi han 5MB) de phuc vu log loi/log cham | Khong tu goi `EnableBuffering()` — phu thuoc hoan toan vao viec buffering da duoc bat tu truoc (xem muc 2.7 va muc 3) |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `Microsoft.AspNetCore.Http` (`HttpContext`, `RequestDelegate`, `IHeaderDictionary`) | Toan bo 4 middleware doc/ghi header, body, status code qua `HttpContext` |
| `Microsoft.Extensions.Primitives.StringValues` | `CorrelationIdMiddleWare` dung de `TryGetValue` header dang multi-value |
| `System.Diagnostics.Activity`/`Stopwatch` | `CorrelationIdMiddleWare` doc `Activity.Current?.TraceId`; `MeasureExecutionTimeMiddleWare` do thoi gian bang `Stopwatch.GetTimestamp()`/`Stopwatch.Frequency` |
| `Serilog.Context.LogContext`, `Serilog.Core.Enrichers.PropertyEnricher` | `CorrelationIdMiddleWare` va `SerilogHandlerMiddleWare` day property vao ngu canh log cho toan request |
| `Microsoft.AspNetCore.Mvc.Filters` (`IAsyncResultFilter`, `ResultExecutingContext`) | `ResponseFTELCoreWrapperFilter` la MVC result filter, khong phai middleware |
| `HeaderConstant` (`FTELSRCore.Shared/Constants/HeaderConstant.cs`) | Cung cap `CorrelationIdHeaderKey = "X-Correlation-Id"`, `UserAgentHeaderKey = "User-Agent"`, `ForwardedHeaderKey = "X-Forwarded-For"` |
| `SerilogConstant` (`FTELSRCore.Shared/Constants/SerilogConstant.cs`) | Ten property log: `CorrelationIdPropertyName`, `ForwardedPropertyName`, `UserPropertyName`, `UserAgentPropertyName`, `UserInfoPropertyName` |
| `CommonBaseConstant` (`FTELSRCore.Shared/Constants/CommonBaseConstant.cs`) | `System` (ten he thong mac dinh), `Anonymous`, `DateTimeUtc(int addHour = 7)` dung de dung `Timestamp` |
| `ClaimTypesConstant.SRRoles`, `RoleSR` (enum), `RoleDataConstant.GetRoleData` | `SerilogHandlerMiddleWare` suy ra `roleDataName` tu claim `SR.SRRoles` cua user |
| `ConvertHelpers.GetUserAgent`, `ConvertHelpers.GetClientIpAddress`, `ConvertHelpers.ConvertHttpStatusCodeCodeByName` | `SerilogHandlerMiddleWare` dung 2 ham dau de doc user-agent/IP da nguon; `ExceptionHandlerMiddleWare` dung ham thu 3 de doi `int` status code sang ten enum `HttpStatusCode` |
| `DelimiterConstant.CHAR_COMMA` | `SerilogHandlerMiddleWare` join danh sach role thanh 1 chuoi bang dau `,` |
| `Result`, `IResult`, `ResultFTelCoreErrorModel`, `ResultFTelCoreMetadataModel` (`FTELSRCore.Shared/Wrappers`) | `ExceptionHandlerMiddleWare` dung response loi qua `Result.FailSystem`; `ResponseFTELCoreWrapperFilter` kiem tra `context.Result is ObjectResult { Value: IResult result }` |
| `ResponseWrapperByCodeMapper.FromStatusCode`, `CatalogsErrorCodeModel`, `ErrorSourceType` (`FTELSRCore.Shared/Wrappers/ErrorCodes`) | `ExceptionHandlerMiddleWare` map HTTP status code sang ma loi noi bo (`Code` dang string, `Retryable`) |
| `CustomException` (`FTELSRCore.Shared/Exceptions/CustomException.cs`) | `ExceptionHandlerMiddleWare` map rieng nhanh exception noi bo nay de lay `Code` tuy bien — **da duoc tai lieu hoa day du trong `CrossCutting-SmallUtilities.md`**, khong lap lai chi tiet o day |
| `EnvironmentExtensions.GetEnvironment()`, hang `EProd`, `EStag` (`FTELSRCore.Shared/Extensions/EnvironmentExtensions.cs`) | `ExceptionHandlerMiddleWare` quyet dinh co che message loi goc hay khong dua theo bien moi truong `ASPNETCORE_ENVIRONMENT` |
| `LoggerExtensions.ErrorException`, `.Warning`, `.Response` (`FTELSRCore.Shared/Extensions/Loggers/LoggerExtensions.cs`) | Ca 2 middleware `ExceptionHandlerMiddleWare` va `MeasureExecutionTimeMiddleWare` dung cac extension log nay (noi dung method log da ton tai san trong repo, khong thuoc pham vi tai lieu module nay) |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `CorrelationIdMiddleWare.Invoke(HttpContext)` | Middleware | Sinh/lan truyen `X-Correlation-Id` |
| `ExceptionHandlerMiddleWare.Invoke(HttpContext)` | Middleware | Bat exception toan cuc, tra response JSON chuan `Result` |
| `MeasureExecutionTimeMiddleWare.Invoke(HttpContext)` | Middleware | Do latency, log warning khi loi/cham |
| `SerilogHandlerMiddleWare.InvokeAsync(HttpContext)` | Middleware | Enrich Serilog `LogContext` voi user-agent/IP/role/username |
| `ResponseFTELCoreWrapperFilter.OnResultExecutionAsync(ResultExecutingContext, ResultExecutionDelegate)` | MVC Result Filter | Gan `Meta`/`System` vao `IResult` tra ve tu Controller |
| `BuildMetaHelper.Build(HttpContext)` | Helper (static) | Dung `ResultFTelCoreMetadataModel` |
| `ReadRequestBodyHelper.ReadAsync(HttpContext)` | Helper (static) | Doc lai body request (bounded 5MB) phuc vu log |

## 2. Chi tiet API

### 2.1 CorrelationIdMiddleWare.Invoke

**Signature**
```csharp
public class CorrelationIdMiddleWare(RequestDelegate _next)
{
    public async Task Invoke(HttpContext httpContext)
}
```

**Muc dich** — Dam bao moi request co mot `correlationId` xuyen suot: lay tu header request neu co, hoac sinh moi; day gia tri nay vao Serilog `LogContext` (property `CorrelationId`) va co gang phan hoi lai gia tri do trong response header.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `httpContext` | `HttpContext` | Co | Khong co validate null (neu `httpContext` la `null`, code se nem `NullReferenceException` ngay dong dau) | Khong co |

**Output** — Kieu tra ve `Task` (void async). Khong co gia tri tra ve mang y nghia nghiep vu; hieu ung quan sat duoc la qua side effect (header, log context) va viec pipeline co tiep tuc chay `_next` hay khong.

**Dieu kien xu ly** (theo thu tu thuc thi, `CorrelationIdMiddleWare.cs:12-51`)

1. Neu request header `X-Correlation-Id` (`HeaderConstant.CorrelationIdHeaderKey`) ton tai → lay `correlationIds.FirstOrDefault()` lam `correlationId` (dong 14-18). Neu client gui nhieu gia tri, chi gia tri dau tien duoc dung; **khong co validate dinh dang/do dai**.
2. Neu khong co header → thu `Activity.Current?.TraceId.ToString()` (dong 21); neu van rong/whitespace → sinh `Guid.NewGuid().ToString("N")` (dong 25); sau do **ghi nguoc gia tri nay vao `httpContext.Request.Headers`** (dong 28) — tuc middleware mutate luon request header dang xu ly.
3. Dang ky callback `httpContext.Response.OnStarting(...)` (dong 31-41): tai thoi diem response bat dau ghi header, neu response **chua co** header `X-Correlation-Id` thi moi gan `correlationId` vao response header. Neu mot middleware/handler phia sau da tu set header nay truoc do, gia tri do duoc giu nguyen (khong bi de).
4. Mo `using LogContext.PushProperty(name: SerilogConstant.CorrelationIdPropertyName, value: correlationId)` (dong 43) bao trum toan bo phan con lai cua request.
5. Trong block `using` do: neu `httpContext.Response.HasStarted` da la `true` **tai thoi diem nay** (dong 45-48) → `return` ngay, **khong goi `_next`** — bo qua toan bo pipeline phia sau.
6. Nguoc lai → `await _next.Invoke(httpContext)` (dong 50).

**Side effect**
- Mutate `httpContext.Request.Headers[X-Correlation-Id]` khi tu sinh id moi (dong 28).
- Dang ky callback tren `Response.OnStarting` — co the mutate `httpContext.Response.Headers` sau nay.
- Day 1 property vao Serilog `LogContext` cho toan bo scope cua request.
- Co the **bo qua toan bo phan con lai cua pipeline** (buoc 5) neu `Response.HasStarted` da `true` khi middleware nay thuc thi.

**Error handling** — Khong co `try/catch` nao trong toan ham. Bat ky exception nao (ke ca tu `_next.Invoke`) se thoat thang ra khoi middleware nay, khong bi bat/log tai day (viec bat exception la trach nhiem cua `ExceptionHandlerMiddleWare` — thu tu dang ky giua 2 middleware do Program.cs cua service quyet dinh, xem muc "Gioi han").

**Khi nao NEN dung** — Dang ky som trong pipeline (gan dau) de moi middleware/log phia sau deu co `CorrelationId` trong `LogContext` va moi response deu co co hoi mang header `X-Correlation-Id`.

**Khi nao KHONG dung** — Neu he thong can validate/whitelist dinh dang correlation id den tu client (chong log injection hoac gia tri qua dai) — middleware nay khong lam viec do, xem muc 3.

**Gioi han**
- Gia tri `X-Correlation-Id` tu client duoc tin tuong tuyet doi, echo thang vao response header va log — khong kiem tra ky tu dac biet/do dai.
- Guard `httpContext.Response.HasStarted` o buoc 5 chi co y nghia neu middleware nao do *truoc* middleware nay trong pipeline da flush response — trat tu dang ky middleware phu thuoc `app.Use...` trong `Program.cs` cua service tieu thu, **khong the xac dinh tu source cua thu vien nay**.
- Khong co co che inject `TraceId` dang W3C chuan vao response — chi echo lai dung chuoi id da dung o request.

### 2.2 ExceptionHandlerMiddleWare.Invoke

**Signature**
```csharp
public class ExceptionHandlerMiddleWare(RequestDelegate _next, ILogger<ExceptionHandlerMiddleWare> logger, ExceptionHandlerMiddleWareModel middleWareModel)
{
    public async Task Invoke(HttpContext httpContext)
}
```

**Muc dich** — La "global exception handler" dang middleware: bat moi `Exception` khong duoc xu ly o tang duoi, ghi log chi tiet (method/path/query/body + stack trace), va tra ve cho client mot response JSON dong nhat theo cau truc `Result`/`IResult`, kem ma loi noi bo (`ResultFTelCoreErrorModel.Code`) va co `Retryable`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `httpContext` | `HttpContext` | Co | Khong validate null tuong minh | Khong co |
| `middleWareModel.ServiceName` | `string` (qua constructor `ExceptionHandlerMiddleWareModel`) | Khong | Neu `null` → fallback `CommonBaseConstant.System` (dong 51) | `null` |

**Output** — `Task` (void async). Hieu ung quan sat duoc: response HTTP JSON (neu response chua bat dau ghi) voi `Content-Type: application/json`, `StatusCode` mot trong `403/404/<Code cua CustomException>/500`.

**Dieu kien xu ly** (`ExceptionHandlerMiddleWare.cs:15-105`)

1. `try { await _next(httpContext); }` — neu khong co exception, ham ket thuc binh thuong, khong lam gi them.
2. `catch (Exception exception)` — bat **moi** loai exception (khong gioi han subtype).
3. Dung `message` (`StringBuilder`) gom `Method` + `Path`; neu co `QueryString` thi append; doc `requestBody` qua `ReadRequestBodyHelper.ReadAsync(httpContext)` (muc 2.7) va append neu khong rong (dong 21-33).
4. Log bang `logger.ErrorException(nameof(ExceptionHandlerMiddleWare), nameof(Invoke), e: exception, message: message)` (dong 35) — **luon log**, khong phu thuoc `Response.HasStarted`.
5. Neu `httpContext.Response.HasStarted` → `return` ngay (dong 37-40) — **khong ghi response, khong rethrow** exception. Client da nhan mot phan response (vi du streaming) se khong biet request that bai o phan con lai.
6. Set `response.ContentType = MediaTypeNames.Application.Json` (dong 44).
7. Dung `responseModel` bang `Result.FailSystem(message: exception.Message, statusCode: 500, metadata: BuildMetaHelper.Build(httpContext), serviceName: middleWareModel.ServiceName ?? CommonBaseConstant.System)` (dong 46-51) — tham so `message` la `string` nen loi goi nay khop overload `FailSystem(string message, ...)` (`Result.cs:162-174`), **khong phai** overload `FailSystem(List<string> messages, ...)` (`Result.cs:138-150`); tai day `Messages = [message]` tuc `[exception.Message]`, `Succeeded = false`, `Dispatched = false` (theo `Result.cs:166-173`).
8. `switch (exception)` (dong 53-83) — xem bang duoi.
9. `wrapperByCode = ResponseWrapperByCodeMapper.FromStatusCode(statusCode: (HttpStatusCode)responseModel.Code, sourceType: ErrorSourceType.General)` (dong 85-87) — **luon dung `ErrorSourceType.General`**, bat ke case nao o buoc 8.
10. `responseModel.Error = new ResultFTelCoreErrorModel { Code = wrapperByCode.Code, Retryable = wrapperByCode.Retryable }` (dong 89-93).
11. `responseModel.Messages` bi **ghi de** thanh `["Co su co xay ra vui long thu lai sau"]` **chi khi** `EnvironmentExtensions.GetEnvironment()` tra ve `EProd` hoac `EStag`; cac truong hop con lai (bao gom chuoi rong neu bien moi truong `ASPNETCORE_ENVIRONMENT` chua duoc set) giu nguyen `Messages` goc (chua `exception.Message`) (dong 95-102).
12. `await response.WriteAsJsonAsync(responseModel)` (dong 104).

**Bang map exception → HTTP status code** (`ExceptionHandlerMiddleWare.cs:53-83`)

| Loai exception | `response.StatusCode` | `responseModel.Code` | `responseModel.Status` |
|---|---|---|---|
| `UnauthorizedAccessException` | `403` (`HttpStatusCode.Forbidden`) | `403` | `"Forbidden"` (`nameof(HttpStatusCode.Forbidden)`) |
| `KeyNotFoundException` | `404` (`HttpStatusCode.NotFound`) | `404` | `"NotFound"` |
| `CustomException customerException` | `customerException.Code` | `customerException.Code` | `customerException.Code.ConvertHttpStatusCodeCodeByName()` (ten enum `HttpStatusCode` tuong ung, hoac `""` neu `Code` khong khop gia tri enum nao) |
| Moi exception khac (`default`) | `500` (`HttpStatusCode.InternalServerError`) | `500` | `"InternalServerError"` |

**Side effect**
- Ghi log `Error` (qua `logger.ErrorException`) cho moi exception.
- Ghi response HTTP (status code + JSON body) khi `Response.HasStarted` con `false`.
- Doc lai `httpContext.Request.Body` qua `ReadRequestBodyHelper.ReadAsync` (side effect tren stream — xem muc 2.7).

**Error handling** — Bat `Exception` (moi loai) tai 1 diem duy nhat; **khong rethrow trong bat ky nhanh nao**, ke ca nhanh `HasStarted` (buoc 5) — exception luon bi swallow sau khi log.

**Khi nao NEN dung** — Dang ky lam middleware ngoai cung (gan dau pipeline, "outer" nhat co the) de boc toan bo phan con lai, dam bao khong co exception nao roi ra ngoai duoi dang loi 500 mac dinh khong dinh dang cua ASP.NET Core.

**Khi nao KHONG dung** — Khi can phan biet loi 401 (`Unauthorized`) khoi 403 (`Forbidden`) mot cach tuong minh: middleware nay **khong co nhanh nao tra 401** — moi `UnauthorizedAccessException` deu thanh 403.

**Gioi han**
- Luon dung `ErrorSourceType.General` khi tra `ResponseWrapperByCodeMapper.FromStatusCode` (buoc 9) — xem muc 3, van de #1 (bang `CatalogsErrorCode.StatusMap` co cac entry `(401, Authentication)`/`(403, Authentication)` rieng nhung middleware nay khong bao gio dung `ErrorSourceType.Authentication` nen cac entry do khong bao gio duoc match tu middleware nay).
- O moi truong khong phai `Production`/`Staging` (bao gom ca truong hop bien `ASPNETCORE_ENVIRONMENT` chua duoc set — `GetEnvironment()` tra ve `string.Empty`), `responseModel.Messages` chua **nguyen van `exception.Message`** tra ve client — co the lo thong tin noi bo neu trien khai thieu bien moi truong nay.
- Thu tu dang ky middleware nay so voi `CorrelationIdMiddleWare`/`SerilogHandlerMiddleWare`/`MeasureExecutionTimeMiddleWare` phu thuoc `app.Use...` trong `Program.cs` cua service tieu thu, **khong the xac dinh tu source cua thu vien nay**.

### 2.3 MeasureExecutionTimeMiddleWare.Invoke

**Signature**
```csharp
public class MeasureExecutionTimeMiddleWare(RequestDelegate _next, ILogger<MeasureExecutionTimeMiddleWare> logger)
{
    public async Task Invoke(HttpContext httpContext)
}
```

**Muc dich** — Do thoi gian xu ly cua toan bo phan pipeline phia sau (`_next`), log canh bao khi response loi (status ≥ 400) hoac khi request chay cham (≥ 10 giay), va luon ghi 1 log `Response` mang latency cho moi request.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `httpContext` | `HttpContext` | Co | Khong validate null | Khong co |

**Output** — `Task` (void async). Khong tra du lieu; hieu ung la cac log entry.

**Dieu kien xu ly** (`MeasureExecutionTimeMiddleWare.cs:11-53`)

1. `start = Stopwatch.GetTimestamp()` truoc khi goi pipeline (dong 13).
2. `try { await _next(httpContext); } finally { ... }` (dong 15-52) — khoi do/log nam trong `finally`, **khong co `catch`**: neu `_next` nem exception, exception do van tiep tuc bay len sau khi doan `finally` chay xong (middleware nay **khong** nuot exception).
3. Trong `finally`: tinh `elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency` va `elapseds = elapsedMs / 1000.0` (dong 21-23).
4. Dung `message` (`StringBuilder`) chi gom `Method` + `Path` (dong 25-26).
5. Neu `httpContext.Response.StatusCode >= 400` (`HttpStatusCode.BadRequest`, dong 28): append `QueryString` (neu co) va `requestBody` (qua `ReadRequestBodyHelper.ReadAsync`, neu khong rong) vao `message` (dong 30-40), roi log `logger.Warning(..., message: $"[#SR{StatusCode}] Request took {elapsedMs} milliseconds for {message}")` (dong 42-43).
6. `else if (elapseds >= 10)` (dong 45): log `logger.Warning(..., message: $"[PERFORMANCE] Long Running Request took {elapsedMs} milliseconds for {message}")` (dong 47-48) — **`message` o nhanh nay chi co Method/Path, khong co QueryString/RequestBody** vi doan append o buoc 5 khong chay trong nhanh `else if` nay.
7. Hai nhanh 5 va 6 la `if`/`else if` — **loai tru lan nhau**: mot request vua loi (≥400) vua cham (≥10s) chi vao nhanh 5, khong bao gio sinh log `[PERFORMANCE]`.
8. Bat ke co roi vao nhanh 5/6 hay khong, luon goi `logger.Response(className: nameof(MeasureExecutionTimeMiddleWare), methodName: nameof(Invoke), latency: elapsedMs, message: message.ToString())` (dong 51) — log nay chay cho **moi** request.

**Side effect**
- Ghi log `Warning` co dieu kien (loi hoac cham).
- Ghi log `Response` (kem latency) cho **moi** request, khong dieu kien.
- Doc lai `httpContext.Request.Body` qua `ReadRequestBodyHelper.ReadAsync` (chi trong nhanh loi ≥400).

**Error handling** — Khong bat exception; `finally` dam bao log luon chay du `_next` co nem exception hay khong, nhung exception goc van tiep tuc lan len middleware ngoai (vi du `ExceptionHandlerMiddleWare`, tuy thu tu dang ky).

**Khi nao NEN dung** — Dang ky de theo doi latency/slow request theo chuan noi bo (nguong cung 10s), khong can code them o tung endpoint.

**Khi nao KHONG dung** — Khi can log body/query cho **moi** request cham bat ke status code — nhanh "cham nhung khong loi" (buoc 6) khong bao gom 2 thong tin nay.

**Gioi han**
- Nguong "chay cham" (`10` giay) la literal cung trong code, khong cau hinh duoc qua tham so/constructor (dong 45).
- `elapsedMs` tinh bang chia nguyen (`long`) truoc khi ra `elapseds` (`double`) — co sai so lam tron nho o mili-giay cuoi, khong anh huong dang ke toi nguong so sanh.
- Vi tri middleware nay trong pipeline (truoc/sau `ExceptionHandlerMiddleWare`) quyet dinh `httpContext.Response.StatusCode` doc duoc o buoc 5 la status "da duoc `ExceptionHandlerMiddleWare` chuan hoa" hay status goc luc exception chua bi xu ly — **khong xac dinh duoc tu source cua thu vien nay**, phu thuoc `Program.cs` cua service tieu thu.
- Doc lai request body o nhanh loi (buoc 5) chiu cung han che voi `ReadRequestBodyHelper.ReadAsync` (muc 2.7, muc 3) — co the tra rong neu body chua duoc buffering.

### 2.4 ResponseFTELCoreWrapperFilter.OnResultExecutionAsync

**Signature**
```csharp
public sealed class ResponseFTELCoreWrapperFilter(ResponseFTELCoreWrapperModel wrapperModel) : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(
        ResultExecutingContext context, ResultExecutionDelegate next)
}
```

**Muc dich** — Tu dong dien `System` va `Meta` (request id, trace id, timestamp) vao doi tuong `IResult` ma Controller tra ve, neu Controller chua tu gan `Meta`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `context` | `ResultExecutingContext` | Co | Khong validate null | Khong co |
| `next` | `ResultExecutionDelegate` | Co | Khong validate null | Khong co |
| `wrapperModel.ServiceName` | `string` (qua constructor `ResponseFTELCoreWrapperModel`) | Khong | `null` → fallback `CommonBaseConstant.System` (dong 14) | `null` |

**Output** — `Task` (void async). Hieu ung quan sat duoc la `context.Result` (khi thoa dieu kien) bi mutate tai cho (`result.System`, `result.Meta`) truoc khi buoc serialize response chay.

**Dieu kien xu ly** (`ResponseWrapperMidleWare.cs:9-20`)

1. Kiem tra pattern `context.Result is ObjectResult { Value: IResult result } _` — chi dung khi action result la `ObjectResult` (vi du tra ve tu `Ok(result)`/`return result;` trong controller co `[ApiController]`) **va** `ObjectResult.Value` implement `IResult` (tuc la `Result`/`Result<T>` cua thu vien nay).
2. Neu dieu kien tren dung **va** `result.Meta is null` (chua duoc set tu truoc) (dong 12): gan `result.System = wrapperModel?.ServiceName ?? CommonBaseConstant.System` (dong 14) va `result.Meta = BuildMetaHelper.Build(context.HttpContext)` (dong 16).
3. Neu dieu kien o buoc 1/2 sai (khong phai `ObjectResult`, `Value` khong phai `IResult`, hoac `Meta` da co gia tri) → **khong lam gi**, bo qua toan bo khoi `if`.
4. `await next()` (dong 19) — luon goi tiep filter chain, khong co nhanh short-circuit.

**Side effect** — Mutate truc tiep doi tuong `result` (tham chieu ben trong `context.Result`) khi dieu kien o buoc 2 dung; khong tao object moi, khong ghi log, khong I/O.

**Error handling** — Khong co `try/catch`; khong bat exception nao. Neu `BuildMetaHelper.Build` nem exception (thuc te `BuildMetaHelper.Build` khong nem — xem muc 2.6), exception se lan len MVC pipeline.

**Khi nao NEN dung** — Khi Controller tra ve `Result`/`Result<T>` va muon tu dong co `Meta`/`System` ma khong can goi tay o moi action.

**Khi nao KHONG dung** — Khi endpoint la Minimal API (khong qua MVC filter pipeline) hoac action result khong phai `ObjectResult` chua `IResult` — filter nay se khong co tac dung, va code goi phai tu gan `Meta`/`System`.

**Gioi han**
- Day la MVC `IAsyncResultFilter`, khong phai middleware — phai duoc dang ky qua co che MVC filter (vi du `options.Filters.Add<ResponseFTELCoreWrapperFilter>()` trong `AddControllers(...)`) cua service tieu thu; cach dang ky cu the **khong co trong source cua thu vien nay**.
- Khong ghi de neu Controller da tu gan `Meta` (du gia tri do co hop le hay khong) — khong co validate noi dung `Meta` da ton tai.
- Ten file (`ResponseWrapperMidleWare.cs`) thieu chu "d" so voi "MiddleWare" — khong nhat quan voi 4 file middleware con lai trong cung thu muc (xem muc 3).

### 2.5 SerilogHandlerMiddleWare.InvokeAsync

**Signature**
```csharp
public class SerilogHandlerMiddleWare(RequestDelegate next, SerilogHandlerMiddleWareModel middleWareModel)
{
    public async Task InvokeAsync(HttpContext context)
}
```

**Muc dich** — Thu thap user-agent, IP, role, username cua request hien tai va day vao Serilog `LogContext` duoi dang 4 `PropertyEnricher`, de moi log phat sinh trong qua trinh xu ly request deu tu dong mang cac thong tin nay.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `context` | `HttpContext` | Co | Khong validate null | Khong co |
| `middleWareModel.ServiceName` | `string` | Khong | Dung lam fallback user-agent khi khong doc duoc user-agent that (dong 24-25) | `null` → `CommonBaseConstant.Anonymous` |

**Output** — `Task` (void async).

**Dieu kien xu ly** (`SerilogHandlerMiddleWare.cs:13-89`)

1. **User-Agent** (dong 13-26): `userAgent = string.Empty` ban dau. Goi `ConvertHelpers.GetUserAgent(context)`; neu khac rong/whitespace → `userAgent = getUserAgent`. **Nguoc lai** → ghi header `context.Request.Headers[User-Agent] = middleWareModel?.ServiceName ?? CommonBaseConstant.Anonymous` — **nhung bien `userAgent` khong duoc cap nhat theo gia tri vua ghi vao header**, van giu `string.Empty` (xem muc 3, van de lien quan den buoc 5 ben duoi).
2. **IP address** (dong 28-41): tuong tu, `ipAddress = string.Empty` ban dau; neu `ConvertHelpers.GetClientIpAddress(context)` khac rong → dung gia tri do; nguoc lai → `ipAddress = context.Connection.RemoteIpAddress?.ToString()` (co the `null`) va ghi vao header `X-Forwarded-For`. O nhanh nay, bien `ipAddress` **co duoc** cap nhat dung theo gia tri moi.
3. **Role** (dong 52-63): mac dinh `roleName = CommonBaseConstant.Anonymous`, `roleDataName = RoleSR.ONLY_CREATE`. Doc `context.User.Claims` loc theo `ClaimTypesConstant.SRRoles` ("SR.SRRoles", so sanh khong phan biet hoa/thuong); neu co it nhat 1 role → `roleDataName = RoleDataConstant.GetRoleData(roles)` (map role string → `RoleSR` theo do uu tien dinh nghia trong `RoleDataConstant`) va `roleName = string.Join(DelimiterConstant.CHAR_COMMA, roles)`.
4. **Username** (dong 65-71): mac dinh `CommonBaseConstant.Anonymous`; neu `context.User.FindFirst(ClaimTypes.Name)?.Value` khac rong → dung gia tri do.
5. Dung 4 `PropertyEnricher` (dong 73-79): `Forwarded = ipAddress`, `User = username ?? CommonBaseConstant.Anonymous`, `UserAgent = userAgent ?? CommonBaseConstant.Anonymous`, `UserInfo = "[User: {username} - Roles: {roleName} - RoleData: {roleDataName}]"`.
6. `using (LogContext.Push(enrichers))` (dong 81): neu `context.Response.HasStarted` → `return` ngay, khong goi `next` (cung pattern voi `CorrelationIdMiddleWare`, muc 2.1 buoc 5); nguoc lai `await next(context)`.

**Side effect**
- Co the ghi header `User-Agent` (dong 23-25) va `X-Forwarded-For` (dong 40) vao **request** dang xu ly.
- Day 4 property vao Serilog `LogContext` cho toan bo scope request.
- Co the bo qua toan bo pipeline phia sau neu `Response.HasStarted` da `true`.

**Error handling** — Khong co `try/catch` trong toan ham; exception tu `next(context)` hoac tu cac buoc doc claim se lan thang ra ngoai, khong bi middleware nay xu ly.

**Khi nao NEN dung** — Dang ky sau middleware xac thuc (de `context.User` da co claims) nhung du som de log cua cac middleware/handler phia sau co day du property.

**Khi nao KHONG dung** — Khi can property `UserAgent` trong log phan anh dung gia tri da ghi vao header fallback — xem muc 3, vi co su sai khac giua header va log property trong truong hop khong doc duoc user-agent that.

**Gioi han**
- Doan code doc claim `SRPermissions` bi comment hoan toan (dong 43-50) — tinh nang log quyen han chi tiet theo permission (khac voi role) hien khong hoat dong.
- `context.User.Claims` duoc truy cap truc tiep (khong dung `?.` nhu doan code bi comment o tren) — chi an toan vi `HttpContext.User` mac dinh khong `null` trong ASP.NET Core (dong 56), nhung khong co validate tuong minh neu framework/pipeline tuy bien lam `User` hoac `Claims` tra ve `null`.
- Xem muc 3 de biet chi tiet sai khac giua `userAgent` (bien log) va header `User-Agent` (gia tri ghi vao request) trong nhanh fallback.

### 2.6 BuildMetaHelper.Build

**Signature**
```csharp
public class BuildMetaHelper
{
    public static ResultFTelCoreMetadataModel Build(HttpContext httpContext)
}
```

**Muc dich** — Dung doi tuong `ResultFTelCoreMetadataModel` (metadata chuan hoa gan vao moi response `IResult`) tu `HttpContext` hien tai: request id, trace id, timestamp.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `httpContext` | `HttpContext` | Co (theo signature) | Duoc truy cap qua `?.` o moi noi trong ham (`httpContext?.TraceIdentifier`) va trong `ResolveTraceId` (`httpContext?.Request?.Headers`) — **chiu duoc `httpContext == null`** | Khong co |

**Output** — `ResultFTelCoreMetadataModel` — luon tra ve mot instance moi (khong bao gio `null`), voi:
- `Request_Id`: `httpContext?.TraceIdentifier` — `null` neu `httpContext` la `null`.
- `Trace_Id`: ket qua `ResolveTraceId(httpContext)` — gia tri `X-Correlation-Id` tu **request header** neu co, nguoc lai `null`.
- `Timestamp`: `CommonBaseConstant.DateTimeUtc().ToString("o")` — luon co gia tri (khong `null`).

**Dieu kien xu ly** (`BuildMetaHelper.cs:7-34`)

1. `Build` goi truc tiep `ResolveTraceId` nhu mot ham con, khong co branching o cap `Build`.
2. `ResolveTraceId` (private, dong 17-34): trong `try`, neu `httpContext?.Request?.Headers is null` → tra `null`; nguoc lai `TryGetValue(HeaderConstant.CorrelationIdHeaderKey, out values)` → tra `values.FirstOrDefault()` neu thanh cong, `null` neu khong tim thay header. Toan bo nam trong `try/catch` → `catch` tra `null` cho bat ky exception nao.

**Side effect** — Khong co (ham thuan, chi doc `httpContext`, khong mutate).

**Error handling** — `ResolveTraceId` tu bat moi exception noi bo (dong 19-33) va tra `null`; `Build` ban than khong co `try/catch` nhung goi cac phep toan an toan (`?.`), nen trong thuc te khong tu nem exception voi input hop le.

**Khi nao NEN dung** — Bat ky noi can dung `Meta` chuan cho response `IResult` (dang duoc goi tu `ExceptionHandlerMiddleWare` va `ResponseFTELCoreWrapperFilter`).

**Khi nao KHONG dung** — Khi can `Trace_Id` la W3C trace id thuc su (`Activity.Current?.TraceId`) — ham nay chi doc lai header `X-Correlation-Id` cua **request**, khong doc `Activity`.

**Gioi han**
- **Mau thuan giua comment va gia tri thuc te cua `Timestamp`**: field `ResultFTelCoreMetadataModel.Timestamp` duoc XML-doc la "ISO 8601 UTC timestamp luc tao response" (`IResult.cs:105-110`), nhung gia tri thuc te lay tu `CommonBaseConstant.DateTimeUtc()` — ham nay **khong tra UTC** ma tra `TimeProvider.System.GetUtcNow().DateTime.AddHours(7)` theo mac dinh `addHour = 7` (`CommonBaseConstant.cs:47-50`), roi format bang `"o"` tren mot `DateTime` co `Kind = Unspecified` (khong co hau to `Z`/offset). Ket qua la chuoi ISO 8601 **trong giong UTC nhung thuc chat da lech +7 gio**, khong mang thong tin timezone — client de hieu sai gio neu tin theo ten field/summary (`BuildMetaHelper.cs:13`).
- `Trace_Id` chi co gia tri neu request gui len header `X-Correlation-Id` — neu `CorrelationIdMiddleWare` (muc 2.1) chay **sau** diem goi `Build` trong pipeline, hoac khong duoc dang ky, `Trace_Id` gan nhu luon `null` vi middleware do moi la noi tu sinh gia tri nay khi thieu.
- **Mau thuan thu hai giua comment va code, chua tung duoc implement**: field `ResultFTelCoreMetadataModel.Trace_Id` duoc XML-doc la "ID trace cross-system (W3C `activity?.TraceId` hoac fallback x-correlation-id)" (`IResult.cs:98-103`), tuc comment mo ta 2 nguon du lieu; nhung `ResolveTraceId` (`BuildMetaHelper.cs:17-34`) **khong co bat ky dong nao** doc `Activity.Current` — toan bo ham chi `TryGetValue` tren `httpContext.Request.Headers[X-Correlation-Id]`. Nhanh "W3C `activity?.TraceId`" trong comment khong ton tai trong implementation hien tai cua `BuildMetaHelper`, khac voi `CorrelationIdMiddleWare` (muc 2.1) — noi duy nhat trong module thuc su co doc `Activity.Current?.TraceId` (nhung chi dung de sinh `correlationId` ban dau, khong lien quan toi `Trace_Id` trong `Meta`).

### 2.7 ReadRequestBodyHelper.ReadAsync

**Signature**
```csharp
public class ReadRequestBodyHelper
{
    public static async Task<string> ReadAsync(HttpContext httpContext)
}
```

**Muc dich** — Doc lai noi dung `httpContext.Request.Body` thanh chuoi de phuc vu log loi (`ExceptionHandlerMiddleWare`) hoac log request cham/loi (`MeasureExecutionTimeMiddleWare`), co gioi han kich thuoc de tranh log qua lon.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `httpContext` | `HttpContext` | Co | Khong validate null tuong minh truoc khi truy cap `httpContext.Response`/`httpContext.Request` — neu `null` se roi vao `catch` chung (dong 64-67) va tra `string.Empty` | Khong co |

**Output** — `Task<string>`. Cac gia tri co the tra ve va y nghia:

| Gia tri tra ve | Khi nao |
|---|---|
| `string.Empty` | `httpContext.Response.HasStarted == true` (dong 14-17); hoac `Request.Body.CanSeek == false` (dong 24-27); hoac co exception bat ky trong `try` (dong 64-67) |
| `"File upload"` | `Request.ContentType` chua `"multipart/form-data"` (dong 19-22) — **khong doc noi dung that** cua multipart body |
| `"Body too large"` | Tong byte doc duoc vuot `MaxSizeContent` (5MB = `5 * 1024 * 1024`, dong 10, kiem tra tai dong 45-50) |
| Chuoi UTF-8 cua body | Cac truong hop con lai — `Encoding.UTF8.GetString(boundedBody.ToArray())` (dong 62) |

**Dieu kien xu ly** (`ReadRequestBodyHelper.cs:12-67`, theo thu tu)

1. Neu `httpContext.Response.HasStarted` → tra `string.Empty` ngay (dong 14-17).
2. Neu `Request.ContentType` chua `"multipart/form-data"` → tra `"File upload"` (dong 19-22), bo qua toan bo phan doc stream — tranh buffer body multipart (co the chua file lon) vao memory.
3. Neu `!Request.Body.CanSeek` → tra `string.Empty` (dong 24-27) — **day la dieu kien quan trong**: ham nay **khong tu bat buffering**, chi kiem tra xem stream co `CanSeek` san hay chua.
4. `Request.Body.Position = 0` (dong 29) — yeu cau stream phai seek duoc (da qua buoc 3).
5. Doc tuan tu vao `MemoryStream boundedBody` theo buffer 81920 byte/lan (dong 33, 41-53); neu `totalBytesRead > MaxSizeContent` → dat `tooLarge = true` va `break` (dong 45-50), dung doc them.
6. `Request.Body.Position = 0` lai lan nua sau khi doc xong (dong 55) — dua stream ve dau de co the doc lai lan nua o noi khac (neu co).
7. Neu `tooLarge` → tra `"Body too large"` (dong 57-60); nguoc lai tra `Encoding.UTF8.GetString(boundedBody.ToArray())` (dong 62).
8. Toan bo buoc 1-7 nam trong `try`; `catch` (khong typed, bat moi `Exception`) tra `string.Empty` (dong 64-67).

**Side effect**
- Doc va di chuyen `Position` cua `httpContext.Request.Body` (2 lan set `Position = 0` — truoc va sau khi doc).
- Cap phat `MemoryStream` tam va buffer `byte[81920]` trong moi lan goi — voi body hop le toi da 5MB, toan bo 5MB do duoc giu trong memory (`boundedBody`) truoc khi convert sang `string` (them mot ban copy nua qua `ToArray()` + `GetString()`) — voi nhieu request dong thoi bi loi/cham, co the tao ap luc cap phat bo nho tam thoi dang ke.

**Error handling** — `catch` khong phan loai, nuot moi exception (bao gom `ObjectDisposedException` neu stream da bi dispose, loi I/O, v.v.) va tra `string.Empty` — khong log, khong phan biet duoc nguyen nhan loi tu gia tri tra ve.

**Khi nao NEN dung** — Trong cac nhanh log loi/cham (da duoc 2 middleware goi) khi can dua noi dung body vao message log, voi rang buoc body khong phai multipart va khong vuot 5MB.

**Khi nao KHONG dung** — Khi can dam bao luon doc duoc body (bat ke `CanSeek`) — ham nay **khong tu** goi `httpContext.Request.EnableBuffering()`; neu ung dung tieu thu (`Program.cs`) khong tu bat buffering tu truoc khi body duoc doc boi model binding/handler, `CanSeek` se la `false` tai thoi diem 2 middleware nay goi ham (sau khi `_next` da chay xong) va ham **luon tra `string.Empty`** mot cach im lang, khong co cach nao phan biet voi truong hop body thuc su rong.

**Gioi han**
- Khong tu goi `EnableBuffering()` — phu thuoc hoan toan vao cau hinh cua service tieu thu; da tim trong toan bo `FTELSRCore.Shared` (`grep EnableBuffering`) va **khong co loi goi nao** trong thu vien nay.
- Nguong 5MB (`MaxSizeContent`) la `const long` cung trong ham, khong cau hinh duoc tu ben ngoai.
- Vi ca 2 noi goi ham nay (`ExceptionHandlerMiddleWare`, `MeasureExecutionTimeMiddleWare`) deu goi **sau** `await _next(httpContext)` da hoan tat (thanh cong hoac nem exception), neu handler/model-binder phia trong pipeline da doc het stream non-seekable truoc do ma khong bat buffering, ham nay khong con cach nao doc lai duoc — im lang tra rong, khong co log canh bao ve viec "khong doc duoc body vi thieu buffering".
- Khong doc body cho request multipart (tra co dinh `"File upload"`) du client gui field text nho kem 1 file — khong co cach log rieng phan field text trong truong hop nay.

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `ExceptionHandlerMiddleWare` luon goi `ResponseWrapperByCodeMapper.FromStatusCode(..., sourceType: ErrorSourceType.General)` bat ke loai exception nao, trong khi bang `CatalogsErrorCode.StatusMap` co cac entry rieng cho `(401, Authentication)` va `(403, Authentication)` | `ExceptionHandlerMiddleWare.cs:85-87` so voi `CatalogsErorrCode.cs` (cac dong khai bao `[(401, ErrorSourceType.Authentication)]`, `[(403, ErrorSourceType.Authentication)]`) | Trung binh-Cao. Response 403 (`UnauthorizedAccessException`) qua middleware nay **khong bao gio** map trung entry `Forbidden`/`Unauthorized` chuyen biet theo `Authentication`; luon roi vao `FromStatusCodeDefault` (`ResponseWrapperByCodeMapper.cs:16-27`), sinh ma loi generic dang `SYS_Forbidden` thay vi ma catalog da dinh nghia san cho nhom loi xac thuc |
| 2 | Field `ResultFTelCoreMetadataModel.Timestamp` duoc XML-doc la "ISO 8601 UTC timestamp" nhung gia tri thuc te do `BuildMetaHelper.Build` gan lai lay tu `CommonBaseConstant.DateTimeUtc()` — ham nay cong them 7 gio theo mac dinh (`addHour = 7`) va khong giu `Kind=Utc`, nen chuoi `"o"` xuat ra khong co hau to `Z`/offset | `BuildMetaHelper.cs:13`; `IResult.cs:105-110` (comment); `CommonBaseConstant.cs:47-50` (dinh nghia `DateTimeUtc`) | Trung binh. Client/consumer tin theo ten field va comment se hieu sai gio he thong lech 7 gio so voi UTC that, de gay sai lech khi so sanh log/metadata giua cac he thong dung UTC chuan |
| 3 | `SerilogHandlerMiddleWare`: khi khong doc duoc user-agent that, code ghi gia tri fallback (`ServiceName`/`Anonymous`) vao **request header** `User-Agent`, nhung bien cuc bo `userAgent` dung de tao `PropertyEnricher` khong duoc cap nhat theo gia tri do — van giu `string.Empty` | `SerilogHandlerMiddleWare.cs:13-26` (bien `userAgent` khong duoc set trong nhanh `else`) so voi dong 77 (`new PropertyEnricher(SerilogConstant.UserAgentPropertyName, userAgent ?? CommonBaseConstant.Anonymous)`) | Thap-Trung binh. Log property `UserAgent` ghi ra chuoi rong (khong phai `null` nen `?? CommonBaseConstant.Anonymous` khong kich hoat) trong khi header thuc te cua request da duoc set thanh `ServiceName`/`Anonymous` — hai nguon du lieu (header vs. log) khong khop nhau cho cung 1 request |
| 4 | `CorrelationIdMiddleWare` va `SerilogHandlerMiddleWare` deu co guard "neu `Response.HasStarted` thi `return` truoc khi goi `next`/`_next`" ngay ben trong `using (LogContext...)` | `CorrelationIdMiddleWare.cs:45-48`; `SerilogHandlerMiddleWare.cs:83-86` | Thong tin/Thap. Neu mot middleware nao do phia truoc 2 middleware nay (theo thu tu dang ky trong `Program.cs` cua service tieu thu) da flush response, toan bo pipeline phia sau (bao gom `ExceptionHandlerMiddleWare`, `MeasureExecutionTimeMiddleWare`, MVC action) se bi bo qua hoan toan cho request do — hanh vi dung theo code nhung phu thuoc thu tu dang ky ma thu vien nay khong kiem soat duoc |
| 5 | `ReadRequestBodyHelper.ReadAsync` khong tu goi `HttpRequest.EnableBuffering()` va cung khong log khi `CanSeek` la `false` | `ReadRequestBodyHelper.cs:24-27`; xac nhan khong co loi goi `EnableBuffering` nao trong toan bo `FTELSRCore.Shared` | Cao doi voi kha nang quan sat (observability). Neu service tieu thu khong tu bat buffering truoc khi body duoc doc boi model binder, ca `ExceptionHandlerMiddleWare` va `MeasureExecutionTimeMiddleWare` se luon nhan `requestBody = string.Empty` mot cach im lang khi log loi/cham — khong co cach phan biet voi truong hop body thuc su rong, va khong co canh bao nao cho biet nguyen nhan |
| 6 | `MeasureExecutionTimeMiddleWare`: nhanh log "chay cham" (`elapseds >= 10`, khong loi) khong dinh kem `QueryString`/`RequestBody` vao message, khac voi nhanh loi (`StatusCode >= 400`) | `MeasureExecutionTimeMiddleWare.cs:28-49` (doan append `QueryString`/`RequestBody` chi nam trong `if`, khong co trong `else if`) | Thap. Log `[PERFORMANCE]` cho request cham nhung thanh cong thieu chi tiet query/body de debug nguyen nhan cham, khac biet hanh vi so voi log `[#SR{code}]` |
| 7 | Ten file `ResponseWrapperMidleWare.cs` thieu chu "d" ("Midle" thay vi "Middle"), khong nhat quan voi 4 file con lai trong cung thu muc (`CorrelationIdMiddleWare.cs`, `ExceptionHandlerMiddleWare.cs`, `MeasureExecutionTimeMiddleWare.cs`, `SerilogHandlerMiddleWare.cs`); noi dung file cung khong chua middleware nao ma la 1 MVC `IAsyncResultFilter` | `ResponseWrapperMidleWare.cs` (ten file); `ResponseWrapperMidleWare.cs:7,9` (khong co `RequestDelegate`/`Invoke`) | Thong tin. Khong anh huong hanh vi runtime, nhung de gay nham lan khi tim kiem/tai lieu hoa theo quy uoc ten "MiddleWare" cua ca module |
| 8 | Doi chieu nguoc voi 8 file Knowledge Base hien co (`Utilizes-CallApiWithHttp.md`, `Utilizes-CallApi.md`, `Data-MongoDB-CoreMongoDB.md`, `Data-SQL-CoreSQL.md`, `Data-SQL-CoreSQL-TwoEntity.md`, `Data-SQL-UnitOfWork-DbContexts.md`, `Data-SQL-Dapper.md`, `Data-SQL-Resilience.md`): khong phat hien mo ta sai/thieu nao lien quan truc tiep den module nay. `CustomException` (kieu duy nhat trong danh sach doi chieu thuc su xuat hien trong source cua module nay, tai `ExceptionHandlerMiddleWare.cs:69-75`) duoc `Utilizes-CallApi.md`/`Utilizes-CallApiWithHttp.md` mo ta la "Exception noi bo mang `Code` (mac dinh 500)" — khop voi hanh vi doc `customerException.Code` trong middleware nay; cac kieu con lai trong danh sach doi chieu (`AuditModel`, `HttpOptionModel`, `ErrorModel`, `ProjectToExtensions`, `PrecateBuilderExtensions`, `MeasureExecutionTimeExtensions.InvokeForHTTP`, `MongoResiliencePolicyFactory`, `BaseEntityMongoDB`/`BaseEntitySQL`) khong xuat hien trong 7 file source cua module nay | `Utilizes-CallApi.md`, `Utilizes-CallApiWithHttp.md` (mo ta `CustomException`) | Thong tin. Khong co hanh dong sua nao can thuc hien tren cac file KB cu tai buoc nay |
| 9 | Field `ResultFTelCoreMetadataModel.Trace_Id` duoc XML-doc la "ID trace cross-system (W3C `activity?.TraceId` hoac fallback x-correlation-id)", ngu y co 2 nguon du lieu; nhung `BuildMetaHelper.ResolveTraceId` — ham duy nhat gan gia tri nay — **khong co bat ky dong nao** doc `Activity.Current`, chi doc header `X-Correlation-Id` cua request | `IResult.cs:98-103` (comment) so voi `BuildMetaHelper.cs:17-34` (`ResolveTraceId`, chi dung `httpContext.Request.Headers.TryGetValue`) | Thap-Trung binh. Nhanh "W3C trace id" trong comment chua tung duoc implement o `BuildMetaHelper`; team de hieu lam `Trace_Id` trong `Meta` phan anh `Activity`/W3C trace context that, trong khi no chi la echo lai `X-Correlation-Id` (do middleware khac — `CorrelationIdMiddleWare` — tu sinh khi thieu, xem muc 2.1) |
