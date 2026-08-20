# SqlResiliencePolicyFactory & ReadUncommittedConnectionInterceptor

> Nguon:
> - `FTELSRCore.Shared/Data/SQL/Helpers/Policies/SqlResiliencePolicyFactory.cs` (283 dong)
> - `FTELSRCore.Shared/Data/SQL/Helpers/ReadUncommittedConnectionInterceptor.cs` (33 dong)
>
> Loai:
> - `SqlResiliencePolicyFactory`: `public class` (khong phai `static class`, xem muc 4)
> - `ReadUncommittedConnectionInterceptor`: `public sealed class`, ke thua `DbConnectionInterceptor`
>
> Cap nhat theo commit: `2262829`

## 1. Tong quan

Module gom hai thanh phan doc lap thuoc tang truy cap du lieu SQL Server (namespace `FTELSRCore.Data.SQL.Helpers`).

`SqlResiliencePolicyFactory` la factory cau hinh **Polly v8 resilience pipeline** cho ket noi SQL Server. Class cung cap hai ham cau hinh tach biet: `ConfigureReadPolicy` (luong doc, chinh sach retry rong) va `ConfigureWritePolicy` (luong ghi, chinh sach retry hep). Moi pipeline gom dung hai strategy: **circuit breaker** (ben ngoai) va **retry** (ben trong).

`ReadUncommittedConnectionInterceptor` la EF Core `DbConnectionInterceptor` chay lenh `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED` ngay sau khi mot `DbConnection` duoc mo bang duong bat dong bo, danh doi tinh nhat quan du lieu (dirty read) de giam khoa (lock) tren luong truy van doc.

