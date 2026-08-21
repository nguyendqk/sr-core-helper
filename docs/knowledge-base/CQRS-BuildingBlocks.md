# CQRS Building Blocks & LoggingBehavior

> Nguon: `FTELSRCore.Shared/CQRS/BuildingBlocks/ICommand.cs`, `FTELSRCore.Shared/CQRS/BuildingBlocks/ICommandHandler.cs`, `FTELSRCore.Shared/CQRS/BuildingBlocks/IQuery.cs`, `FTELSRCore.Shared/CQRS/BuildingBlocks/IQueryHandler.cs`, `FTELSRCore.Shared/CQRS/Behaviors/LoggingBehavior.cs`
> Loai: 4 interface (marker/building block, khong co than) + 1 class (pipeline behavior, co logic)
> Cap nhat theo commit: `89c1ce9`

## 1. Tong quan

Module nay cung cap phan "khung" (building block) cho pattern CQRS dua tren thu vien **MediatR** (xac nhan qua `global using MediatR;` tai `FTELSRCore.Shared/GlobalUsing.cs:11` va `PackageReference Include="MediatR" Version="12.4.1"` trong `FTELSRCore.Shared/FTELSRCore.Shared.csproj`). Bon interface `ICommand<TResponse>`, `ICommandHandler<TCommand,TResponse>`, `IQuery<TResponse>`, `IQueryHandler<TQuery,TResponse>` chi la **alias/marker interface** khong co member rieng - chung dat them rang buoc generic (`notnull`) tren cac interface goc cua MediatR (`IRequest<TResponse>`, `IRequestHandler<TRequest,TResponse>`) de chuan hoa quy uoc dat ten Command/Query trong toan bo he thong. `LoggingBehavior<TRequest,TResponse>` la mot **MediatR pipeline behavior** (`IPipelineBehavior<TRequest,TResponse>`) - lop nam trong tang cross-cutting, chay bao quanh (wrap) moi handler khi request duoc gui qua `IMediator.Send()`, dam nhiem do thoi gian thuc thi va phat OpenTelemetry `Activity`.

