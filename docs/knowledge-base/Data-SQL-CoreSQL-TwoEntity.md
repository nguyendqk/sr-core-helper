# CoreSQL&lt;TEntityFrom, TEntityTo, DBContextRead, DBContextWrite&gt;

> Nguon: `FTELSRCore.Shared/Data/SQL/Core/CoreSQLTenant.cs`
> Loai: abstract partial class (generic, 4 type parameter) - implement `ICoreSQL<TEntityFrom, DBContextRead, DBContextWrite>`
> Cap nhat theo commit: `2262829`

---

## 1. Tong quan

`CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>` la lop base repository truu tuong cho tang truy cap du lieu SQL Server, tach doi ngu canh doc/ghi (CQRS-style: `DBContextRead` va `DBContextWrite`), ket hop EF Core (truy van LINQ) va Dapper (raw SQL). Diem khac biet duy nhat so voi `CoreSQL<TEntity, DBContextRead, DBContextWrite>` (3 type parameter, trong `CoreSQL.cs`) la lop nay lam viec voi **hai kieu entity**: `TEntityFrom` la kieu o tang domain/service (kieu vao - ra cua moi API public), con `TEntityTo` la kieu thuc su duoc EF Core dung de truy van/ghi bang (`Set<TEntityTo>()` tai `CoreSQLTenant.cs:279, 317, 661, 939, ...`). Moi lan doc/ghi deu di kem mot buoc **anh xa (mapping/projection) theo ten thuoc tinh bang reflection / expression tree**.

> [!WARNING]
> **Ten file gay hieu nham - lop nay KHONG co logic multi-tenant.**
> Ket qua grep chuoi `enant` tren toan bo `CoreSQLTenant.cs` la **0 ket qua**. Trong toan file khong co: tenant id, tenant column, tenant context, tenant resolver, global query filter theo tenant, hay bat ky doan code loc du lieu theo don vi thue nao. Tu `Tenant` chi ton tai o **ten file**, khong ton tai o ten class (`CoreSQL`, dong 17), ten region (`BaseSQLRepository Tồn tại <TEntityFrom, TEntityTo>`, dong 15), hay bat ky thanh vien nao.
> Vai tro thuc te cua `TEntityFrom`/`TEntityTo` la **mapping entity nguon sang entity dich**, duoc xac nhan boi:
> - `CoreSQLTenant.cs:641` - `ProjectToExtensions.MapUsingExpression<TEntityFrom, TEntityTo>(entity)` khi ghi.
> - `CoreSQLTenant.cs:291` - `result?.ProjectTo<TEntityTo, TEntityFrom>()` khi doc.
> - `CoreSQLTenant.cs:483-484` - `filters.ReplaceParameters<TEntityFrom, TEntityTo>()` de dich bieu thuc loc tu kieu nguon sang kieu dich.
> - Chinh XML doc trong `CoreSQL.cs:318-321` mo ta lop nay la "lớp CoreSQL&lt;TEntityFrom, TEntityTo,...&gt; (ánh xạ TEntityFrom -&gt; TEntityTo)".

Lop nay la `abstract partial`, khong the khoi tao truc tiep; repository nghiep vu phai ke thua. Trong repo hien tai **khong tim thay lop con nao ke thua lop nay** (grep `: CoreSQL<` khong co ket qua ngoai worktree).

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Truy van raw SQL / stored procedure qua Dapper, tra ve 1 dong, nhieu dong, hoac 1 gia tri scalar (`CoreSQLTenant.cs:73, 125, 175`) | **Khong co bat ky logic multi-tenant nao** (grep `enant` = 0 ket qua). Khong loc theo tenant, khong resolve tenant context |
| Thuc thi lenh non-query (INSERT/UPDATE/DELETE raw SQL) tren `DbConnection` + `DbTransaction` do caller cung cap (`CoreSQLTenant.cs:226`) | **Khong co API Delete / Remove / SoftDelete nao** (grep `delete`, `remove` chi ra `const IsDeleted` va `isDeleted` param) |
| Tim theo khoa chinh qua `FindAsync` tren `DBContextRead` (`CoreSQLTenant.cs:261`) | Khong ho tro phan trang (`Skip`/`Take`), khong co `CountAsync`, `AnyAsync`, `ExistsAsync` |
| Loc bang mang `Expression<Func<TEntityFrom, bool>>[]`, tu dong dich sang `Expression<Func<TEntityTo, bool>>[]` (`CoreSQLTenant.cs:484-487`) | Khong ho tro sap xep (`OrderBy`/`sorting`). Overload `FindOneSortDeletedAsync` co `sorting` chi ton tai o lop 3 type parameter (`CoreSQL.cs:330`), **khong co o lop nay** |
| Loc "soft delete" theo cot `IsDeleted` qua tham so `isDeleted` (`CoreSQLTenant.cs:319, 361, 479, 521`) | Khong tu dong loai bo ban ghi da xoa mem: cac API khong co hau to `SortDeleted` (`FindOneAsync`, `FindAllAsync`, `FindByIdAsync`) **khong loc `IsDeleted`** |
| Insert / Update mot doi tuong hoac danh sach, co hoac khong co `DBContextWrite` do caller truyen vao (8 overload `CreateAsync`/`UpdateAsync`) | Khong tu mo/commit/rollback transaction. Khong co `BeginTransactionAsync`, khong co `SaveChangesAsync` rieng le |
| Chuyen tiep `AuditModel` sang `WriteDbContext.SaveChangesAsync(audit, ...)` (`WriteDbContext.cs:75`). Tac dung thuc te cua `auditLog` la **dien cac cot audit tren chinh ban ghi** (`IsDeleted`, `CreatedUser/CreatedDate/...`, `ModifiedUser/ModifiedDate/...`) qua `OnBeforeSaveChanges`, va **chi voi entity implement `IBaseEntitySQL`** (`WriteDbContext.cs:129-184`) | **Khong sinh audit trail (ban ghi audit log) nao.** `DetectChangesAudit` luon `return []` - toan bo than ham thu thap thay doi bi comment voi nhan `NOT SUPPORT` (`WriteDbContext.cs:201-353`), nen `DispatchAuditLog` luon thoat ngay (`WriteDbContext.cs:373-378`). Truyen `auditLog` khac `null` **khong** tao them ban ghi audit o bat ky dau |
| Chuyen tiep `DomainEvents` tu `TEntityFrom` sang `TEntityTo` khi ca hai implement `IAggregate` (`CoreSQLTenant.cs:651-654`) | Khong tu publish domain event; viec publish do `WriteDbContext.DispatchDomainEvents` thuc hien va **chi khi `SaveChangesAsync` tra ve `> 0`** (`WriteDbContext.cs:88-91`). Ngoai ra `DispatchDomainEvents` chi thu event tu `ChangeTracker.Entries<Aggregate>()` - **lop truu tuong `Aggregate`, khong phai interface `IAggregate`** (`WriteDbContext.cs:421`); entity chi implement `IAggregate` ma khong ke thua `Aggregate` se **khong bao gio duoc publish** |
| | **Khong bao ve `ChangeTracker` cua `context` do caller truyen vao**: sau moi lan `SaveChangesAsync` thanh cong, `DispatchDomainEvents` goi `ChangeTracker.Clear()` (`WriteDbContext.cs:433`) - xoa sach toan bo entity dang duoc theo doi tren context dung chung, ke ca entity khong lien quan den lan ghi nay |
| Bao boc moi lenh doc bang `_pipelineRead` va moi `SaveChangesAsync` bang `_pipelineWrite` (Polly `ResiliencePipeline`) | **`IsExecuteNonQueryAsync` KHONG duoc bao boc bang Polly** (`CoreSQLTenant.cs:244` goi truc tiep `context.ExecuteAsync`) |
| Ghi log nghiep vu qua `_logger.FailLogic(...)` khi guard clause chan input | Khong `try/catch` bat ky exception nao trong toan bo file. Moi exception cua EF Core / Dapper / Polly deu nem ra ngoai |
| | Khong validate noi dung raw SQL (chi kiem tra rong/trang). Khong chong SQL injection |
| | Khong co cache. `MapUsingExpression` compile lai expression tree cho **tung** entity moi lan goi (`ProjectToExtensions.cs:185`) |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `ILogger<CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>>` (`CoreSQLTenant.cs:28`) | Ghi log guard clause qua extension `FailLogic` (`LoggerExtensions.cs:358`), category `BIZ_LOGIC`, `EventId = 107`. **Muc log la `LogLevel.Information`** (`LoggerExtensions.cs:179-182`), khong phai `Warning`/`Error` - moi guard clause "am tham tra ve 0/null" se bi mat neu minimum level cua he thong tu `Warning` tro len |
| `Lazy<IDapperSQLDBContext>` (`CoreSQLTenant.cs:34`) | Thuc thi raw SQL / stored procedure. Moi lenh mo mot `SqlConnection` moi roi giai phong (`DapperSQLDBContext.cs:8-16`) |
| `Lazy<IDbContextFactory<DBContextRead>>` (`CoreSQLTenant.cs:36`) | Tao `DBContextRead` cho tung lan doc bang EF Core |
| `Lazy<IDbContextFactory<DBContextWrite>>` (`CoreSQLTenant.cs:38`) | Tao `DBContextWrite` cho cac overload `CreateAsync`/`UpdateAsync` khong nhan `context` |
| `ResiliencePipeline _pipelineRead` (`CoreSQLTenant.cs:30`) | Polly pipeline cho luong doc. Cau hinh mau: circuit breaker 60%/5 request/10s, break 20s + retry 3 lan exponential + jitter (`SqlResiliencePolicyFactory.cs:59-146`) |
| `ResiliencePipeline _pipelineWrite` (`CoreSQLTenant.cs:32`) | Polly pipeline cho luong ghi. Cau hinh mau: circuit breaker 50%/10 request/15s, break 60s + retry **1 lan**, chi loi connection-level (`SqlResiliencePolicyFactory.cs:154-212`) |
| `ProjectToExtensions.ProjectTo<TEntity, TDto>` (`ProjectToExtensions.cs:27, 76`) | Anh xa ket qua doc (`TEntityTo` -> `TEntityFrom`/`TDto`) bang reflection `PropertyInfo.SetValue`. Chi copy thuoc tinh cung ten, ghi duoc va khong danh dau `[NoMap]` (`ProjectToExtensions.cs:47-52`). **Khong kiem tra kieu** truoc khi `SetValue` - sai kieu se nem exception va bi `catch` bo qua. Yeu cau kieu dich co constructor khong tham so (`Activator.CreateInstance`, `:29` va `:92`) |
| `ProjectToExtensions.MapUsingExpression<TFrom, TTo>` (`ProjectToExtensions.cs:138`) | Anh xa entity truoc khi ghi (`TEntityFrom` -> `TEntityTo`) bang expression tree compile |
| `ProjectToExtensions.ReplaceParameters<TFrom, TTo>` (`ProjectToExtensions.cs:347`) | Dich mang bieu thuc loc tu tham so kieu `TEntityFrom` sang `TEntityTo` (dung `WhereReplacerVisitor`, `ProjectToExtensions.cs:392`) |
| `CollectionHelpers.IsNullOrEmpty<T>` (`CollectionHelpers.cs:14`) | Kiem tra danh sach null/rong |
| `IAggregate` / `IDomainEvent` (`Abstractions/Aggregate.cs:5`) | Chuyen tiep `List<IDomainEvent>` giua entity nguon va entity dich |
| `AuditModel` - `record`, namespace `FTELSRCore.Models.Audits` (`Models/Audits/AuditModel.cs:3`) | Thong tin nguoi thuc hien (`Ip`, `Device`, `Method`, `Address`, `CreatorInfo`), truyen xuong `WriteDbContext.SaveChangesAsync`. Chi `CreatorInfo.Name/Code/Organization` duoc dung thuc te (`WriteDbContext.cs:139-146`); `Ip`, `Device`, `Method`, `Address` **khong duoc doc o dau** trong duong ghi SQL (chi xuat hien trong doan code da bi comment `NOT SUPPORT`) |
| `Dapper` (`DynamicParameters`, `SqlMapper.ExecuteAsync`) | Tham so hoa va thuc thi raw SQL |
| `Microsoft.EntityFrameworkCore` (`EF.Property<bool>`, `AsNoTracking`, `FindAsync`) | Truy van LINQ, doc thuoc tinh shadow/`IsDeleted` |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `CoreSQL(...)` | Constructor | `protected`, nhan 6 dependency, chi gan field, khong validate |
| `FindOneWithScriptAsync<TDto>` | Raw SQL - doc | Tra ve 1 dong dau tien, mapping bang Dapper |
| `FindAllWithScriptAsync<TDto>` | Raw SQL - doc | Tra ve `IEnumerable<TDto>` |
| `FindOneWithScalarScriptAsync<TDto>` | Raw SQL - doc | Tra ve 1 gia tri scalar (`ExecuteScalarAsync`) |
| `IsExecuteNonQueryAsync` | Raw SQL - ghi | Thuc thi lenh non-query tren connection + transaction do caller cung cap |
| `FindByIdAsync` | EF Core - doc | Tim theo khoa chinh, khong loc `IsDeleted` |
| `FindOneSortDeletedAsync<TDto>` | EF Core - doc | 1 ban ghi, loc `IsDeleted`, tra ve `TDto` |
| `FindOneSortDeletedAsync` | EF Core - doc | 1 ban ghi, loc `IsDeleted`, tra ve `TEntityFrom` |
| `FindOneAsync<TDto>` | EF Core - doc | 1 ban ghi, **khong** loc `IsDeleted`, tra ve `TDto` |
| `FindOneAsync` | EF Core - doc | 1 ban ghi, **khong** loc `IsDeleted`, tra ve `TEntityFrom` |
| `FindAllSortDeletedAsync<TDto>` | EF Core - doc | Danh sach, loc `IsDeleted`, tra ve `List<TDto>` |
| `FindAllSortDeletedAsync` | EF Core - doc | Danh sach, loc `IsDeleted`, tra ve `List<TEntityFrom>` |
| `FindAllAsync<TDto>` | EF Core - doc | Danh sach, **khong** loc `IsDeleted`, tra ve `List<TDto>` |
| `FindAllAsync` | EF Core - doc | Danh sach, **khong** loc `IsDeleted`, tra ve `List<TEntityFrom>` |
| `CreateAsync(TEntityFrom, AuditModel, CancellationToken)` | EF Core - ghi | Insert 1 ban ghi, tu tao context, tra ve `int` |
| `CreateAsync(TEntityFrom, DBContextWrite, AuditModel, CancellationToken)` | EF Core - ghi | Insert 1 ban ghi tren context ngoai, tra ve tuple `(int Result, TEntityFrom Data)` |
| `CreateAsync(IEnumerable<TEntityFrom>, AuditModel, CancellationToken)` | EF Core - ghi | Insert nhieu ban ghi, tu tao context, tra ve `int` |
| `CreateAsync(IEnumerable<TEntityFrom>, DBContextWrite, AuditModel, CancellationToken)` | EF Core - ghi | Insert nhieu ban ghi tren context ngoai, tra ve tuple |
| `UpdateAsync(TEntityFrom, AuditModel, CancellationToken)` | EF Core - ghi | Update 1 ban ghi, tu tao context, tra ve `int` |
| `UpdateAsync(TEntityFrom, DBContextWrite, AuditModel, CancellationToken)` | EF Core - ghi | Update 1 ban ghi tren context ngoai, tra ve tuple |
| `UpdateAsync(IEnumerable<TEntityFrom>, AuditModel, CancellationToken)` | EF Core - ghi | Update nhieu ban ghi, tu tao context, tra ve `int` |
| `UpdateAsync(IEnumerable<TEntityFrom>, DBContextWrite, AuditModel, CancellationToken)` | EF Core - ghi | Update nhieu ban ghi tren context ngoai, tra ve tuple |