Ca hai class deu la thanh phan cua thu vien dung chung `FTELSRCore.Shared`. **Trong pham vi repository nay khong co bat ky doan code nao goi `ConfigureReadPolicy`/`ConfigureWritePolicy` hay dang ky `ReadUncommittedConnectionInterceptor`** (grep toan repo, tru thu muc `.claude/worktrees`, chi tim thay khai bao chu khong tim thay noi su dung). Viec dang ky thuoc trach nhiem cua ung dung tieu thu thu vien.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Cau hinh circuit breaker + retry cho pipeline SQL doc (`ConfigureReadPolicy`, dong 59-146) | Khong tao/tra ve `ResiliencePipeline`; chi mutate `ResiliencePipelineBuilder` duoc truyen vao (kieu tra ve `void`) |
| Cau hinh circuit breaker + retry cho pipeline SQL ghi (`ConfigureWritePolicy`, dong 154-213) | Khong doc cau hinh tu `appsettings`/`IConfiguration`/`IOptions`; toan bo tham so la hardcode |
| Phan loai exception co the retry theo danh sach 10 ma loi SQL Server (`RetryableSqlErrors`, dong 24-36) | Khong ho tro tuy bien danh sach ma loi tu ben ngoai (`private static readonly`, khong co setter/overload) |
| Phan loai exception muc connection/server theo 7 ma loi (`ConnectionLevelSqlErrors`, dong 42-51) | Khong phan biet ma loi theo `Class`/`State`/`Severity` cua `SqlException`; chi so sanh `sqlEx.Number` |
| Boc tach `SqlException` bi boc trong `InnerException` (`UnwrapSqlException`, dong 267-282) | Khong duyet `AggregateException.InnerExceptions` (chi duyet `InnerException`, dong 278) |
| Ghi log `Warning`/`Info` khi CB doi trang thai va khi retry | Khong ghi log khi exception bi bo qua (`ShouldHandle` tra ve `false`) |
| Tao OpenTelemetry `Activity` + `SetTag` cho cac su kien CB va retry cua **rieng** `ConfigureReadPolicy` | `ConfigureWritePolicy` **khong** tao `Activity` nao (dong 165-211 chi co logger) |
| Set `READ UNCOMMITTED` sau khi connection mo bat dong bo (`ConnectionOpenedAsync`, dong 23-31) | Khong override `ConnectionOpened` (ban dong bo) -> connection mo dong bo khong duoc set isolation level |
| — | Khong reset isolation level khi dong connection (khong override `ConnectionClosing`/`ConnectionClosed`/`ConnectionDisposing`) |
| — | Khong co timeout strategy, fallback strategy, hedging, hay rate limiter trong pipeline |
| — | Khong co `CircuitBreakerStateProvider`/`CircuitBreakerManualControl` -> khong the doc hay dieu khien trang thai CB tu ben ngoai |
| — | Khong dam bao idempotent: goi trung mot ham, hoac goi ca hai ham tren cung mot `builder`, se cong don strategy ma khong co guard nao chan (xem muc 4, #25) |
| — | Khong validate tham so dau vao; moi validate (`null`, `options` hop le, builder da `Build()` chua) do **Polly** thuc hien va nem loi (xem muc 4, #14, #26) |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `Polly` (v8.7.0) — `ResiliencePipelineBuilder` | Doi tuong duoc mutate trong ca hai ham `Configure*Policy` |
| `Polly.CircuitBreaker` — `CircuitBreakerStrategyOptions` | Cau hinh strategy circuit breaker (dong 63, 158) |
| `Polly.Retry` — `RetryStrategyOptions` | Cau hinh strategy retry (dong 120, 195) |
| `Polly` — `DelayBackoffType` | Enum dung tai dong 125, 200. Luu y: enum nay nam trong namespace `Polly` (`Polly.DelayBackoffType`, xac nhan tu XML doc Polly 8.7.0), **khong** thuoc `Polly.Retry`; no duoc nap qua `using Polly;` o dong 2 |
| `Microsoft.Data.SqlClient` (v7.0.2) — `SqlException` | Doc `sqlEx.Number` de phan loai loi (dong 230, 231, 255) |
| `System.Data.Common` — `DbException`, `DbConnection`, `DbCommand` | Phan loai loi non-SQL (dong 238); tao command trong interceptor (dong 28) |
| `System.Net.Sockets` — `SocketException` | Coi la retryable va connection-level (dong 234, 258) |
| `System.Diagnostics` — `ActivitySource`, `Activity`, `ActivityKind` | Tao span OpenTelemetry trong `ConfigureReadPolicy` (dong 18, 73, 90, 105, 129) |
| `OpenTelemetryConstant.SqlResilienceActivitySource` | Ten `ActivitySource` = `"FTELSRCore.Data.SQL.Helpers.Policies.SqlResiliencePolicyFactory"` (`Constants/OpenTelemetryConstant.cs:12`) |
| `Microsoft.Extensions.Logging.ILogger` (global using) | Tham so `logger`; cac method `Warning`/`Info` la extension method trong `Extensions/Loggers/LoggerExtensions.cs:254` va `:344` |
| `Microsoft.EntityFrameworkCore.Diagnostics` — `DbConnectionInterceptor`, `ConnectionEndEventData` | Base class + event data cua interceptor (`ReadUncommittedConnectionInterceptor.cs:12`, `:25`) |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `SqlResiliencePolicyFactory.ConfigureReadPolicy(ResiliencePipelineBuilder, ILogger)` | public static | Gan CB (60%/5req/10s, break 20s) + retry (3 lan, base 200ms) vao builder cho luong doc |
| `SqlResiliencePolicyFactory.ConfigureWritePolicy(ResiliencePipelineBuilder, ILogger)` | public static | Gan CB (50%/10req/15s, break 60s) + retry (1 lan, base 300ms) vao builder cho luong ghi |
| `SqlResiliencePolicyFactory.IsRetryable(Exception, bool)` | private static | Quyet dinh exception co retry hay khong; `handleAllTransient` doi che do rong/hep |
| `SqlResiliencePolicyFactory.IsConnectionLevel(Exception)` | private static | Quyet dinh exception co phai su co connection/server (dieu kien mo CB) |
| `SqlResiliencePolicyFactory.UnwrapSqlException(Exception)` | private static | Duyet chuoi `InnerException` tim `SqlException` dau tien, khong tim thay tra ve `null` |
| `SqlResiliencePolicyFactory.RetryableSqlErrors` | private static readonly field | `HashSet<int>` 10 ma loi SQL dung cho che do retry rong |
| `SqlResiliencePolicyFactory.ConnectionLevelSqlErrors` | private static readonly field | `HashSet<int>` 7 ma loi SQL dung cho CB va retry che do hep |
| `SqlResiliencePolicyFactory.ActivitySource` | private static readonly field | `ActivitySource` dung tao span, chi duoc dung trong `ConfigureReadPolicy` |
| `ReadUncommittedConnectionInterceptor.ConnectionOpenedAsync(DbConnection, ConnectionEndEventData, CancellationToken)` | public override | Chay `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED` tren connection vua mo |

---

## 2. SqlResiliencePolicyFactory

### 2.1 Bang so sanh cau hinh Read vs Write

Tat ca gia tri duoi day duoc doc truc tiep tu than ham, khong phai tu XML doc.

**Circuit breaker**

| Tham so | `ConfigureReadPolicy` | Dong | `ConfigureWritePolicy` | Dong |
|---|---|---|---|---|
| `ShouldHandle` | `IsConnectionLevel(ex)` | 65-66 | `IsConnectionLevel(ex)` | 160 |
| `FailureRatio` | `0.6` (60%) | 67 | `0.5` (50%) | 161 |
| `MinimumThroughput` | `5` | 68 | `10` | 162 |
| `SamplingDuration` | `TimeSpan.FromSeconds(10)` | 69 | `TimeSpan.FromSeconds(15)` | 164 |
| `BreakDuration` | `TimeSpan.FromSeconds(20)` | 70 | `TimeSpan.FromSeconds(60)` | 163 |
| `OnOpened` co tao `Activity` | Co — `"sql.circuit_breaker.open"` | 73 | **Khong** | 165-174 |
| `OnClosed` co tao `Activity` | Co — `"sql.circuit_breaker.closed"` | 90 | **Khong** | 175-183 |
| `OnHalfOpened` co tao `Activity` | Co — `"sql.circuit_breaker.half_open"` | 105 | **Khong** | 184-192 |
| Log level cua `OnOpened` | `Warning` | 80 | `Warning` | 167 |
| Log level cua `OnClosed` | `Warning` | 96 | `Warning` | 177 |
| Log level cua `OnHalfOpened` | `Warning` | 111 | **`Info`** | 186 |
| `BreakDurationGenerator` | Khong cau hinh | — | Khong cau hinh | — |
| `StateProvider` / `ManualControl` | Khong cau hinh | — | Khong cau hinh | — |

**Retry**

| Tham so | `ConfigureReadPolicy` | Dong | `ConfigureWritePolicy` | Dong |
|---|---|---|---|---|
| `ShouldHandle` | `IsRetryable(ex, true)` | 122 | `IsRetryable(ex, false)` | 197 |
| `MaxRetryAttempts` | `3` | 123 | `1` | 198 |
| `Delay` (base delay) | `TimeSpan.FromMilliseconds(200)` | 124 | `TimeSpan.FromMilliseconds(300)` | 199 |
| `BackoffType` | `DelayBackoffType.Exponential` | 125 | `DelayBackoffType.Exponential` | 200 |
| `UseJitter` | `true` | 126 | `true` | 201 |
| `MaxDelay` | Khong cau hinh | — | Khong cau hinh | — |
| `DelayGenerator` | Khong cau hinh | — | Khong cau hinh | — |
| `OnRetry` co tao `Activity` | Co — `"sql.retry"` | 129 | **Khong** | 202-211 |
| Log level cua `OnRetry` | `Warning` | 137 | `Warning` | 204 |
| Tong so lan goi toi da | 4 (1 lan dau + 3 retry) | 123 | 2 (1 lan dau + 1 retry) | 198 |

**Diem khac biet ban chat giua hai chinh sach**

| Khia canh | Read | Write |
|---|---|---|
| Tap loi duoc retry | `RetryableSqlErrors` (10 ma) + `SocketException` + `TimeoutException` + `DbException` khac `SqlException` | Chi `ConnectionLevelSqlErrors` (7 ma) + `SocketException` |
| Tolerance loi truoc khi mo CB | Cao hon (can 60% loi tren >=5 request/10s) | Thap hon (can 50% loi tren >=10 request/15s) |
| Thoi gian chan sau khi CB mo | 20 giay | 60 giay |
| Kha nang quan sat (tracing) | Co span OTel cho ca 4 callback | Khong co span nao |

### 2.2 Thu tu strategy trong pipeline

Trong Polly v8, strategy duoc `Add*` **truoc** la strategy **ngoai cung**. Ca hai ham deu goi `AddCircuitBreaker` truoc (`SqlResiliencePolicyFactory.cs:62`, `:157`) roi moi `AddRetry` (`:119`, `:194`).

Cau truc thuc thi:

```
CircuitBreaker (ngoai cung)
  └── Retry
        └── delegate goi SQL
```

Hau qua thuc te doc ra tu thu tu nay:

| Hanh vi | Giai thich |
|---|---|
| Circuit breaker chi ghi nhan **mot** outcome cho **moi lan chay pipeline** | Retry nam trong, no hap thu cac lan thu that bai va chi nem lai exception cuoi cung ra ngoai cho CB thay. Vi vay `MinimumThroughput = 5` nghia la 5 **lan goi pipeline**, khong phai 5 **lan thu (attempt)**. |
| Retry **khong** retry `BrokenCircuitException` | Khi CB dang `Open`, CB (ngoai cung) nem `BrokenCircuitException` truoc khi luong thuc thi di vao retry. Ngoai ra `IsRetryable` cung khong nhan dien `BrokenCircuitException` (khong phai `SqlException`/`SocketException`/`TimeoutException`/`DbException`). |
| CB mo cham hon so voi thu tu nguoc (retry ngoai, CB trong) | Moi lan goi pipeline tieu ton tron bo budget retry truoc khi dong gop 1 don vi vao cua so lay mau cua CB. Voi Read: toi da 4 lan goi DB moi 1 lan CB dem. |
| Tong do tre truoc khi CB dem 1 lan that bai (Read) | Tong delay retry ~1.4s **theo trung vi** (200ms/400ms/800ms) cong voi 4 lan cho SQL timeout/loi. Voi `UseJitter = true`, XML doc Polly 8.7.0 dinh nghia `Delay` la "median delay to target before the first retry" nen do tre thuc te duoc random hoa quanh cac moc nay va **co the vuot qua** chung; gia tri chinh xac **khong xac dinh duoc tu source code**. Xem muc "Gioi han" cua `ConfigureReadPolicy`. |

### 2.3 Bang ma loi SQL Server trong code

**`RetryableSqlErrors`** (`SqlResiliencePolicyFactory.cs:24-36`) — dung khi `handleAllTransient == true` (luong Read).

| Ma loi | Comment trong source | Co trong `ConnectionLevelSqlErrors`? |
|---|---|---|
| `-2` | Command timeout | Khong |
| `-1` | Connection broken | **Co** |
| `64` | Communication link failure | **Co** |
| `233` | Connection initialization error | **Co** |
| `1205` | Deadlock | Khong |
| `20000` | Instance not found | Khong |
| `40613` | Azure SQL database unavailable | **Co** |
| `49918` | Cannot process request, not enough resources | **Co** |
| `49919` | Cannot process create/update request | **Co** |
| `49920` | Cannot process request, too many operations | **Co** |

**`ConnectionLevelSqlErrors`** (`SqlResiliencePolicyFactory.cs:42-51`) — dung cho `ShouldHandle` cua circuit breaker (ca Read va Write) **va** cho retry cua luong Write.

| Ma loi | Comment trong source |
|---|---|
| `-1` | Connection broken |
| `64` | Communication link failure |
| `233` | Connection initialization error |
| `40613` | Azure SQL database unavailable |
| `49918` | Not enough resources |
| `49919` | Cannot process create/update request |
| `49920` | Too many operations |

> [!NOTE]
> Y nghia cua tung ma loi trong hai bang tren duoc lay tu **comment** trong source. Than ham chi thuc hien `HashSet<int>.Contains(sqlEx.Number)` (dong 230, 231, 255), khong chua logic nao xac nhan y nghia nghiep vu cua tung ma. **Khong xac dinh duoc tu source code** viec cac mo ta nay co khop voi tai lieu SQL Server hay khong.

> [!IMPORTANT]
> `ConnectionLevelSqlErrors` la tap con thuc su cua `RetryableSqlErrors`. Ba ma `-2` (command timeout), `1205` (deadlock) va `20000` (instance not found) **duoc retry o luong Read nhung khong bao gio lam mo circuit breaker** — ke ca khi chung xay ra lien tuc 100%.

---

### 2.4 ConfigureReadPolicy

**Signature**

```csharp
public static void ConfigureReadPolicy(ResiliencePipelineBuilder builder, ILogger logger)
```

**Muc dich** — Gan lien tiep hai strategy vao `builder`: mot `CircuitBreakerStrategyOptions` (dong 62-118) roi mot `RetryStrategyOptions` (dong 119-145), voi cau hinh khoan dung cao danh cho luong truy van doc.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `builder` | `ResiliencePipelineBuilder` | Co | **Khong co guard clause nao trong file nay.** `AddCircuitBreaker` (dong 62) va `AddRetry` (dong 119) la **extension method** cua Polly nen `builder == null` **khong** gay `NullReferenceException` tai cho goi; theo XML doc Polly 8.7.0, hai method nay nem `ArgumentNullException` khi `builder` hoac `options` la `null`, va `ValidationException` khi `options` khong hop le | Khong co |
| `logger` | `ILogger` (`Microsoft.Extensions.Logging`, global using tai `GlobalUsing.cs:2`) | Co | **Khong co guard clause nao**; duoc capture vao closure cua 4 callback. `logger.Warning` cung la extension method (`LoggerExtensions.cs:254`) nen khong nem tai cho goi (dong 80, 96, 111, 137); voi `logger == null`, `NullReferenceException` phat sinh ben trong delegate `LoggerMessage.Define` (`LoggerExtensions.cs:134-137`) khi callback duoc Polly goi | Khong co |

**Output** — Kieu tra ve `void`. Ham khong tra ve `builder`, khong tra ve `ResiliencePipeline`. Ket qua duy nhat quan sat duoc la trang thai cua `builder` truyen vao da co them hai strategy. Caller phai tu goi `builder.Build()`.

**Dieu kien xu ly** (theo thu tu thuc thi tai thoi diem **cau hinh**)

1. Dong 62: goi `AddCircuitBreaker` voi options duoc khoi tao inline.
2. Dong 119: goi `AddRetry` voi options duoc khoi tao inline. Khong co nhanh `if`/`switch`/`try` nao trong than ham.

**Dieu kien xu ly** (theo thu tu thuc thi tai thoi diem **runtime**, do cac lambda quyet dinh)

1. `ShouldHandle` cua CB (dong 65-66): `args.Outcome.Exception is { } ex && IsConnectionLevel(ex)`. Neu `Exception` la `null` (ket qua thanh cong) -> `false`. Neu khac `null` -> uy quyen cho `IsConnectionLevel`.
2. `ShouldHandle` cua retry (dong 122): `args.Outcome.Exception is { } ex && IsRetryable(ex, true)`. Truyen `handleAllTransient = true`.
3. `OnOpened`/`OnClosed`/`OnHalfOpened`/`OnRetry`: khong co nhanh dieu kien; luon goi `ActivitySource.StartActivity(...)`, luon ghi log, luon `return default` (`ValueTask` hoan thanh). Luu y `StartActivity` tra ve `null` khi khong co `ActivityListener` nao lang nghe `ActivitySource` nay — khi do cac `activity?.SetTag(...)` **bi bo qua** (toan tu `?.`), rieng log van duoc ghi.

**Side effect**

| Side effect | Vi tri | Chi tiet |
|---|---|---|
| Mutate tham so dau vao | `:62`, `:119` | Them 2 strategy vao `builder` (day la muc dich chinh cua ham, nhung ve ky thuat la mutate tham so) |
| Capture `logger` vao closure ton tai lau | `:80`, `:96`, `:111`, `:137` | 4 delegate giu tham chieu den `logger` suot vong doi pipeline duoc build tu `builder` |
| Tao OpenTelemetry `Activity` khi CB mo | `:73` | `ActivitySource.StartActivity("sql.circuit_breaker.open", ActivityKind.Internal)`, dat 4 tag: `db.system=mssql`, `resilience.state=open`, `resilience.type=circuit_breaker`, `resilience.break_duration_ms=<args.BreakDuration.TotalMilliseconds>` (dong 75-78) |
| Tao `Activity` khi CB dong | `:90` | `"sql.circuit_breaker.closed"`, 3 tag: `db.system=mssql`, `resilience.state=closed`, `resilience.type=circuit_breaker` (dong 92-94) |
| Tao `Activity` khi CB half-open | `:105` | `"sql.circuit_breaker.half_open"`, 3 tag: `db.system=mssql`, `resilience.state=half_open`, `resilience.type=circuit_breaker` (dong 107-109) |
| Tao `Activity` khi retry | `:129` | `"sql.retry"`, 5 tag: `db.system=mssql`, `retry.max_attempts=3` (hardcode literal `3`), `resilience.type=retry`, `retry.attempt=<args.AttemptNumber + 1>`, `retry.delay_ms=<args.RetryDelay.TotalMilliseconds>` (dong 131-135) |
| Ghi log `Warning` khi CB mo | `:80-84` | `className="SqlResiliencePolicyFactory"`, `methodName="ConfigureReadPolicy"`, kem `Exception`, message `"[CB OPEN] blocking DB for {N}s"` |
| Ghi log `Warning` khi CB dong | `:96-99` | Message `"[CB CLOSED] DB restored"`, **khong** kem exception |
| Ghi log `Warning` khi CB half-open | `:111-114` | Message `"[CB HALF-OPEN] probing DB"`, **khong** kem exception |
| Ghi log `Warning` khi retry | `:137-141` | Message `"[RETRY {n}/{3}] wait {ms}ms"`, kem `Exception`. Mau so `3` la literal hardcode (dong 141), khong lay tu `MaxRetryAttempts` |
| Chan truy cap DB | (hanh vi Polly) | Khi CB o trang thai `Open`, moi lan goi pipeline bi tu choi trong 20 giay |
| Tre luong thuc thi | (hanh vi Polly) | Retry cho theo `args.RetryDelay` truoc moi lan thu lai |

Khong ghi DB, khong goi API ngoai truc tiep trong than ham.

**Error handling**

- Than ham **khong co `try`/`catch` nao**.
- Ham khong bat va khong nem exception cua rieng no.
- Xu ly loi thuc chat duoc uy quyen cho Polly qua hai `ShouldHandle` (dong 65-66, 122) va cac callback.
- Neu retry can kiet 3 lan ma van loi, Polly nem lai exception goc len cho CB, roi len caller. **Khong co fallback**, khong co gia tri mac dinh tra ve.
- Cac callback tra ve `default` (`ValueTask` da hoan thanh) va khong bao gio nem exception trong code hien tai; nhung neu `logger` la `null` thi `logger.Warning(...)` (extension method) se nem `NullReferenceException` **tu ben trong callback cua Polly**.
- Cac lenh `using Activity activity = ...` (dong 73, 90, 105, 129) an toan voi `activity == null` vi `using` chap nhan `null` va moi truy cap sau do dung `activity?.`.

**Khi nao NEN dung**

- Cau hinh pipeline cho cac truy van **chi doc** (`SELECT`, `ReadDbContext`, Dapper query, report) — noi ma viec thu lai nhieu lan la an toan vi khong co side effect nghiep vu.
- Khi muon retry ca cac loi transient khong phai `SqlException`: `TimeoutException`, `DbException` khac (dong 238).
- Khi muon retry deadlock (`1205`) va command timeout (`-2`) — hai ma nay chi co trong `RetryableSqlErrors`.
- Khi can span OpenTelemetry cho su kien resilience (day la cau hinh **duy nhat** trong module co tracing).

**Khi nao KHONG dung**

- **Khong dung cho luong ghi.** Retry 3 lan tren mot operation co side effect (INSERT/UPDATE/DELETE, `SaveChangesAsync`) co the tao ban ghi trung lap khi loi xay ra sau khi SQL Server da commit nhung truoc khi client nhan phan hoi. Dung `ConfigureWritePolicy` (retry 1 lan, chi loi connection-level).
- **Khong dung cho stored procedure vua doc vua ghi.** Ham nay khong phan biet duoc; no phan loai theo ma loi, khong theo ban chat cua cau lenh.
- **Khong dung khi can budget do tre chat che.** Toi da 4 lan goi DB + ~1.4s delay retry (tong trung vi cua exponential 200/400/800ms; `UseJitter = true` lam gia tri thuc te dao dong va co the lon hon), cong voi `CommandTimeout` cua tung lan.
- **Khong dung khi can cau hinh dong theo moi truong.** Tat ca tham so hardcode; muon doi phai sua code va build lai.

**Gioi han**

| # | Gioi han | Dong |
|---|---|---|
| 1 | Toan bo 9 tham so (`FailureRatio`, `MinimumThroughput`, `SamplingDuration`, `BreakDuration`, `MaxRetryAttempts`, `Delay`, `BackoffType`, `UseJitter`, va tap ma loi) la **hardcode**, khong bind tu `IConfiguration`/`IOptions` | 67-70, 123-126, 24-36 |
| 2 | Khong co guard `ArgumentNullException.ThrowIfNull` cho `builder`/`logger`. Voi `builder == null`, loi `ArgumentNullException` do **Polly** nem (khong phai `NullReferenceException` do file nay); voi `logger == null`, loi bi tri hoan den luc callback chay | 59-61 |
| 3 | Con so `3` xuat hien 3 lan doc lap: `MaxRetryAttempts = 3` (123), tag `retry.max_attempts = 3` (132), va `{3}` trong message log (141). Sua `MaxRetryAttempts` khong tu dong dong bo hai cho con lai | 123, 132, 141 |
| 4 | `MaxDelay` khong duoc dat; mac dinh cua Polly la `null` = **khong cap tran** delay (XML doc Polly 8.7.0). Voi 3 lan retry, delay lon nhat ~800ms theo trung vi nen khong nghiem trong, nhung se thanh van de neu tang `MaxRetryAttempts` | 119-126 |
| 5 | Circuit breaker nam **ngoai** retry -> CB dem 1 don vi cho moi lan goi pipeline (khong phai moi attempt), lam CB phan ung cham hon | 62 va 119 |
| 6 | `-2`, `1205`, `20000` duoc retry nhung **khong bao gio** lam mo CB, vi `ShouldHandle` cua CB dung `IsConnectionLevel` (chi 7 ma) | 66, 42-51 |
| 7 | `ActivitySource` (`"FTELSRCore.Data.SQL.Helpers.Policies.SqlResiliencePolicyFactory"`) **khong duoc dang ky** trong `AddFTELSRTracing` cua repo (`Infrastructure/Extensions/Helpers/OpenTelemetryExtensions/OpenTelemetryExtensions.cs:14-16` chi `AddSource` cho `ServiceName`, `CoreCacheActivitySource`, `LoggingBehaviorActivitySource`). Neu khong co `ActivityListener` nao khac, `StartActivity` tra ve `null` va toan bo `SetTag` bi bo qua | 18; `OpenTelemetryExtensions.cs:14-16` |
| 8 | Su dung `logger.Warning` cho ca `OnClosed` va `OnHalfOpened` — hai su kien phuc hoi/tham do, khong phai canh bao | 96, 111 |
| 9 | Khong co timeout strategy trong pipeline -> phu thuoc hoan toan vao `CommandTimeout` cua ADO.NET/EF | 61-145 |
| 10 | Ham **khong idempotent** va khong kiem tra `builder` da co strategy hay chua: goi `ConfigureReadPolicy` hai lan, hoac goi ca `ConfigureReadPolicy` va `ConfigureWritePolicy` tren cung mot `builder`, se **cong don** strategy (2 CB + 2 retry long nhau) ma khong co guard nao chan | 61-145 |
| 11 | Neu `builder` da duoc dung de `Build()`, `AddCircuitBreaker`/`AddRetry` nem `InvalidOperationException` ("The builder cannot be modified after it has been used" — XML doc Polly 8.7.0); ham khong bat loi nay. Tuong tu, `options` khong hop le se cho `ValidationException` (cac gia tri hardcode hien tai deu hop le) | 62, 119 |
| 12 | `MaxRetryAttempts = 3` trung voi **gia tri mac dinh** cua `RetryStrategyOptions` trong Polly 8.7.0 -> dong 123 khong lam thay doi hanh vi so voi mac dinh, chi mang y nghia tuong minh | 123 |

---

### 2.5 ConfigureWritePolicy

**Signature**

```csharp
public static void ConfigureWritePolicy(ResiliencePipelineBuilder builder, ILogger logger)
```

**Muc dich** — Gan lien tiep mot `CircuitBreakerStrategyOptions` (dong 157-193) va mot `RetryStrategyOptions` (dong 194-212) vao `builder`, voi cau hinh **thu than hon** `ConfigureReadPolicy`: chi retry 1 lan va chi voi loi connection-level.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `builder` | `ResiliencePipelineBuilder` | Co | Khong co guard clause. `AddCircuitBreaker` (dong 157) va `AddRetry` (dong 194) la extension method cua Polly -> `builder == null` cho `ArgumentNullException` **do Polly nem**, khong phai `NullReferenceException` | Khong co |
| `logger` | `ILogger` | Co | Khong co guard clause; capture vao 4 closure. `logger.Warning`/`logger.Info` la extension method (`LoggerExtensions.cs:254`, `:344`) -> voi `logger == null`, `NullReferenceException` phat sinh ben trong delegate `LoggerMessage.Define` khi callback chay (dong 167, 177, 186, 204) | Khong co |

**Output** — `void`. Giong `ConfigureReadPolicy`: chi mutate `builder`, khong tra ve gi, caller phai tu `Build()`.

**Dieu kien xu ly**

1. Dong 157: `AddCircuitBreaker` (strategy ngoai cung).
2. Dong 194: `AddRetry` (strategy trong).
3. Runtime — `ShouldHandle` CB (dong 160): `IsConnectionLevel(ex)`.
4. Runtime — `ShouldHandle` retry (dong 197): `IsRetryable(ex, false)`. Chu y `handleAllTransient = false`, khac hoan toan `ConfigureReadPolicy`.
5. Khong co `if`/`switch`/`try` nao trong than ham.

**Side effect**

| Side effect | Vi tri | Chi tiet |
|---|---|---|
| Mutate tham so `builder` | `:156-157`, `:194` | Them 2 strategy |
| Capture `logger` vao 4 closure | `:167`, `:177`, `:186`, `:204` | Ton tai suot vong doi pipeline |
| Ghi log `Warning` khi CB mo | `:167-171` | `className="SqlResiliencePolicyFactory"`, `methodName="ConfigureWritePolicy"`, kem `Exception`, message `"[CB OPEN] blocking DB for {N}s"` |
| Ghi log `Warning` khi CB dong | `:177-180` | Message `"[CB CLOSED] DB restored"`, khong kem exception |
| Ghi log **`Info`** khi CB half-open | `:186-189` | Message `"[CB HALF-OPEN] probing DB"`. **Khac** `ConfigureReadPolicy` (dung `Warning`) |
| Ghi log `Warning` khi retry | `:204-208` | Message `"[RETRY {n}/{1}] wait {ms}ms"`, kem `Exception`. Mau so `1` la literal hardcode |
| **Khong** tao OpenTelemetry `Activity` | `:165-211` | Khong co dong `ActivitySource.StartActivity` nao trong ham nay. Field `ActivitySource` (dong 18) chi duoc `ConfigureReadPolicy` su dung |
| Chan truy cap DB | (hanh vi Polly) | Khi CB `Open`, moi lan goi pipeline bi tu choi trong **60 giay** |

Khong ghi DB, khong goi API ngoai truc tiep.

**Error handling**

- Khong co `try`/`catch`.
- Uy quyen hoan toan cho Polly qua `ShouldHandle` (dong 160, 197).
- Sau 1 lan retry that bai, exception goc duoc nem lai len CB roi len caller. Khong co fallback.
- Voi loi **khong** phai connection-level (vi du `1205` deadlock, `-2` timeout, `TimeoutException`, violation constraint, `DbUpdateConcurrencyException`), `IsRetryable(ex, false)` tra ve `false` -> **khong retry, khong log, exception nem thang len caller ngay lap tuc**.
- Rui ro `NullReferenceException` tu callback neu `logger == null` (giong `ConfigureReadPolicy`).

**Khi nao NEN dung**

- Cau hinh pipeline cho `SaveChangesAsync`, `INSERT`/`UPDATE`/`DELETE`, stored procedure co side effect.
- Khi khong duoc phep thu lai nhieu lan (rui ro double-write) nhung van muon chiu duoc mot cu mat ket noi nhat thoi.
- Khi muon chan luong ghi lau hon (60 giay) de DB co thoi gian phuc hoi truoc khi nhan tai ghi tro lai.
- `UnwrapSqlException` duoc thiet ke dac biet cho luong nay: EF Core boc loi ghi trong `DbUpdateException`, `SqlException` that nam o `InnerException` (comment dong 261-263, logic dong 269-279).

**Khi nao KHONG dung**

- **Khong dung cho luong doc.** Deadlock (`1205`) va command timeout (`-2`) **khong** duoc retry o cau hinh nay — day la hai loi transient pho bien nhat cua truy van doc nang.
- **Khong dung khi can tracing.** Cau hinh nay khong phat span OTel nao; chi co log.
- **Khong dung tren luong ghi tan suat rat thap.** `MinimumThroughput = 10` trong `SamplingDuration = 15s` nghia la can it nhat 10 lan goi pipeline trong 15 giay; neu luu luong ghi thap hon nguong nay thi **circuit breaker khong bao gio mo**, moi lan goi deu di truc tiep xuong DB dang loi.
- **Khong dung khi cham nhan operation idempotent.** Voi operation idempotent, retry 1 lan la qua bao thu; can pipeline rieng.

**Gioi han**

| # | Gioi han | Dong |
|---|---|---|
| 1 | Toan bo tham so hardcode, khong bind cau hinh | 161-164, 198-201 |
| 2 | Khong co guard null cho `builder`/`logger`. `builder == null` -> `ArgumentNullException` tu Polly; `logger == null` -> `NullReferenceException` bi tri hoan den luc callback chay | 154-157 |
| 3 | **Khong co bat ky OpenTelemetry `Activity` nao** — bat doi xung ro ret voi `ConfigureReadPolicy`, gay mat kha nang quan sat o dung luong quan trong nhat (luong ghi) | 165-211 |
| 4 | `MinimumThroughput = 10` + `SamplingDuration = 15s` la nguong kha cao; CB co the khong bao gio mo tren service co tan suat ghi thap | 162, 164 |
| 5 | `BreakDuration = 60s` — mot lan CB mo se chan toan bo luong ghi 1 phut; khong co `BreakDurationGenerator` de giam dan | 163 |
| 6 | `MaxRetryAttempts = 1` + `BackoffType = Exponential`: **exponential khong co tac dung** vi chi co mot delay duy nhat. Rieng `UseJitter = true` **van co tac dung** — no random hoa chinh delay duy nhat quanh trung vi 300ms, giup phi tuong quan cac caller dong thoi | 198-201 |
| 7 | Literal `1` trong message log (dong 208) khong dong bo tu dong voi `MaxRetryAttempts` (dong 198) | 198, 208 |
| 8 | Log `OnHalfOpened` o muc `Info` trong khi `ConfigureReadPolicy` dung `Warning` -> alert rule dua tren log level se bo sot su kien half-open cua luong ghi | 186 vs 111 |
| 9 | Khong co timeout strategy | 156-212 |
| 10 | Ham **khong idempotent**, khong kiem tra `builder` da co strategy hay chua -> goi nhieu lan se cong don strategy; khong co guard nao chan viec ap ca hai policy len cung mot `builder` | 156-212 |
| 11 | Neu `builder` da `Build()`, Polly nem `InvalidOperationException`; `options` khong hop le -> `ValidationException`. Ham khong bat ca hai truong hop | 157, 194 |
| 12 | `Delay = 300ms` chi la **trung vi** khi `UseJitter = true` (XML doc Polly 8.7.0), khong phai do tre co dinh; gia tri thuc te khong xac dinh duoc tu source code | 199, 201 |

---

### 2.6 IsRetryable

**Signature**

```csharp
private static bool IsRetryable(Exception ex, bool handleAllTransient)
```

**Muc dich** — Quyet dinh mot exception co duoc coi la "co the thu lai" hay khong, theo hai che do do `handleAllTransient` dieu khien.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `ex` | `Exception` | Co | Khong co `null` check truc tiep. Voi `ex == null`: `UnwrapSqlException(null)` tra ve `null` (vong `while` khong chay), sau do `null is SocketException` -> `false`, `null is TimeoutException` -> `false` -> ham tra ve `false`. **Khong nem exception.** | Khong co |
| `handleAllTransient` | `bool` | Co | Khong validate. `true` = che do rong (goi tu `ConfigureReadPolicy:122`), `false` = che do hep (goi tu `ConfigureWritePolicy:197`) | Khong co (khong phai optional parameter) |

**Output** — `bool`.

| Truong hop | Gia tri tra ve |
|---|---|
| Tim thay `SqlException` trong chuoi inner, `handleAllTransient == true`, `Number` thuoc `RetryableSqlErrors` | `true` |
| Tim thay `SqlException`, `handleAllTransient == true`, `Number` **khong** thuoc `RetryableSqlErrors` | `false` (tra ve ngay, khong xet tiep cac nhanh duoi) |
| Tim thay `SqlException`, `handleAllTransient == false`, `Number` thuoc `ConnectionLevelSqlErrors` | `true` |
| Tim thay `SqlException`, `handleAllTransient == false`, `Number` **khong** thuoc `ConnectionLevelSqlErrors` | `false` (tra ve ngay) |
| Khong tim thay `SqlException`, `ex` la `SocketException` | `true` (bat ke `handleAllTransient`) |
| Khong tim thay `SqlException`, khong la `SocketException`, `handleAllTransient == true`, `ex` la `TimeoutException` | `true` |
| Khong tim thay `SqlException`, khong la `SocketException`, `handleAllTransient == true`, `ex` la `DbException` (va khong la `SqlException`) | `true` |
| Khong tim thay `SqlException`, khong la `SocketException`, `handleAllTransient == true`, cac kieu khac | `false` |
| Khong tim thay `SqlException`, khong la `SocketException`, `handleAllTransient == false` | `false` |
| `ex == null` | `false` |

**Dieu kien xu ly** (dung thu tu thuc thi trong code)

1. Dong 227: `if (UnwrapSqlException(ex) is { } sqlEx)` — uu tien tuyet doi cho `SqlException`. Neu tim thay, dong 229-231 tra ve ket qua ngay va **thoat ham**.
2. Dong 234: `if (ex is SocketException) return true;` — chi cham toi khi khong tim thay `SqlException` nao trong ca chuoi. Kiem tra tren `ex` (exception ngoai cung), **khong** duyet chuoi inner.
3. Dong 236-239: `if (handleAllTransient)` -> `return ex is TimeoutException || (ex is DbException && ex is not SqlException);`
4. Dong 241: `return false;`

**Side effect** — Khong co. Ham thuan (pure): chi doc hai `HashSet` `static readonly` va kiem tra kieu. Khong ghi log, khong ghi DB, khong mutate tham so.

**Error handling** — Khong co `try`/`catch`. Khong nem exception (ke ca voi `ex == null`, xem bang Output). Khong ghi log khi tra ve `false` -> **cac exception bi loai bo khong de lai dau vet nao**.

**Khi nao NEN dung** — Chi duoc goi noi bo tu `ShouldHandle` cua hai retry strategy (dong 122, 197). Day la `private static` nen khong the goi tu ben ngoai class.

**Khi nao KHONG dung** — Khong dung nhu ham phan loai loi tong quat: no khong nhan dien loi timeout HTTP, loi Mongo, hay `OperationCanceledException`/`TaskCanceledException` (`TaskCanceledException` ke thua `OperationCanceledException`, khong ke thua `TimeoutException`, nen tra ve `false`).

**Gioi han**

| # | Gioi han | Dong |
|---|---|---|
| 1 | Neu `UnwrapSqlException` tim thay `SqlException` co `Number` khong nam trong tap tuong ung, ham tra ve `false` **ngay** — khong ha xuong kiem tra `SocketException`/`TimeoutException` nua. Mot `SqlException` boc `SocketException` ben trong se **khong** duoc retry | 227-232 |
| 2 | `ex is not SqlException` tai dong 238 la dieu kien **khong bao gio false**: neu `ex` la `SqlException` thi `UnwrapSqlException(ex)` da tra ve `ex` tai dong 227 va ham da thoat. Day la kiem tra du thua | 238 |
| 3 | Kiem tra `SocketException` (dong 234) chi ap dung cho exception ngoai cung, khong duyet `InnerException`. `SocketException` bi boc trong `IOException`/`AggregateException` se khong duoc nhan dien | 234 |
| 4 | Khong ho tro `AggregateException` nhieu nhanh (`UnwrapSqlException` chi di theo `InnerException`) | 278 |
| 5 | `handleAllTransient == true` chap nhan **moi** `DbException` khong phai `SqlException` la retryable — mot pham vi rat rong, gom ca loi cu phap/schema tu provider khac | 238 |
| 6 | Khong ghi log khi loai bo exception -> kho debug tai sao mot loi khong duoc retry | 225-242 |
| 7 | `OperationCanceledException`/`TaskCanceledException` khong duoc nhan dien tach biet; se roi vao nhanh `return false` | 236-241 |

---

### 2.7 IsConnectionLevel

**Signature**

```csharp
private static bool IsConnectionLevel(Exception ex)
```

**Muc dich** — Quyet dinh mot exception co phai su co o muc connection/server hay khong. Ket qua nay la dieu kien **duy nhat** de circuit breaker (ca Read va Write) ghi nhan mot lan that bai.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `ex` | `Exception` | Co | Khong co `null` check. Voi `ex == null`: `UnwrapSqlException(null)` tra ve `null`, roi `null is SocketException` -> `false`. Khong nem exception | Khong co |

**Output** — `bool`.

| Truong hop | Gia tri tra ve |
|---|---|
| Tim thay `SqlException` trong chuoi inner, `Number` thuoc `ConnectionLevelSqlErrors` (7 ma) | `true` |
| Tim thay `SqlException`, `Number` **khong** thuoc `ConnectionLevelSqlErrors` | `false` (thoat ngay, khong xet `SocketException`) |
| Khong tim thay `SqlException`, `ex` la `SocketException` | `true` |
| Khong tim thay `SqlException`, khong la `SocketException` | `false` |
| `ex == null` | `false` |

**Dieu kien xu ly**

1. Dong 253: `if (UnwrapSqlException(ex) is { } sqlEx)` -> dong 255: `return ConnectionLevelSqlErrors.Contains(sqlEx.Number);` (thoat ham).
2. Dong 258: `return ex is SocketException;`

**Side effect** — Khong co. Ham thuan.

**Error handling** — Khong co `try`/`catch`, khong nem exception, khong ghi log.

**Khi nao NEN dung** — Chi duoc goi noi bo tu `ShouldHandle` cua hai circuit breaker (dong 66, 160).

**Khi nao KHONG dung** — Khong dung de phan loai loi cho retry: `IsConnectionLevel` khong nhan dien `TimeoutException`, deadlock, hay command timeout.

**Gioi han**

| # | Gioi han | Dong |
|---|---|---|
| 1 | Neu tim thay `SqlException` co `Number` khong nam trong `ConnectionLevelSqlErrors`, ham tra ve `false` ngay — khong ha xuong nhanh `SocketException` | 253-256 |
| 2 | Kiem tra `SocketException` chi tren exception ngoai cung, khong duyet `InnerException` | 258 |
| 3 | Khong nhan dien `TimeoutException` la loi connection-level -> DB treo hoan toan (moi truy van timeout, khong nem ma loi ket noi) **khong lam mo circuit breaker** | 251-259 |
| 4 | Khong nhan dien loi pool can kiet (`InvalidOperationException: Timeout expired... pool`) la connection-level | 251-259 |

---

### 2.8 UnwrapSqlException

**Signature**

```csharp
private static SqlException UnwrapSqlException(Exception ex)
```

**Muc dich** — Duyet chuoi `InnerException` bat dau tu `ex` de tim `SqlException` dau tien. Comment trong source (dong 261-263) neu ly do: EF Core boc loi cua `SaveChangesAsync` trong `DbUpdateException`, `SqlException` that nam o `InnerException`; can boc tach de policy nhan dien dung loi connection-level. Than ham (dong 269-281) khop voi comment nay.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `ex` | `Exception` | Khong (chap nhan `null`) | Khong co `null` check tuong minh; `while (current is not null)` (dong 271) khien `null` khong vao vong lap | Khong co |

**Output** — `SqlException` hoac `null`.

| Truong hop | Gia tri tra ve |
|---|---|
| `ex` chinh la `SqlException` | Tra ve `ex` (dong 275) — khong duyet tiep |
| `ex` khong la `SqlException` nhung mot `InnerException` nao trong chuoi la `SqlException` | Tra ve `SqlException` **dau tien** tim thay theo huong ngoai vao trong |
| Khong co `SqlException` nao trong chuoi | `null` (dong 281) |
| `ex == null` | `null` (dong 281) |

Project dat `<Nullable>disable</Nullable>` (`FTELSRCore.Shared/FTELSRCore.Shared.csproj:6`) nen kieu tra ve khong duoc khai bao nullable du ham co the tra ve `null`.

**Dieu kien xu ly**

1. Dong 269: `Exception current = ex;`
2. Dong 271: `while (current is not null)` — thoat vong khi het chuoi.
3. Dong 273-276: `if (current is SqlException sqlException) return sqlException;`
4. Dong 278: `current = current.InnerException;`
5. Dong 281: `return null;`

**Side effect** — Khong co. Chi doc thuoc tinh `InnerException`.

**Error handling** — Khong co `try`/`catch`, khong nem exception, khong ghi log.

**Khi nao NEN dung** — Chi duoc goi noi bo tu `IsRetryable` (dong 227) va `IsConnectionLevel` (dong 253).

**Khi nao KHONG dung** — Khong dung khi can lay **tat ca** `SqlException` trong mot `AggregateException` nhieu nhanh; ham chi tra ve mot ket qua theo mot duong duy nhat.

**Gioi han**

| # | Gioi han | Dong |
|---|---|---|
| 1 | Chi duyet `InnerException` (mot nhanh). Voi `AggregateException`, `.InnerException` chi tra ve inner **dau tien**; cac `InnerExceptions` con lai bi bo qua hoan toan | 278 |
| 2 | Khong co gioi han so vong lap va khong co bao ve chong chuoi `InnerException` tu tham chieu (vong lap vo han neu ton tai chu trinh). Chuoi `InnerException` thong thuong khong co chu trinh, nhung code khong ngan chan | 271-279 |
| 3 | Tra ve `SqlException` dau tien (gan ngoai nhat). Neu chuoi co nhieu `SqlException` long nhau, ma loi cua cai trong cung bi bo qua | 273-276 |
| 4 | Kieu tra ve khong nullable-annotated du co the tra ve `null`; caller phai tu kiem tra (ca hai caller dung pattern `is { }` nen an toan) | 267, 281 |

---

## 3. ReadUncommittedConnectionInterceptor

> Nguon: `FTELSRCore.Shared/Data/SQL/Helpers/ReadUncommittedConnectionInterceptor.cs`
> Loai: `public sealed class`, ke thua `Microsoft.EntityFrameworkCore.Diagnostics.DbConnectionInterceptor`

Class chi co **mot** thanh vien duoc override, khong co field, khong co constructor tuong minh, khong co state.

### 3.1 ConnectionOpenedAsync

**Signature**

```csharp
public override async Task ConnectionOpenedAsync(
    DbConnection connection,
    ConnectionEndEventData eventData,
    CancellationToken cancellationToken = default)
```

**Muc dich** — Ngay sau khi EF Core mo mot `DbConnection` bang duong bat dong bo, tao mot `DbCommand` tren chinh connection do va thuc thi cau lenh `" SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED "` (dong 29-30). Muc tieu (theo XML doc dong 6-11): cho phep dirty read de tang thong luong truy van doc.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `connection` | `DbConnection` | Co (do EF Core truyen) | **Khong validate**; `connection.CreateCommand()` goi truc tiep tai dong 28 -> `NullReferenceException` neu `null`. Khong kiem tra `connection.State`, khong kiem tra provider co phai SQL Server hay khong | Khong co |
| `eventData` | `ConnectionEndEventData` | Co (do EF Core truyen) | **Khong duoc su dung o bat ky dong nao** trong than ham | Khong co |
| `cancellationToken` | `CancellationToken` | Khong | Khong kiem tra `IsCancellationRequested`; duoc truyen thang vao `ExecuteNonQueryAsync` (dong 30) | `default` |

**Output** — `Task` (khong co gia tri tra ve). `Task` hoan thanh sau khi `ExecuteNonQueryAsync` xong. Gia tri `int` tra ve tu `ExecuteNonQueryAsync` bi bo qua (dong 30 khong gan bien).

**Dieu kien xu ly** — **Khong co nhanh re nao.** Than ham la 3 lenh tuan tu, chay khong dieu kien:

1. Dong 28: `await using DbCommand cmd = connection.CreateCommand();`
2. Dong 29: `cmd.CommandText = " SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED ";` (luu y co dau cach o dau va cuoi chuoi)
3. Dong 30: `await cmd.ExecuteNonQueryAsync(cancellationToken);`
4. `await using` dispose `cmd` bat dong bo khi ra khoi scope.

Khong goi `base.ConnectionOpenedAsync(connection, eventData, cancellationToken)`.

**Side effect**

| Side effect | Vi tri | Chi tiet |
|---|---|---|
| Thuc thi lenh T-SQL tren DB | `:30` | Mot round-trip mang bo sung cho **moi** lan connection duoc mo qua duong async |
| Thay doi state cua doi tuong dung chung (`connection`) | `:29-30` | Doi isolation level cua **session** ung voi connection do. Anh huong den moi cau lenh chay sau tren cung connection, khong chi cau lenh cua DbContext hien tai |
| Tao va dispose `DbCommand` | `:28` | Tao mot `DbCommand` moi moi lan connection mo |
| Ghi log | — | **Khong co.** Ham khong nhan `ILogger` va khong goi bat ky API log nao |
| Tao OpenTelemetry `Activity` | — | **Khong co** |

Khong mutate `eventData`, khong ghi DB (nghiep vu), khong goi API ngoai.

**Error handling**

- Than ham **khong co `try`/`catch`**.
- Neu `ExecuteNonQueryAsync` that bai (mat ket noi, khong du quyen, provider khong hieu cu phap), exception **duoc nem ra ngoai** va lan truyen len EF Core, lam thao tac mo connection that bai -> truy van/`SaveChanges` that bai.
- Khong co co che degrade (vi du: neu set isolation level that bai thi van tiep tuc voi isolation level mac dinh).
- Neu `cancellationToken` bi cancel, `ExecuteNonQueryAsync` ket thuc bang loi huy; kieu exception cu the (`OperationCanceledException`/`TaskCanceledException`/`SqlException`) do provider quyet dinh — **khong xac dinh duoc tu source code** cua file nay.
- Khong ghi log truoc khi nem -> khong co dau vet chan doan tu chinh interceptor.

**Khi nao NEN dung**

- Gan vao `DbContextOptionsBuilder.AddInterceptors(...)` cua **rieng** DbContext doc (`ReadDbContext<TContext>`, xem `Data/SQL/DbContexts/Read/ReadDbContext.cs`) trong cac tinh huong:
  - Bao cao/dashboard/thong ke doc du lieu lich su, chap nhan sai lech nho.
  - Truy van danh sach/tim kiem tren bang co luu luong ghi cao, noi ma khoa doc (shared lock) gay blocking dang ke.
  - Tra cuu du lieu chi de hien thi, khong dung lam co so cho quyet dinh ghi.
- Khi doi tac nghiep vu da xac nhan **chap nhan dirty read**: doc duoc du lieu chua commit va co the bi rollback ve sau.

**Khi nao KHONG dung**

| Tinh huong | Ly do ky thuat |
|---|---|
| Bat ky truy van co ket qua duoc dung lam dieu kien cho mot phep ghi (read-then-write, kiem tra trung, kiem tra so du/ton kho) | `READ UNCOMMITTED` cho phep doc du lieu chua commit; du lieu doc duoc co the bien mat sau rollback -> quyet dinh ghi dua tren du lieu khong ton tai |
| Doi soat so lieu, bao cao tai chinh, tinh cong no, quyet toan | `READ UNCOMMITTED` khong bao dam tinh nhat quan; ngoai dirty read con co the doc thieu/doc trung dong khi trang chi muc (page split) xay ra dong thoi voi truy van |
| Gan vao `WriteDbContext<TContext>` hoac DbContext dung chung ca doc lan ghi | Interceptor doi isolation level cua session; moi transaction ghi tren connection do se chay o `READ UNCOMMITTED`, lam mat kha nang bao ve cua khoa |
| DbContext mo connection bang duong **dong bo** | Class chi override `ConnectionOpenedAsync` (dong 23), **khong** override `ConnectionOpened` (ban dong bo). Voi luong dong bo, isolation level khong duoc set va truy van chay o isolation level mac dinh -> hanh vi khac nhau giua duong sync va async |
| Ung dung yeu cau isolation level xac dinh, kiem soat duoc | Khong co doan code nao trong file reset isolation level ve mac dinh khi connection dong (khong override `ConnectionClosing`/`ConnectionClosed`/`ConnectionDisposing`) |
| Provider khong phai SQL Server | `CommandText` la cu phap T-SQL; khong co kiem tra provider tai dong 28-30 |

**Gioi han**

| # | Gioi han | Dong |
|---|---|---|
| 1 | Chi override `ConnectionOpenedAsync`; **khong** override `ConnectionOpened` (dong bo) -> hanh vi khong nhat quan giua duong sync va async | 23 |
| 2 | Khong co doan code nao dua isolation level tro lai mac dinh khi connection dong hoac duoc tra ve pool (khong co override nao khac trong file) | toan file (1-33) |
| 3 | Khong validate `connection` (null, `State`, loai provider) | 28 |
| 4 | `CommandText` la string hardcode cu phap T-SQL, khong the cau hinh muc isolation khac | 29 |
| 5 | Chuoi lenh co dau cach du o dau va cuoi: `" SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED "` | 29 |
| 6 | Them mot round-trip mang cho moi lan mo connection; voi ung dung mo/dong connection thuong xuyen, chi phi nay khong nho | 30 |
| 7 | Khong ghi log, khong tao span -> khong quan sat duoc interceptor da chay hay chua, cung khong biet khi no that bai | toan file |
| 8 | `eventData` duoc nhan nhung khong su dung -> khong the phan biet connection nao thuoc DbContext nao de ap dung co dieu kien | 25 |
| 9 | Khong goi `base.ConnectionOpenedAsync(...)`. `DbConnectionInterceptor` la "abstract base class for `IDbConnectionInterceptor` for use when implementing a subset of the interface methods" (XML doc EF Core Relational 9.0.18), cac hien thuc mac dinh la **no-op**, nen viec khong goi base **khong lam mat hanh vi nao quan sat duoc**. Day la lech chuan viet code, khong phai loi hanh vi | 23-31 |
| 10 | Khong co `try`/`catch`: loi khi set isolation level lam that bai ca thao tac mo connection | 28-30 |
| 11 | Khong dat `cmd.CommandTimeout`; lenh `SET TRANSACTION ISOLATION LEVEL` chay voi timeout mac dinh cua provider, khong the dieu chinh | 28-30 |
| 12 | Trong repository nay khong co doan code nao goi `AddInterceptors` de dang ky class (grep `AddInterceptors`/`AddDbContext`/`UseSqlServer` toan repo, tru `.claude/worktrees`, khong co ket qua). `ReadDbContext<TContext>` cung khong tu gan interceptor (`ReadDbContext.cs:12-32` chi co constructor va `OnModelCreating`) | — |

---

## 4. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `ActivitySource` cua module **khong duoc dang ky** trong `AddFTELSRTracing`. Ham nay chi `AddSource` cho `model.ServiceName`, `CoreCacheActivitySource`, `LoggingBehaviorActivitySource` | `SqlResiliencePolicyFactory.cs:18`; `Infrastructure/Extensions/Helpers/OpenTelemetryExtensions/OpenTelemetryExtensions.cs:14-16` | Neu ung dung khong tu `AddSource` ten `"FTELSRCore.Data.SQL.Helpers.Policies.SqlResiliencePolicyFactory"`, `StartActivity` tra ve `null` va toan bo `SetTag` (dong 75-78, 92-94, 107-109, 131-135) khong tao du lieu telemetry nao. Toan bo doan code tracing thanh vo hieu |
| 2 | `ConfigureWritePolicy` khong co bat ky `Activity`/`SetTag` nao, trong khi `ConfigureReadPolicy` co day du o ca 4 callback | `SqlResiliencePolicyFactory.cs:165-211` vs `:71-144` | Mat kha nang quan sat su kien resilience o luong ghi — luong nghiep vu quan trong hon. Dashboard/alert dua tren span se khong thay CB cua write mo/dong |
| 3 | Circuit breaker duoc `Add` **truoc** retry o ca hai ham -> CB la strategy ngoai cung, retry nam trong | `SqlResiliencePolicyFactory.cs:62` va `:119`; `:157` va `:194` | CB chi dem 1 outcome cho moi lan goi pipeline (khong phai moi attempt). Voi Read, can toi 4 lan goi DB that bai de CB dem 1 don vi -> CB phan ung cham hon dang ke so voi thu tu nguoc |
| 4 | `ConnectionLevelSqlErrors` la tap con thuc su cua `RetryableSqlErrors`; `-2`, `1205`, `20000` khong co trong tap connection-level | `SqlResiliencePolicyFactory.cs:24-36` vs `:42-51` | Truong hop DB qua tai gay command timeout (`-2`) hay deadlock (`1205`) lien tuc 100%: o luong Read moi lan goi pipeline van tieu ton tron bo 3 lan retry (tong 4 lan goi DB) ma **circuit breaker khong bao gio mo** -> khong co co che chan tai de DB phuc hoi. Luu y so lan retry **co gioi han** (3 lan/lan goi), khong phai retry vo han |
| 5 | `IsConnectionLevel` khong nhan dien `TimeoutException` | `SqlResiliencePolicyFactory.cs:251-259` | DB treo hoan toan (truy van timeout thay vi nem ma loi ket noi) khong lam mo circuit breaker o ca Read va Write |
| 6 | Dieu kien `ex is not SqlException` tai dong 238 la du thua, khong bao gio `false` khi luong thuc thi cham toi (vi `SqlException` da duoc `UnwrapSqlException` bat tai dong 227) | `SqlResiliencePolicyFactory.cs:238` | Khong gay loi hanh vi, nhung gay hieu nham la co truong hop `SqlException` di qua nhanh nay |
| 7 | Sau khi `UnwrapSqlException` tim thay `SqlException` co `Number` ngoai tap, ca `IsRetryable` va `IsConnectionLevel` tra ve `false` ngay — khong ha xuong nhanh `SocketException` | `SqlResiliencePolicyFactory.cs:227-232`, `:253-256` | `SqlException` boc `SocketException` (mat ket noi TCP duoc bao qua ma loi khong nam trong danh sach) se khong duoc retry va khong lam mo CB |
| 8 | `SocketException` chi duoc kiem tra tren exception ngoai cung, khong duyet chuoi `InnerException` | `SqlResiliencePolicyFactory.cs:234`, `:258` | `SocketException` bi boc trong `IOException`/`AggregateException` khong duoc nhan dien |
| 9 | `UnwrapSqlException` chi di theo `InnerException`, khong duyet `AggregateException.InnerExceptions` | `SqlResiliencePolicyFactory.cs:278` | Voi `AggregateException` nhieu nhanh (vi du tu `Task.WhenAll`), `SqlException` nam o nhanh thu hai tro di bi bo sot -> khong retry, khong mo CB |
| 10 | Toan bo tham so cau hinh la **hardcode** trong code; khong co doan code nao doc `IConfiguration`/`IOptions`/`appsettings` | `SqlResiliencePolicyFactory.cs:24-36`, `:42-51`, `:67-70`, `:123-126`, `:161-164`, `:198-201` | Khong the tinh chinh retry/CB theo moi truong (DEV/UAT/PROD) hay theo su co runtime; moi thay doi doi hoi sua code, build va deploy lai |
| 11 | So `3` (Read) va `1` (Write) duoc viet lap lai dang literal trong tag va message log, khong tham chieu `MaxRetryAttempts` | `:123` vs `:132` va `:141`; `:198` vs `:208` | Sua `MaxRetryAttempts` ma quen sua literal se tao log/telemetry sai lech (vi du log `"[RETRY 5/3]"`) |
| 12 | Log level khong nhat quan giua hai ham cho cung su kien half-open: Read dung `Warning`, Write dung `Info` | `:111` vs `:186` | Alert rule loc theo `LogLevel.Warning` se bat duoc su kien half-open cua Read nhung bo sot cua Write |
| 13 | `OnClosed` va `OnHalfOpened` dung `logger.Warning` (Read) cho su kien phuc hoi/tham do | `:96`, `:111` | Nhieu (noise) trong kenh canh bao; kho tach tin hieu su co that su |
| 14 | Khong co guard null cho `builder` va `logger` o ca hai ham public | `:59-61`, `:154-157` | `builder == null` -> `ArgumentNullException` **do chinh Polly nem** (`AddCircuitBreaker`/`AddRetry` la extension method co validate `builder`, xac nhan tu XML doc Polly 8.7.0), **khong** phai `NullReferenceException`; loi bung ra ngay tai thoi diem cau hinh. `logger == null` -> loi **bi tri hoan**: `logger.Warning`/`logger.Info` cung la extension method nen khong nem tai cho goi, `NullReferenceException` bung ra ben trong delegate `LoggerMessage.Define` (`LoggerExtensions.cs:134-137`, `:174-177`) khi Polly goi callback (CB doi trang thai hoac retry), rat kho chan doan |
| 15 | `SqlResiliencePolicyFactory` khai bao la `public class` du chi chua thanh vien `static` | `SqlResiliencePolicyFactory.cs:16` | Class co constructor mac dinh public, co the `new SqlResiliencePolicyFactory()` mot cach vo nghia; compiler khong bao ve khoi viec them instance member sau nay. Nen la `static class` |
| 16 | XML doc cua `ConfigureReadPolicy` liet ke `<param name="logger">` truoc `<param name="builder">` trong khi signature la `(builder, logger)` | `SqlResiliencePolicyFactory.cs:56-57` vs `:59` | Tooltip IntelliSense hien thu tu tham so sai, de gay goi sai thu tu doi so. (Cac gia tri so trong XML doc dong 54 va 149 **khop** voi than ham, khong co mau thuan) |
| 17 | Khong co timeout strategy, fallback strategy, hay `CircuitBreakerStateProvider`/`ManualControl` trong ca hai pipeline | `:61-145`, `:156-212` | Khong co gioi han thoi gian o tang Polly (phu thuoc hoan toan `CommandTimeout`); khong co gia tri du phong khi loi; khong the doc/dieu khien trang thai CB tu health check hay admin endpoint |
| 18 | `ReadUncommittedConnectionInterceptor` chi override `ConnectionOpenedAsync`, khong override `ConnectionOpened` (dong bo) | `ReadUncommittedConnectionInterceptor.cs:23` | Truy van chay qua duong dong bo **khong** duoc set `READ UNCOMMITTED` -> cung mot DbContext cho hai ket qua/hanh vi khoa khac nhau tuy theo goi sync hay async. Rat kho phat hien |
| 19 | Khong co code nao reset isolation level khi connection dong hoac duoc tra ve connection pool | `ReadUncommittedConnectionInterceptor.cs` (toan file) | Isolation level la thuoc tinh cua session; file nay khong chua bat ky doan code nao dua no ve mac dinh. Rui ro connection dung chung giu lai muc `READ UNCOMMITTED` khong duoc xu ly trong source |
| 20 | Interceptor khong ghi log va khong bat exception | `ReadUncommittedConnectionInterceptor.cs:28-30` | Loi khi set isolation level lam that bai ca thao tac mo connection, va khong co dau vet chan doan tu chinh interceptor |
| 21 | Trong repository nay **khong** tim thay noi goi `ConfigureReadPolicy`/`ConfigureWritePolicy`, cung khong tim thay `AddInterceptors`/`AddDbContext`/`UseSqlServer` de dang ky interceptor | grep toan repo (tru `.claude/worktrees`) | Ca hai thanh phan chua duoc wire vao bat ky pipeline nao trong repo. Muc do dung dan/hieu qua thuc te **khong xac dinh duoc tu source code** trong repository nay; trach nhiem dang ky thuoc ung dung tieu thu |
| 22 | `MinimumThroughput = 10` va `SamplingDuration = 15s` o `ConfigureWritePolicy` | `SqlResiliencePolicyFactory.cs:162`, `:164` | Service co tan suat ghi thap hon 10 lan goi/15 giay se **khong bao gio** mo circuit breaker, tuc mat hoan toan co che bao ve o luong ghi |
| 23 | `ConfigureWritePolicy` dat `BackoffType = Exponential` va `UseJitter = true` nhung `MaxRetryAttempts = 1` | `SqlResiliencePolicyFactory.cs:198-201` | Exponential backoff khong co tac dung khi chi co 1 lan retry; cau hinh gay hieu nham ve hanh vi thuc te |
| 24 | `IsRetryable` voi `handleAllTransient = true` coi **moi** `DbException` khong phai `SqlException` la retryable | `SqlResiliencePolicyFactory.cs:238` | Pham vi retry rat rong; cac loi khong the phuc hoi bang retry (cu phap SQL sai, thieu cot, sai kieu du lieu tu provider khac) van bi thu lai 3 lan, lam tang do tre va tai vo ich |
| 25 | Ca hai ham **khong idempotent** va khong kiem tra `builder` da chua strategy hay chua | `SqlResiliencePolicyFactory.cs:61-145`, `:156-212` | Goi trung mot ham hai lan, hoac ap ca `ConfigureReadPolicy` va `ConfigureWritePolicy` len cung mot `builder`, se tao pipeline 4 strategy long nhau (CB→Retry→CB→Retry) — retry bi nhan doi, CB dem sai. Khong co guard nao ngan chan; loi nay chi lo ra o runtime |
| 26 | Ca hai ham khong bat cac exception ma Polly co the nem ngay tai thoi diem cau hinh: `ArgumentNullException` (`builder`/`options` null), `ValidationException` (`options` khong hop le), `InvalidOperationException` (builder da `Build()`) | `SqlResiliencePolicyFactory.cs:62`, `:119`, `:157`, `:194` | Caller phai tu xu ly. Cac gia tri hardcode hien tai deu vuot qua validate cua Polly, nhung neu sau nay sua `MinimumThroughput` < 2 hay `SamplingDuration` < 500ms thi ham se nem `ValidationException` ngay khi startup |
| 27 | `Delay = 200ms`/`300ms` di kem `UseJitter = true` khong phai do tre co dinh | `SqlResiliencePolicyFactory.cs:124`, `:126`, `:199`, `:201` | Theo XML doc Polly 8.7.0, voi `DelayBackoffType.Exponential` thi `Delay` la "median delay to target before the first retry". Do tre thuc te duoc random hoa (decorrelated jitter) va **khong xac dinh duoc tu source code**; moi con so do tre trong tai lieu nay chi la uoc luong theo trung vi |
| 28 | `MaxRetryAttempts = 3` o `ConfigureReadPolicy` trung voi gia tri mac dinh cua `RetryStrategyOptions` (Polly 8.7.0) | `SqlResiliencePolicyFactory.cs:123` | Dong nay khong lam thay doi hanh vi so voi mac dinh cua Polly; chi mang tinh tuong minh. Nguoc lai, `ConfigureWritePolicy` phai dat `= 1` moi khac duoc mac dinh |