Ca 5 kieu nay nam trong `FTELSRCore.Shared` (thu vien chia se), duoc thiet ke de cac service khac tham chieu va tu dang ky vao pipeline MediatR cua rieng minh. Trong pham vi repo `sr-core-helper` nay, khong tim thay bat ky lenh dang ky MediatR (`AddMediatR`, `AddTransient(typeof(IPipelineBehavior<,>), ...)`) hay bat ky class nao implement `ICommand`/`IQuery`/`ICommandHandler`/`IQueryHandler` - nghia la day la dinh nghia "hop dong" thuan tuy, viec su dung thuc te nam o cac service tieu thu, **khong xac dinh duoc tu source code trong repo nay**.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Chuan hoa 2 vai tro request trong CQRS: Command (`ICommand<TResponse>`) va Query (`IQuery<TResponse>`), ca hai deu la `IRequest<TResponse>` cua MediatR voi rang buoc `TResponse : notnull` (`ICommand.cs:3`, `IQuery.cs:3`) | Khong dinh nghia logic phan biet Command/Query (khong co code rieng nao xu ly khac nhau giua 2 loai - ve mat runtime, ca hai deu la `IRequest<TResponse>` thuan tuy) |
| Chuan hoa hop dong handler tuong ung: `ICommandHandler<TCommand,TResponse>` / `IQueryHandler<TQuery,TResponse>` deu la `IRequestHandler<TIn,TOut>` cua MediatR voi rang buoc `TCommand : ICommand<TResponse>` / `TQuery : IQuery<TResponse>` (`ICommandHandler.cs:3-5`, `IQueryHandler.cs:3-5`) | Khong redeclare method `Handle` - method nay ke thua nguyen tu `IRequestHandler<TIn,TOut>` cua package MediatR (ben ngoai repo), nen signature chinh xac **khong xac dinh duoc tu source code trong repo nay** |
| `LoggingBehavior` do thoi gian thuc thi cua handler tiep theo trong pipeline va ghi log canh bao khi vuot ngưỡng 5 giay (`LoggingBehavior.cs:30-31`, `MeasureExecutionTimeExtensions.cs:38-44`) | `LoggingBehavior` khong ghi log noi dung (payload) cua request/response - chi ghi 1 chuoi khoa (`measureByKey`) va so lieu latency (xem muc 2.5 va "Van de da biet") |
| `LoggingBehavior` tao 1 OpenTelemetry `Activity` span cho moi request, gan tag `mediatr.name`, danh dau `Ok`/`Error`, gan exception vao activity khi loi (`LoggingBehavior.cs:17-19,33,39-41`) | `LoggingBehavior` khong swallow loi - luon `throw;` nguyen ven exception tu handler (`LoggingBehavior.cs:43`); khong validate, khong transform, khong cache, khong retry |
| `LoggingBehavior` chi ap dung cho request/response ma `TResponse` implement `FTELSRCore.Wrappers.IResult` (`LoggingBehavior.cs:9`) | Khong ap dung cho MediatR request/response tuy y - request tra ve kieu khong ke thua `IResult` khong thoa rang buoc generic cua class nay |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `MediatR` (NuGet 12.4.1, qua `global using MediatR;`) | Cung cap `IRequest<TResponse>`, `IRequestHandler<TIn,TOut>`, `IPipelineBehavior<TRequest,TResponse>`, `RequestHandlerDelegate<TResponse>` - toan bo interface goc ma module nay mo rong/rang buoc them |
| `FTELSRCore.Wrappers.IResult` (`Wrappers/IResult.cs`) | Rang buoc generic bat buoc cho `TResponse` cua `LoggingBehavior` (`LoggingBehavior.cs:9`) - chi cho phep behavior nay chay voi cac request tra ve kieu ke thua chuan response wrapper cua FTELSRCore |
| `System.Diagnostics.ActivitySource` / `Activity` | Phat OpenTelemetry trace span cho moi request qua `LoggingBehavior` (`LoggingBehavior.cs:1,11,17`) |
| `FTELSRCore.Constants.OpenTelemetryConstant.LoggingBehaviorActivitySource` (`Constants/OpenTelemetryConstant.cs:10`, gia tri `"FTELSRCore.CQRS.Behaviors.LoggingBehavior"`) | Ten (Name) cua `ActivitySource` dung trong `LoggingBehavior` |
| `FTELSRCore.Extensions.MeasureExecutionTimeExtensions.InvokeForMediaR` (`Extensions/MeasureExecutionTimeExtensions.cs:18-54`) | Do thoi gian thuc thi `next()`, ghi log canh bao khi vuot `desiredTime`, ghi log info latency cho moi lan goi |
| `FTELSRCore.Extensions.Loggers.LoggerExtensions.Warning` / `.MediaRResult` (`Extensions/Loggers/LoggerExtensions.cs:254-266,495-498`) | Cac extension method ghi log thuc te duoc `InvokeForMediaR` goi ben trong |
| `Microsoft.Extensions.Logging.ILogger<LoggingBehavior<TRequest,TResponse>>` (DI) | Logger duoc tiem qua primary constructor (`LoggingBehavior.cs:5-6`) |
| `FTELSRCore.Infrastructure.Extensions.Helpers.OpenTelemetryExtensions.AddFTELSRTracing` / `AddFTELSRMetrics` (`Infrastructure/Extensions/Helpers/OpenTelemetryExtensions/OpenTelemetryExtensions.cs:16,54`) | Noi `OpenTelemetryConstant.LoggingBehaviorActivitySource` duoc dang ky vao `TracerProviderBuilder`/`MeterProviderBuilder` - xac nhan ActivitySource cua `LoggingBehavior` **CO** duoc dang ky (khac voi `SqlResiliencePolicyFactory` - xem muc 3) |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `ICommand<TResponse>` | Building block - marker interface | Danh dau mot request la Command; ke thua `IRequest<TResponse>` |
| `ICommandHandler<TCommand,TResponse>` | Building block - marker interface | Danh dau handler xu ly `ICommand<TResponse>`; ke thua `IRequestHandler<TCommand,TResponse>` |
| `IQuery<TResponse>` | Building block - marker interface | Danh dau mot request la Query; ke thua `IRequest<TResponse>` |
| `IQueryHandler<TQuery,TResponse>` | Building block - marker interface | Danh dau handler xu ly `IQuery<TResponse>`; ke thua `IRequestHandler<TQuery,TResponse>` |
| `LoggingBehavior<TRequest,TResponse>.Handle` | Pipeline behavior - method co logic | Bao quanh moi request MediatR (co `TResponse : IResult`) de do latency, ghi log va tao trace span |

## 2. Chi tiet API

### 2.1 `ICommand<TResponse>`