Tong: **1 constructor + 21 method public** (dung bang so luong thanh vien khai bao trong `ICoreSQL.cs`, 21 method).

---

## 2. Chi tiet API

### 2.1 CoreSQL (constructor)

**Signature**

```csharp
protected CoreSQL(
    ILogger<CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>> logger,
    Lazy<IDapperSQLDBContext> dapperDbContext,
    Lazy<IDbContextFactory<DBContextRead>> contextRead,
    Lazy<IDbContextFactory<DBContextWrite>> contextWrite,
    ResiliencePipeline pipelineRead, ResiliencePipeline pipelineWrite)
```

Rang buoc generic (`CoreSQLTenant.cs:19-22`):

```csharp
where TEntityFrom : class
where TEntityTo : class
where DBContextRead : ReadDbContext<DBContextRead>
where DBContextWrite : WriteDbContext<DBContextWrite>
```

**Muc dich** - Gan 6 dependency vao field `private readonly` (`CoreSQLTenant.cs:46-57`). Khong lam gi khac.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `logger` | `ILogger<CoreSQL<...>>` | Co | Khong validate (khong null-check) | Khong co |
| `dapperDbContext` | `Lazy<IDapperSQLDBContext>` | Co | Khong validate | Khong co |
| `contextRead` | `Lazy<IDbContextFactory<DBContextRead>>` | Co | Khong validate | Khong co |
| `contextWrite` | `Lazy<IDbContextFactory<DBContextWrite>>` | Co | Khong validate | Khong co |
| `pipelineRead` | `ResiliencePipeline` | Co | Khong validate | Khong co |
| `pipelineWrite` | `ResiliencePipeline` | Co | Khong validate | Khong co |

**Output** - Khong co (constructor).

**Dieu kien xu ly** - Khong co nhanh re. 6 phep gan tuan tu (`CoreSQLTenant.cs:46-57`).

**Side effect** - Thay doi state noi bo cua doi tuong (gan field). Khong ghi DB, khong ghi log.

**Error handling** - Khong co `try/catch`. Vi khong null-check, truyen `null` se khong loi tai day nhung se gay `NullReferenceException` o lan goi method dau tien.

**Khi nao NEN dung** - Goi tu constructor cua repository con qua `: base(...)`, voi cac dependency lay tu DI container.

**Khi nao KHONG dung** - Khong the goi truc tiep (`abstract` + `protected`).

**Gioi han** - Khong validate dependency; loi cau hinh DI chi bieu hien tai runtime khi goi method. `Lazy<T>` nghia la connection/DbContext chi duoc khoi tao o lan truy cap `.Value` dau tien.

---

### 2.2 FindOneWithScriptAsync&lt;TDto&gt;

**Signature**

```csharp
public virtual async Task<TDto> FindOneWithScriptAsync<TDto>(
    string scriptSQLQuery,
    DynamicParameters parameters,
    int commandTimeout = 30,
    CommandType commandType = CommandType.Text,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Thuc thi `scriptSQLQuery` qua Dapper `QueryFirstOrDefaultAsync<TDto>` va tra ve dong dau tien duoc anh xa sang `TDto` (`CoreSQLTenant.cs:99-109` -> `DapperSQLDBContext.cs:64-87`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `scriptSQLQuery` | `string` | Co | `string.IsNullOrWhiteSpace` -> log + `return default` (`CoreSQLTenant.cs:82-87`). **Khong** validate cu phap, khong whitelist, khong chong injection | Khong co |
| `parameters` | `DynamicParameters` | Khong (co the `null`) | Khong validate; truyen thang cho Dapper | Khong co |
| `commandTimeout` | `int` | Khong | Khong validate (khong chan gia tri am hoac 0) | `30` |
| `commandType` | `CommandType` | Khong | Chi phan biet `CommandType.Text` va cac gia tri con lai (`CoreSQLTenant.cs:89-96`) | `CommandType.Text` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` ngay dau ham (`CoreSQLTenant.cs:80`) | `default` |

**Output** - `Task<TDto>`.
- Script rong/trang: tra ve `default` cua `TDto` (`null` neu `TDto` la reference type, `0`/`false`/struct rong neu la value type) - `CoreSQLTenant.cs:86`.
- Truy van khong co dong nao: tra ve `default` do Dapper `QueryFirstOrDefaultAsync` (`DapperSQLDBContext.cs:80-86`).
- Thanh cong: doi tuong `TDto` do Dapper anh xa.
- Loi: khong tra ve gia tri - nem exception.

**Dieu kien xu ly** (theo thu tu thuc thi)
1. `cancellationToken.ThrowIfCancellationRequested()` (`:80`).
2. Neu `scriptSQLQuery` null/rong/trang: `_logger.FailLogic(...)` roi `return default` (`:82-87`).
3. `switch` tren `commandType`: neu `CommandType.Text` thi **chen them tien to** `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;` vao truoc script (`:89-96`); cac `commandType` khac giu nguyen script.
4. Goi `_pipelineRead.ExecuteAsync(...)` bao boc `_dapperDbContext.Value.GetOne<TDto>(...)` (`:99-109`).
5. Trong `GetOne`, Dapper mo `SqlConnection` moi, thuc thi, dispose (`DapperSQLDBContext.cs:77-86`).

**Side effect** - Ghi log khi guard clause chan (`:84`). Mo/dong mot `SqlConnection` cho moi lan thuc thi. Neu `scriptSQLQuery` chua lenh ghi (INSERT/UPDATE/DELETE), lenh do **se duoc thuc thi** - phuong thuc khong ngan raw SQL ghi du ten la `FindOne...`. Khong mutate tham so dau vao.

**Error handling** - Khong co `try/catch`. `SqlException`, `TimeoutException`, `OperationCanceledException` deu nem ra caller. Polly `_pipelineRead` co the retry (mac dinh 3 lan voi loi transient) va co the nem `BrokenCircuitException` khi circuit breaker mo (`SqlResiliencePolicyFactory.cs:59-146`). Khong co log exception tai lop nay.

**Khi nao NEN dung** - Truy van doc phuc tap (JOIN nhieu bang, CTE, window function) ma LINQ kho dien dat; goi stored procedure tra ve 1 dong (`commandType = CommandType.StoredProcedure`); can hieu nang cao hon EF Core cho truy van doc.

**Khi nao KHONG dung** -
- Khi script duoc ghep tu chuoi nguoi dung nhap: khong co lop chong injection nao, script duoc noi suy truc tiep vao SQL (`:89-96`).
- Khi can doc du lieu chinh xac tuyet doi (khong dirty read): voi `CommandType.Text`, isolation level bi ha xuong `READ UNCOMMITTED`.
- Khi can ghi du lieu co dien cac cot audit tren ban ghi va co publish domain event: dung `CreateAsync`/`UpdateAsync`. Luu y: **khong duong nao trong module sinh audit trail dang ban ghi audit log** (xem muc 3, van de 26).
- Khi can chay trong transaction cua caller: phuong thuc nay luon mo connection rieng.

**Gioi han** - Hardcode `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;` cho `CommandType.Text`, khong the tat. `commandTimeout` mac dinh 30 giay, khong co gioi han tren. Khong phan biet duoc "khong tim thay" voi "script rong" khi `TDto` la reference type (ca hai deu `null`). `TDto` khong co rang buoc `class`, nen `default` co the la gia tri hop le cua value type gay nham lan. Retry cua Polly ap dung cho ca script co side effect ghi, co the gay thuc thi lai lenh ghi.

---

### 2.3 FindAllWithScriptAsync&lt;TDto&gt;

**Signature**

```csharp
public virtual async Task<IEnumerable<TDto>> FindAllWithScriptAsync<TDto>(
    string scriptSQLQuery,
    DynamicParameters parameters,
    int commandTimeout = 30,
    CommandType commandType = CommandType.Text,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Thuc thi `scriptSQLQuery` qua Dapper `QueryAsync<TDto>` va tra ve toan bo tap ket qua (`CoreSQLTenant.cs:151-161` -> `DapperSQLDBContext.cs:100-123`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `scriptSQLQuery` | `string` | Co | `IsNullOrWhiteSpace` -> log + `return null` (`:134-139`) | Khong co |
| `parameters` | `DynamicParameters` | Khong | Khong validate | Khong co |
| `commandTimeout` | `int` | Khong | Khong validate | `30` |
| `commandType` | `CommandType` | Khong | `Text` thi chen tien to isolation level (`:141-148`) | `CommandType.Text` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:132`) | `default` |

**Output** - `Task<IEnumerable<TDto>>`.
- Script rong/trang: **`null`** (`:138`) - khac voi `FindOneWithScriptAsync` tra ve `default`, va khac voi cac ham `FindAll...` cua EF Core tra ve `[]`.
- Truy van khong co dong nao: `IEnumerable<TDto>` rong (Dapper `QueryAsync` tra ve collection rong, khong phai `null`).
- Thanh cong: collection cac `TDto`.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (`:132`).
2. Guard `IsNullOrWhiteSpace(scriptSQLQuery)` -> log + `return null` (`:134-139`).
3. `switch` `commandType`: `Text` -> them `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;` (`:141-148`).
4. `_pipelineRead.ExecuteAsync` bao boc `_dapperDbContext.Value.GetAll<TDto>(...)` (`:151-161`).

**Side effect** - Ghi log khi guard chan (`:136`). Mo/dong `SqlConnection`. Script ghi van duoc thuc thi.

**Error handling** - Khong `try/catch`. Exception nem ra caller. Polly `_pipelineRead` retry / circuit breaker nhu 2.2.

**Khi nao NEN dung** - Doc danh sach lon voi truy van SQL toi uu thu cong; goi stored procedure tra ve nhieu dong.

**Khi nao KHONG dung** -
- Khi caller khong xu ly `null`: ham tra ve `null` (khong phai list rong) neu script rong, de gay `NullReferenceException` khi `.Count()`/`foreach`.
- Khi can phan trang server-side: khong co tham so `Skip`/`Take`; toan bo tap ket qua duoc keo ve bo nho.
- Khi script co the do nguoi dung dieu khien (injection).

**Gioi han** - Bat doi xung ve gia tri tra ve (`null` vs `[]`) so voi `FindAllAsync`/`FindAllSortDeletedAsync`. Khong gioi han so dong tra ve. Dirty read hardcode voi `CommandType.Text`. `DapperSQLDBContext.GetAll` cung tra ve `default` (null) neu script rong (`DapperSQLDBContext.cs:108-111`).

---

### 2.4 FindOneWithScalarScriptAsync&lt;TDto&gt;

**Signature**

```csharp
public virtual async Task<TDto> FindOneWithScalarScriptAsync<TDto>(
    string scriptSQLQuery,
    DynamicParameters parameters,
    int commandTimeout = 30,
    CommandType commandType = CommandType.Text,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Thuc thi `scriptSQLQuery` qua Dapper `ExecuteScalarAsync<TDto>` de lay mot gia tri don (COUNT, SUM, MAX, mot cot...) (`CoreSQLTenant.cs:201-212` -> `DapperSQLDBContext.cs:137-162`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `scriptSQLQuery` | `string` | Co | `IsNullOrWhiteSpace` -> log + `return default` (`:184-189`) | Khong co |
| `parameters` | `DynamicParameters` | Khong | Khong validate | Khong co |
| `commandTimeout` | `int` | Khong | Khong validate | `30` |
| `commandType` | `CommandType` | Khong | `Text` -> chen tien to isolation level (`:191-198`) | `CommandType.Text` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:182`) | `default` |

