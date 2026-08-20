# Data / SQL / Dapper — Tang truy van raw SQL (DapperSQLDBContext, ExecuteSQLContext, ConfigurationHelpers)

> Nguon:
> - `FTELSRCore.Shared/Data/SQL/Dapper/IDapperSQLDBContext.cs`
> - `FTELSRCore.Shared/Data/SQL/Dapper/DapperSQLDBContext.cs`
> - `FTELSRCore.Shared/Data/SQL/Dapper/ExecuteSQLContext.cs`
> - `FTELSRCore.Shared/Data/SQL/Dapper/Helpers/ConfigurationHelpers.cs`
>
> Loai: `interface` (`IDapperSQLDBContext`) + `sealed class` (`DapperSQLDBContext`) + `abstract class` generic (`ExecuteSQLContext<TClass>`) + `static class` (`ConfigurationHelpers`)
>
> Cap nhat theo commit: `2262829`

---

## 1. Tong quan

Nhom 4 file nay tao thanh tang truy van raw SQL bang Dapper cua `FTELSRCore.Shared`, nam **duoi** tang repository (`FTELSRCore.Data.SQL.Core.CoreSQL`) va **song song** voi tang EF Core (`ReadDbContext` / `WriteDbContext`). Tang nay chi lam mot viec: nhan chuoi SQL (hoac ten stored procedure) cung bo tham so, mo mot `SqlConnection` moi, day lenh xuong SQL Server qua Dapper, roi giai phong connection.

`IDapperSQLDBContext` + `DapperSQLDBContext` phuc vu SQL tuy y (`CommandType.Text` hoac `CommandType.StoredProcedure` do caller quyet dinh). `ExecuteSQLContext<TClass>` la lop co so rieng cho truong hop **chi goi stored procedure** — `CommandType` bi hardcode thanh `CommandType.StoredProcedure` (`ExecuteSQLContext.cs:48`, `ExecuteSQLContext.cs:79`). `ConfigurationHelpers` la mot helper chi co mot ham factory tao `SqlConnection`.

Toan bo 4 file **khong co try/catch, khong co logging, khong co Polly, khong co transaction, khong goi `Open()`/`Close()`** — da xac nhan bang grep tren ca 4 file.

> [!IMPORTANT]
> Ban than tang Dapper KHONG co bat ky co che resilience nao. Retry / circuit breaker chi ton tai o tang goi khi lop do tu boc lenh goi trong `_pipelineRead.ExecuteAsync(...)`. Xem muc 6.3.
>
> Luu y ve cach doc reference: trong repo **khong ton tai type nao ten `CoreSQLTenant`**. File `CoreSQLTenant.cs` chua overload 4 generic `CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>` (`CoreSQLTenant.cs:17`); file `CoreSQL.cs` chua overload 3 generic `CoreSQL<TEntity, DBContextRead, DBContextWrite>` (`CoreSQL.cs:16`). Duoi day moi reference dang `CoreSQLTenant.cs:<dong>` la tro den **file** do, khong phai mot class ten `CoreSQLTenant`.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Thuc thi SQL raw text tra ve 0 dong / 1 dong / nhieu dong / scalar (`DapperSQLDBContext.cs:28,64,100,137,175`) | Khong co retry, khong co circuit breaker, khong co timeout policy o tang nay (khong co dong code nao tham chieu `Polly` / `ResiliencePipeline` trong 4 file) |
| Thuc thi stored procedure (caller truyen `commandType: CommandType.StoredProcedure`) | `DapperSQLDBContext` khong tu suy ra `CommandType`; mac dinh la `CommandType.Text` (`DapperSQLDBContext.cs:31,67,103,140,178`). Rieng `ExecuteSQLContext<TClass>` thi nguoc lai: hardcode `CommandType.StoredProcedure`, khong co tham so de doi (`ExecuteSQLContext.cs:48,79`) |
| Truyen tham so qua `DynamicParameters` — Dapper tu parameterize (`DapperSQLDBContext.cs:47`) | Khong validate / sanitize noi dung `pSqlQuery`; caller tu chiu trach nhiem ve chuoi SQL |
| Mo connection moi cho tung lenh goi va tu giai phong (`await using`, `DapperSQLDBContext.cs:41-42`) | Khong ho tro chia se connection giua nhieu lenh goi, khong ho tro `DbTransaction` (`CommandDefinition` khong duoc truyen `transaction`) |
| Short-circuit khi `pSqlQuery` rong/null, khong mo connection (`DapperSQLDBContext.cs:36-39`) | Khong ghi log khi short-circuit — caller khong biet vi sao nhan `false` / `null` |
| Ton trong `CancellationToken`: kiem tra truoc khi chay + truyen xuong `CommandDefinition` (`DapperSQLDBContext.cs:34,50`) | Khong bat exception; moi `SqlException` / `TimeoutException` deu nem thang len caller |
| `ExecuteSQLContext<TClass>`: goi stored procedure va doc gia tri output parameter `P_RESULT` (`ExecuteSQLContext.cs:82`) | `ExecuteSQLContext<TClass>` khong co ham thuc thi non-query, khong ho tro `CommandType.Text`, khong kiem tra `StoreName` rong |
| `ConfigurationHelpers.CreateConnection` tao `SqlConnection` tu connection string (`ConfigurationHelpers.cs:16-19`) | `ConfigurationHelpers` **khong doc va khong build** bat ky gia tri config nao (khong co `IConfiguration`, khong co appsettings, khong co `SqlConnectionStringBuilder`) — ten class gay nham lan |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `Dapper` (v2.1.79) | `DynamicParameters`, `CommandDefinition`, cac extension `ExecuteAsync` / `QueryAsync` / `QueryFirstOrDefaultAsync` / `ExecuteScalarAsync` |
| `Microsoft.Data.SqlClient` (v7.0.2) | `SqlConnection` — kieu connection duy nhat duoc dung |
| `System.Data` | `CommandType` (`Text` / `StoredProcedure`) |
| `FTELSRCore.Data.SQL.Dapper.Helpers.ConfigurationHelpers` | Factory tao `SqlConnection` cho ca `DapperSQLDBContext` va `ExecuteSQLContext<TClass>` |
| `net9.0`, `Nullable=disable`, `ImplicitUsings=enable` | Cau hinh project (`FTELSRCore.Shared.csproj`) — `Nullable=disable` khien `return default` cho reference type khong sinh warning |

> [!NOTE]
> Trong repo nay **khong tim thay dong code nao dang ky `IDapperSQLDBContext` vao DI container** (grep `AddScoped<IDapperSQLDBContext` / `AddSingleton<IDapperSQLDBContext` / `new DapperSQLDBContext(` tra ve 0 ket qua ngoai chinh khai bao class). Ca hai overload `CoreSQL` nhan `Lazy<IDapperSQLDBContext>` qua constructor (`CoreSQL.cs:37`, `CoreSQLTenant.cs:42`), nen viec dang ky va cap `connectionString` do ung dung tieu thu (consumer application) thuc hien.

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `DapperSQLDBContext(string connectionString)` | Constructor | Primary constructor, luu connection string vao field `_dbConnection` |
| `ExecuteNonQueryAsync(...)` | DapperSQLDBContext — Write | Thuc thi lenh khong tra tap ket qua; tra `true` khi rows affected > 0 |
| `GetOne<T>(...)` | DapperSQLDBContext — Read | `QueryFirstOrDefaultAsync<T>` — lay ban ghi dau tien |
| `GetAll<T>(...)` | DapperSQLDBContext — Read | `QueryAsync<T>` — lay danh sach ban ghi |
| `GetOneExecute<T>(...)` | DapperSQLDBContext — Read | `ExecuteScalarAsync<T>` — lay gia tri scalar |
| `GetAllExecuteAsync<T>(...)` | DapperSQLDBContext — Read | `QueryAsync<T>` — **trung hoan toan hanh vi voi `GetAll<T>`**: cung guard, cung ham Dapper, cung tra ve `null` khi query rong (chi khac cach viet `return default` (`:110`) vs `return null` (`:185`)) |
| `ExecuteSQLContext<TClass>(string connectionString)` | Constructor | Primary constructor cua abstract class; `connectionString` duoc dung truc tiep, khong co field `readonly` rieng |
| `StoreName` | ExecuteSQLContext — abstract member | Ten stored procedure; duoc dung o `Execute` va `ExecuteScalar` |
| `TypeConnection` | ExecuteSQLContext — abstract member | Khai bao `protected abstract byte` nhung **khong duoc tham chieu o dau** (vestigial) |
| `GetDynamicParameters(TClass entry)` | ExecuteSQLContext — abstract member | Anh xa `TClass` sang `DynamicParameters`; duoc dung o ca 2 ham public |
| `Execute<TResult>(...)` | ExecuteSQLContext — Read | Goi stored procedure, tra ve toan bo tap ket qua |
| `ExecuteScalar<TResult>(...)` | ExecuteSQLContext — Read | Goi stored procedure bang `QueryAsync` (khong phai `ExecuteScalarAsync`), roi doc output parameter `P_RESULT`; **chi** khi gia tri doc duoc la `null` moi fallback ve dong dau tien cua result set — voi `TResult` la value type khong nullable, nhanh fallback khong bao gio chay (xem 4.6) |
| `ConfigurationHelpers.CreateConnection(string connection)` | Helper | `return new SqlConnection(connection)` — khong mo connection |