**Signature**
```csharp
public interface ICommand<out TResponse> : IRequest<TResponse> where TResponse : notnull;
```
(`ICommand.cs:3` - toan bo noi dung file, khai bao interface khong co than nho `;` thay cho `{ }`)

**Muc dich** - Danh dau mot kieu la "Command" trong CQRS: mot yeu cau thay doi trang thai he thong, gui qua MediatR va nhan lai `TResponse`.

**Thanh vien** - Khong co method/property nao duoc khai bao. Day la interface rong (marker), toan bo hanh vi (`Send`, dispatch, ...) thuoc ve `MediatR.IRequest<TResponse>` ma no ke thua - phan nay nam trong package MediatR, khong phai source cua repo nay.

**Rang buoc generic** - `TResponse` la covariant (`out`) va phai `notnull`.

**Input hop le / Output** - Khong ap dung (interface khong co method).

**Dieu kien xu ly / Side effect / Error handling** - Khong co (khong co than thi hanh).

**Khi nao NEN dung** - Khi dinh nghia mot lop request the hien hanh dong "ghi"/thay doi du lieu, muon duoc MediatR dispatch va (tuy chon) di qua cac `IPipelineBehavior<,>` nhu `LoggingBehavior`.

**Khi nao KHONG dung** - Cho cac yeu cau chi doc du lieu (dung `IQuery<TResponse>` thay the) - day chi la quy uoc dat ten/kien truc, `ICommand` va `IQuery` **giong nhau hoan toan ve mat runtime** (ca hai chi la `IRequest<TResponse> where TResponse : notnull`), khong co co che nao trong source code phan biet hanh vi doc/ghi giua chung.

**Gioi han** - Vi la marker interface rong, khong co gia tri kiem tra (validate) nao duoc ap dung boi chinh interface nay; viec phan tach Command/Query hoan toan phu thuoc quy uoc cua nguoi viet code, khong duoc cuong che boi compiler/runtime.

### 2.2 `ICommandHandler<TCommand, TResponse>`

**Signature**
```csharp
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
    where TResponse : notnull;
```
(`ICommandHandler.cs:3-5`)

**Muc dich** - Danh dau mot kieu la handler xu ly cho mot `ICommand<TResponse>` cu the.

**Thanh vien** - Khong co method/property tu khai bao them; ke thua nguyen `IRequestHandler<TCommand,TResponse>` cua MediatR (bao gom method `Handle`, khong duoc redeclare trong file nay nen signature chi tiet **khong xac dinh duoc tu source code trong repo nay**).

**Rang buoc generic** - `TCommand` la contravariant (`in`) va phai la `ICommand<TResponse>`; `TResponse` khong khai bao variance (invariant) va phai `notnull`.

**Cac muc con lai (Input/Output/Dieu kien xu ly/Side effect/Error handling)** - Khong ap dung, tuong tu 2.1.

**Khi nao NEN/KHONG dung** - Dung khi viet class handler cho mot Command; **khong** dung cho handler cua Query (dung `IQueryHandler` - nhung ve runtime hai interface nay tuong duong nhau, chi khac ten va rang buoc `TCommand : ICommand<TResponse>` so voi `TQuery : IQuery<TResponse>`).

**Gioi han** - Khong co.

### 2.3 `IQuery<TResponse>`

**Signature**
```csharp
public interface IQuery<out TResponse> : IRequest<TResponse> where TResponse : notnull;
```
(`IQuery.cs:3`)

Noi dung va cau truc **giong het** `ICommand<TResponse>` (muc 2.1), chi khac ten kieu. Xem muc 2.1 de biet chi tiet day du (muc dich, rang buoc, gioi han).

### 2.4 `IQueryHandler<TQuery, TResponse>`

**Signature**
```csharp
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
    where TResponse : notnull;
```
(`IQueryHandler.cs:3-5`)

Noi dung va cau truc **giong het** `ICommandHandler<TCommand,TResponse>` (muc 2.2), chi khac rang buoc `TQuery : IQuery<TResponse>` thay vi `TCommand : ICommand<TResponse>`. Xem muc 2.2 de biet chi tiet day du.

### 2.5 `LoggingBehavior<TRequest, TResponse>.Handle`

**Signature**
```csharp
public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
    where TResponse : notnull, IResult
{
    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
}
```
(`LoggingBehavior.cs:5-16`, class dung primary constructor de tiem `logger`)