**Output** - `Task<TDto>`.
- Script rong/trang: `default` cua `TDto` (`:188`).
- Truy van khong tra ve dong nao hoac tra ve `NULL`: `default` cua `TDto` (hanh vi `ExecuteScalarAsync`).
- Thanh cong: gia tri cot dau tien cua dong dau tien, convert sang `TDto`.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (`:182`).
2. Guard script rong -> log + `return default` (`:184-189`).
3. `switch` `commandType` chen tien to isolation level cho `Text` (`:191-198`).
4. `_pipelineRead.ExecuteAsync` bao boc `GetOneExecute<TDto>(...)` (`:201-212`).

> [!NOTE]
> Vi tien to `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;` duoc chen truoc script, `ExecuteScalarAsync` lay **cot dau tien cua dong dau tien cua tap ket qua dau tien**. Voi script `Text` co nhieu cau lenh, ket qua thuc te phu thuoc vao thu tu cau lenh trong script - **khong xac dinh duoc tu source code** la SQL Server luon bo qua statement `SET` khi tinh tap ket qua dau tien; can kiem chung thuc te truoc khi dua vao production.

**Side effect** - Ghi log khi guard chan (`:186`). Mo/dong `SqlConnection`. Script ghi van duoc thuc thi.

**Error handling** - Khong `try/catch`. Exception nem ra caller. Loi convert kieu (`InvalidCastException`) tu Dapper cung nem ra.

**Khi nao NEN dung** - Lay `COUNT(*)`, `SUM`, `MAX`, kiem tra ton tai (`SELECT 1 WHERE EXISTS ...`), hoac lay mot gia tri cau hinh don.

**Khi nao KHONG dung** -
- Khi can nhieu cot hoac nhieu dong: dung 2.2 / 2.3.
- Khi can dem chinh xac trong giao dich: dirty read do `READ UNCOMMITTED`.
- Khi script do nguoi dung dieu khien.

**Gioi han** - Khong co API `CountAsync` bang EF Core, nen day la cach duy nhat trong lop de dem, buoc phai viet raw SQL. `TDto` khong co rang buoc, `default` co the trung voi gia tri nghiep vu hop le (vi du `0`).

---

### 2.5 IsExecuteNonQueryAsync

**Signature**

```csharp
public virtual async Task<bool> IsExecuteNonQueryAsync(
    string scriptSQLQuery,
    DbConnection context,
    DbTransaction transaction,
    DynamicParameters parameters,
    int commandTimeout = 30,
    CommandType commandType = CommandType.Text,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Thuc thi mot lenh SQL khong tra ve tap ket qua (INSERT/UPDATE/DELETE/DDL) truc tiep tren `DbConnection` va `DbTransaction` do caller cung cap, tra ve `true` neu so dong bi anh huong > 0 (`CoreSQLTenant.cs:244-249`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `scriptSQLQuery` | `string` | Co | `IsNullOrWhiteSpace` -> log + `return false` (`:237-242`) | Khong co |
| `context` | `DbConnection` | Co | **Khong null-check.** `null` gay `NullReferenceException` tai `:244` | Khong co |
| `transaction` | `DbTransaction` | Khong (co the `null`) | Khong validate; truyen thang cho Dapper | Khong co |
| `parameters` | `DynamicParameters` | Khong | Khong validate | Khong co |
| `commandTimeout` | `int` | Khong | Khong validate | `30` |
| `commandType` | `CommandType` | Khong | **Khong** co `switch` chen isolation level | `CommandType.Text` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:235`) roi **khong duoc truyen tiep** cho `ExecuteAsync` | `default` |

**Output** - `Task<bool>`.
- Script rong/trang: `false` (`:241`).
- Thuc thi thanh cong nhung 0 dong bi anh huong: `false` (bieu thuc `> 0` tai `:249`).
- Thuc thi thanh cong va >= 1 dong bi anh huong: `true`.
- Loi: khong tra ve gia tri - nem exception.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (`:235`).
2. Guard `IsNullOrWhiteSpace(scriptSQLQuery)` -> log + `return false` (`:237-242`).
3. Goi `context.ExecuteAsync(...)` (Dapper extension) va so sanh `> 0` (`:244-249`).

**Side effect** - **Ghi du lieu vao DB** (day la muc dich). Tham gia vao `transaction` do caller quan ly. Ghi log khi guard chan (`:239`). **Khong** goi `SaveChangesAsync`, nen **khong dien cac cot audit (`ModifiedUser`, `ModifiedDate`, ...) va khong publish domain event**.

**Error handling** - Khong `try/catch`. `SqlException` nem ra caller. **Khong duoc bao boc bang `_pipelineWrite`** nen khong co retry, khong co circuit breaker, khong co log resilience - khac voi moi API ghi khac trong lop.

**Khi nao NEN dung** - Khi can thuc thi lenh ghi raw SQL trong cung mot transaction do caller mo (vi du bulk delete, update theo dieu kien phuc tap, goi stored procedure ghi).

**Khi nao KHONG dung** -
- Khi khong co san `DbConnection` dang mo: ham khong tu tao connection, `context` `null` gay `NullReferenceException`.
- Khi can dien cot audit / publish domain event: hai co che nay chi chay trong `WriteDbContext.SaveChangesAsync` (`WriteDbContext.cs:75-93`), ham nay bo qua hoan toan. Luu y: **audit trail dang bang audit log khong ton tai o bat ky duong nao** trong module (xem muc 3, van de 26), nen day khong phai diem khac biet giua `IsExecuteNonQueryAsync` va `CreateAsync`/`UpdateAsync`.
- Khi can retry tu dong khi loi transient: khong co Polly.
- Khi muon huy tac vu giua duong: `cancellationToken` chi duoc kiem tra mot lan o dau ham, khong truyen vao `CommandDefinition`, nen khong huy duoc lenh dang chay.

**Gioi han** -
- Thu tu tham so trong signature (`scriptSQLQuery, context, transaction, parameters, ...`) **khac** thu tu trong XML doc (`scriptSQLQuery, transaction, parameters, context, ...`, `:216-222`) - doc sai, code dung.
- Khong su dung `_dapperDbContext`, tuc khong dung connection string cua lop; hoan toan phu thuoc connection cua caller.
- Khong ap dung `CancellationToken` cho lenh SQL.
- Khong chong SQL injection.
- Khong co Polly, la ngoai le duy nhat trong lop.

---

### 2.6 FindByIdAsync

**Signature**

```csharp
public virtual async Task<TEntityFrom> FindByIdAsync(
    object id, CancellationToken cancellationToken = default)
```