---

## 2. Chi tiet API — `IDapperSQLDBContext` (interface)

> Nguon: `FTELSRCore.Shared/Data/SQL/Dapper/IDapperSQLDBContext.cs`
> Loai: `interface`

Interface khai bao dung 5 member, tat ca deu la method tra ve `Task` / `Task<T>` (khai bao trong interface **khong** co tu khoa `async` — `async` chi xuat hien o than ham trien khai), khong co property, khong ke thua interface khac (`IDapperSQLDBContext.cs:6`). Chu ky cua 5 member (ten tham so, thu tu, kieu, gia tri mac dinh) trung khop 100% voi phan trien khai trong `DapperSQLDBContext`.

| Member | Dong khai bao | Kieu tra ve |
|---|---|---|
| `ExecuteNonQueryAsync` | `IDapperSQLDBContext.cs:18-19` | `Task<bool>` |
| `GetOne<T>` | `IDapperSQLDBContext.cs:35-36` | `Task<T>` |
| `GetAll<T>` | `IDapperSQLDBContext.cs:50-51` | `Task<IEnumerable<T>>` |
| `GetOneExecute<T>` | `IDapperSQLDBContext.cs:65-66` | `Task<T>` |
| `GetAllExecuteAsync<T>` | `IDapperSQLDBContext.cs:82-86` | `Task<IEnumerable<T>>` |

> [!WARNING]
> XML doc cua interface mo ta hanh vi **khong dung** voi than ham trien khai o hai diem (chi tiet o muc 7, item 6 va item 8):
> 1. `IDapperSQLDBContext.cs:79` viet "Neu cau truy van rong hoac null, phuong thuc se tra ve `Enumerable.Empty{T}`" — nhung `DapperSQLDBContext.cs:185` tra ve `null`.
> 2. XML doc cua nhieu member noi "neu xay ra loi, tra ve gia tri mac dinh / null" (`IDapperSQLDBContext.cs:32,47,62`) — nhung trong than ham khong co try/catch nao, moi exception deu duoc nem thang len caller.
> Theo nguyen tac Source Code > Documentation, hay tin than ham.

---

## 3. Chi tiet API — `DapperSQLDBContext` (sealed class)

> Nguon: `FTELSRCore.Shared/Data/SQL/Dapper/DapperSQLDBContext.cs`
> Loai: `sealed class`, trien khai `IDapperSQLDBContext`

### 3.1 Constructor `DapperSQLDBContext(string connectionString)`

**Signature**

```csharp
public sealed class DapperSQLDBContext(string connectionString) : IDapperSQLDBContext
{
    private readonly string _dbConnection = connectionString;
    // ...
}
```