**Muc dich** - La mot MediatR pipeline behavior chay bao quanh (wrap) buoc goi handler thuc te (`next()`) cho moi request co `TResponse` implement `FTELSRCore.Wrappers.IResult`. Cong viec: mo mot OpenTelemetry `Activity`, do thoi gian thuc thi, ghi log latency, va bao dam trang thai loi/thanh cong duoc phan anh vao `Activity`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `request` | `TRequest` (`notnull, IRequest<TResponse>`) | Co | Khong duoc doc gia tri/field cua `request` trong than ham - chi `typeof(TRequest).Name` (kieu, khong phai instance) duoc dung (`LoggingBehavior.cs:17,30`) | Khong co |
| `next` | `RequestHandlerDelegate<TResponse>` | Co | Duoc goi dung 1 lan, khong kiem tra null truoc khi goi (`LoggingBehavior.cs:28`) | Khong co |
| `cancellationToken` | `CancellationToken` | Co | Truyen tiep vao `InvokeForMediaR`, ham nay goi `cancellationToken.ThrowIfCancellationRequested()` 2 lan (truoc khi bat dau va truoc khi do gio - `MeasureExecutionTimeExtensions.cs:21,27`) | Khong co |

**Output** - `Task<TResponse>`: chinh xac gia tri ma `next()` (handler/behavior ke tiep trong pipeline) tra ve, **khong bi bien doi**. Khong co truong hop tra ve `null`/rieng biet cho "khong tim thay" - `LoggingBehavior` khong tu tao ra gia tri, chi truyen nguyen (pass-through) hoac nem lai exception.

**Dieu kien xu ly** (theo thu tu thuc thi trong `LoggingBehavior.cs:17-44`)
1. Tao `Activity` ten = `typeof(TRequest).Name`, kind `Internal`, tu `ActivitySource` tinh (static) cua lop dong `LoggingBehavior<TRequest,TResponse>` (dong 17). Neu khong co `ActivityListener` nao dang ky nghe nguon `"FTELSRCore.CQRS.Behaviors.LoggingBehavior"`, `StartActivity` tra ve `null`.
2. `activity?.SetTag("mediatr.name", $"CQRS [{typeof(TRequest).Name}]")` (dong 19) - bo qua neu `activity` la `null`.
3. Vao `try`: goi `MeasureExecutionTimeExtensions.InvokeForMediaR` voi `func` la `async () => await next()`, `measureByKey = $"{nameof(LoggingBehavior<TRequest,TResponse>)}_{typeof(TRequest).Name}"` (dong 23-31). Ben trong `InvokeForMediaR` (`MeasureExecutionTimeExtensions.cs:18-54`):
   - Do thoi gian bang `Stopwatch.GetTimestamp()` truoc/sau khi `await func()`.
   - Neu thoi gian (giay) > `desiredTime` (mac dinh 5, khong duoc `LoggingBehavior` truyen khac 5 - dong 31 dung mac dinh) -> `logger.Warning(...)` voi message `"[PERFORMANCE] Long Running Request [{measureByKey}] took {elapseds} seconds."`.
   - **Luon luon** (khong dieu kien) goi `logger.MediaRResult(className, methodName, latency: elapsedMs, message: measureByKey)` sau khi `func()` hoan tat - day la log info ghi cho MOI request, khong chi request cham.
4. Neu khong co exception: `activity?.SetStatus(ActivityStatusCode.Ok)` (dong 33) roi `return result` (dong 35).
5. Neu co exception (`catch (Exception exception)`, dong 37): `activity?.SetStatus(ActivityStatusCode.Error)` (dong 39), `activity?.AddException(exception)` (dong 41), roi `throw;` (dong 43) - nem lai nguyen ven exception goc, khong bat loai exception cu the nao, khong wrap.

**Side effect**
- Ghi log qua `ILogger` (thong qua `LoggerExtensions.Warning` khi cham, va `LoggerExtensions.MediaRResult` cho moi lan goi) - **khong ghi noi dung request/response**, chi ghi chuoi `measureByKey` (dang `"LoggingBehavior_<TenLopTRequest>"`, xem "Gioi han") va so `latency` (ms). `LoggerExtensions.MediaRResult` (`LoggerExtensions.cs:495-498`) truyen `message` (o day la `measureByKey`, mot `string`) thang vao delegate `LoggerMessage.Define`, **khong qua `JsonSerializer.Serialize`** nhu cac ham `Warning`/`Response`/`Info` khac trong cung file - vi vay khong co chi phi serialize payload lien quan den buoc log nay.
- Tao/dispose 1 `Activity` (OpenTelemetry) cho moi request - neu co `ActivityListener`/exporter dang lang nghe, day la ghi du lieu tracing ra ngoai (vd: OTLP collector).
- Khong ghi DB, khong goi API ngoai, khong mutate `request`/state chung nao khac ngoai logging/tracing noi tren.