**Muc dich** - Tim mot ban ghi `TEntityTo` theo khoa chinh bang `DbSet<TEntityTo>.FindAsync`, detach khoi `ChangeTracker`, roi anh xa sang `TEntityFrom` (`CoreSQLTenant.cs:272-291`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `id` | `object` | Co | `id == null` -> log + `return null` (`:266-271`). Khong kiem tra kieu; phai khop kieu khoa chinh cua `TEntityTo` | Khong co |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:264`) | `default` |

**Output** - `Task<TEntityFrom>`.
- `id` la `null`: `null` (`:270`).
- Khong tim thay ban ghi: `null` (do `result?.ProjectTo<...>()` tai `:291`, `result` la `null`).
- Tim thay: doi tuong `TEntityFrom` **moi** tao boi `ProjectTo` (reflection, copy theo ten thuoc tinh trung khop va ghi duoc - `ProjectToExtensions.cs:27-63`).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (`:264`).
2. Guard `id == null` -> log + `return null` (`:266-271`).
3. `_pipelineRead.ExecuteAsync`: tao `DBContextRead` (`:276-277`), goi `Set<TEntityTo>().FindAsync(keyValues: [id], ...)` (`:279-280`).
4. Neu entity khac `null`: `createDbContext.Entry(entity).State = EntityState.Detached` (`:282-285`).
5. Context duoc dispose khi thoat `await using`.
6. `result?.ProjectTo<TEntityTo, TEntityFrom>()` (`:291`).

**Side effect** - Ghi log khi `id` null (`:268`). Tao va dispose mot `DBContextRead` moi. Khong ghi DB. Khong mutate tham so.

**Error handling** - Khong `try/catch`. `InvalidOperationException` (sai so luong/kieu khoa chinh) nem ra caller. `ProjectTo` co `try/catch` **ben trong** cho tung thuoc tinh, chi ghi log ra Console qua `CommonBaseConstant.ConfigLoggerExceptionByConsole` va **bo qua thuoc tinh loi** (`ProjectToExtensions.cs:56-59`) - nghia la mapping co the that bai am tham tung field ma khong nem loi.

**Khi nao NEN dung** - Doc mot ban ghi theo khoa chinh don, khi khong quan tam trang thai soft delete.

**Khi nao KHONG dung** -
- Khi bang co khoa chinh phuc hop: chi truyen duoc mot gia tri (`keyValues: [id]`, `:280`).
- Khi **can loai bo ban ghi da xoa mem**: ham nay **khong** loc `IsDeleted`, nen ban ghi da xoa mem van duoc tra ve. Dung `FindOneSortDeletedAsync` de loc.
- Khi can du lieu vua ghi trong cung transaction: ham doc tu `DBContextRead` (co the la replica), khong thay du lieu chua commit cua `DBContextWrite`.

**Gioi han** - Chi ho tro khoa chinh don. `id` la `object` nen sai kieu chi phat hien tai runtime. `FindAsync` uu tien tim trong `ChangeTracker` truoc, nhung vi context vua duoc tao moi nen thuc te luon di DB. Mapping bang reflection cham hon mapping tinh, va bo qua am tham cac thuoc tinh khong khop ten/kieu.

---

### 2.7 FindOneSortDeletedAsync&lt;TDto&gt;

**Signature**

```csharp
public virtual async Task<TDto> FindOneSortDeletedAsync<TDto>(
    Expression<Func<TEntityFrom, bool>>[] filters,
    bool isDeleted = false,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Tim ban ghi dau tien trong `Set<TEntityTo>()` thoa dieu kien `IsDeleted == isDeleted` cong cac `filters`, roi anh xa sang `TDto` (`CoreSQLTenant.cs:310-333`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filters` | `Expression<Func<TEntityFrom, bool>>[]` | Khong | `filters is not null && filters.Length > 0` moi ap dung (`:321-327`). `null` hoac rong -> chi loc theo `IsDeleted` | Khong co (phai truyen, co the `null`) |
| `isDeleted` | `bool` | Khong | Khong validate. **`TEntityTo` bat buoc phai co thuoc tinh `IsDeleted` kieu `bool`**, neu khong `EF.Property<bool>(x, "IsDeleted")` (`:319`) se loi khi dich truy van | `false` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:308`) | `default` |

**Output** - `Task<TDto>`.
- Khong tim thay: `default` cua `TDto` (`:333`).
- Tim thay: doi tuong `TDto` moi tao boi `ProjectTo` (`:333`).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (`:308`).
2. `_pipelineRead.ExecuteAsync`: tao `DBContextRead` (`:314-315`).
3. `query = createDbContext.Set<TEntityTo>()` (`:317`).
4. `query = query.Where(x => EF.Property<bool>(x, IsDeleted) == isDeleted)` - **luon ap dung, khong dieu kien** (`:319`).
5. Neu `filters` khac `null` va co phan tu: `filters.ReplaceParameters<TEntityFrom, TEntityTo>()` dich tham so lambda sang `TEntityTo` (`:323-324`), roi `Aggregate` ghep bang `Where` lien tiep (AND) (`:326`).
6. `query.AsNoTracking().FirstOrDefaultAsync(ct)` (`:329`).
7. `result is null ? default : result.ProjectTo<TEntityTo, TDto>()` (`:333`).

**Side effect** - Tao va dispose mot `DBContextRead`. Khong ghi DB, khong ghi log, khong mutate tham so.

**Error handling** - Khong `try/catch`. `InvalidOperationException` neu `TEntityTo` khong co thuoc tinh `IsDeleted`. `ArgumentException` tu `WhereReplacerVisitor` neu `TEntityTo` khong co thuoc tinh cung ten voi thuoc tinh duoc dung trong `filters` (`ProjectToExtensions.cs:419-422` dung `Expression.PropertyOrField`). `MissingMethodException` neu `TDto` khong co constructor cong khai khong tham so: `Activator.CreateInstance(typeof(TDto))` nam **ngoai** khoi `try/catch` cua `ProjectTo` (`ProjectToExtensions.cs:29`), nen loi nay nem ra caller. Polly `_pipelineRead` co the retry / nem `BrokenCircuitException`.

**Khi nao NEN dung** - Lay mot ban ghi theo dieu kien nghiep vu, co phan biet ban ghi da xoa mem, va can DTO chieu ra ngoai.

**Khi nao KHONG dung** -
- Khi `TEntityTo` khong co cot `IsDeleted`: ham se loi. Dung `FindOneAsync<TDto>` thay the.
- Khi can sap xep truoc khi lay ban ghi dau tien: **khong co tham so `sorting`**; ket qua la dong bat ky theo thu tu DB tra ve. Overload co `sorting` chi ton tai o lop 3 type parameter (`CoreSQL.cs:330`), va XML doc tai `CoreSQL.cs:318-321` giai thich ro ly do khong tai su dung duoc cho lop nay.
- Khi bieu thuc `filters` dung phuong thuc/thuoc tinh khong co ben `TEntityTo`: `ReplaceParameters` chi thay tham so theo **ten** thanh vien, khong ho tro doi ten cot.

**Gioi han** - Ten cot soft delete hardcode la chuoi `"IsDeleted"` (`:26`), khong cau hinh duoc. Khong sap xep, khong phan trang. Mapping `ProjectTo` bang reflection, bo qua am tham thuoc tinh khong khop. `filters` duoc ghep bang AND, khong ho tro OR o cap API nay. `WhereReplacerVisitor.VisitMember` **chi thay tham so khi truy cap thanh vien truc tiep tren tham so lambda** (`node.Expression is ParameterExpression`, `ProjectToExtensions.cs:419`); bieu thuc truy cap long nhau kieu `x => x.Child.Code == "A"` khong duoc dich sang `TEntityTo` mot cach nhat quan va co the nem `ArgumentException` - **chua kiem chung duoc hanh vi chinh xac tu source code**, can test truoc khi dung.

---

### 2.8 FindOneSortDeletedAsync

**Signature**

```csharp
public virtual async Task<TEntityFrom> FindOneSortDeletedAsync(
    Expression<Func<TEntityFrom, bool>>[] filters,
    bool isDeleted = false,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Giong 2.7 nhung tra ve `TEntityFrom` thay vi `TDto` (`CoreSQLTenant.cs:352-375`).

**Input hop le** - Giong bang o 2.7 (`filters` kiem tra tai `:363-369`, `isDeleted` dung tai `:361`, `cancellationToken` tai `:350`).

**Output** - `Task<TEntityFrom>`.
- Khong tim thay: `null` (`result?.ProjectTo<...>()` tai `:375`).
- Tim thay: doi tuong `TEntityFrom` moi tao boi `ProjectTo`.

> [!NOTE]
> Khac biet hanh vi so voi 2.7: overload `TDto` dung `result is null ? default : ...` (`:333`) con overload nay dung toan tu `?.` (`:375`). Voi `TEntityFrom : class` ket qua tuong duong (`null`), nhung voi `TDto` la value type thi 2.7 tra ve `default(TDto)` chu khong phai `null`.

**Dieu kien xu ly** - Giong 2.7, buoc 1-6 (`:350-372`), buoc cuoi la `result?.ProjectTo<TEntityTo, TEntityFrom>()` (`:375`).

**Side effect** - Tao va dispose mot `DBContextRead`. Khong ghi DB, khong ghi log.

**Error handling** - Giong 2.7. Khong `try/catch`.

**Khi nao NEN dung** - Khi tang goi can chinh kieu domain `TEntityFrom` (khong can DTO rieng), co phan biet soft delete.

**Khi nao KHONG dung** - Giong 2.7. Ngoai ra khong dung khi can chi lay mot vai cot: `ProjectTo` luon `SELECT` toan bo cot cua `TEntityTo` roi moi map trong bo nho.

**Gioi han** - Giong 2.7. Doi tuong tra ve la ban sao detached (`AsNoTracking`, `:372`), sua doi no khong tu dong duoc luu; phai goi `UpdateAsync`.

---

### 2.9 FindOneAsync&lt;TDto&gt;

**Signature**

```csharp
public virtual async Task<TDto> FindOneAsync<TDto>(
    Expression<Func<TEntityFrom, bool>>[] filters,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Tim ban ghi dau tien thoa `filters`, **khong** ap dung dieu kien `IsDeleted`, roi anh xa sang `TDto` (`CoreSQLTenant.cs:392-413`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filters` | `Expression<Func<TEntityFrom, bool>>[]` | Khong | `filters is not null && filters.Length > 0` (`:401-407`). `null`/rong -> **khong co dieu kien nao**, lay ban ghi dau tien cua bang | Khong co |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:390`) | `default` |

**Output** - `Task<TDto>`. Khong tim thay: `default` cua `TDto`. Tim thay: `TDto` moi tao boi `ProjectTo` (`:413`).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (`:390`).
2. Tao `DBContextRead` trong `_pipelineRead.ExecuteAsync` (`:396-397`).
3. `query = createDbContext.Set<TEntityTo>()` (`:399`) - **khong co `Where` cho `IsDeleted`**.
4. Neu `filters` co phan tu: `ReplaceParameters` + `Aggregate` `Where` (`:401-407`).
5. `AsNoTracking().FirstOrDefaultAsync(ct)` (`:409`).
6. `result is null ? default : result.ProjectTo<TEntityTo, TDto>()` (`:413`).

**Side effect** - Tao va dispose `DBContextRead`. Khong ghi DB, khong ghi log (khong co guard clause nao ghi log trong ham nay).

**Error handling** - Khong `try/catch`. `ArgumentException` tu `WhereReplacerVisitor` neu ten thanh vien khong ton tai o `TEntityTo`. Polly retry / `BrokenCircuitException`.

**Khi nao NEN dung** - Khi bang **khong co** cot `IsDeleted`, hoac khi co chu dich lay ca ban ghi da xoa mem (vi du doi soat, audit).

**Khi nao KHONG dung** -
- Khi nghiep vu can loai ban ghi da xoa mem: ham nay **khong** loc; phai dung `FindOneSortDeletedAsync`. Day la nguyen nhan loi logic rat de xay ra vi ten ham gan giong nhau.
- Khi truyen `filters = null`: se lay ban ghi bat ky trong bang ma khong bao loi.
- Khi can sap xep: khong ho tro.

**Gioi han** - Khong guard `filters` null (chi bo qua), nen goi sai khong duoc canh bao. Khong sap xep, khong phan trang. Mapping reflection.

---

### 2.10 FindOneAsync

**Signature**

```csharp
public virtual async Task<TEntityFrom> FindOneAsync(
    Expression<Func<TEntityFrom, bool>>[] filters,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Giong 2.9 nhung tra ve `TEntityFrom` (`CoreSQLTenant.cs:430-451`).

**Input hop le** - Giong 2.9 (`filters` tai `:439-445`, `cancellationToken` tai `:428`).

**Output** - `Task<TEntityFrom>`. Khong tim thay: `null` (`result?.ProjectTo<...>()`, `:451`). Tim thay: `TEntityFrom` moi.

**Dieu kien xu ly** - Giong 2.9: `ThrowIfCancellationRequested` (`:428`) -> `Set<TEntityTo>()` khong loc `IsDeleted` (`:437`) -> ap `filters` neu co (`:439-445`) -> `AsNoTracking().FirstOrDefaultAsync` (`:447`) -> `result?.ProjectTo<TEntityTo, TEntityFrom>()` (`:451`).

**Side effect** - Tao va dispose `DBContextRead`. Khong ghi DB, khong ghi log.

**Error handling** - Khong `try/catch`. Giong 2.9.

**Khi nao NEN dung** - Bang khong co cot `IsDeleted`, hoac can lay ca ban ghi da xoa mem, va tang goi dung truc tiep kieu `TEntityFrom`.

**Khi nao KHONG dung** - Giong 2.9. Khong dung khi ky vong hanh vi "chi lay ban ghi con hieu luc".

**Gioi han** - Giong 2.9.

---

### 2.11 FindAllSortDeletedAsync&lt;TDto&gt;

**Signature**

```csharp
public virtual async Task<List<TDto>> FindAllSortDeletedAsync<TDto>(
    Expression<Func<TEntityFrom, bool>>[] filters,
    bool isDeleted = false,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Lay toan bo ban ghi thoa `IsDeleted == isDeleted` va `filters`, anh xa sang `List<TDto>` (`CoreSQLTenant.cs:470-493`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filters` | `Expression<Func<TEntityFrom, bool>>[]` | Khong | `filters is not null && filters.Length > 0` (`:481-487`) | Khong co |
| `isDeleted` | `bool` | Khong | `TEntityTo` phai co thuoc tinh `IsDeleted` kieu `bool` (`:479`) | `false` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:468`) | `default` |

**Output** - `Task<List<TDto>>`.
- Khong co ban ghi nao: **`[]` (list rong)**, khong phai `null` (`:493`).
- Co ban ghi: `List<TDto>` do `ProjectTo` (overload `List<TEntity>`, `ProjectToExtensions.cs:76-126`).

> [!IMPORTANT]
> XML doc tai `CoreSQLTenant.cs:461` viet "...hoặc null nếu không tìm thấy kết quả", nhung than ham tai `:493` tra ve `[]`. **Tin than ham: ket qua la list rong, khong bao gio `null`.**

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (`:468`).
2. Tao `DBContextRead` (`:474-475`).
3. `query = Set<TEntityTo>()` (`:477`).
4. `query.Where(x => EF.Property<bool>(x, IsDeleted) == isDeleted)` - luon ap dung (`:479`).
5. Ap `filters` neu co, ghep AND (`:481-487`).
6. `AsNoTracking().ToListAsync(ct)` (`:489`).
7. `result.IsNullOrEmpty() ? [] : result.ProjectTo<TEntityTo, TDto>()` (`:493`).

**Side effect** - Tao va dispose `DBContextRead`. Khong ghi DB, khong ghi log.

**Error handling** - Khong `try/catch`. `InvalidOperationException` neu thieu cot `IsDeleted`. `ProjectTo` bat exception cho **tung entity** va **tung thuoc tinh**, ghi log Console va bo qua (`ProjectToExtensions.cs:110-123`) - entity loi hoan toan se bi loai khoi ket qua ma khong nem loi, khien so luong phan tu tra ve co the **it hon** so ban ghi trong DB.

**Khi nao NEN dung** - Lay danh sach ban ghi con hieu luc theo dieu kien, tra ra DTO cho API.

**Khi nao KHONG dung** -
- Khi bang co nhieu du lieu: **khong co phan trang**, `ToListAsync` keo toan bo ket qua ve bo nho.
- Khi can thu tu xac dinh: khong co `OrderBy`.
- Khi `TEntityTo` khong co cot `IsDeleted`.

**Gioi han** - Khong `Skip`/`Take`, khong `OrderBy`, khong gioi han so dong -> rui ro OOM va timeout voi bang lon. `ProjectTo` chay reflection cho tung thuoc tinh cua tung phan tu -> chi phi CPU tuyen tinh theo `so ban ghi x so thuoc tinh`. Ten cot soft delete hardcode `"IsDeleted"`.

---

### 2.12 FindAllSortDeletedAsync

**Signature**

```csharp
public virtual async Task<List<TEntityFrom>> FindAllSortDeletedAsync(
    Expression<Func<TEntityFrom, bool>>[] filters,
    bool isDeleted = false,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Giong 2.11 nhung tra ve `List<TEntityFrom>` (`CoreSQLTenant.cs:510-535`).

**Input hop le** - Giong 2.11 (`filters` tai `:523-529`, `isDeleted` tai `:521`, `cancellationToken` tai `:510`).

**Output** - `Task<List<TEntityFrom>>`. Khong co ban ghi: `[]` (`:535`). Co ban ghi: `List<TEntityFrom>`. XML doc tai `:503` viet "hoặc null" - **mau thuan voi than ham**, than ham tra ve `[]`.

**Dieu kien xu ly** - Giong 2.11: `ThrowIfCancellationRequested` (`:510`) -> `Set<TEntityTo>()` (`:519`) -> `Where` `IsDeleted` (`:521`) -> `filters` (`:523-529`) -> `AsNoTracking().ToListAsync` (`:531`) -> `result.IsNullOrEmpty() ? [] : result.ProjectTo<TEntityTo, TEntityFrom>()` (`:535`).

**Side effect** - Tao va dispose `DBContextRead`. Khong ghi DB, khong ghi log.

**Error handling** - Khong `try/catch`. Giong 2.11.

**Khi nao NEN dung** - Lay danh sach entity domain con hieu luc de xu ly nghiep vu trong process.

**Khi nao KHONG dung** - Giong 2.11.

**Gioi han** - Giong 2.11.

---

### 2.13 FindAllAsync&lt;TDto&gt;

**Signature**

```csharp
public virtual async Task<List<TDto>> FindAllAsync<TDto>(
    Expression<Func<TEntityFrom, bool>>[] filters,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Lay toan bo ban ghi thoa `filters`, **khong** loc `IsDeleted`, anh xa sang `List<TDto>` (`CoreSQLTenant.cs:550-573`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filters` | `Expression<Func<TEntityFrom, bool>>[]` | Khong | `filters is not null && filters.Length > 0` (`:561-567`). `null`/rong -> **lay toan bo bang** | Khong co |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:550`) | `default` |

**Output** - `Task<List<TDto>>`. Khong co ban ghi: `[]` (`:573`). Co ban ghi: `List<TDto>`. XML doc tai `:544` viet "hoặc null" - mau thuan, than ham tra ve `[]`.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (`:550`).
2. Tao `DBContextRead` (`:556-557`).
3. `query = Set<TEntityTo>()` (`:559`) - **khong co `Where` `IsDeleted`**.
4. Ap `filters` neu co (`:561-567`).
5. `AsNoTracking().ToListAsync(ct)` (`:569`).
6. `result.IsNullOrEmpty() ? [] : result.ProjectTo<TEntityTo, TDto>()` (`:573`).

**Side effect** - Tao va dispose `DBContextRead`. Khong ghi DB, khong ghi log.

**Error handling** - Khong `try/catch`. Giong 2.11.

**Khi nao NEN dung** - Bang khong co cot `IsDeleted`; hoac can lay ca ban ghi da xoa mem (bao cao, doi soat).

**Khi nao KHONG dung** -
- Khi nghiep vu can loai ban ghi da xoa mem.
- Khi `filters` co the la `null`/rong ma khong co chu dich: se `SELECT` toan bang.
- Khi bang lon: khong phan trang.

**Gioi han** - Truong hop `filters = null` va bang lon la truong hop nguy hiem nhat: khong guard, khong log, khong gioi han so dong. Khong `OrderBy`.

---

### 2.14 FindAllAsync

**Signature**

```csharp
public virtual async Task<List<TEntityFrom>> FindAllAsync(
    Expression<Func<TEntityFrom, bool>>[] filters,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Giong 2.13 nhung tra ve `List<TEntityFrom>` (`CoreSQLTenant.cs:588-611`).

**Input hop le** - Giong 2.13 (`filters` tai `:599-605`, `cancellationToken` tai `:588`).

**Output** - `Task<List<TEntityFrom>>`. Khong co ban ghi: `[]` (`:611`). XML doc tai `:582` viet "hoặc null" - mau thuan, than ham tra ve `[]`.

**Dieu kien xu ly** - Giong 2.13: `ThrowIfCancellationRequested` (`:588`) -> `Set<TEntityTo>()` khong loc `IsDeleted` (`:597`) -> `filters` (`:599-605`) -> `AsNoTracking().ToListAsync` (`:607`) -> `result.IsNullOrEmpty() ? [] : result.ProjectTo<TEntityTo, TEntityFrom>()` (`:611`).

**Side effect** - Tao va dispose `DBContextRead`. Khong ghi DB, khong ghi log.

**Error handling** - Khong `try/catch`. Giong 2.13.

**Khi nao NEN dung** - Giong 2.13, khi tang goi dung truc tiep `TEntityFrom`.

**Khi nao KHONG dung** - Giong 2.13.

**Gioi han** - Giong 2.13.

---

### 2.15 CreateAsync (TEntityFrom, AuditModel, CancellationToken)

**Signature**

```csharp
public virtual async Task<int> CreateAsync(
    TEntityFrom entity,
    AuditModel auditLog = null,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Anh xa `entity` tu `TEntityFrom` sang `TEntityTo`, chuyen tiep domain event, `AddAsync` vao `DBContextWrite` moi tao, roi `SaveChangesAsync(audit)` trong Polly write pipeline; tra ve so ban ghi bi anh huong (`CoreSQLTenant.cs:628-671`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entity` | `TEntityFrom` | Co | `entity is null` -> log + `return 0` (`:630-635`) | Khong co |
| `auditLog` | `AuditModel` | Khong | Khong validate. `null` -> `OnBeforeSaveChanges` van chay va van dien `CreatedUser/CreatedUserCode/CreatedUserOrganization` bang gia tri mac dinh `Anonymous`/`AnonymousCode`/`OrganizationForISC` cho entity `Added` (`WriteDbContext.cs:139-159`); `DetectChangesAudit` thoat som (`WriteDbContext.cs:194-197`) nhung dieu do khong doi ket qua vi ham nay luon `return []` | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:628`) | `default` |

**Output** - `Task<int>`.
- `entity` la `null`: `0` (`:634`).
- `entityConvert` la `null`: `0` (`:647`) - xem "Gioi han".
- Thanh cong: so ban ghi bi anh huong do `SaveChangesAsync` tra ve (thuong `1`, co the > 1 neu co cascade/owned entity).
- `SaveChangesAsync` tra ve `0`: `0`.
- Loi: nem exception, khong tra ve gia tri.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (`:628`).
2. Guard `entity is null` -> log + `return 0` (`:630-635`).
3. Lay `domainEvents`: neu `entity is IAggregate` thi `entityWithDomainEventsFrom.DomainEvents`, nguoc lai `[]` (`:637-638`).
4. `entityConvert = ProjectToExtensions.MapUsingExpression<TEntityFrom, TEntityTo>(entity)` (`:640-641`).
5. Guard `entityConvert is null` -> log + `return 0` (`:643-648`).
6. Neu `entityConvert is IAggregate`: `DomainEvents.AddRange(domainEvents)` (`:651-654`).
7. Tao `DBContextWrite` moi qua factory (`:656-657`).
8. `createDbContext.Set<TEntityTo>().AddAsync(entityConvert, cancellationToken)` (`:661-662`).
9. `_pipelineWrite.ExecuteAsync(... createDbContext.SaveChangesAsync(audit: auditLog, ct) ...)` (`:664-669`).
10. `return result` (`:671`).

**Side effect** - **Ghi DB (INSERT)**. Ghi log khi guard chan (`:632`, `:645`). Tao va dispose `DBContextWrite`. `WriteDbContext.SaveChangesAsync` con:
- Goi `OnBeforeSaveChanges(audit)` (`WriteDbContext.cs:77`) - voi entity `Added` implement `IBaseEntitySQL` thi **cuong che `IsDeleted = false`** (`:154`), dien `CreatedDate/CreatedUser/CreatedUserCode/CreatedUserOrganization` neu dang `null` (`:156-159`), va **cuong che `ModifiedDate/ModifiedUser/ModifiedUserCode/ModifiedUserOrganization` ve `null`** (`:161-164`) - ghi de moi gia tri caller da dat cho nhom cot Modified.
- Goi `DetectChangesAudit(audit)` khi `auditLog` khac `null`, nhung ham nay **luon tra ve danh sach rong** (`WriteDbContext.cs:201-353`, than ham bi comment `NOT SUPPORT`) nen **khong sinh ban ghi audit log nao**.
- Chi khi `SaveChangesAsync` tra ve `> 0` moi goi `OnAfterSaveChanges` (`WriteDbContext.cs:88-91`), trong do `DispatchDomainEvents` publish domain event cua cac entity **ke thua lop `Aggregate`** roi goi `ChangeTracker.Clear()` (`WriteDbContext.cs:421-433`). Voi overload nay context la noi bo nen `ChangeTracker.Clear()` khong anh huong caller.

**Mutate `entityConvert`** (doi tuong noi bo) - `entity` dau vao khong bi mutate boi lop nay, nhung vi `MapUsingExpression` copy **tham chieu** cho cac thuoc tinh kieu reference, cac object long nhau la dung chung giua `entity` va `entityConvert`.

**Error handling** - Khong `try/catch` trong lop nay. `DbUpdateException`, `SqlException` nem ra caller. `_pipelineWrite` retry toi da **1 lan** va chi voi loi connection-level (`SqlResiliencePolicyFactory.cs:194-210`); co the nem `BrokenCircuitException`. `MapUsingExpression` bat exception cho tung binding va bo qua thuoc tinh loi (`ProjectToExtensions.cs:174-177`), khong nem ra.

**Khi nao NEN dung** - Insert mot ban ghi doc lap, khong can transaction chung voi thao tac khac, va khong can lay lai gia tri do DB sinh (identity, computed column).

**Khi nao KHONG dung** -
- Khi can khoa chinh vua sinh: overload nay chi tra ve `int`. Dung 2.16 de nhan `(int Result, TEntityFrom Data)`.
- Khi can nam trong transaction cung voi thao tac khac: ham tu tao context rieng va `SaveChangesAsync` ngay. Dung 2.16 voi `DBContextWrite` do caller quan ly.
- Khi `TEntityTo` khong co constructor khong tham so hoac la abstract: `MapUsingExpression` dung `Expression.New(typeof(TTo))` (`ProjectToExtensions.cs:183`) se nem exception.
- Khi `TEntityFrom` va `TEntityTo` co cac thuoc tinh cung ten nhung **khac kieu**: `MapUsingExpression` chi bind khi `sourceProp.PropertyType == targetProp.PropertyType` (`ProjectToExtensions.cs:157-160`), cac thuoc tinh khac kieu se bi bo qua am tham -> **ghi gia tri mac dinh (`null`/`0`) vao DB**.
- Khi `TEntityFrom` co thuoc tinh chi doc (get-only): `MapUsingExpression` yeu cau `sourceProp.CanWrite` (`:157`) nen thuoc tinh chi doc **khong** duoc map.

**Gioi han** -
- Guard `entityConvert is null` (`:643`) **thuc te la code khong bao gio chay**: `MapUsingExpression` luon tra ve ket qua cua `Expression.MemberInit(Expression.New(typeof(TTo)), bindings)` (`ProjectToExtensions.cs:183-187`), tuc luon la mot instance moi, khong bao gio `null`. Kiem tra nay khong bao ve duoc gi.
- `MapUsingExpression` **compile expression tree moi cho moi lan goi** (`.Compile()` tai `ProjectToExtensions.cs:185`), khong cache -> chi phi dang ke.
- Retry cua `_pipelineWrite` bao boc `SaveChangesAsync`. Neu lan dau da commit thanh cong o DB nhung ket noi dut truoc khi client nhan phan hoi, lan retry co the **insert trung** ban ghi. Khong co idempotency key trong code.
- Khong tu mo transaction; neu `SaveChangesAsync` ghi nhieu bang, EF Core tu bao boc trong transaction noi bo, nhung khong the mo rong ra thao tac ngoai.

---

### 2.16 CreateAsync (TEntityFrom, DBContextWrite, AuditModel, CancellationToken)

**Signature**

```csharp
public virtual async Task<(int Result, TEntityFrom Data)> CreateAsync(
    TEntityFrom entity,
    DBContextWrite context,
    AuditModel auditLog = null,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Giong 2.15 nhung dung `context` do caller cung cap va tra ve ca doi tuong sau khi ghi (de lay gia tri do DB sinh) (`CoreSQLTenant.cs:690-741`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entity` | `TEntityFrom` | Co | `entity is null` -> log + `return (0, null)` (`:692-697`) | Khong co |
| `context` | `DBContextWrite` | Co | **Khong null-check.** `null` gay `NullReferenceException` tai `:720` | Khong co |
| `auditLog` | `AuditModel` | Khong | Khong validate | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:690`) | `default` |

**Output** - `Task<(int Result, TEntityFrom Data)>`.
- `entity` la `null`: `(Result: 0, Data: null)` (`:696`).
- `entityConvert` la `null`: `(Result: 0, Data: entity)` - tra ve **chinh entity dau vao** (`:709`).
- `SaveChangesAsync` tra ve `0`: `(Result: 0, Data: entity)` - tra ve **entity dau vao**, khong phai entity da ghi (`:730-735`).
- Thanh cong (`result != 0`): `(Result: result, Data: entityConvert.ProjectTo<TEntityTo, TEntityFrom>())` - doi tuong `TEntityFrom` **moi**, mang cac gia tri sau `SaveChanges` (bao gom khoa chinh do DB sinh) (`:736-739`).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (`:690`).
2. Guard `entity is null` -> log + `return (0, null)` (`:692-697`).
3. Lay `domainEvents` neu `entity is IAggregate` (`:699-700`).
4. `entityConvert = MapUsingExpression<TEntityFrom, TEntityTo>(entity)` (`:702-703`).
5. Guard `entityConvert is null` -> log + `return (0, entity)` (`:705-710`).
6. `DomainEvents.AddRange(domainEvents)` neu `entityConvert is IAggregate` (`:713-716`).
7. `context.Set<TEntityTo>().AddAsync(entityConvert, cancellationToken)` (`:720-721`).
8. `_pipelineWrite.ExecuteAsync(... context.SaveChangesAsync(audit: auditLog, ct) ...)` (`:723-728`).
9. `switch (result is 0)`: `true` -> `(0, entity)`; `false` -> `(result, entityConvert.ProjectTo<TEntityTo, TEntityFrom>())` (`:730-740`).

**Side effect** - **Ghi DB (INSERT)** thong qua `context` cua caller. Ghi log khi guard chan (`:694`, `:707`). Them entity vao `ChangeTracker` cua `context` (thay doi state doi tuong dung chung). Goi `SaveChangesAsync` **ngay**, keo theo dien cot audit + publish domain event (`WriteDbContext.cs:75-93`). **Khong** dispose `context` - trach nhiem cua caller.

> [!WARNING]
> **Side effect nang nhat cua overload nay: `ChangeTracker` cua `context` bi xoa sach.**
> Khi `SaveChangesAsync` tra ve `> 0`, `OnAfterSaveChanges` -> `DispatchDomainEvents` goi `ChangeTracker.Clear()` (`WriteDbContext.cs:433`) - **truoc** ca khi kiem tra co domain event hay khong, tuc **luon chay**. Moi entity khac ma caller da `Add`/`Attach`/doc len tren cung `context` deu bi detach; cac thay doi chua luu cua chung bi mat va cac lan `SaveChangesAsync` sau se khong ghi gi. Khong dung overload nay giua mot unit-of-work nhieu buoc tren cung `context` neu chua kiem chung diem nay.

**Error handling** - Khong `try/catch`. `NullReferenceException` neu `context` la `null`. `DbUpdateException` nem ra caller (entity da nam trong `ChangeTracker` cua caller, caller phai tu don). `_pipelineWrite` retry 1 lan voi loi connection-level.

**Khi nao NEN dung** - Khi can khoa chinh / gia tri computed sau khi insert; khi muon insert nhieu entity khac nhau tren cung mot `DBContextWrite` (va tu quan ly `IDbContextTransaction`).

**Khi nao KHONG dung** -
- Khi ky vong ham **khong** commit ngay: du XML doc noi "lưu vào cơ sở dữ liệu theo cùng một transaction" (`:675`), than ham **goi `SaveChangesAsync` ngay** (`:723-728`). Chi nam trong transaction chung neu caller da tu `BeginTransactionAsync` truoc do; **lop nay khong mo transaction**.
- Khi khong co san `DBContextWrite`: khong null-check.
- Khi muon phan biet "khong ghi duoc" voi "ghi duoc nhung 0 dong": ca hai deu cho `Result = 0`.

**Gioi han** -
- Khi that bai, `Data` tra ve **entity dau vao** (`:734`) chu khong phai `null`, de gay ngo nhan la da ghi thanh cong neu caller chi kiem tra `Data != null`.
- `Data` khi thanh cong la doi tuong **moi** do `ProjectTo` tao, khong phai `entity` dau vao; moi thay doi caller lam tren `entity` sau do se khong phan anh vao `Data` va nguoc lai.
- `ProjectTo` chi copy thuoc tinh **cung ten va ghi duoc** (`ProjectToExtensions.cs:47-54`); khoa chinh do DB sinh chi lay lai duoc neu `TEntityFrom` co thuoc tinh cung ten va `set` duoc.
- Guard `entityConvert is null` la code khong bao gio chay (xem 2.15).
- Retry `SaveChangesAsync` co the gay insert trung.

---

### 2.17 CreateAsync (IEnumerable&lt;TEntityFrom&gt;, AuditModel, CancellationToken)

**Signature**

```csharp
public virtual async Task<int> CreateAsync(
    IEnumerable<TEntityFrom> entities,
    AuditModel auditLog = null,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Anh xa tung phan tu sang `TEntityTo`, `AddRangeAsync` vao `DBContextWrite` moi tao, `SaveChangesAsync(audit)` mot lan; tra ve so ban ghi bi anh huong (`CoreSQLTenant.cs:757-810`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entities` | `IEnumerable<TEntityFrom>` | Co | `entities.IsNullOrEmpty()` -> log + `return default` (`:759-764`). `IsNullOrEmpty` xu ly ca `null` (`CollectionHelpers.cs:14`) | Khong co |
| `auditLog` | `AuditModel` | Khong | Khong validate | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:757`) | `default` |

**Output** - `Task<int>`.
- `entities` null/rong: `default`, tuc `0` (`:763`).
- Sau khi map, `entitiesConvert` rong: `0` (`:792`).
- Thanh cong: so ban ghi bi anh huong.
- `SaveChangesAsync` tra ve `0`: `0`.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (`:757`).
2. Guard `entities.IsNullOrEmpty()` -> log + `return default` (`:759-764`).
3. `foreach` tung `entity` (`:768-786`):
   a. Lay `domainEvents` neu `entity is IAggregate` (`:770-771`).
   b. `entityConvert = MapUsingExpression<TEntityFrom, TEntityTo>(entity)` (`:773-774`).
   c. Neu `entityConvert is not null`: `DomainEvents.AddRange` neu la `IAggregate` (`:778-782`), roi `entitiesConvert.Add(entityConvert)` (`:784`). **Neu `null` thi bo qua am tham, khong log** (`:776`).
4. Guard `entitiesConvert.IsNullOrEmpty()` -> log + `return 0` (`:788-793`).
5. Tao `DBContextWrite` moi (`:797-798`).
6. `Set<TEntityTo>().AddRangeAsync(entitiesConvert, cancellationToken)` (`:800-801`).
7. `_pipelineWrite.ExecuteAsync(... SaveChangesAsync(audit: auditLog, ct) ...)` (`:803-808`).

**Side effect** - **Ghi DB (INSERT nhieu dong trong mot `SaveChanges`)**. Ghi log khi guard chan (`:761`, `:790`). Tao va dispose `DBContextWrite`. Dien cot audit (`OnBeforeSaveChanges`) + publish domain event qua `WriteDbContext`; **khong sinh ban ghi audit log** (xem 2.15). Mutate cac `entityConvert` noi bo.

**Error handling** - Khong `try/catch`. Neu mot phan tu gay loi khi ghi, **toan bo `SaveChanges` roll back** (EF Core tu bao boc transaction). `_pipelineWrite` retry 1 lan voi loi connection-level. `MapUsingExpression` bo qua thuoc tinh loi khong nem.

**Khi nao NEN dung** - Insert lo nho den trung binh (vai chuc den vai tram ban ghi) can tinh nguyen tu (all-or-nothing) va co audit.

**Khi nao KHONG dung** -
- Khi so luong rat lon (hang chuc nghin): khong co batching/chunking, khong dung `BulkInsert`; `AddRangeAsync` + `SaveChangesAsync` mot lan de gay timeout va phinh `ChangeTracker`.
- Khi can biet ban ghi nao khong map duoc: phan tu map ra `null` bi bo qua **khong ghi log** (`:776`), so ban ghi ghi duoc co the it hon so phan tu dau vao ma khong co canh bao.
- Khi can lay lai khoa chinh: chi tra ve `int`. Dung 2.18.
- Khi can transaction chung voi thao tac khac: dung 2.18.

**Gioi han** - `MapUsingExpression` **compile lai expression tree cho tung phan tu** trong vong lap (`:773-774` -> `ProjectToExtensions.cs:185`) -> chi phi CPU tang tuyen tinh, la diem nghen ro rang khi lo lon. `entities` duoc duyet nhieu lan (`IsNullOrEmpty` roi `foreach`), nen `IEnumerable` chi duyet duoc mot lan (lazy stream) se cho ket qua sai hoac loi. Khong co gioi han so phan tu.

---

### 2.18 CreateAsync (IEnumerable&lt;TEntityFrom&gt;, DBContextWrite, AuditModel, CancellationToken)

**Signature**

```csharp
public virtual async Task<(int Result, IEnumerable<TEntityFrom> Data)> CreateAsync(
    IEnumerable<TEntityFrom> entities,
    DBContextWrite context,
    AuditModel auditLog = null,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Giong 2.17 nhung dung `context` do caller cung cap va tra ve ca danh sach sau khi ghi (`CoreSQLTenant.cs:829-889`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entities` | `IEnumerable<TEntityFrom>` | Co | `entities.IsNullOrEmpty()` -> log + `return (0, null)` (`:831-836`) | Khong co |
| `context` | `DBContextWrite` | Co | **Khong null-check.** `null` gay `NullReferenceException` tai `:869` | Khong co |
| `auditLog` | `AuditModel` | Khong | Khong validate | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:829`) | `default` |

**Output** - `Task<(int Result, IEnumerable<TEntityFrom> Data)>`.
- `entities` null/rong: `(Result: 0, Data: null)` (`:835`).
- Sau map, `entitiesConvert` rong: `(Result: 0, Data: null)` (`:864`).
- `SaveChangesAsync` tra ve `0`: `(Result: 0, Data: [])` - **list rong**, khac voi hai truong hop tren tra ve `null` (`:879-884`).
- Thanh cong: `(Result: result, Data: entitiesConvert.ProjectTo<TEntityTo, TEntityFrom>())` (`:885-888`).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (`:829`).
2. Guard `entities.IsNullOrEmpty()` -> log + `return (0, null)` (`:831-836`).
3. `foreach` map tung phan tu, bo qua am tham phan tu map ra `null` (`:840-858`).
4. Guard `entitiesConvert.IsNullOrEmpty()` -> log + `return (0, null)` (`:860-865`).
5. `context.Set<TEntityTo>().AddRangeAsync(entitiesConvert, cancellationToken)` (`:869-870`).
6. `_pipelineWrite.ExecuteAsync(... context.SaveChangesAsync(audit: auditLog, ct) ...)` (`:872-877`).
7. `switch (result is 0)`: `true` -> `(0, [])`; `false` -> `(result, entitiesConvert.ProjectTo<TEntityTo, TEntityFrom>())` (`:879-889`).

**Side effect** - **Ghi DB (INSERT)** qua `context` cua caller. Ghi log khi guard chan (`:833`, `:862`). Them nhieu entity vao `ChangeTracker` cua `context`. `SaveChangesAsync` chay ngay, keo theo dien cot audit + publish domain event. Khong dispose `context`. **Khi `result > 0`, `ChangeTracker` cua `context` bi `Clear()`** (`WriteDbContext.cs:433`) - xem canh bao o 2.16.

**Error handling** - Khong `try/catch`. `NullReferenceException` neu `context` la `null`. `DbUpdateException` nem ra caller, `ChangeTracker` cua caller van con entity o trang thai `Added`. `_pipelineWrite` retry 1 lan.

**Khi nao NEN dung** - Insert lo trong cung transaction voi cac thao tac khac (caller tu `BeginTransactionAsync`), va can lay lai gia tri do DB sinh cho tung ban ghi.

**Khi nao KHONG dung** -
- Khi ky vong ham chi `Add` ma khong commit: than ham goi `SaveChangesAsync` ngay (`:872-877`), du ten va XML doc goi y "theo cùng một transaction" (`:814`).
- Khi lo rat lon: khong batching.
- Khi caller chi kiem tra `Data != null` de biet thanh cong: gia tri `null` va `[]` deu ung voi `Result = 0`, khong nhat quan.

**Gioi han** - Bat doi xung `Data` giua cac nhanh loi (`null` vs `[]`). `MapUsingExpression` compile lai cho tung phan tu. `entities` bi duyet nhieu lan. `ProjectTo` chay reflection cho tung phan tu ket qua. Phan tu map that bai bi bo qua khong log.

---

### 2.19 UpdateAsync (TEntityFrom, AuditModel, CancellationToken)

**Signature**

```csharp
public virtual async Task<int> UpdateAsync(
    TEntityFrom entity,
    AuditModel auditLog = null,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Anh xa `entity` sang `TEntityTo`, goi `DbSet<TEntityTo>.Update` tren `DBContextWrite` moi tao, roi `SaveChangesAsync(audit)`; tra ve so ban ghi bi anh huong (`CoreSQLTenant.cs:906-946`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entity` | `TEntityFrom` | Co | `entity is null` -> log + `return 0` (`:908-913`). **Khong kiem tra khoa chinh co gia tri hay khong** | Khong co |
| `auditLog` | `AuditModel` | Khong | Khong validate | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:906`) | `default` |

**Output** - `Task<int>`.
- `entity` la `null`: `0` (`:912`).
- `entityConvert` la `null`: `0` (`:925`) - code khong bao gio chay.
- Thanh cong: so ban ghi bi anh huong.
- `SaveChangesAsync` tra ve `0`: `0`.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (`:906`).
2. Guard `entity is null` -> log + `return 0` (`:908-913`).
3. Lay `domainEvents` neu `entity is IAggregate` (`:915-916`).
4. `entityConvert = MapUsingExpression<TEntityFrom, TEntityTo>(entity)` (`:918-919`).
5. Guard `entityConvert is null` -> log + `return 0` (`:921-926`).
6. `DomainEvents.AddRange(domainEvents)` neu `entityConvert is IAggregate` (`:929-932`).
7. Tao `DBContextWrite` moi (`:936-937`).
8. `createDbContext.Set<TEntityTo>().Update(entity: entityConvert)` (`:939`).
9. `_pipelineWrite.ExecuteAsync(... SaveChangesAsync(audit: auditLog, ct) ...)` (`:941-946`).

**Side effect** - **Ghi DB (UPDATE)**. Ghi log khi guard chan (`:910`, `:923`). Tao va dispose `DBContextWrite`. Publish domain event qua `WriteDbContext`; **khong sinh ban ghi audit log** (xem 2.15). `DbSet.Update` danh dau **toan bo** thuoc tinh la `Modified` -> UPDATE toan bo cot.

> [!IMPORTANT]
> **Cac cot `Modified*` chi duoc dien khi `auditLog` khac `null`.** `OnBeforeSaveChanges` thoat som o nhanh `EntityState.Modified` neu `audit is null` (`WriteDbContext.cs:168-173`). Ket hop voi full-row update, goi `UpdateAsync(entity)` ma khong truyen `auditLog` se **ghi de gia tri cua `entityConvert`** (thuong la `null` neu `TEntityFrom` khong mang cac cot nay) len `ModifiedUser`/`ModifiedDate`/`ModifiedUserCode`/`ModifiedUserOrganization` trong DB. Nhom cot `Created*` **khong bao gio** duoc dien lai o nhanh `Modified` (`WriteDbContext.cs:175-178` chi dien `Modified*`), nen cung bi ghi de theo gia tri cua `entityConvert`.

**Error handling** - Khong `try/catch`. `DbUpdateConcurrencyException` (khong tim thay dong can update) nem ra caller. `_pipelineWrite` retry 1 lan voi loi connection-level.

**Khi nao NEN dung** - Cap nhat toan bo ban ghi khi da co day du **moi** gia tri cot can luu, khong can transaction chung.

**Khi nao KHONG dung** -
- **Khi chi muon cap nhat mot vai cot (partial update).** `Update` danh dau moi cot la modified, va `entityConvert` la doi tuong **moi** do `MapUsingExpression` tao ra: moi thuoc tinh khong map duoc (khac kieu, source chi doc, khac ten) se giu gia tri mac dinh (`null`/`0`/`DateTime.MinValue`) va **duoc ghi de len DB**. Day la rui ro mat du lieu cao nhat cua API nay (`:918-919` ket hop `:939`).
- Khi khoa chinh chua co gia tri: `Update` tren entity detached voi khoa chinh mac dinh se bi EF Core coi la `Added` -> INSERT thay vi UPDATE. Khong co guard nao chan.
- Khi ban ghi co the da bi xoa mem: khong kiem tra `IsDeleted`.
- Khi can transaction chung: dung 2.20.

**Gioi han** - Khong kiem tra su ton tai cua ban ghi truoc khi update. Khong ho tro optimistic concurrency tuong minh (phu thuoc cau hinh `RowVersion` cua entity, **khong xac dinh duoc tu source code** cua lop nay). Guard `entityConvert is null` la code khong bao gio chay. `MapUsingExpression` compile expression tree moi lan goi.

---

### 2.20 UpdateAsync (TEntityFrom, DBContextWrite, AuditModel, CancellationToken)

**Signature**

```csharp
public virtual async Task<(int Result, TEntityFrom Data)> UpdateAsync(
    TEntityFrom entity,
    DBContextWrite context,
    AuditModel auditLog = null,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Giong 2.19 nhung dung `context` cua caller va tra ve ca doi tuong sau khi cap nhat (`CoreSQLTenant.cs:967-1017`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entity` | `TEntityFrom` | Co | `entity is null` -> log + `return (0, null)` (`:969-974`) | Khong co |
| `context` | `DBContextWrite` | Co | **Khong null-check.** `null` gay `NullReferenceException` tai `:997` | Khong co |
| `auditLog` | `AuditModel` | Khong | Khong validate | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:967`) | `default` |

**Output** - `Task<(int Result, TEntityFrom Data)>`.
- `entity` la `null`: `(Result: 0, Data: null)` (`:973`).
- `entityConvert` la `null`: `(Result: 0, Data: entity)` (`:986`) - code khong bao gio chay.
- `SaveChangesAsync` tra ve `0`: `(Result: 0, Data: entity)` - entity dau vao (`:1006-1011`).
- Thanh cong: `(Result: result, Data: entityConvert.ProjectTo<TEntityTo, TEntityFrom>())` (`:1012-1015`).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (`:967`).
2. Guard `entity is null` -> log + `return (0, null)` (`:969-974`).
3. Lay `domainEvents` neu `entity is IAggregate` (`:976-977`).
4. `entityConvert = MapUsingExpression<TEntityFrom, TEntityTo>(entity)` (`:979-980`).
5. Guard `entityConvert is null` -> log + `return (0, entity)` (`:982-987`).
6. `DomainEvents.AddRange(domainEvents)` neu la `IAggregate` (`:990-993`).
7. `context.Set<TEntityTo>().Update(entity: entityConvert)` (`:997`).
8. `_pipelineWrite.ExecuteAsync(... context.SaveChangesAsync(audit: auditLog, ct) ...)` (`:999-1004`).
9. `switch (result is 0)`: `true` -> `(0, entity)`; `false` -> `(result, entityConvert.ProjectTo<TEntityTo, TEntityFrom>())` (`:1006-1016`).

**Side effect** - **Ghi DB (UPDATE)** qua `context` cua caller. Ghi log khi guard chan (`:971`, `:984`). Thay doi `ChangeTracker` cua `context` (danh dau entity `Modified`). `SaveChangesAsync` chay ngay. Khong dispose `context`. **Khi `result > 0`, `ChangeTracker` cua `context` bi `Clear()`** (`WriteDbContext.cs:433`) - xem canh bao o 2.16. Cac cot `Modified*` chi duoc dien khi `auditLog` khac `null` - xem canh bao o 2.19.

**Error handling** - Khong `try/catch`. `NullReferenceException` neu `context` la `null`. `InvalidOperationException` neu `context` da track mot instance khac cung khoa chinh (`entityConvert` la instance moi). `DbUpdateConcurrencyException` nem ra caller. `_pipelineWrite` retry 1 lan.

**Khi nao NEN dung** - Update trong cung transaction voi thao tac khac (caller tu mo transaction), va can doi tuong ket qua sau cap nhat.

**Khi nao KHONG dung** -
- **Khi `context` da dang track ban ghi cung khoa chinh** (vi du vua doc len tu `context` do): `Update(entityConvert)` voi instance moi se nem `InvalidOperationException` do trung khoa trong `ChangeTracker`.
- Khi chi muon cap nhat mot vai cot: xem canh bao mat du lieu o 2.19.
- Khi ky vong khong commit ngay: `SaveChangesAsync` duoc goi ngay tai `:999-1004`.

**Gioi han** - Khi that bai, `Data` la entity dau vao (`:1010`), de gay ngo nhan. `Data` khi thanh cong la doi tuong moi (khong cung tham chieu voi `entity`). Guard `entityConvert is null` khong bao gio chay. Van la full-row update.

---

### 2.21 UpdateAsync (IEnumerable&lt;TEntityFrom&gt;, AuditModel, CancellationToken)

**Signature**

```csharp
public virtual async Task<int> UpdateAsync(
    IEnumerable<TEntityFrom> entities,
    AuditModel auditLog = null,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Anh xa tung phan tu sang `TEntityTo`, `UpdateRange` tren `DBContextWrite` moi tao, `SaveChangesAsync(audit)` mot lan (`CoreSQLTenant.cs:1033-1085`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entities` | `IEnumerable<TEntityFrom>` | Co | `entities.IsNullOrEmpty()` -> log + `return 0` (`:1035-1040`) | Khong co |
| `auditLog` | `AuditModel` | Khong | Khong validate | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:1033`) | `default` |

**Output** - `Task<int>`.
- `entities` null/rong: `0` (`:1039`).
- Sau map, `entitiesConvert` rong: `0` (`:1068`).
- Thanh cong: so ban ghi bi anh huong.
- `SaveChangesAsync` tra ve `0`: `0`.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (`:1033`).
2. Guard `entities.IsNullOrEmpty()` -> log + `return 0` (`:1035-1040`).
3. `foreach` map tung phan tu, chuyen tiep domain event; phan tu map ra `null` bi bo qua am tham (`:1044-1062`).
4. Guard `entitiesConvert.IsNullOrEmpty()` -> log + `return 0` (`:1064-1069`).
5. Tao `DBContextWrite` moi (`:1071-1072`).
6. `createDbContext.Set<TEntityTo>().UpdateRange(entities: entitiesConvert)` (`:1076`).
7. `_pipelineWrite.ExecuteAsync(... SaveChangesAsync(audit: auditLog, ct) ...)` (`:1078-1083`).

**Side effect** - **Ghi DB (UPDATE nhieu dong trong mot `SaveChanges`)**. Ghi log khi guard chan (`:1037`, `:1066`). Tao va dispose `DBContextWrite`. Publish domain event; **khong sinh ban ghi audit log** (xem 2.15). Cac cot `Modified*` chi duoc dien khi `auditLog` khac `null` - xem canh bao o 2.19, nhan len theo so phan tu.

**Error handling** - Khong `try/catch`. Neu mot dong that bai, toan bo `SaveChanges` roll back. `DbUpdateConcurrencyException` nem ra caller. `_pipelineWrite` retry 1 lan.

**Khi nao NEN dung** - Cap nhat lo nho/trung binh voi day du gia tri moi cot, can tinh nguyen tu.

**Khi nao KHONG dung** -
- Khi la partial update: rui ro ghi de gia tri mac dinh nhu 2.19, nhan len theo so phan tu.
- Khi lo rat lon: khong batching, `UpdateRange` giu toan bo entity trong `ChangeTracker`.
- Khi mot so phan tu chua co khoa chinh: se bi EF Core coi la `Added` -> INSERT ngoai y muon.
- Khi can biet phan tu nao khong map duoc: bi bo qua khong log.

**Gioi han** - `MapUsingExpression` compile lai cho tung phan tu (`:1049-1050`). `entities` bi duyet nhieu lan. Khong gioi han so phan tu. Full-row update cho moi phan tu.

---

### 2.22 UpdateAsync (IEnumerable&lt;TEntityFrom&gt;, DBContextWrite, AuditModel, CancellationToken)

**Signature**

```csharp
public virtual async Task<(int Result, IEnumerable<TEntityFrom> Data)> UpdateAsync(
    IEnumerable<TEntityFrom> entities,
    DBContextWrite context,
    AuditModel auditLog = null,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Giong 2.21 nhung dung `context` cua caller va tra ve ca danh sach ket qua (`CoreSQLTenant.cs:1104-1163`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entities` | `IEnumerable<TEntityFrom>` | Co | `entities.IsNullOrEmpty()` -> log + `return (0, null)` (`:1106-1111`) | Khong co |
| `context` | `DBContextWrite` | Co | **Khong null-check.** `null` gay `NullReferenceException` tai `:1144` | Khong co |
| `auditLog` | `AuditModel` | Khong | Khong validate | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`:1104`) | `default` |

**Output** - `Task<(int Result, IEnumerable<TEntityFrom> Data)>`.
- `entities` null/rong: `(Result: 0, Data: null)` (`:1110`).
- Sau map, `entitiesConvert` rong: `(Result: 0, Data: null)` (`:1139`).
- `SaveChangesAsync` tra ve `0`: `(Result: 0, Data: entities)` - **danh sach dau vao**, khac 2.18 tra ve `[]` (`:1153-1158`).
- Thanh cong: `(Result: result, Data: entitiesConvert.ProjectTo<TEntityTo, TEntityFrom>())` (`:1159-1162`).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (`:1104`).
2. Guard `entities.IsNullOrEmpty()` -> log + `return (0, null)` (`:1106-1111`).
3. `foreach` map tung phan tu, chuyen tiep domain event; phan tu `null` bi bo qua am tham (`:1115-1133`).
4. Guard `entitiesConvert is null || entitiesConvert.Count <= 0` -> log + `return (0, null)` (`:1135-1140`). **Luu y**: day la kiem tra tuong minh, khong dung `IsNullOrEmpty()` nhu cac overload khac; hanh vi tuong duong.
5. `context.Set<TEntityTo>().UpdateRange(entities: entitiesConvert)` (`:1144`).
6. `_pipelineWrite.ExecuteAsync(... context.SaveChangesAsync(audit: auditLog, ct) ...)` (`:1146-1151`).
7. `switch (result is 0)`: `true` -> `(0, entities)`; `false` -> `(result, entitiesConvert.ProjectTo<TEntityTo, TEntityFrom>())` (`:1153-1163`).

**Side effect** - **Ghi DB (UPDATE)** qua `context` cua caller. Ghi log khi guard chan (`:1108`, `:1137`). Thay doi `ChangeTracker` cua `context`. `SaveChangesAsync` chay ngay. Khong dispose `context`. **Khi `result > 0`, `ChangeTracker` cua `context` bi `Clear()`** (`WriteDbContext.cs:433`) - xem canh bao o 2.16. Cac cot `Modified*` chi duoc dien khi `auditLog` khac `null` - xem canh bao o 2.19.

**Error handling** - Khong `try/catch`. `NullReferenceException` neu `context` la `null`. `InvalidOperationException` neu `context` da track ban ghi cung khoa chinh. `_pipelineWrite` retry 1 lan.

**Khi nao NEN dung** - Cap nhat lo trong cung transaction voi thao tac khac va can danh sach ket qua sau cap nhat.

**Khi nao KHONG dung** -
- Khi la partial update (rui ro mat du lieu, xem 2.19).
- Khi ky vong khong commit ngay.
- Khi `context` da track cac ban ghi lien quan.
- Khi caller phan biet thanh/that bai qua `Data`: `Data` co the la `null` (guard) hoac chinh `entities` (that bai) - khong nhat quan.

**Gioi han** - `MapUsingExpression` compile lai cho tung phan tu. `entities` bi duyet nhieu lan. Khong batching. `Data` khi that bai la danh sach dau vao chu khong phai `[]` - **bat doi xung voi 2.18** (`CreateAsync` tra ve `[]` tai `:883`, `UpdateAsync` tra ve `entities` tai `:1157`).

---

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | **Ten file `CoreSQLTenant.cs` gay hieu nham: khong co bat ky logic multi-tenant nao.** Grep chuoi `enant` tren toan file = 0 ket qua. Khong co tenant id, tenant column, tenant context, tenant filter | `CoreSQLTenant.cs` (toan file); class khai bao tai `CoreSQLTenant.cs:17` la `CoreSQL`, khong phai `CoreSQLTenant` | Developer/AI doc ten file se tuong lop nay cung cap tenant isolation va **bo qua viec tu loc theo tenant** -> ro ri du lieu giua cac don vi thue. Vai tro thuc te chi la mapping `TEntityFrom` -> `TEntityTo` |
| 2 | XML doc cua cac ham `FindAll*` ghi "hoặc null nếu không tìm thấy kết quả" nhung than ham tra ve `[]` | Doc: `CoreSQLTenant.cs:461, 503, 544, 582`; code: `:493, 535, 573, 611` | Doc sai lech voi code. Theo nguyen tac Source Code > Documentation: **luon la `[]`, khong bao gio `null`** |
| 3 | Bat doi xung gia tri tra ve khi input rong: `FindAllWithScriptAsync` tra ve `null`, con `FindAllAsync`/`FindAllSortDeletedAsync` tra ve `[]` | `CoreSQLTenant.cs:138` vs `:493, 535, 573, 611` | Caller de gap `NullReferenceException` khi dung chung mot cach xu ly ket qua cho ca hai nhom API |
| 4 | Guard `if (entityConvert is null)` la **code khong bao gio chay**: `MapUsingExpression` luon tra ve instance moi qua `Expression.MemberInit(Expression.New(typeof(TTo)), ...)` | `CoreSQLTenant.cs:643, 705, 921, 982`; nguon: `ProjectToExtensions.cs:183-187` | Tao cam giac an toan gia. Truong hop map that bai thuc su (thuoc tinh bi bo qua) **khong** bi guard nay phat hien -> ghi gia tri mac dinh vao DB |
| 5 | `MapUsingExpression` chi bind khi ten trung **va** kieu trung **va** thuoc tinh nguon `CanWrite`; thuoc tinh khong thoa bi bo qua am tham. Ket hop `DbSet.Update` (full-row update) gay **ghi de `null`/`0` len cot dang co du lieu** | Map: `ProjectToExtensions.cs:157-160`; update: `CoreSQLTenant.cs:939, 997, 1076, 1144` | **Rui ro mat du lieu cao nhat cua module.** Bat ky sai khac ten/kieu giua `TEntityFrom` va `TEntityTo` deu dan den xoa du lieu cot tuong ung khi UPDATE |
| 6 | `MapUsingExpression` goi `.Compile()` moi lan, khong cache; trong cac overload nhan `IEnumerable` thi compile **cho tung phan tu** | `ProjectToExtensions.cs:185`; vong lap: `CoreSQLTenant.cs:773-774, 846, 1049-1050, 1121` | Chi phi CPU va allocation tang tuyen tinh theo so phan tu; diem nghen hieu nang ro rang khi ghi lo lon |
| 7 | `IsExecuteNonQueryAsync` **khong** duoc bao boc bang `_pipelineWrite` (khong retry, khong circuit breaker), **khong** goi `SaveChangesAsync` (khong dien cot audit, khong publish domain event), va **khong** truyen `cancellationToken` vao lenh SQL | `CoreSQLTenant.cs:244-249` | Ngoai le duy nhat trong lop: mat co che resilience, mat viec tu dien cac cot `ModifiedUser`/`ModifiedDate`/..., mat publish domain event va mat kha nang huy tac vu. **Khong** mat "audit trail" vi module khong sinh audit trail o bat ky duong nao (xem van de 26) |
| 8 | Thu tu tham so trong XML doc cua `IsExecuteNonQueryAsync` khac thu tu trong signature | Doc: `CoreSQLTenant.cs:216-222`; signature: `:226-233` | Doc sai. Truyen tham so theo doc se sai vi tri; theo signature dung la `(scriptSQLQuery, context, transaction, parameters, ...)` |
| 9 | Cac tham so `DBContextWrite context` va `DbConnection context` **khong duoc null-check** o bat ky overload nao | `CoreSQLTenant.cs:244, 720, 869, 997, 1144` | `NullReferenceException` khong co thong tin ngu canh, khong ghi log, thay vi `ArgumentNullException` ro rang |
| 10 | Raw SQL **khong duoc validate** ngoai kiem tra rong/trang; script duoc noi suy truc tiep vao chuoi SQL | `CoreSQLTenant.cs:82, 89-96, 134, 141-148, 184, 191-198, 237` | Neu caller ghep chuoi tu input nguoi dung -> **SQL injection**. Module khong co lop phong ve nao |
| 11 | Hardcode `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;` cho moi truy van `CommandType.Text`, khong the tat | `CoreSQLTenant.cs:92, 144, 194` | Dirty read tren toan bo duong doc raw SQL: co the doc du lieu chua commit, doc trung hoac bo sot dong. Khong phu hop cho nghiep vu tai chinh/doi soat |
| 12 | Ten cot soft delete hardcode la chuoi `"IsDeleted"`, khong cau hinh duoc; cac ham khong co hau to `SortDeleted` **khong loc** soft delete | Hang so: `CoreSQLTenant.cs:26`; su dung: `:319, 361, 479, 521`; khong loc: `FindByIdAsync` (`:279`), `FindOneAsync` (`:399, 437`), `FindAllAsync` (`:559, 597`) | Dat ten gan giong nhau (`FindOneAsync` vs `FindOneSortDeletedAsync`) rat de dung sai ham -> lo ban ghi da xoa mem ra ngoai. Neu `TEntityTo` khong co cot `IsDeleted`, nhom `SortDeleted` se nem `InvalidOperationException` |
| 13 | **Khong co API Delete/Remove nao** trong ca class va interface | `CoreSQLTenant.cs` (khong ton tai); `ICoreSQL.cs` (21 thanh vien, khong co Delete) | Muon xoa (cung hoac mem) phai dung `UpdateAsync` (mang rui ro full-row update) hoac raw SQL qua `IsExecuteNonQueryAsync` (khong qua `OnBeforeSaveChanges` nen **khong dien cac cot audit tren ban ghi**, khong publish domain event — xem van de 7; day khong phai "mat audit trail" vi module khong sinh audit trail o bat ky duong nao, xem van de 26) |
| 14 | **Khong ho tro phan trang, sap xep, dem**; `ToListAsync` keo toan bo tap ket qua | `CoreSQLTenant.cs:489, 531, 569, 607` | Rui ro OOM va command timeout voi bang lon. Overload co `sorting` chi ton tai o lop 3 type parameter (`CoreSQL.cs:330`), ly do duoc giai thich tai `CoreSQL.cs:318-321` |
| 15 | `ProjectTo` bat exception cho **tung thuoc tinh** va **tung phan tu**, chi ghi log ra Console roi bo qua | `ProjectToExtensions.cs:56-59, 111-123` | Mapping that bai am tham: field bi thieu gia tri, hoac phan tu bi loai khoi danh sach ket qua, ma **khong co exception va khong vao log he thong** (chi Console) |
| 16 | `_pipelineWrite` retry bao boc `SaveChangesAsync`; khong co idempotency key | `CoreSQLTenant.cs:664-669, 723-728, 803-808, 872-877, 941-946, 999-1004, 1078-1083, 1146-1151`; chinh sach: `SqlResiliencePolicyFactory.cs:194-210` | Neu lan dau da commit tai DB nhung ket noi dut truoc khi client nhan phan hoi, lan retry co the **insert/update trung** |
| 17 | Cac overload nhan `context` co XML doc noi "theo cùng một transaction" nhung than ham goi `SaveChangesAsync` **ngay** | Doc: `CoreSQLTenant.cs:675, 814, 952, 1089`; code: `:723-728, 872-877, 999-1004, 1146-1151` | Ham chi nam trong transaction chung **neu caller da tu mo transaction**. Lop nay khong mo transaction; hieu sai doc dan den ky vong nguyen tu khong duoc dam bao |
| 18 | Bat doi xung `Data` trong tuple khi that bai: `(0, null)`, `(0, entity)`, `(0, entities)`, hoac `(0, [])` tuy overload va tuy nhanh | `CoreSQLTenant.cs:696, 709, 734, 835, 864, 883, 973, 986, 1010, 1110, 1139, 1157` | Caller khong the dua vao `Data != null` de biet thanh cong; buoc phai kiem tra `Result`. `Data` la entity dau vao khi that bai de gay ngo nhan da ghi thanh cong |
| 19 | Phan tu map that bai trong cac overload `IEnumerable` bi bo qua **khong ghi log** | `CoreSQLTenant.cs:776, 848, 1052, 1123` (nhanh `if (entityConvert is not null)` khong co `else`) | So ban ghi ghi duoc co the it hon so phan tu dau vao ma khong co canh bao nao |
| 20 | Cac overload `IEnumerable` duyet `entities` **nhieu lan** (`IsNullOrEmpty()` roi `foreach`) | `CoreSQLTenant.cs:759 + 768`, `:831 + 840`, `:1035 + 1044`, `:1106 + 1115` | `IEnumerable` lazy chi duyet mot lan (stream tu DB reader, generator) se cho ket qua sai hoac nem exception |
| 21 | Khong co `try/catch` o bat ky dau trong `CoreSQLTenant.cs` | Toan file | Moi exception cua EF Core/Dapper/Polly nem thang len caller **khong duoc ghi log tai lop repository**; chi guard clause nghiep vu duoc log qua `FailLogic` |
| 22 | Kiem tra danh sach rong khong nhat quan: `entitiesConvert.IsNullOrEmpty()` o 3 cho, nhung `entitiesConvert is null \|\| entitiesConvert.Count <= 0` o cho thu 4 | `CoreSQLTenant.cs:788, 860, 1064` vs `:1135` | Chi la khong nhat quan ve style, hanh vi tuong duong. Gay kho bao tri |
| 23 | Lop nay `abstract` nhung trong repo **khong co lop con nao ke thua** (grep `: CoreSQL<` khong co ket qua) | Toan repo | Khong co vi du su dung thuc te trong repo de doi chieu hanh vi; moi ket luan ve cach dung phai dua tren source cua chinh lop nay |
| 24 | `FindByIdAsync` chi ho tro **khoa chinh don** (`keyValues: [id]`), tham so kieu `object` | `CoreSQLTenant.cs:261, 280` | Bang co khoa chinh phuc hop khong dung duoc ham nay. Sai kieu `id` chi phat hien tai runtime |
| 25 | `FindOneWithScalarScriptAsync` chen `SET TRANSACTION ISOLATION LEVEL ...;` truoc script roi goi `ExecuteScalarAsync` | `CoreSQLTenant.cs:191-198, 201-212` | Voi script `Text` nhieu cau lenh, ket qua scalar phu thuoc statement nao tra ve tap ket qua dau tien. **Khong xac dinh duoc tu source code** hanh vi chinh xac cua SQL Server trong moi truong hop; can kiem chung thuc te |
| 26 | **`AuditModel` KHONG sinh audit trail.** `DetectChangesAudit` co toan bo than ham thu thap thay doi bi comment (`#region NOT SUPPORT`) va **luon `return []`**; do do `DispatchAuditLog` luon thoat ngay. Khong co ban ghi audit log nao duoc tao boi bat ky API ghi nao cua lop | `WriteDbContext.cs:192-357` (`return []` tai `:356`), `:373-378` | Tham so `auditLog` cua 8 overload `CreateAsync`/`UpdateAsync` **chi co tac dung dien cac cot audit tren chinh ban ghi**, khong tao vet kiem toan. Bat ky ky vong "co audit trail day du" deu sai. Nhu vay dong "khong audit" cua `IsExecuteNonQueryAsync` (van de 7) **khong** la diem khac biet so voi cac API con lai |
| 27 | **`ChangeTracker.Clear()` duoc goi tren `context` do caller truyen vao** sau moi `SaveChangesAsync` tra ve `> 0`; goi truoc ca khi kiem tra co domain event hay khong nen **luon chay** | `WriteDbContext.cs:433`; duong goi: `CoreSQLTenant.cs:723-728, 872-877, 999-1004, 1146-1151` -> `WriteDbContext.cs:88-91` -> `:366-370` | Side effect nang nhat len object dung chung: moi entity khac dang duoc `context` theo doi (da `Add`/`Attach`/vua doc len) bi detach, thay doi chua luu cua chung bi mat, cac `SaveChangesAsync` sau khong ghi gi. Pha vo pattern unit-of-work nhieu buoc tren cung mot `DBContextWrite` |
| 28 | Lop nay chuyen tiep domain event khi `entityConvert is IAggregate`, nhung `DispatchDomainEvents` chi thu event tu `ChangeTracker.Entries<Aggregate>()` - **lop truu tuong `Aggregate`**, khong phai interface `IAggregate` | Chuyen tiep: `CoreSQLTenant.cs:651-654, 713-716, 779-782, 851-854, 929-932, 990-993, 1055-1058, 1126-1129`; thu event: `WriteDbContext.cs:421` | Neu `TEntityTo` implement `IAggregate` truc tiep ma khong ke thua `Aggregate` (`Abstractions/Aggregate.cs:12`), domain event duoc gan vao entity nhung **khong bao gio duoc publish** - mat su kien am tham, khong log, khong exception |
| 29 | Nhom cot `Modified*` chi duoc dien khi `auditLog` khac `null`; nhom cot `Created*` khong bao gio duoc dien lai o nhanh `Modified` | `WriteDbContext.cs:168-181` | Ket hop voi full-row update (van de 5): `UpdateAsync(entity)` khong truyen `auditLog` se ghi de `ModifiedUser/ModifiedDate/...` bang gia tri cua `entityConvert` (thuong `null`), va luon ghi de `CreatedUser/CreatedDate/...` bang gia tri cua `entityConvert`. Mat thong tin tao/sua |
| 30 | Khi INSERT, `OnBeforeSaveChanges` **cuong che** `IsDeleted = false` va **cuong che** `ModifiedDate/ModifiedUser/ModifiedUserCode/ModifiedUserOrganization` ve `null`, bo qua gia tri caller da dat | `WriteDbContext.cs:154, 161-164` | Khong the tao ban ghi o trang thai da xoa mem, va khong the ghi san thong tin sua khi tao. Chi ap dung voi entity implement `IBaseEntitySQL` (`WriteDbContext.cs:132`); entity khong implement interface nay **khong duoc dien cot audit nao** |
| 31 | `ProjectTo` yeu cau kieu dich co constructor cong khai khong tham so, va **hai overload xu ly loi khac nhau**: overload doi tuong don goi `Activator.CreateInstance` **ngoai** `try/catch` -> nem `MissingMethodException` ra caller; overload `List<T>` goi **trong** `try/catch` -> bo qua tung phan tu va tra ve `[]` | `ProjectToExtensions.cs:29` vs `:92` (trong `try` tai `:90-123`) | Cung mot loi cau hinh kieu cho ket qua hoan toan khac nhau: `FindOneAsync<TDto>` nem exception, con `FindAllAsync<TDto>` tra ve list rong nhu the "khong co du lieu" |
| 32 | Guard clause duoc ghi log qua `FailLogic` o muc **`LogLevel.Information`**, khong phai `Warning`/`Error` | `LoggerExtensions.cs:179-182, 358` | Cac truong hop "am tham tra ve `0`/`null`/`false`" (script rong, `entity` null, `entities` rong) se **khong xuat hien** trong log neu minimum level cua he thong tu `Warning` tro len -> loi nghiep vu bien mat hoan toan |
| 33 | Dead code: bien `result` duoc khoi tao `0` roi bi ghi de ngay o cau lenh ke tiep, khong nhanh nao doc gia tri `0` ban dau; mot cho dung `var` thay vi `int` | `CoreSQLTenant.cs:659, 718, 795, 867, 995, 1074, 1142`; rieng `:934` dung `var result = 0;` thay vi `int` | Khong anh huong hanh vi, chi gay nhieu khi doc code va khong nhat quan style |
| 34 | `MapUsingExpression` doi hoi **thuoc tinh nguon phai `CanWrite`** moi map (`sourceProp.CanWrite is false` -> bo qua), trong khi de **doc** gia tri chi can `CanRead` | `ProjectToExtensions.cs:157-163` | Moi thuoc tinh chi doc (computed property, `{ get; }`, `=> expression`) cua `TEntityFrom` bi bo qua khi ghi -> cot tuong ung nhan gia tri mac dinh va (voi full-row update) ghi de len DB. Day la mot **loi trong source code**, khong phai gioi han co y |