**Muc dich** — Primary constructor (C# 12) nhan connection string va gan vao field readonly `_dbConnection` (`DapperSQLDBContext.cs:14,16`). Toan bo 5 method dung chung field nay de tao connection.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `connectionString` | `string` | Co (positional) | **Khong co validate nao** — khong kiem tra null, khong kiem tra rong, khong parse | Khong co |

**Output** — Instance `DapperSQLDBContext`. Khong co truong hop that bai o buoc khoi tao.

**Dieu kien xu ly** — Khong co nhanh re; chi mot phep gan field.

**Side effect** — Khong co (khong mo connection, khong ping DB).

**Error handling** — Khong co. Constructor khong the nem loi.

**Khi nao NEN dung** — Khi dang ky implementation cua `IDapperSQLDBContext` trong DI container cua ung dung tieu thu, truyen connection string doc tu configuration cua ung dung do.

**Khi nao KHONG dung** — Khong dung instance nay cho nhieu database khac nhau: connection string bi co dinh tai thoi diem khoi tao, khong co API nao doi duoc. Muon multi-tenant/multi-DB phai tao nhieu instance hoac tu quan ly factory.

**Gioi han**
- Connection string sai/rong **khong** bi phat hien tai day; loi chi no ra o lan goi query dau tien, khi `new SqlConnection(...)` hoac khi Dapper mo connection.
- Class la `sealed` (`DapperSQLDBContext.cs:14`) — khong the ke thua de chen thu hanh vi (log, retry, metric). Muon bo sung phai boc bang decorator qua interface `IDapperSQLDBContext`.

---

### 3.2 `ExecuteNonQueryAsync`

**Signature**

```csharp
public async Task<bool> ExecuteNonQueryAsync(string pSqlQuery,
                                   DynamicParameters pParams,
                                   int commandTimeout = 30,
                                   CommandType commandType = CommandType.Text,
                                   CancellationToken cancellationToken = default)
```

**Muc dich** — Thuc thi mot lenh SQL khong can doc tap ket qua (INSERT / UPDATE / DELETE / DDL / stored procedure) qua `connection.ExecuteAsync(...)`, roi tra ve ket qua so sanh `rows affected > 0` (`DapperSQLDBContext.cs:44-50`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `pSqlQuery` | `string` | Co | `string.IsNullOrWhiteSpace` → tra `false` ngay (`DapperSQLDBContext.cs:36-39`) | Khong co |
| `pParams` | `DynamicParameters` | Co (positional) | **Khong validate** — `null` duoc truyen thang vao `CommandDefinition` | Khong co |
| `commandTimeout` | `int` | Khong | **Khong validate** — gia tri am hoac 0 duoc truyen thang xuong Dapper | `30` |
| `commandType` | `CommandType` | Khong | **Khong validate** — khong kiem tra gia tri enum hop le | `CommandType.Text` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` tai `DapperSQLDBContext.cs:34` truoc moi thao tac | `default` |

**Output** — `Task<bool>`:
- `false` khi `pSqlQuery` rong/null/toan whitespace (khong he mo connection) — `DapperSQLDBContext.cs:38`.
- `true` khi gia tri `ExecuteAsync` tra ve **lon hon 0** (`DapperSQLDBContext.cs:50`).
- `false` khi gia tri `ExecuteAsync` tra ve `<= 0` (bao gom ca `-1`).
- Khong co gia tri tra ve nao rieng cho "loi" — loi duoc nem thanh exception.

**Dieu kien xu ly** (theo thu tu thuc thi)
1. `cancellationToken.ThrowIfCancellationRequested()` (`:34`) — nem `OperationCanceledException` neu token da bi huy.
2. Guard `string.IsNullOrWhiteSpace(pSqlQuery)` → `return false` (`:36-39`).
3. `ConfigurationHelpers.CreateConnection(_dbConnection)` trong `await using` (`:41-42`).
4. `connection.ExecuteAsync(new CommandDefinition(...))` roi so sanh `> 0` (`:44-50`).

**Side effect**
- Ghi DB: **Co** — day lenh SQL do caller cung cap xuong SQL Server; tac dong phu thuoc hoan toan vao noi dung `pSqlQuery`.
- Tao va giai phong mot `SqlConnection` moi cho moi lan goi (`await using`, `:41`) — thuc te la lay/tra connection tu ADO.NET connection pool.
- Ghi log: **Khong co** (khong co logger trong class).
- Mutate tham so dau vao: khong co dong code nao gan lai `pParams`. Tuy nhien Dapper co the gan `AttachedParam` vao cac `ParamInfo` ben trong `pParams` khi thuc thi, nen **khong nen tai su dung cung mot instance `DynamicParameters` cho nhieu lan goi song song**.
- Khong thay doi state dung chung nao khac (chi doc field readonly `_dbConnection`).

**Error handling** — Khong co try/catch, khong co logging. Moi exception (`SqlException`, `InvalidOperationException` do connection string sai, `TimeoutException`, `OperationCanceledException`) **nem thang** len caller. `await using` van dam bao connection duoc dispose khi co exception.

**Khi nao NEN dung**
- Chay lenh ghi raw SQL don le (INSERT/UPDATE/DELETE) khi khong can tham gia transaction cua EF Core.
- Goi stored procedure khong tra tap ket qua (truyen `commandType: CommandType.StoredProcedure`).

**Khi nao KHONG dung**
- Khi can biet **chinh xac so dong bi anh huong**: ham chi tra `bool`, thong tin so dong bi mat (`:50`).
- Khi can chay nhieu lenh trong **cung mot transaction**: moi lan goi mo connection rieng, `CommandDefinition` khong duoc truyen `transaction` → khong co cach nao join transaction. Truong hop nay dung `CoreSQL.IsExecuteNonQueryAsync(...)` (nhan `DbConnection` + `DbTransaction` tu ben ngoai, `CoreSQL.cs:217-224`).
- Khi lenh ghi can retry/circuit breaker: tang nay khong co resilience; phai tu boc `ResiliencePipeline` o ngoai. Trong repo nay **khong co dong code nao goi `ExecuteNonQueryAsync`**, nen chua co tien le boc pipeline cho no.
- Khi caller dua noi dung nguoi dung vao truc tiep chuoi `pSqlQuery` — tang nay khong sanitize gi.

**Gioi han**
- Ket qua `bool` khong phan biet duoc "chay thanh cong nhung 0 dong bi anh huong" voi "khong chay vi query rong": ca hai deu tra `false` (`:38` va `:50`).
- Moi truong hop ADO.NET tra ve so dong `<= 0` (vi du `-1`) deu cho ket qua `false` du lenh da chay thanh cong.
- `commandTimeout` mac dinh 30 giay bi **hardcode trong signature** (`:30`) — khong doc tu configuration.
- Khong ho tro transaction, khong ho tro `DbConnection` truyen tu ngoai.
- Khong co resilience: mot loi transient (deadlock 1205, connection broken -1, timeout -2) khien lenh that bai ngay, khac hoan toan voi duong EF Core duoc `SqlResiliencePolicyFactory` bao ve.

---

### 3.3 `GetOne<T>`

**Signature**

```csharp
public async Task<T> GetOne<T>(string pSqlQuery,
                         DynamicParameters pParams,
                         int commandTimeout = 30,
                         CommandType commandType = CommandType.Text,
                         CancellationToken cancellationToken = default)
```

**Muc dich** — Truy van va anh xa **dong dau tien** cua tap ket qua sang `T` bang `connection.QueryFirstOrDefaultAsync<T>(...)` (`DapperSQLDBContext.cs:80-86`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `pSqlQuery` | `string` | Co | `string.IsNullOrWhiteSpace` → `return default` (`:72-75`) | Khong co |
| `pParams` | `DynamicParameters` | Co (positional) | Khong validate | Khong co |
| `commandTimeout` | `int` | Khong | Khong validate | `30` |
| `commandType` | `CommandType` | Khong | Khong validate | `CommandType.Text` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:70`) | `default` |
| `T` (type param) | — | Co | **Khong co generic constraint** — chap nhan ca class, struct, primitive | — |

**Output** — `Task<T>`:
- `default(T)` khi `pSqlQuery` rong/null (`:74`) — voi reference type la `null`, voi value type la `0` / `false` / `default struct`.
- `default(T)` khi truy van chay nhung khong co dong nao (hanh vi cua `QueryFirstOrDefaultAsync`).
- Doi tuong `T` da map tu dong dau tien khi co ket qua.
- **Khong the phan biet** 3 truong hop "query rong", "khong co du lieu", "co du lieu nhung tat ca cot = default" chi qua gia tri tra ve.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (`:70`).
2. Guard query rong → `return default` (`:72-75`).
3. Tao connection trong `await using` (`:77-78`).
4. `QueryFirstOrDefaultAsync<T>` voi `CommandDefinition` day du 5 tham so (`:80-86`).

**Side effect**
- Doc DB. Tao/giai phong mot `SqlConnection` moi cho moi lan goi.
- Khong ghi log, khong mutate `pParams` truc tiep trong code.
- Neu `pSqlQuery` la lenh ghi (`UPDATE ... ; SELECT ...`) thi ham nay **van ghi DB** — code khong kiem tra loai lenh.

**Error handling** — Khong co try/catch. Moi exception nem thang len caller (bao gom loi map kieu khi cot khong khop `T`).

**Khi nao NEN dung**
- Lay 1 record theo dieu kien (`SELECT TOP 1 ... WHERE ...`) va map sang DTO.
- Goi stored procedure tra ve dung 1 dong (truyen `commandType: CommandType.StoredProcedure`).
- Duong dung chuan trong repo: qua `CoreSQL.FindOneWithScriptAsync` — call site `CoreSQL.cs:96` (overload 3 generic) va `CoreSQLTenant.cs:103` (overload 4 generic) — hai ham nay da boc `_pipelineRead.ExecuteAsync(...)`, tuc **co** retry + circuit breaker.
- Khi can dung `SELECT` khong dinh nghia duoc bang LINQ/EF Core (pivot, CTE phuc tap, hint).

**Khi nao KHONG dung**
- Khi can biet co du lieu hay khong ma `T` la value type: `default(T)` (vi du `0`) trung voi gia tri du lieu hop le. Dung `T` nullable hoac `GetAll<T>` roi kiem tra so luong.
- Khi truy van co the tra ve nhieu dong va ban can tat ca — dung `GetAll<T>`.
- Khi goi truc tiep instance `DapperSQLDBContext` (khong qua `CoreSQL`): khong co resilience, khong co log, khong co `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED` (chuoi nay do `CoreSQL` chen vao, `CoreSQL.cs:84-88`).

**Gioi han**
- Khong log gi khi short-circuit vi query rong → loi silent, rat kho debug.
- `commandTimeout` mac dinh 30 hardcode trong signature (`:66`).
- Khong co resilience (xem muc 6.3).
- Khong co generic constraint tren `T` → loi kieu chi phat hien tai runtime.

---

### 3.4 `GetAll<T>`

**Signature**

```csharp
public async Task<IEnumerable<T>> GetAll<T>(string pSqlQuery,
                                            DynamicParameters pParams,
                                            int commandTimeout = 30,
                                            CommandType commandType = CommandType.Text,
                                            CancellationToken cancellationToken = default)
```

**Muc dich** — Truy van va anh xa toan bo tap ket qua sang `IEnumerable<T>` bang `connection.QueryAsync<T>(...)` (`DapperSQLDBContext.cs:116-122`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `pSqlQuery` | `string` | Co | `string.IsNullOrWhiteSpace` → `return default` (`:108-111`) | Khong co |
| `pParams` | `DynamicParameters` | Co (positional) | Khong validate | Khong co |
| `commandTimeout` | `int` | Khong | Khong validate | `30` |
| `commandType` | `CommandType` | Khong | Khong validate | `CommandType.Text` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:106`) | `default` |
| `T` (type param) | — | Co | Khong co generic constraint | — |

**Output** — `Task<IEnumerable<T>>`:
- **`null`** khi `pSqlQuery` rong/null — vi `default` cua `IEnumerable<T>` la `null` (`:110`). Day KHONG phai empty list.
- Danh sach da buffer (Dapper `QueryAsync` mac dinh buffered) khi truy van chay xong; neu khong co dong nao thi la **collection rong**, khong phai `null`.

> [!CAUTION]
> Caller phai kiem tra `null` truoc khi `foreach` / `.Count()`. Bo qua kiem tra se gap `NullReferenceException` khi `pSqlQuery` rong.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (`:106`).
2. Guard query rong → `return default` (tuc `null`) (`:108-111`).
3. Tao connection trong `await using` (`:113-114`).
4. `QueryAsync<T>` voi `CommandDefinition` (`:116-122`).

**Side effect** — Doc DB; tao/giai phong mot `SqlConnection` moi moi lan goi; khong ghi log.

**Error handling** — Khong co try/catch; exception nem thang len caller.

**Khi nao NEN dung**
- Lay danh sach ban ghi bang raw SQL / stored procedure tra ve 1 result set.
- Duong dung chuan trong repo: `CoreSQL.FindAllWithScriptAsync` — call site `CoreSQL.cs:146` (overload 3 generic) va `CoreSQLTenant.cs:155` (overload 4 generic) — da boc `_pipelineRead`.

**Khi nao KHONG dung**
- Voi truy van tra ve rat nhieu dong: `QueryAsync` mac dinh buffer toan bo ket qua vao memory; code **khong** truyen `flags: CommandFlags.None` de tat buffering, cung khong co API streaming. Rui ro memory pressure.
- Khi ban muon nhan empty list thay vi `null` cho query rong — hanh vi tra `null` la co that trong code (`:110`).
- Khi can nhieu result set (`QueryMultiple`) — khong ho tro.

**Gioi han**
- Tra `null` (khong phai `Enumerable.Empty<T>()`) khi query rong — nguon loi `NullReferenceException` pho bien.
- Buffer toan bo ket qua, khong co paging/streaming, khong co gioi han so dong.
- Khong co resilience, khong co log.
- Trung lap chuc nang voi `GetAllExecuteAsync<T>` (xem muc 3.6 va muc 7, item 9).

---

### 3.5 `GetOneExecute<T>`

**Signature**

```csharp
public async Task<T> GetOneExecute<T>(string pSqlQuery,
                                      DynamicParameters pParams,
                                      int commandTimeout = 30,
                                      CommandType commandType = CommandType.Text,
                                      CancellationToken cancellationToken = default)
```

**Muc dich** — Thuc thi lenh SQL va lay **mot gia tri scalar** (cot dau tien cua dong dau tien) bang `connection.ExecuteScalarAsync<T>(...)`, gan vao bien `result` roi tra ve (`DapperSQLDBContext.cs:153-161`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `pSqlQuery` | `string` | Co | `string.IsNullOrWhiteSpace` → `return default` (`:145-148`) | Khong co |
| `pParams` | `DynamicParameters` | Co (positional) | Khong validate | Khong co |
| `commandTimeout` | `int` | Khong | Khong validate | `30` |
| `commandType` | `CommandType` | Khong | Khong validate | `CommandType.Text` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:143`) | `default` |
| `T` (type param) | — | Co | Khong co generic constraint | — |

**Output** — `Task<T>`:
- `default(T)` khi `pSqlQuery` rong/null (`:147`).
- Gia tri scalar da convert sang `T` khi truy van tra ve du lieu.
- `default(T)` khi truy van khong tra ve dong nao hoac gia tri la `NULL` (hanh vi `ExecuteScalarAsync<T>` cua Dapper).
- Khong phan biet duoc "query rong" vs "khong co du lieu" vs "gia tri that bang default".

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (`:143`).
2. Guard query rong → `return default` (`:145-148`).
3. Tao connection trong `await using` (`:150-151`).
4. `ExecuteScalarAsync<T>` (`:153-159`), gan vao bien local `result`.
5. `return result` (`:161`) — **khong co xu ly bo sung nao** giua buoc 4 va 5; bien local nay la du thua so voi `return` truc tiep.

**Side effect** — Thuc thi lenh SQL do caller cung cap (co the la lenh ghi neu caller truyen lenh ghi); tao/giai phong mot `SqlConnection`; khong ghi log.

**Error handling** — Khong co try/catch; exception nem thang len caller (ke ca loi convert kieu khi cot khong tuong thich `T`).

**Khi nao NEN dung**
- `SELECT COUNT(*)`, `SELECT SUM(...)`, `SELECT MAX(Id)`, `SELECT 1 WHERE EXISTS(...)`, lay mot cot don.
- Lay gia tri identity vua sinh: `INSERT ...; SELECT SCOPE_IDENTITY();`.
- Duong dung chuan trong repo: `CoreSQL.FindOneWithScalarScriptAsync` — call site `CoreSQL.cs:196` (overload 3 generic) / `CoreSQLTenant.cs:205` (overload 4 generic) — da boc `_pipelineRead`.

**Khi nao KHONG dung**
- Khi can nhieu cot: `ExecuteScalarAsync` chi doc cot dau tien cua dong dau tien; cac cot con lai bi bo. Dung `GetOne<T>`.
- Khi can phan biet 0 vs NULL vs khong co dong: dung `T` la kieu nullable, hoac dung `GetOne<T>` voi DTO.
- Khi goi stored procedure tra ve gia tri qua **output parameter**: `ExecuteScalarAsync` doc result set, khong doc output parameter. Truong hop nay dung `ExecuteSQLContext<TClass>.ExecuteScalar<TResult>` (muc 4.6).

**Gioi han**
- Bien local `result` (`:153`, `:161`) khong them logic gi — chi lam ham dai hon, khong co hanh vi khac biet so voi `return await ...`.
- Khong co resilience, khong log, `commandTimeout` hardcode 30 trong signature (`:139`).

---

### 3.6 `GetAllExecuteAsync<T>`

**Signature**

```csharp
public async Task<IEnumerable<T>> GetAllExecuteAsync<T>(string pSqlQuery,
                                                        DynamicParameters pParams,
                                                        int commandTimeout = 30,
                                                        CommandType commandType = CommandType.Text,
                                                        CancellationToken cancellationToken = default)
```

**Muc dich** — Thuc thi lenh SQL hoac stored procedure va tra ve toan bo tap ket qua bang `connection.QueryAsync<T>(...)` (`DapperSQLDBContext.cs:191-197`). **Ve cai dat, ham nay giong `GetAll<T>` gan nhu tuyet doi.**

**So sanh voi `GetAll<T>`**

| Diem | `GetAll<T>` | `GetAllExecuteAsync<T>` |
|---|---|---|
| Guard query rong | `return default` (`:110`) | `return null` (`:185`) — cung cho ra `null`, chi khac cach viet |
| Ham Dapper duoc goi | `QueryAsync<T>` (`:116`) | `QueryAsync<T>` (`:191`) |
| Bien trung gian | Khong (return truc tiep) | Co bien local `result` (`:191`, `:199`) |
| `CommandType` mac dinh | `CommandType.Text` | `CommandType.Text` |
| Ket luan | **Khong co khac biet hanh vi nao doc ra duoc tu source code** | |

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `pSqlQuery` | `string` | Co | `string.IsNullOrWhiteSpace` → `return null` (`:183-186`) | Khong co |
| `pParams` | `DynamicParameters` | Co (positional) | Khong validate | Khong co |
| `commandTimeout` | `int` | Khong | Khong validate | `30` |
| `commandType` | `CommandType` | Khong | Khong validate | `CommandType.Text` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:181`) | `default` |
| `T` (type param) | — | Co | Khong co generic constraint | — |

**Output** — `Task<IEnumerable<T>>`:
- **`null`** khi `pSqlQuery` rong/null (`:185`). XML doc cua interface noi la `Enumerable.Empty<T>` (`IDapperSQLDBContext.cs:79`) — **doc sai so voi code**.
- Collection da buffer khi truy van chay xong; rong neu khong co dong nao.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (`:181`).
2. Guard query rong → `return null` (`:183-186`).
3. Tao connection trong `await using` (`:188-189`).
4. `QueryAsync<T>` (`:191-197`), gan vao `result`.
5. `return result` (`:199`).

**Side effect** — Doc (hoac ghi, tuy lenh caller truyen) DB; tao/giai phong mot `SqlConnection`; khong ghi log.

**Error handling** — Khong co try/catch; exception nem thang len caller.

**Khi nao NEN dung**
- Goi stored procedure tra ve mot result set — dat `commandType: CommandType.StoredProcedure` va `pSqlQuery` la ten SP. Luu y: **mac dinh van la `CommandType.Text`**, phai truyen tay.

**Khi nao KHONG dung**
- Khi ban ky vong nhan empty list cho query rong: ham tra `null` (`:185`).
- Truy van khoi luong lon: buffer toan bo vao memory.
- Khi ban tim mot ham "chuyen cho stored procedure": ten ham goi y nhu vay nhung code **khong** ep `CommandType.StoredProcedure`; neu quen truyen `commandType`, SQL Server se coi ten SP la cau lenh text.

**Gioi han**
- Trung lap voi `GetAll<T>` ma khong co su khac biet hanh vi nao — nguon gay nham lan khi chon API. Trong repo nay **khong co dong code nao goi `GetAllExecuteAsync`**.
- XML doc interface (`IDapperSQLDBContext.cs:79`) mo ta sai gia tri tra ve.
- Khong co resilience, khong log, `commandTimeout` hardcode 30 (`:177`; dong `:178` la khai bao `commandType`).

---

## 4. Chi tiet API — `ExecuteSQLContext<TClass>` (abstract class)

> Nguon: `FTELSRCore.Shared/Data/SQL/Dapper/ExecuteSQLContext.cs`
> Loai: `abstract class` generic, primary constructor, rang buoc `where TClass : class` (`ExecuteSQLContext.cs:15`)

Lop co so danh cho pattern "mot lop = mot stored procedure": lop con khai bao ten SP va cach map DTO dau vao sang `DynamicParameters`. `CommandType` bi **hardcode** thanh `CommandType.StoredProcedure` o ca hai ham public (`:48`, `:79`).

> [!NOTE]
> Trong repo nay **khong tim thay lop con nao ke thua `ExecuteSQLContext<...>`** (grep `: ExecuteSQLContext` tra ve 0 ket qua ngoai chinh khai bao class). Day la API danh cho ung dung tieu thu.

### 4.1 Constructor `ExecuteSQLContext(string connectionString)`

**Signature**

```csharp
public abstract class ExecuteSQLContext<TClass>(string connectionString) where TClass : class
```

**Muc dich** — Nhan connection string qua primary constructor. Khac voi `DapperSQLDBContext`, o day **khong co field readonly rieng**: `connectionString` duoc tham chieu truc tiep trong than ham (`:39`, `:69`), compiler tu sinh backing field.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `connectionString` | `string` | Co (positional) | Khong co validate nao | Khong co |
| `TClass` (type param) | — | Co | `where TClass : class` (`:15`) — chi chap nhan reference type | — |

**Output** — Khong ap dung (abstract class, chi goi qua lop con).

**Dieu kien xu ly** — Khong co nhanh re.

**Side effect** — Khong co.

**Error handling** — Khong co.

**Khi nao NEN dung** — Khi tao lop wrapper cho mot stored procedure cu the trong ung dung tieu thu.

**Khi nao KHONG dung** — Khi mot lop can goi nhieu stored procedure khac nhau: `StoreName` la property abstract tra ve mot gia tri, khong nhan tham so → mot lop chi map duoc mot SP.

**Gioi han** — Lop con **buoc phai** override ca 3 member abstract, ke ca `TypeConnection` du member nay khong duoc dung o dau (xem 4.3).

---

### 4.2 `StoreName` (protected abstract property)

**Signature**

```csharp
protected abstract string StoreName { get; }
```

**Muc dich** — Cung cap ten stored procedure se duoc dat vao `CommandDefinition.commandText` (`ExecuteSQLContext.cs:17`).

**Input hop le** — Khong co tham so. Lop con quyet dinh gia tri.

**Output** — `string`. Duoc dung o **2 vi tri**: `Execute<TResult>` (`:45`) va `ExecuteScalar<TResult>` (`:76`).

**Dieu kien xu ly** — Gia tri tra ve **khong duoc kiem tra** null/rong o bat ky dong nao truoc khi truyen vao Dapper.

**Side effect** — Tuy cai dat cua lop con. Ban than khai bao khong co side effect.

**Error handling** — Khong co. Neu lop con tra ve `null` hoac chuoi rong, loi chi no ra khi Dapper/SQL Server xu ly `commandText`.

**Khi nao NEN dung** — Override bang mot `string` hang so, vi du `protected override string StoreName => "SP_GET_REQUEST";`.

**Khi nao KHONG dung** — Khong nen override bang logic doc DB/config dong: property duoc doc lai o moi lan goi (`:45`, `:76`), khong co cache.

**Gioi han** — La `protected` nen khong doc duoc tu ben ngoai lop; khong co validate; khong ho tro schema-qualified name kiem tra.

---

### 4.3 `TypeConnection` (protected abstract property) — member vestigial

**Signature**

```csharp
protected abstract byte TypeConnection { get; }
```

**Muc dich** — **Khong xac dinh duoc tu source code.** Khai bao tai `ExecuteSQLContext.cs:19` nhung grep toan repo cho tu khoa `TypeConnection` chi tra ve **dung mot ket qua duy nhat la dong khai bao nay** — khong co dong code nao doc gia tri cua no.

**Input hop le** — Khong co tham so.

**Output** — `byte`. Gia tri **khong duoc su dung o bat ky dau**.

**Dieu kien xu ly** — Khong co.

**Side effect** — Khong co.

**Error handling** — Khong co.

**Khi nao NEN dung** — Khong co tinh huong nao: member nay khong anh huong den hanh vi cua `Execute` hay `ExecuteScalar`.

**Khi nao KHONG dung** — Khong duoc ky vong `TypeConnection` chon read/write connection hay switch database: **khong co dong code nao lam viec do**. Ca hai ham public deu dung chung `connectionString` truyen vao constructor (`:39`, `:69`).

**Gioi han**
- Day la **dead member** (vestigial): bat buoc lop con phai override nhung khong co tac dung gi → chi phi thua cho moi lop con, va gay hieu nham rang lop co so ho tro chon loai connection.
- Kieu `byte` khong kem enum hay hang so nao trong repo de biet y nghia cac gia tri.

---

### 4.4 `GetDynamicParameters(TClass entry)` (protected abstract method)

**Signature**

```csharp
protected abstract DynamicParameters GetDynamicParameters(TClass entry);
```

**Muc dich** — Lop con anh xa doi tuong dau vao `TClass` thanh `DynamicParameters` de Dapper parameterize (`ExecuteSQLContext.cs:21`). Duoc goi o `Execute<TResult>` (`:41`) va `ExecuteScalar<TResult>` (`:71`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entry` | `TClass` | Co | **Khong co null-check** o lop co so truoc khi goi (`:41`, `:71` truyen `pParams` truc tiep) | Khong co |

**Output** — `DynamicParameters`. Ket qua khong duoc kiem tra `null` truoc khi dua vao `CommandDefinition`.

**Dieu kien xu ly** — Khong co guard nao o lop co so.

**Side effect** — Tuy cai dat lop con.

**Error handling** — Khong co try/catch. Neu lop con nem exception (vi du `NullReferenceException` khi `entry` la `null`), exception nem thang len caller; connection da duoc tao truoc do (`:38-39`, `:68-69`) van duoc `await using` dispose.

**Khi nao NEN dung** — Override de `Add` tung tham so, ke ca output parameter. **Neu lop con dinh dung `ExecuteScalar<TResult>` thi bat buoc phai `Add("P_RESULT", ..., direction: ParameterDirection.Output)`** o day — xem 4.6.

**Khi nao KHONG dung** — Khong nen dat side effect (goi API, ghi log nghiep vu) trong ham nay: no duoc goi **sau khi connection da duoc tao** (`:38-41`, `:68-71`), nen loi trong ham lam viec tao connection thanh vo nghia.

**Gioi han**
- Lop co so khong kiem tra `entry == null` va khong kiem tra ket qua tra ve `!= null`.
- Tra ve `DynamicParameters` cu the (khong phai `object`) → khong the truyen anonymous object hay list de Dapper lam bulk.

---

### 4.5 `Execute<TResult>`

**Signature**

```csharp
public async Task<IEnumerable<TResult>> Execute<TResult>(
    TClass pParams, int commandTimeout = 30, CancellationToken cancellationToken = default)
```

**Muc dich** — Goi stored procedure `StoreName` voi bo tham so lay tu `GetDynamicParameters(pParams)` va tra ve toan bo result set qua `connection.QueryAsync<TResult>(...)` (`ExecuteSQLContext.cs:33-50`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `pParams` | `TClass` | Co (positional) | **Khong validate, khong null-check** | Khong co |
| `commandTimeout` | `int` | Khong | Khong validate | `30` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:36`) | `default` |
| `TResult` (type param) | — | Co | Khong co generic constraint | — |
| `commandType` | — | — | **Khong phai tham so** — hardcode `CommandType.StoredProcedure` (`:48`) | — |

**Output** — `Task<IEnumerable<TResult>>`: collection da buffer tra ve tu `QueryAsync<TResult>`; rong neu SP khong tra dong nao. **Khong co nhanh nao tra ve `null` tu code cua ham** (khac han `DapperSQLDBContext.GetAll<T>`).

**Dieu kien xu ly**
1. `cancellationToken.ThrowIfCancellationRequested()` (`:36`).
2. Tao connection trong `await using` (`:38-39`) — **khong co guard nao truoc buoc nay**, ke ca `StoreName` rong.
3. `GetDynamicParameters(pParams)` (`:41`).
4. `QueryAsync<TResult>` voi `commandType: CommandType.StoredProcedure` (`:43-49`).

**Side effect**
- Thuc thi stored procedure → moi side effect ben trong SP (ghi DB, gui message, goi linked server) deu xay ra.
- Tao/giai phong mot `SqlConnection` moi cho moi lan goi.
- Ghi log: **Khong co**.
- Dapper co the gan `AttachedParam` vao `DynamicParameters` vua tao boi `GetDynamicParameters`; vi object nay duoc tao moi moi lan goi (`:41`), khong co rui ro chia se state giua cac lan goi.

**Error handling** — Khong co try/catch; exception nem thang len caller. `await using` dam bao dispose connection.

**Khi nao NEN dung**
- Goi stored procedure tra ve mot result set, khi muon dong goi ten SP + mapping tham so vao mot lop rieng.
- Khi muon chu ky goi gon (`Execute<TDto>(request)`) thay vi truyen chuoi SQL o moi call site.

**Khi nao KHONG dung**
- Khi can chay SQL text: `CommandType` hardcode `StoredProcedure` (`:48`), khong the doi.
- Khi can lay gia tri tu **output parameter**: dung `ExecuteScalar<TResult>` (4.6) — `Execute` khong doc output parameter.
- Khi can retry/circuit breaker: khong co resilience o day va **khong co lop nao trong repo boc `Execute` bang `ResiliencePipeline`** (khac voi `GetOne`/`GetAll`/`GetOneExecute` duoc `CoreSQL` boc).
- Khi SP tra ve nhieu result set: khong ho tro `QueryMultiple`.

**Gioi han**
- Khong guard `StoreName` rong/null → neu lop con cai dat sai, connection van duoc tao roi loi phat sinh tu SQL Server.
- Khong ho tro transaction, khong ho tro truyen `DbConnection` tu ngoai.
- `commandTimeout` mac dinh 30 hardcode trong signature (`:34`).
- Buffer toan bo result set vao memory.
- Khong co resilience, khong co log, khong co OpenTelemetry activity (khac voi `SqlResiliencePolicyFactory` co `ActivitySource`, `SqlResiliencePolicyFactory.cs:18`).

---

### 4.6 `ExecuteScalar<TResult>`

**Signature**

```csharp
public async Task<TResult> ExecuteScalar<TResult>(
    TClass pParams, int commandTimeout = 30, CancellationToken cancellationToken = default)
```

**Muc dich** — Goi stored procedure `StoreName` bang `QueryAsync<TResult>` (**khong** phai `ExecuteScalarAsync`), sau do doc output parameter ten `P_RESULT` tu `DynamicParameters`; neu gia tri doc duoc la `null` thi lay dong dau tien cua result set (`ExecuteSQLContext.cs:63-85`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `pParams` | `TClass` | Co (positional) | Khong validate, khong null-check | Khong co |
| `commandTimeout` | `int` | Khong | Khong validate | `30` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:66`) | `default` |
| `TResult` (type param) | — | Co | Khong co generic constraint | — |
| Output parameter `P_RESULT` | — | Ngam dinh **bat buoc** | Ten `"P_RESULT"` **hardcode** tai `:82`; lop con phai tu khai bao trong `GetDynamicParameters` | Khong co |

**Output** — `Task<TResult>`, quyet dinh boi bieu thuc `:84`:

```csharp
TResult result = parameters.Get<TResult>("P_RESULT");

return result is null ? data.FirstOrDefault() : result;
```

| Truong hop | Gia tri tra ve |
|---|---|
| `P_RESULT` co gia tri khac `null` | Gia tri cua `P_RESULT` |
| `P_RESULT` doc ra `null` **va** `TResult` la reference/nullable type | `data.FirstOrDefault()` — dong dau tien cua result set, hoac `default` neu result set rong |
| `TResult` la value type khong nullable (`int`, `long`, `bool`, `decimal`...) | **Luon tra ve `result`**: voi generic khong constraint, `result is null` cho value type luon la `false`, nen nhanh `data.FirstOrDefault()` khong bao gio chay |
| `P_RESULT` **khong duoc khai bao** trong `GetDynamicParameters` | Khong xac dinh duoc tu source code cua repo nay — phu thuoc cai dat `DynamicParameters.Get<T>` cua Dapper. Code **khong co** try/catch hay kiem tra ton tai truoc khi goi (`:82`), nen day la duong that bai khong duoc bao ve |

**Dieu kien xu ly** (theo thu tu thuc thi)
1. `cancellationToken.ThrowIfCancellationRequested()` (`:66`).
2. Tao connection trong `await using` (`:68-69`) — khong co guard nao truoc do.
3. `GetDynamicParameters(pParams)` (`:71`).
4. `QueryAsync<TResult>` voi `commandType: CommandType.StoredProcedure` (`:73-80`) → bien `data`.
5. `parameters.Get<TResult>("P_RESULT")` (`:82`) → bien `result`.
6. `return result is null ? data.FirstOrDefault() : result;` (`:84`).

> [!IMPORTANT]
> Thu tu buoc 4 truoc buoc 5 la co y nghia ky thuat: gia tri output parameter cua SQL Server chi san sang sau khi result set duoc doc xong. Vi `QueryAsync` mac dinh buffered (khong truyen `flags` nao o `:73-80`), tap ket qua da duoc doc het truoc khi `:82` chay.

**Side effect**
- Thuc thi stored procedure → moi side effect ben trong SP xay ra.
- Tao/giai phong mot `SqlConnection` moi cho moi lan goi.
- Mutate `DynamicParameters` do `GetDynamicParameters` tao: Dapper ghi gia tri output parameter vao object nay truoc khi `:82` doc. Object nay duoc tao moi moi lan goi nen khong ro ri state ra ngoai.
- Ghi log: **Khong co**.

**Error handling** — Khong co try/catch. Moi exception nem thang len caller, **bao gom ca loi phat sinh tu `parameters.Get<TResult>("P_RESULT")` khi lop con khong khai bao tham so nay**. `await using` van dispose connection.

**Khi nao NEN dung**
- Goi stored procedure tra ket qua qua output parameter dat ten dung `P_RESULT` (quy uoc dat ten kieu Oracle/PL-SQL), va lop con da `Add("P_RESULT", dbType: ..., direction: ParameterDirection.Output)` trong `GetDynamicParameters`.
- Khi SP vua tra output parameter vua co the tra mot result set du phong: co san co che fallback (chi hieu luc voi `TResult` la reference/nullable type).

**Khi nao KHONG dung**
- Khi SP **khong** co output parameter ten `P_RESULT`: ten nay hardcode (`:82`) va khong co guard; day la duong loi khong duoc xu ly.
- Khi output parameter mang ten khac (`@Result`, `P_OUT`, `RETURN_VALUE`...): khong co cach cau hinh, phai viet lai lop co so.
- Khi `TResult` la value type va ban dua vao co che fallback: fallback khong hoat dong (xem bang Output).
- Khi can lay nhieu gia tri (nhieu output parameter): chi doc duoc dung mot.
- Khi can retry/circuit breaker: khong co resilience.

**Gioi han**
- Ten `"P_RESULT"` hardcode (`:82`), khong co constant, khong cau hinh duoc, khong duoc kiem tra ton tai truoc khi doc.
- Nhanh fallback `data.FirstOrDefault()` la **dead code khi `TResult` la value type khong nullable** — `result is null` khong bao gio dung.
- Ten ham `ExecuteScalar` gay hieu nham: ham **khong** goi `ExecuteScalarAsync` ma goi `QueryAsync` (`:74`), tuc **luon** doc va buffer toan bo result set du chi can 1 gia tri → chi phi cao hon `ExecuteScalarAsync` khi SP tra ve nhieu dong.
- `commandTimeout` mac dinh 30 hardcode trong signature (`:64`).
- Khong co log, khong co resilience, khong ho tro transaction.

---

## 5. Chi tiet API — `ConfigurationHelpers` (static class)

> Nguon: `FTELSRCore.Shared/Data/SQL/Dapper/Helpers/ConfigurationHelpers.cs`
> Loai: `static class`

### 5.1 `CreateConnection`

**Signature**

```csharp
public static SqlConnection CreateConnection(string connection)
```

**Muc dich** — Tao mot instance `SqlConnection` moi tu chuoi ket noi. Than ham chi co dung mot dong: `return new SqlConnection(connection);` (`ConfigurationHelpers.cs:18`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `connection` | `string` | Co | **Khong co validate nao** trong ham — khong null-check, khong kiem tra dinh dang | Khong co |

**Output** — `SqlConnection` **chua duoc mo** (`State == Closed`). Khong co nhanh nao tra ve `null`.

**Dieu kien xu ly** — Khong co nhanh re, khong guard clause, khong switch.

**Side effect** — Khong mo connection, khong ghi log, khong cache, khong doc configuration. Chi cap phat mot object moi.

**Error handling** — Khong co try/catch. Neu chuoi ket noi sai dinh dang, exception do constructor `SqlConnection` nem se truyen thang len caller.

**Khi nao NEN dung**
- Trong tang Dapper cua library nay: ca 7 vi tri goi deu boc trong `await using` (`DapperSQLDBContext.cs:41,77,113,150,188` va `ExecuteSQLContext.cs:38,68`).
- Khi can mot `SqlConnection` moi ma caller tu chiu trach nhiem dispose.

**Khi nao KHONG dung**
- Khi ky vong ham nay **doc hoac build gia tri config**: bat chap ten class, ham **khong** doc `IConfiguration`, khong doc appsettings, khong dung `SqlConnectionStringBuilder`, khong sinh gia tri mac dinh nao.
- Khi ky vong nhan connection da mo: caller phai tu mo, hoac dua vao viec Dapper tu mo (trong 4 file nay **khong co dong nao goi `Open()` / `OpenAsync()`**).
- Khi can `DbConnection` cho provider khac SQL Server: kieu tra ve co dinh la `SqlConnection`.

**Gioi han**
- Lop `static` → khong the mock/stub trong unit test; muon thay the phai tach interface hoac dung shim.
- Khong co validate, khong co logging, khong co telemetry.
- Ten class va namespace (`...Dapper.Helpers.ConfigurationHelpers`) **trung ten** voi `FTELSRCore.Data.MongoDB.Helpers.ConfigurationHelpers` (`FTELSRCore.Shared/Data/MongoDB/Helpers/ConfigurationHelpers.cs:13`) — de nham lan khi doc code hoac khi `using` ca hai namespace.

---

## 6. Bao mat, resilience va CommandType — ket luan tu source code

### 6.1 Tham so hoa (SQL injection)

| Cau hoi | Ket luan dua tren code |
|---|---|
| Tham so duoc truyen the nao? | Qua `DynamicParameters` dat vao `CommandDefinition.parameters` — `DapperSQLDBContext.cs:47,83,119,156,194`; `ExecuteSQLContext.cs:46,77`. Dapper tao SQL parameter thuc su → **gia tri tham so duoc parameterize, chong SQL injection cho phan gia tri**. |
| Co noi chuoi tham so vao SQL trong tang nay khong? | **Khong.** Trong 4 file khong co bat ky phep noi chuoi / interpolation nao de dung tham so vao `pSqlQuery`. |
| Rui ro con lai? | Ban than chuoi `pSqlQuery` do **caller** cung cap va tang nay khong sanitize. Vi du o tang tren, `CoreSQL` noi chuoi bang string interpolation: `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; {scriptSQLQuery}` (`CoreSQL.cs:84-88`, `CoreSQL.cs:134-138`, `CoreSQL.cs:184-188`). Neu caller cua `CoreSQL` tu ghep input nguoi dung vao `scriptSQLQuery` thi injection xay ra **truoc khi** vao tang Dapper. |
| `ExecuteSQLContext<TClass>` | Tham so luon di qua `DynamicParameters` (`ExecuteSQLContext.cs:41,45,71,76`) va `commandType` luon la `StoredProcedure` (`:48`, `:79`) nen `commandText` duoc SQL Server hieu la **ten** SP chu khong phai cau lenh. Nhung `StoreName` la `protected abstract string` (`:17`) — **source code cua lop co so khong ep no phai la hang so va khong validate gi**; neu lop con sinh `StoreName` tu input ben ngoai thi rui ro van ton tai. |

### 6.2 `CommandType`

| Thanh phan | `CommandType` |
|---|---|
| `DapperSQLDBContext` — ca 5 method | Tham so `commandType`, **mac dinh `CommandType.Text`**; caller phai truyen `CommandType.StoredProcedure` neu goi SP (`DapperSQLDBContext.cs:31,67,103,140,178`) |
| `ExecuteSQLContext<TClass>.Execute` | **Hardcode `CommandType.StoredProcedure`** (`ExecuteSQLContext.cs:48`) |
| `ExecuteSQLContext<TClass>.ExecuteScalar` | **Hardcode `CommandType.StoredProcedure`** (`ExecuteSQLContext.cs:79`) |

### 6.3 Resilience — so sanh voi `SqlResiliencePolicyFactory`

`SqlResiliencePolicyFactory` (`FTELSRCore.Shared/Data/SQL/Helpers/Policies/SqlResiliencePolicyFactory.cs`) dung Polly de xay 2 pipeline: `ConfigureReadPolicy` (retry 3 lan exponential + jitter, circuit breaker 60% / 5 request / 10s → break 20s, `SqlResiliencePolicyFactory.cs:59-146`) va `ConfigureWritePolicy` (retry 1 lan chi voi loi connection-level, CB 50% / 10 request / 15s → break 60s, `:154-213`), kem `ActivitySource` cho OpenTelemetry (`:18`).

| Thanh phan | Co resilience noi tai? | Ghi chu |
|---|---|---|
| `DapperSQLDBContext` (ca 5 method) | **Khong** — khong co dong code nao tham chieu `Polly` / `ResiliencePipeline` trong file | |
| `ExecuteSQLContext<TClass>` | **Khong** | |
| `ConfigurationHelpers` | **Khong** | |
| `GetOne` / `GetAll` / `GetOneExecute` khi goi **qua** `CoreSQL` (ca 2 overload) | **Co, do tang goi cung cap** | Duoc boc trong `_pipelineRead.ExecuteAsync(...)`: `CoreSQL.cs:92-102`, `CoreSQL.cs:142-152`, `CoreSQL.cs:192-202`; `CoreSQLTenant.cs:99-109`, `CoreSQLTenant.cs:151-161`, `CoreSQLTenant.cs:201-211` |
| `ExecuteNonQueryAsync` / `GetAllExecuteAsync` | **Khong** o moi cap | Khong co dong code nao trong repo goi hai method nay, nen cung khong co noi nao boc pipeline cho chung |
| `ExecuteSQLContext.Execute` / `ExecuteScalar` | **Khong** o moi cap | Khong co lop con nao trong repo, khong co noi nao boc pipeline |

> [!WARNING]
> **SQL di qua tang Dapper nay bo qua toan bo retry va circuit breaker neu caller khong tu boc pipeline.** Cac pipeline `ResiliencePipeline` duoc **inject vao `CoreSQL` qua constructor** (`CoreSQL.cs:40`, `CoreSQLTenant.cs:45`) chu khong duoc tao ben trong tang Dapper. Luu y: `SqlResiliencePolicyFactory` chi **cau hinh** builder (`ConfigureReadPolicy` / `ConfigureWritePolicy` la `static void`, `SqlResiliencePolicyFactory.cs:59,154`) — no khong tao san pipeline nao va khong duoc tang Dapper tham chieu. Hau qua:
> - Goi truc tiep `IDapperSQLDBContext` (khong qua `CoreSQL`) → khong retry, khong circuit breaker, khong ghi log canh bao. Mot loi transient (deadlock `1205`, connection broken `-1`, command timeout `-2` — cac ma nam trong `RetryableSqlErrors`, `SqlResiliencePolicyFactory.cs:24-36`) se lam that bai ngay lap tuc.
> - Ke ca khi goi qua `CoreSQL`, ca 3 duong Dapper deu dung `_pipelineRead` (pipeline **read**) — bao gom truong hop caller truyen SQL co tac dung ghi. Khong co duong Dapper nao dung `_pipelineWrite`.
> - Circuit breaker cua Polly la state per-pipeline-instance; vi tang Dapper khong tham gia, cac lan goi truc tiep khong dong gop vao (cung khong bi chan boi) trang thai circuit breaker.

### 6.4 Vong doi connection

| Cau hoi | Ket luan tu code |
|---|---|
| Moi lan goi co tao `SqlConnection` moi? | **Co.** Moi method goi `ConfigurationHelpers.CreateConnection(...)` rieng: `DapperSQLDBContext.cs:41,77,113,150,188`; `ExecuteSQLContext.cs:38,68`. Khong co connection nao duoc luu vao field hay cache. |
| Connection duoc dong/dispose the nao? | Bang `await using` (async dispose) tai chinh cac dong tren — dispose khi ra khoi scope method, ke ca khi co exception. Khong co `Close()` tuong minh, khong co `finally`. |
| Connection duoc mo o dau? | **Khong co dong `Open()` / `OpenAsync()` nao trong 4 file** (da xac nhan bang grep). `CreateConnection` tra ve connection dang `Closed` (`ConfigurationHelpers.cs:16-19`). Viec mo connection do Dapper dam nhiem khi thuc thi command. |
| Co ho tro transaction? | **Khong.** `CommandDefinition` duoc khoi tao ma khong truyen `transaction` o ca 7 vi tri. Muon transaction phai dung `CoreSQL.IsExecuteNonQueryAsync(...)` — nhan `DbConnection` + `DbTransaction` tu ngoai (`CoreSQL.cs:217-224`). |
| Rui ro concurrency | `DapperSQLDBContext` chi giu mot field `readonly string _dbConnection` (`DapperSQLDBContext.cs:16`), khong co mutable state → **an toan de goi song song tu nhieu luong**. Voi `ExecuteSQLContext<TClass>`: ban than lop co so cung khong khai bao field mutable nao, nhung `connectionString` la primary-constructor parameter duoc compiler capture thanh backing field **khong readonly** (khong co khai bao `readonly` nao trong file), va ca `StoreName` / `GetDynamicParameters` deu la abstract → **muc do an toan khi goi song song phu thuoc cai dat cua lop con**, khong xac dinh duoc tu lop co so. Ngoai ra, caller tai su dung cung mot instance `DynamicParameters` cho nhieu lan goi dong thoi la rui ro chung, vi Dapper co the gan state vao object do. |

---

## 7. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | Tang Dapper khong co bat ky resilience nao (khong Polly, khong retry, khong circuit breaker), khac han duong EF Core duoc `SqlResiliencePolicyFactory` bao ve. Retry chi co khi tang goi tu boc pipeline. | `DapperSQLDBContext.cs:28-200`, `ExecuteSQLContext.cs:33-85`; doi chieu `SqlResiliencePolicyFactory.cs:59-213` | Loi transient (deadlock 1205, connection broken -1, timeout -2) lam that bai ngay khi goi truc tiep `IDapperSQLDBContext` hoac `ExecuteSQLContext` |
| 2 | `TypeConnection` la `protected abstract byte` nhung **khong duoc tham chieu o bat ky dong nao** trong repo (member vestigial) | `ExecuteSQLContext.cs:19` | Moi lop con bat buoc override mot member vo dung; gay hieu nham rang lop co so biet chon read/write connection — thuc te khong |
| 3 | Ten output parameter `"P_RESULT"` hardcode, khong co constant, khong cau hinh duoc, khong kiem tra ton tai truoc khi doc | `ExecuteSQLContext.cs:82` | Neu lop con khong khai bao `P_RESULT` trong `GetDynamicParameters`, `parameters.Get<TResult>("P_RESULT")` la duong that bai khong duoc guard/try-catch bao ve |
| 4 | Nhanh fallback `data.FirstOrDefault()` la dead code khi `TResult` la value type khong nullable: voi generic khong constraint, `result is null` luon `false` cho value type | `ExecuteSQLContext.cs:84` | `ExecuteScalar<int>` / `<long>` / `<bool>` khong bao gio dung fallback; hanh vi khac tai lieu XML tai `ExecuteSQLContext.cs:53-55,61` |
| 5 | `ExecuteScalar` khong dung `ExecuteScalarAsync` ma dung `QueryAsync` → doc va buffer toan bo result set du chi lay 1 gia tri | `ExecuteSQLContext.cs:74` | Ten ham gay hieu nham; chi phi bo nho/thoi gian cao hon can thiet khi SP tra ve nhieu dong |
| 6 | XML doc interface noi `GetAllExecuteAsync` tra ve `Enumerable.Empty{T}` khi query rong, nhung code tra ve `null` (Source Code > Documentation) | Doc: `IDapperSQLDBContext.cs:79`; Code: `DapperSQLDBContext.cs:185` | Caller tin tai lieu se gap `NullReferenceException` khi `foreach` |
| 7 | `GetAll<T>` tra ve `default` (tuc `null`) thay vi empty collection khi query rong | `DapperSQLDBContext.cs:110` | Nguon `NullReferenceException` pho bien; caller buoc phai null-check |
| 8 | XML doc nhieu member noi "neu xay ra loi, tra ve gia tri mac dinh / null", nhung **khong co try/catch nao** trong ca 4 file — moi exception nem thang len caller | Doc: `IDapperSQLDBContext.cs:32,47,62`; Code: `DapperSQLDBContext.cs:28-200` | Tai lieu mo ta sai contract loi; caller khong bat exception se de vo luong |
| 9 | `GetAllExecuteAsync<T>` trung lap hoan toan hanh vi voi `GetAll<T>` (cung `QueryAsync`, cung guard, cung tra `null`), khong co khac biet nao doc ra duoc tu code | `DapperSQLDBContext.cs:100-123` vs `:175-200` | API du thua, gay nham lan khi chon method; ca hai deu khong ep `CommandType.StoredProcedure` du ten `...Execute...` goi y nhu vay |
| 10 | Toan bo tang khong ghi log: khong co `ILogger`, khong log khi short-circuit vi `pSqlQuery` rong, khong log khi loi | `DapperSQLDBContext.cs:36-39,72-75,108-111,145-148,183-186` | Tra ve `false`/`null` silent, rat kho debug production. Doi chieu: `CoreSQL` co log `_logger.FailLogic(...)` cho cung tinh huong (`CoreSQL.cs:77,127,177`) |
| 11 | Khong validate `connectionString`, `pParams`, `commandTimeout`, `commandType`, `StoreName`, `entry` o bat ky dau | `DapperSQLDBContext.cs:14,16`; `ExecuteSQLContext.cs:15,41,71` | Loi cau hinh chi bieu hien o tang SQL Server / Dapper voi thong bao kho truy nguyen |
| 12 | `commandTimeout = 30` hardcode lam gia tri mac dinh trong signature cua tat ca 7 method public, khong doc tu configuration | `DapperSQLDBContext.cs:30,66,102,139,177`; `ExecuteSQLContext.cs:34,64` | Khong the dieu chinh timeout toan he thong ma khong sua code hoac truyen tay o moi call site |
| 13 | Khong ho tro transaction: `CommandDefinition` khong bao gio duoc truyen `transaction`, moi lan goi mo connection rieng | `DapperSQLDBContext.cs:45-50,81-86,117-122,154-159,192-197`; `ExecuteSQLContext.cs:44-49,75-80` | Khong the goi nhieu lenh atomic qua tang nay; phai chuyen sang `CoreSQL.IsExecuteNonQueryAsync` (`CoreSQL.cs:217-224`) |
| 14 | `ExecuteNonQueryAsync` tra `bool` bang phep so sanh `> 0` → mat thong tin so dong bi anh huong va khong phan biet duoc "query rong" voi "0 dong bi anh huong" | `DapperSQLDBContext.cs:38`, `DapperSQLDBContext.cs:50` | Caller khong biet lenh da chay hay chua; moi truong hop ADO.NET tra `<= 0` (vi du `-1`) deu cho `false` du lenh thanh cong |
| 15 | `ConfigurationHelpers` khong doc/build gia tri config nao (khong `IConfiguration`, khong `SqlConnectionStringBuilder`) — ten class khong phan anh chuc nang; lai trung ten voi lop Mongo cung ten | `ConfigurationHelpers.cs:8-19`; trung ten voi `Data/MongoDB/Helpers/ConfigurationHelpers.cs:13` | Gay hieu nham khi doc code; de `using` nham namespace |
| 16 | `DapperSQLDBContext` la `sealed` va `ConfigurationHelpers` la `static` → kho mo rong va kho mock trong unit test | `DapperSQLDBContext.cs:14`; `ConfigurationHelpers.cs:8` | Muon them log/metric/retry phai viet decorator qua `IDapperSQLDBContext`; khong the stub viec tao connection |
| 17 | `GetAll<T>` / `GetAllExecuteAsync<T>` / `Execute<TResult>` / `ExecuteScalar<TResult>` deu buffer toan bo result set (khong truyen `CommandFlags` de tat buffering, khong co API streaming/paging) | `DapperSQLDBContext.cs:116,191`; `ExecuteSQLContext.cs:43,74` | Rui ro memory pressure voi truy van tra ve nhieu dong |
| 18 | Khong co dong code nao trong repo dang ky `IDapperSQLDBContext` vao DI container, va khong co lop con nao ke thua `ExecuteSQLContext<...>` | Grep toan repo: 0 ket qua cho `AddScoped<IDapperSQLDBContext`, `new DapperSQLDBContext(`, `: ExecuteSQLContext` | Ung dung tieu thu phai tu dang ky va tu cap connection string; `ExecuteSQLContext<TClass>` chua co tien le su dung trong repo nay |
| 19 | Bien local du thua khong them hanh vi: `result` trong `GetOneExecute<T>` va `GetAllExecuteAsync<T>` | `DapperSQLDBContext.cs:153,161` va `:191,199` | Chi la nhieu code hon; khong khac biet hanh vi so voi `return await ...` |
| 20 | `ExecuteSQLContext<TClass>` **khong co interface tuong ung** (khac `DapperSQLDBContext` co `IDapperSQLDBContext`) | `ExecuteSQLContext.cs:15` | Khong the mock trong unit test, khong the boc decorator de them log/retry/metric; muon thay doi hanh vi phai sua truc tiep lop co so hoac tu viet lop trung gian |
| 21 | Trong `ExecuteSQLContext`, connection duoc tao **truoc** khi `GetDynamicParameters` chay (thu tu nguoc voi `DapperSQLDBContext`: guard truoc, connection sau) | `ExecuteSQLContext.cs:38-41`, `:68-71` | Neu lop con nem exception khi map tham so (vi du `entry` la `null`), mot `SqlConnection` da duoc cap phat vo ich; ngoai ra `StoreName` rong cung khong bi phat hien truoc do |
| 22 | Ca 4 file khong co API nao doc **nhieu result set** (`QueryMultiple`) va khong co che do unbuffered/streaming: khong vi tri nao truyen `flags` cho `CommandDefinition` | `DapperSQLDBContext.cs:45-50,81-86,117-122,154-159,192-197`; `ExecuteSQLContext.cs:44-49,75-80` | SP tra nhieu result set chi doc duoc result set dau tien; truy van lon buoc phai buffer toan bo vao memory |
| 23 | `ExecuteSQLContext` doc ket qua duy nhat qua output parameter `P_RESULT`; **khong co dong code nao doc `ReturnValue` cua stored procedure** (`ParameterDirection.ReturnValue`) o bat ky file nao trong 4 file | `ExecuteSQLContext.cs:82` | SP dung `RETURN <code>` de bao trang thai thi gia tri do bi mat hoan toan qua tang nay |