**Error handling** - Bat `Exception` (moi loai, khong loc theo type cu the). Khi co loi: chi cap nhat `Activity` (`SetStatus(Error)` + `AddException`), **khong** goi bat ky ham log loi nao trong `LoggerExtensions` (khong `Error`, khong `ErrorException`) cho chinh exception nay, roi `throw;` nem lai nguyen ven (giu stack trace goc) de tang tren trong pipeline (hoac exception handling middleware cua ung dung) xu ly tiep.

**Khi nao NEN dung** - Khi mot service tieu thu `FTELSRCore.Shared` muon co san 1 lop do latency + tracing dong nhat cho toan bo request MediatR cua minh, VA cac request/handler cua service do tra ve kieu implement `FTELSRCore.Wrappers.IResult` (chuan response wrapper cua FTELSRCore). Can tu dang ky vao DI container cua service (vi du dang mo hinh `services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))`) - **khong tim thay dong dang ky nao trong repo `sr-core-helper` nay**, nen buoc dang ky cu the (thu tu behavior, scope, co ap dung toan cuc hay khong) **khong xac dinh duoc tu source code trong repo nay**.

**Khi nao KHONG dung** - Cho cac request/handler ma `TResponse` **khong** implement `IResult` (vi du tra ve DTO thuan, `bool`, kieu nguyen thuy...) - khong thoa rang buoc generic nen khong the dung `LoggingBehavior<TRequest,TResponse>` cho truong hop do; can tu viet behavior khac hoac dieu chinh response wrapper.

**Gioi han**
- `desiredTime` hardcode ngam dinh la 5 (giay) - `LoggingBehavior.cs:31` khong truyen tham so `desiredTime` khac, nen luon dung gia tri mac dinh cua `InvokeForMediaR` (`desiredTime = 5`, `MeasureExecutionTimeExtensions.cs:19`). Khong co cach cau hinh ngưỡng nay tu ben ngoai `LoggingBehavior`.
- `measureByKey` duoc tao tu `nameof(LoggingBehavior<TRequest, TResponse>)` - theo ngu phap C#, `nameof` tren mot kieu generic chi tra ve ten don gian khong kem tham so kieu (vi du `"LoggingBehavior"`), nen chuoi ket qua co dang `"LoggingBehavior_<TenNganCuaTRequest>"` (chi `typeof(TRequest).Name`, khong co namespace) - hai `TRequest` khac namespace nhung cung ten class ngan se cho ra **cung mot `measureByKey`** trong log, co the gay nham lan khi tra cuu log theo khoa nay.
- `ActivitySource` la field `private static readonly` cua lop generic `LoggingBehavior<TRequest, TResponse>` (`LoggingBehavior.cs:11`) - vi la field tinh cua mot **lop generic**, CLR tao 1 instance `ActivitySource` rieng cho MOI to hop `(TRequest, TResponse)` khac nhau duoc dung trong ung dung, du tat ca deu mang cung gia tri `Name` (`OpenTelemetryConstant.LoggingBehaviorActivitySource`). Anh huong runtime cu the (nhieu instance `ActivitySource` trung ten hoat dong voi 1 `ActivityListener` nhu the nao) **khong xac dinh duoc tu source code trong repo nay**.
- `activity` co the la `null` (khi khong co `ActivityListener` nghe nguon `"FTELSRCore.CQRS.Behaviors.LoggingBehavior"`) - moi loi goi `activity?.SetTag/SetStatus/AddException` se la no-op tham lang, khong bao loi, khong log thay the. Trong repo nay, `OpenTelemetryConstant.LoggingBehaviorActivitySource` **duoc dang ky** trong ca `AddFTELSRTracing` va `AddFTELSRMetrics` (`OpenTelemetryExtensions.cs:16,54`), nen neu service tieu thu goi cac ham nay, `activity` se khac `null`.
- Khong co co che retry/circuit breaker/timeout rieng trong `LoggingBehavior` - chi do va ghi log, moi xu ly loi/resilience khac (neu co) phai nam o layer khac.
- Khong log gia tri `request` hay `result` - phu hop cho hieu nang (tranh serialize payload lon) nhung dong nghia log sinh ra tu class nay **khong the dung de debug noi dung du lieu** cua request/response, chi dung de theo doi latency va co request nao dang chay/loi.

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `ICommand<TResponse>` va `IQuery<TResponse>` (tuong tu, `ICommandHandler` va `IQueryHandler`) giong nhau hoan toan ve mat runtime - ca hai chi la alias cho `IRequest<TResponse> where TResponse : notnull` (khac ten, khong khac hanh vi/rang buoc bo sung nao khac) | `ICommand.cs:3`, `IQuery.cs:3`, `ICommandHandler.cs:3-5`, `IQueryHandler.cs:3-5` | Viec phan loai Command/Query hoan toan la quy uoc dat ten cua nguoi viet code, khong duoc compiler/runtime cuong che; de bi dung sai (vd goi mot Query nhung dat ten class la `...Command`) ma khong co canh bao |
| 2 | Namespace thuc te cua ca 5 file la `FTELSRCore.CQRS.BuildingBlocks` / `FTELSRCore.CQRS.Behaviors` (khong co doan `.Shared`), trong khi project/thu muc vat ly la `FTELSRCore.Shared` | `ICommand.cs:1`, `LoggingBehavior.cs:3`, so voi ten project `FTELSRCore.Shared.csproj` | Chi la thong tin ve cau truc (khong phai loi logic) - can luu y khi tim kiem/them using, vi ten namespace khong khop 1-1 voi ten project/thu muc |
| 3 | `LoggingBehavior` khong ghi log bat ky thong tin loi nao qua `LoggerExtensions` (khong `Error`/`ErrorException`) khi handler nem exception - chi dua vao OpenTelemetry `Activity.AddException` | `LoggingBehavior.cs:37-43` | Neu he thong giam sat khong thu thap/hien thi OpenTelemetry trace (span exception), thong tin loi tu buoc nay se khong xuat hien trong bat ky log text/JSON nao do chinh `LoggingBehavior` tao ra |
| 4 | Khong tim thay code dang ky `LoggingBehavior<,>` vao pipeline MediatR (`AddTransient(typeof(IPipelineBehavior<,>), ...)`) hay `AddMediatR` trong repo `sr-core-helper` | Tim kiem toan repo (ngoai thu muc `.claude/worktrees`) khong co ket qua | Khong xac nhan duoc tu source code cach thuc/thu tu behavior nay duoc ap dung trong pipeline thuc te - phu thuoc hoan toan vao cau hinh cua tung service tieu thu `FTELSRCore.Shared` |
| 5 | `measureByKey` dung `typeof(TRequest).Name` (ten ngan, khong kem namespace) ket hop `nameof` cua kieu generic (khong kem tham so kieu) | `LoggingBehavior.cs:30` | Cac `TRequest` co cung ten class ngan nhung khac namespace se tao ra cung mot `measureByKey` trong log latency, co the gay nham lan khi loc/tra log theo khoa nay |
| 6 | Doi chieu voi 8 file Knowledge Base hien co (`Data-SQL-CoreSQL.md`, `Data-SQL-CoreSQL-TwoEntity.md`, `Data-SQL-UnitOfWork-DbContexts.md`, `Data-SQL-Dapper.md`, `Data-SQL-Resilience.md`, `Data-MongoDB-CoreMongoDB.md`, `Utilizes-CallApi.md`, `Utilizes-CallApiWithHttp.md`): khong file nao trong so nay dinh nghia hoac mo ta sai/thieu ve `ICommand`/`ICommandHandler`/`IQuery`/`IQueryHandler`/`LoggingBehavior` - cac kieu duoc liet ke can doi chieu (AuditModel, HttpOptionModel, ErrorModel, CustomException, ProjectToExtensions, PrecateBuilderExtensions, `MeasureExecutionTimeExtensions.InvokeForHTTP`, MongoResiliencePolicyFactory, BaseEntityMongoDB/BaseEntitySQL) khong xuat hien trong 5 file source cua module nay. Rieng `Data-SQL-Resilience.md` co de cap `LoggingBehaviorActivitySource` duoc dang ky trong `AddFTELSRTracing`/`AddFTELSRMetrics` - noi dung nay **khop** voi source code doc duoc trong module nay (`OpenTelemetryExtensions.cs:16,54`), khong phat hien sai lech | `Data-SQL-Resilience.md` (dong 261, 619) | Khong co hanh dong sua doi nao can thuc hien tren cac file KB cu tai buoc nay |
