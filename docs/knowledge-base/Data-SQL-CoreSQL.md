# CoreSQL&lt;TEntity, DBContextRead, DBContextWrite&gt; / ICoreSQL&lt;TEntity, DBContextRead, DBContextWrite&gt;

> Nguon: `FTELSRCore.Shared/Data/SQL/Core/CoreSQL.cs`, `FTELSRCore.Shared/Data/SQL/Core/ICoreSQL.cs`
> Loai: `CoreSQL<TEntity, DBContextRead, DBContextWrite>` la **abstract partial class**; `ICoreSQL<TEntity, DBContextRead, DBContextWrite>` la **interface**
> Cap nhat theo commit: `2262829`

> [!IMPORTANT]
> **Phan biet danh tinh type.** File `CoreSQL.cs:16` khai bao `public abstract partial class CoreSQL<TEntity, DBContextRead, DBContextWrite>` (**3 type parameter**). File `CoreSQLTenant.cs:17` khai bao `public abstract partial class CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>` (**4 type parameter**). Vi khac arity nen day la **hai generic type doc lap**, khong phai hai phan partial cua cung mot class. Tai lieu nay chi mo ta type **3 type parameter**.

## 1. Tong quan

`CoreSQL<TEntity, DBContextRead, DBContextWrite>` la lop co so truu tuong (base repository) cua tang truy cap du lieu SQL Server trong thu vien `FTELSRCore.Shared`. Lop nay gom hai kenh truy cap vao mot API duy nhat: **EF Core** (qua `IDbContextFactory<DBContextRead>` cho doc va `IDbContextFactory<DBContextWrite>` cho ghi) va **raw SQL/Dapper** (qua `IDapperSQLDBContext`). Moi lenh EF Core va moi lenh Dapper doc deu duoc boc trong Polly `ResiliencePipeline` (`_pipelineRead` / `_pipelineWrite`) duoc inject tu ngoai (`CoreSQL.cs:23`, `CoreSQL.cs:25`).

Lop nam o tang Data Access, duoc cac repository nghiep vu cua tung microservice ke thua (constructor la `protected` — `CoreSQL.cs:35`). Trong repo `sr-core-helper` khong co class nao ke thua `CoreSQL<,,>` va cung khong co doan code nao dang ky DI cho no; day la thu vien duoc build ra DLL roi copy sang cac repo API khac (xem `Target Name="CopyToOtherLibs"` trong `FTELSRCore.Shared/FTELSRCore.Shared.csproj`).

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Doc 1 ban ghi theo khoa chinh bang EF Core `FindAsync` (`CoreSQL.cs:268-269`) | **Khong co bat ky API xoa nao** (khong `Delete`, `Remove`, `RemoveRange`, `ExecuteDelete`) — ca hard delete lan soft delete deu khong duoc cung cap |
| Doc 1 / nhieu ban ghi theo mang `Expression<Func<TEntity, bool>>[]` filter, to hop bang `Aggregate` + `Where` (AND logic) | Khong ho tro OR giua cac filter (moi phan tu trong `filters` duoc noi bang `Where` lien tiep -> luon la AND) |
| Loc theo cot soft delete bang `EF.Property<bool>(x, "IsDeleted")` trong nhom `*SortDeletedAsync` | Khong doc duoc entity ma **model EF Core** khong khai bao property `IsDeleted` kieu `bool` (CLR property hoac shadow property deu duoc `EF.Property` chap nhan) bang nhom `*SortDeletedAsync` — ten cot hardcode tai `CoreSQL.cs:27` |
| Anh xa entity -> DTO bang reflection qua `ProjectTo<TEntity, TDto>()` | Khong co AutoMapper/expression-based mapping mac dinh; chi overload `FindOneSortDeletedAsync<TDto>` co `selector` la chieu server-side |
| Sap xep (`sorting`) va chieu server-side (`selector`) — chi o overload `FindOneSortDeletedAsync<TDto>` tai `CoreSQL.cs:330` | Khong co phan trang (`Skip`/`Take`), khong co `Count`/`Any`/`Exists`, khong co `Include`/eager loading, khong co `GroupBy` |
| Thuc thi raw SQL / stored procedure doc: 1 dong, nhieu dong, 1 gia tri scalar | Khong co API raw SQL doc nao nhan `DbConnection`/`DbTransaction` cua caller (chi `IsExecuteNonQueryAsync` nhan) |
| Thuc thi raw SQL non-query tren `DbConnection` + `DbTransaction` do caller cung cap (`IsExecuteNonQueryAsync`) | `IsExecuteNonQueryAsync` **khong** duoc boc Polly pipeline va **khong** truyen `CancellationToken` xuong Dapper |
| Them / cap nhat 1 entity hoac 1 tap entity, kem `AuditModel` tuy chon — `AuditModel` chi duoc dung de gan cac field `Created*`/`Modified*` tren entity implement `IBaseEntitySQL` | Khong co upsert, khong co bulk update kieu `ExecuteUpdate`, khong co tracking-free update theo dieu kien. **Khong ghi audit log**: `WriteDbContext.DetectChangesAudit` luon tra `[]` (`WriteDbContext.cs:356`) — xem muc 3 #28. **Khong giu duoc change tracking** cua `DBContextWrite` do caller truyen vao: sau moi lan luu thanh cong `WriteDbContext` goi `ChangeTracker.Clear()` (`WriteDbContext.cs:433`) — xem muc 3 #27 |
| Ap Polly retry + circuit breaker cho toan bo lenh EF Core va Dapper doc | `IsExecuteNonQueryAsync` khong co resilience; cac lenh `AddAsync`/`Update`/`UpdateRange` nam ngoai pipeline (chi `SaveChangesAsync` nam trong) |
| Ghi log nghiep vu khi guard clause chan dau vao (`_logger.FailLogic`, `LogLevel.Information` — `LoggerExtensions.cs:179-182`) | **Khong co try/catch nao trong toan bo file** (`CoreSQL.cs` khong chua tu khoa `catch`) -> moi exception deu noi len caller; khong log exception. Nhom `FindOne*`/`FindAll*` EF Core **khong** co guard clause nao nen cung khong ghi log |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `ILogger<CoreSQL<TEntity, DBContextRead, DBContextWrite>>` (`CoreSQL.cs:21`) | Chi dung de goi `_logger.FailLogic(...)` trong cac guard clause. Extension dinh nghia tai `FTELSRCore.Shared/Extensions/Loggers/LoggerExtensions.cs:358`, gan `EventId` = 107 va category `BIZ_LOGIC` |
| `Polly.ResiliencePipeline _pipelineRead` (`CoreSQL.cs:23`) | Boc moi lenh doc (EF Core + Dapper). Duoc inject qua constructor, **khong** duoc tao ben trong class |
| `Polly.ResiliencePipeline _pipelineWrite` (`CoreSQL.cs:25`) | Boc rieng loi goi `SaveChangesAsync` trong cac method `CreateAsync`/`UpdateAsync` |
| `Lazy<IDapperSQLDBContext> _dapperDbContext` (`CoreSQL.cs:29`) | Thuc thi raw SQL doc: `GetOne`, `GetAll`, `GetOneExecute`. Implementation `DapperSQLDBContext` khoi tao **mot `SqlConnection` moi cho moi lenh goi** (`Dapper/DapperSQLDBContext.cs:77-78`); `ConfigurationHelpers.CreateConnection` chi `new SqlConnection(...)` va **khong tu mo** (`Dapper/Helpers/ConfigurationHelpers.cs:16-19`) — Dapper mo khi thuc thi, `await using` dispose ngay sau. Hai method `ExecuteNonQueryAsync` va `GetAllExecuteAsync` cua interface **khong duoc `CoreSQL` dung** (xem muc 3 #30) |
| `Lazy<IDbContextFactory<DBContextRead>> _dbContextRead` (`CoreSQL.cs:31`) | Tao `DBContextRead` cho moi lenh doc EF Core; context duoc `await using` nen dispose ngay sau lenh |
| `Lazy<IDbContextFactory<DBContextWrite>> _dbContextWrite` (`CoreSQL.cs:33`) | Tao `DBContextWrite` cho cac overload ghi **khong** nhan `context` tu ngoai |
| `ReadDbContext<DBContextRead>` (`DbContexts/Read/ReadDbContext.cs:12`) | Rang buoc generic cho `DBContextRead`. La `DbContext` thuan, khong audit, khong domain event |
| `WriteDbContext<DBContextWrite>` (`DbContexts/Write/WriteDbContext.cs:16`) | Rang buoc generic cho `DBContextWrite`. Cung cap `SaveChangesAsync(AuditModel audit, bool acceptAllChangesOnSuccess, CancellationToken)` (`WriteDbContext.cs:75`) — goi `OnBeforeSaveChanges(audit)`, `DetectChangesAudit(audit)` (chi khi `audit is not null`) va `OnAfterSaveChanges(...)` khi `result > 0`. **Ba gioi han quan trong doc duoc tu source:** (1) `DetectChangesAudit` **luon `return []`** — toan bo than ham thu thap audit bi comment trong region `NOT SUPPORT` (`WriteDbContext.cs:356`), nen `DispatchAuditLog` luon thoat ngay o guard rong (`WriteDbContext.cs:375-378`) -> **khong co ban ghi audit log nao duoc tao**; (2) `OnAfterSaveChanges` -> `DispatchDomainEvents` goi **`ChangeTracker.Clear()` vo dieu kien** (`WriteDbContext.cs:433`); (3) `OnBeforeSaveChanges` chi xu ly entry co `Entity is IBaseEntitySQL` (`WriteDbContext.cs:131-132`) |
| `AuditModel` (`Models/Audits/AuditModel.cs:3`) | Record chua `Ip`, `Device`, `Method`, `Address`, `CreatorInfo`. Truyen xuyen suot xuong `WriteDbContext.SaveChangesAsync` |
| `ProjectToExtensions.ProjectTo<TEntity, TDto>` (`Extensions/ProjectToExtensions.cs:27` va `:76`) | Anh xa entity -> DTO bang reflection: `Activator.CreateInstance(typeof(TDto))` + copy property cung ten, bo qua property khi `dtoProp is null`, `dtoProp.CanWrite is false`, hoac **mot trong hai phia** (entity/DTO) co `NoMapAttribute` (`ProjectToExtensions.cs:47-52`). Hai overload **khong** hanh xu giong nhau khi khoi tao `TDto` that bai — xem 2.11 |
| `CollectionHelpers.IsNullOrEmpty<T>` (`Helpers/CollectionHelpers.cs:14`) | Guard clause cho cac overload nhan `IEnumerable<TEntity>` va kiem tra ket qua rong cua cac `FindAll*` |
| `Dapper` (`DynamicParameters`, `SqlMapper.ExecuteAsync`) | `DynamicParameters` la kieu tham so cua tat ca method raw SQL; `using static Dapper.SqlMapper;` (`CoreSQL.cs:10`) dua extension `ExecuteAsync` vao scope cho `IsExecuteNonQueryAsync` |
| `Microsoft.EntityFrameworkCore` (`EF.Property`, `EntityState`, `AsNoTracking`, `FirstOrDefaultAsync`, `ToListAsync`) | Xay query LINQ tren `DbSet<TEntity>` |
| `SqlResiliencePolicyFactory` (`Data/SQL/Helpers/Policies/SqlResiliencePolicyFactory.cs`) | **Khong duoc `CoreSQL.cs` tham chieu truc tiep.** La factory cau hinh pipeline (read: retry 3 lan exponential+jitter, CB 60%/5req/10s -> break 20s; write: retry 1 lan chi loi connection-level, CB 50%/10req/15s -> break 60s). Viec gan factory nay vao `_pipelineRead`/`_pipelineWrite` phai do code dang ky DI ben ngoai repo thuc hien — trong repo nay **khong co doan code nao goi `ConfigureReadPolicy`/`ConfigureWritePolicy`** |

> [!NOTE]
> **Polly co duoc ap dung o day khong?** Co — khac voi viec chi ton tai factory. `CoreSQL.cs` goi `_pipelineRead.ExecuteAsync(...)` tai 13 vi tri va `_pipelineWrite.ExecuteAsync(...)` tai 8 vi tri. Tuy nhien `IsExecuteNonQueryAsync` (`CoreSQL.cs:217`) la ngoai le duy nhat: no goi Dapper truc tiep, khong qua pipeline nao.

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `FindOneWithScriptAsync<TDto>` | Raw SQL / Dapper (doc) | Chay script SQL, tra 1 dong dau tien map sang `TDto` |
| `FindAllWithScriptAsync<TDto>` | Raw SQL / Dapper (doc) | Chay script SQL, tra danh sach `TDto` |
| `FindOneWithScalarScriptAsync<TDto>` | Raw SQL / Dapper (doc) | Chay script SQL, tra 1 gia tri scalar `TDto` |
| `IsExecuteNonQueryAsync` | Raw SQL / Dapper (ghi) | Chay script SQL non-query tren `DbConnection`/`DbTransaction` cua caller, tra `bool` |
| `FindByIdAsync` | EF Core doc (`DBContextRead`) | Tim entity theo khoa chinh, detach khoi change tracker |
| `FindOneSortDeletedAsync<TDto>(filters, isDeleted, ct)` | EF Core doc (`DBContextRead`) | 1 entity theo filter + `IsDeleted`, map DTO bang reflection |
| `FindOneSortDeletedAsync<TDto>(filters, sorting, isDeleted, selector, ct)` | EF Core doc (`DBContextRead`) | Overload **khong** thuoc `ICoreSQL`: them `sorting` va `selector` chieu server-side |
| `FindOneSortDeletedAsync(filters, isDeleted, ct)` | EF Core doc (`DBContextRead`) | 1 entity theo filter + `IsDeleted`, tra `TEntity` |
| `FindOneAsync<TDto>` | EF Core doc (`DBContextRead`) | 1 entity theo filter (khong xet `IsDeleted`), map DTO |
| `FindOneAsync` | EF Core doc (`DBContextRead`) | 1 entity theo filter (khong xet `IsDeleted`), tra `TEntity` |
| `FindAllSortDeletedAsync<TDto>` | EF Core doc (`DBContextRead`) | Danh sach DTO theo filter + `IsDeleted` |
| `FindAllSortDeletedAsync` | EF Core doc (`DBContextRead`) | Danh sach `TEntity` theo filter + `IsDeleted` |
| `FindAllAsync<TDto>` | EF Core doc (`DBContextRead`) | Danh sach DTO theo filter (khong xet `IsDeleted`) |
| `FindAllAsync` | EF Core doc (`DBContextRead`) | Danh sach `TEntity` theo filter (khong xet `IsDeleted`) |
| `CreateAsync(TEntity, AuditModel, ct)` | EF Core ghi (`DBContextWrite` tu tao) | Them 1 entity, tu tao & dispose context, tra `int` |
| `CreateAsync(TEntity, DBContextWrite, AuditModel, ct)` | EF Core ghi (context cua caller) | Them 1 entity vao context cua caller, tra `(int Result, TEntity Data)` |
| `CreateAsync(IEnumerable<TEntity>, AuditModel, ct)` | EF Core ghi (`DBContextWrite` tu tao) | Them nhieu entity, tra `int` |
| `CreateAsync(IEnumerable<TEntity>, DBContextWrite, AuditModel, ct)` | EF Core ghi (context cua caller) | Them nhieu entity, tra `(int Result, IEnumerable<TEntity> Data)` |
| `UpdateAsync(TEntity, AuditModel, ct)` | EF Core ghi (`DBContextWrite` tu tao) | Cap nhat 1 entity, tra `int` |
| `UpdateAsync(TEntity, DBContextWrite, AuditModel, ct)` | EF Core ghi (context cua caller) | Cap nhat 1 entity, tra `(int Result, TEntity Data)` |
| `UpdateAsync(IEnumerable<TEntity>, AuditModel, ct)` | EF Core ghi (`DBContextWrite` tu tao) | Cap nhat nhieu entity, tra `int` |
| `UpdateAsync(IEnumerable<TEntity>, DBContextWrite, AuditModel, ct)` | EF Core ghi (context cua caller) | Cap nhat nhieu entity, tra `(int Result, IEnumerable<TEntity> Data)` |

Tong: **22 public method** tren class. `ICoreSQL` khai bao **21** trong so do — overload `FindOneSortDeletedAsync<TDto>(filters, sorting, isDeleted, selector, ct)` chi ton tai tren class (`CoreSQL.cs:330`), ly do duoc ghi ngay trong XML doc tai `CoreSQL.cs:319-321`: `sorting` la delegate da bien dich (khong phai expression tree) nen khong the "dich" tham so kieu nhu `ReplaceParameters<TFrom,TTo>` de tai su dung cho lop `CoreSQL<TEntityFrom, TEntityTo, ...>`.

### 1.4 Khai bao type va constructor

**Signature type**

```csharp
public abstract partial class CoreSQL<TEntity, DBContextRead, DBContextWrite> : ICoreSQL<TEntity, DBContextRead, DBContextWrite>
    where TEntity : class
    where DBContextRead : ReadDbContext<DBContextRead>
    where DBContextWrite : WriteDbContext<DBContextWrite>
```

**Signature constructor** (`CoreSQL.cs:35-40`)

```csharp
protected CoreSQL(
    ILogger<CoreSQL<TEntity, DBContextRead, DBContextWrite>> logger,
    Lazy<IDapperSQLDBContext> dapperDbContext,
    Lazy<IDbContextFactory<DBContextRead>> contextRead,
    Lazy<IDbContextFactory<DBContextWrite>> contextWrite,
    ResiliencePipeline pipelineRead, ResiliencePipeline pipelineWrite)
```

**Field & const**

| Thanh vien | Kieu | Ghi chu |
|---|---|---|
| `_logger` | `ILogger<CoreSQL<TEntity, DBContextRead, DBContextWrite>>` | `private readonly` (`CoreSQL.cs:21`) |
| `_pipelineRead` | `ResiliencePipeline` | `private readonly` (`CoreSQL.cs:23`) |
| `_pipelineWrite` | `ResiliencePipeline` | `private readonly` (`CoreSQL.cs:25`) |
| `IsDeleted` | `const string` = `"IsDeleted"` | `private const` (`CoreSQL.cs:27`) — ten property soft delete, **hardcode** |
| `_dapperDbContext` | `Lazy<IDapperSQLDBContext>` | `private readonly` (`CoreSQL.cs:29`) |
| `_dbContextRead` | `Lazy<IDbContextFactory<DBContextRead>>` | `private readonly` (`CoreSQL.cs:31`) |
| `_dbContextWrite` | `Lazy<IDbContextFactory<DBContextWrite>>` | `private readonly` (`CoreSQL.cs:33`) |

> [!WARNING]
> Constructor **khong kiem tra null** cho bat ky tham so nao (`CoreSQL.cs:42-52` chi gan truc tiep). Neu DI truyen `null` cho `pipelineRead`/`pipelineWrite`, loi `NullReferenceException` se chi xuat hien tai lan goi method dau tien, khong phai tai thoi diem khoi tao. Tat ca field deu `private` nen **lop con khong truy cap duoc** `_logger`, `_dapperDbContext`, `_dbContextRead`, `_dbContextWrite`, `_pipelineRead`, `_pipelineWrite` — lop con chi co the `override` cac method `virtual`.

---

## 2. Chi tiet API

### 2.1 FindOneWithScriptAsync&lt;TDto&gt;

**Signature** (`CoreSQL.cs:66-71`)

```csharp
public virtual async Task<TDto> FindOneWithScriptAsync<TDto>(
    string scriptSQLQuery,
    DynamicParameters parameters,
    int commandTimeout = 30,
    CommandType commandType = CommandType.Text,
    CancellationToken cancellationToken = default)
```

**Muc dich** — Thuc thi mot script SQL (hoac stored procedure) qua Dapper va tra ve **dong dau tien** cua tap ket qua, map sang `TDto`. Khi `commandType` la `CommandType.Text`, than ham **noi them** `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;` vao truoc script (`CoreSQL.cs:82-90`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `scriptSQLQuery` | `string` | Co | `string.IsNullOrWhiteSpace` -> log `FailLogic` + `return default` (`CoreSQL.cs:75-80`). **Khong** kiem tra noi dung, khong sanitize | — |
| `parameters` | `DynamicParameters` | Khong (theo code) | **Khong co null-check.** Truyen `null` xuong Dapper la hop le (Dapper coi nhu khong co tham so) | — (khong co default, caller phai truyen) |
| `commandTimeout` | `int` | Khong | **Khong validate.** Gia tri am/0 duoc truyen thang xuong `CommandDefinition` | `30` |
| `commandType` | `CommandType` | Khong | Dung trong `switch` tai `CoreSQL.cs:82`: `CommandType.Text` -> noi prefix; moi gia tri khac -> giu nguyen script | `CommandType.Text` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` o dong dau (`CoreSQL.cs:73`) | `default` |

**Output** — `Task<TDto>`.
- `scriptSQLQuery` rong/whitespace -> `default(TDto)` (`null` voi reference type, `0`/`false` voi value type) va **da ghi log** `FailLogic`.
- Query chay duoc nhung khong co dong nao -> `default(TDto)` (do `QueryFirstOrDefaultAsync` tai `Dapper/DapperSQLDBContext.cs:80`), **khong co log**.
- Co dong -> instance `TDto` do Dapper map.
- Loi SQL -> **khong tra ve gi**, exception duoc nem ra ngoai.

-> Caller **khong the phan biet** "script rong", "khong co du lieu", va "gia tri that bang default" chi qua gia tri tra ve.

**Dieu kien xu ly** (theo thu tu thuc thi)
1. `cancellationToken.ThrowIfCancellationRequested()` — `CoreSQL.cs:73`.
2. Guard `string.IsNullOrWhiteSpace(scriptSQLQuery)` -> log + `return default` — `CoreSQL.cs:75-80`.
3. `switch (commandType)`: `CommandType.Text` -> `sqlQuery` = `"SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;" + scriptSQLQuery` (interpolated string); mac dinh -> `sqlQuery = scriptSQLQuery` — `CoreSQL.cs:82-90`.
4. `_pipelineRead.ExecuteAsync(...)` goi `_dapperDbContext.Value.GetOne<TDto>(...)` — `CoreSQL.cs:92-102`.

**Side effect** — Khoi tao mot `SqlConnection` **moi** cho moi lan goi (`Dapper/DapperSQLDBContext.cs:77-78`); `ConfigurationHelpers.CreateConnection` chi `new SqlConnection(...)` va **khong tu mo** (`Dapper/Helpers/ConfigurationHelpers.cs:16-19`) — Dapper mo khi thuc thi, `await using` dispose ngay sau do. Ghi log khi guard chan. Dat isolation level `READ UNCOMMITTED` tren session cua connection do. Khong mutate tham so dau vao. Khong ghi DB (nhung xem "Gioi han").

**Error handling** — **Khong co try/catch.** Moi exception (`SqlException`, `TimeoutException`, `OperationCanceledException`, loi map Dapper) deu noi len caller. Truoc khi noi len, Polly `_pipelineRead` co the retry / mo circuit breaker tuy cau hinh pipeline duoc inject. Exception **khong duoc ghi log** o lop nay.

**Khi nao NEN dung** — Truy van doc phuc tap (join nhieu bang, CTE, window function) ma EF Core sinh SQL khong toi uu; goi stored procedure tra ve 1 dong (dat `commandType = CommandType.StoredProcedure`); can doc "ban" (dirty read) de tranh block tren bang nong.

**Khi nao KHONG dung** — Khi can doc nhat quan (isolation level bi ha xuong `READ UNCOMMITTED`, co the doc du lieu chua commit). Khi script duoc ghep tu input nguoi dung (xem "Gioi han"). Khi can biet chinh xac "khong co dong nao" so voi "gia tri default". Khi can chay trong transaction do caller mo — method nay luon mo connection rieng, **khong** tham gia transaction cua caller.

**Gioi han**
- **Nguy co SQL injection**: `scriptSQLQuery` chi bi kiem tra rong, khong kiem tra noi dung. Neu caller noi chuoi tu input ben ngoai vao `scriptSQLQuery`, cau lenh doc hai se duoc thuc thi nguyen van. `DynamicParameters` chi an toan cho phan **gia tri** duoc tham so hoa, khong bao ve phan chuoi cau lenh.
- Vi `sqlQuery` duoc ghep bang interpolated string (`CoreSQL.cs:84-88`), method **cho phep thuc thi moi cau lenh** ke ca `INSERT`/`UPDATE`/`DELETE`/DDL — ten method (`FindOne...`) va guard deu khong ngan dieu do. Day la method "doc" chi theo quy uoc dat ten.
- `READ UNCOMMITTED` duoc ap dat **cung** cho `CommandType.Text`, caller khong co tham so nao de tat.
- Khong co gioi han `TOP`/`ROWCOUNT`; script tu chiu trach nhiem.
- Tham so lambda cua Polly duoc dat ten `cancellationToken` (`CoreSQL.cs:93`) trung ten tham so method — token thuc su truyen xuong Dapper la token do Polly cap, khong phai truc tiep tham so method.
- `TDto` khong co rang buoc generic -> trinh bien dich khong dam bao Dapper map duoc.

---

### 2.2 FindAllWithScriptAsync&lt;TDto&gt;

**Signature** (`CoreSQL.cs:116-121`)

```csharp
public virtual async Task<IEnumerable<TDto>> FindAllWithScriptAsync<TDto>(
    string scriptSQLQuery,
    DynamicParameters parameters,
    int commandTimeout = 30,
    CommandType commandType = CommandType.Text,
    CancellationToken cancellationToken = default)
```

**Muc dich** — Thuc thi script SQL qua Dapper va tra ve **toan bo** tap ket qua map sang `IEnumerable<TDto>`. Cung co che noi prefix `READ UNCOMMITTED` nhu 2.1.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `scriptSQLQuery` | `string` | Co | `string.IsNullOrWhiteSpace` -> log `FailLogic` + **`return null`** (`CoreSQL.cs:125-130`) | — |
| `parameters` | `DynamicParameters` | Khong (theo code) | Khong co null-check | — |
| `commandTimeout` | `int` | Khong | Khong validate | `30` |
| `commandType` | `CommandType` | Khong | `switch` nhu 2.1 (`CoreSQL.cs:132-140`) | `CommandType.Text` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`CoreSQL.cs:123`) | `default` |

**Output** — `Task<IEnumerable<TDto>>`.
- `scriptSQLQuery` rong/whitespace -> **`null`** (`CoreSQL.cs:129`). **Khong phai** collection rong.
- Query chay duoc nhung 0 dong -> `IEnumerable<TDto>` **rong** (Dapper `QueryAsync` tra collection rong — `Dapper/DapperSQLDBContext.cs:116`).
- Co dong -> collection da buffer day du (Dapper `QueryAsync` mac dinh `buffered = true`).
- Loi SQL -> exception noi len.

> [!WARNING]
> Day la **diem khong nhat quan quan trong**: `FindAllWithScriptAsync` tra `null` khi guard chan, trong khi `FindAllAsync`, `FindAllAsync<TDto>`, `FindAllSortDeletedAsync`, `FindAllSortDeletedAsync<TDto>` deu tra `[]` (collection rong) khi khong co du lieu (`CoreSQL.cs:510`, `:548`, `:583`, `:617`). Caller `foreach` truc tiep tren ket qua cua `FindAllWithScriptAsync` se gap `NullReferenceException`. Ngoai ra `FindOneWithScriptAsync` va `FindOneWithScalarScriptAsync` trong cung nhom lai tra `default` chu khong phai `null` tuong minh (voi `TDto` la value type, `default` = null).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` — `CoreSQL.cs:123`.
2. Guard rong -> log + `return null` — `CoreSQL.cs:125-130`.
3. `switch (commandType)` noi prefix `READ UNCOMMITTED` cho `CommandType.Text` — `CoreSQL.cs:132-140`.
4. `_pipelineRead.ExecuteAsync` -> `_dapperDbContext.Value.GetAll<TDto>(...)` — `CoreSQL.cs:142-152`.

**Side effect** — Mo/dispose mot `SqlConnection` rieng. Ghi log khi guard chan. Dat `READ UNCOMMITTED`. Khong mutate dau vao.

**Error handling** — Khong co try/catch; exception noi len caller sau khi Polly `_pipelineRead` xu ly retry/CB. Khong log exception.

**Khi nao NEN dung** — Doc danh sach bang SQL toi uu tay, bao cao, goi stored procedure tra nhieu dong.

**Khi nao KHONG dung** — Khi caller khong xu ly duoc gia tri `null` tra ve. Khi can phan trang tren server (method khong co tham so paging — phai tu viet `OFFSET/FETCH` trong script). Khi tap ket qua rat lon (toan bo duoc buffer vao bo nho). Khi can doc trong transaction cua caller.

**Gioi han**
- **Nguy co SQL injection** giong 2.1: `scriptSQLQuery` khong duoc validate noi dung.
- Tra `null` thay vi `[]` -> bay `NullReferenceException`.
- Buffer toan bo ket qua, khong co streaming, khong co limit dong.
- `READ UNCOMMITTED` hardcode cho `CommandType.Text`.
- Khong tham gia transaction/connection cua caller.

---

### 2.3 FindOneWithScalarScriptAsync&lt;TDto&gt;

**Signature** (`CoreSQL.cs:166-171`)

```csharp
public virtual async Task<TDto> FindOneWithScalarScriptAsync<TDto>(
    string scriptSQLQuery,
    DynamicParameters parameters,
    int commandTimeout = 30,
    CommandType commandType = CommandType.Text,
    CancellationToken cancellationToken = default)
```

**Muc dich** — Thuc thi script SQL va lay ve **mot gia tri don** (scalar) qua `ExecuteScalarAsync` (`Dapper/DapperSQLDBContext.cs:153`), thuong dung cho `COUNT`, `SUM`, `MAX`, hoac lay 1 cot cua 1 dong.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `scriptSQLQuery` | `string` | Co | `string.IsNullOrWhiteSpace` -> log + `return default` (`CoreSQL.cs:175-180`) | — |
| `parameters` | `DynamicParameters` | Khong (theo code) | Khong co null-check | — |
| `commandTimeout` | `int` | Khong | Khong validate | `30` |
| `commandType` | `CommandType` | Khong | `switch` noi prefix cho `CommandType.Text` (`CoreSQL.cs:182-190`) | `CommandType.Text` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`CoreSQL.cs:173`) | `default` |

**Output** — `Task<TDto>`.
- Guard chan -> `default(TDto)`.
- Khong co dong nao / cot tra ve `NULL` -> `default(TDto)` (hanh vi cua `ExecuteScalarAsync`).
- Co gia tri -> gia tri da convert sang `TDto`.
- Loi SQL hoac khong convert duoc kieu -> exception noi len.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` — `CoreSQL.cs:173`.
2. Guard rong -> log + `return default` — `CoreSQL.cs:175-180`.
3. `switch (commandType)` — `CoreSQL.cs:182-190`.
4. `_pipelineRead.ExecuteAsync` -> `_dapperDbContext.Value.GetOneExecute<TDto>(...)` — `CoreSQL.cs:192-202`.

**Side effect** — Mo/dispose `SqlConnection` rieng; ghi log khi guard chan; dat `READ UNCOMMITTED`. Khong mutate dau vao.

**Error handling** — Khong co try/catch; exception noi len caller. Khong log exception.

**Khi nao NEN dung** — Lay `COUNT(*)`, `EXISTS`-kieu-`SELECT 1`, `SUM`, hoac mot `id`/`code` duy nhat.

**Khi nao KHONG dung** — Khi can phan biet "`NULL` trong DB" voi "khong co dong" (ca hai deu tra `default`). Khi `TDto` la struct khong nullable ma `default` la gia tri nghiep vu co y nghia (vi du `int` `0`). Khi script tra nhieu cot/nhieu dong can dung du (chi cot dau cua dong dau duoc lay).

**Gioi han**
- **Nguy co SQL injection** giong 2.1 va 2.2.
- `default` bi dung cho ca 3 tinh huong khac nhau (guard, khong co dong, gia tri `NULL`).
- `READ UNCOMMITTED` hardcode.
- Khong tham gia transaction cua caller.

---

### 2.4 IsExecuteNonQueryAsync

**Signature** (`CoreSQL.cs:217-224`)

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

**Muc dich** — Thuc thi mot cau lenh SQL non-query (`INSERT`/`UPDATE`/`DELETE`/DDL/stored procedure) **tren `DbConnection` va `DbTransaction` do caller cung cap**, tra `true` neu so dong bi anh huong `> 0` (`CoreSQL.cs:235-240`). Day la method ghi raw SQL duy nhat cua class.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `scriptSQLQuery` | `string` | Co | `string.IsNullOrWhiteSpace` -> log `FailLogic` + `return false` (`CoreSQL.cs:228-233`). Khong kiem tra noi dung | — |
| `context` | `DbConnection` | Co (thuc te) | **Khong co null-check.** `null` -> `NullReferenceException` tai `CoreSQL.cs:235` | — |
| `transaction` | `DbTransaction` | Khong (theo Dapper) | **Khong co null-check.** `null` -> Dapper chay ngoai transaction | — |
| `parameters` | `DynamicParameters` | Khong (theo code) | Khong co null-check | — |
| `commandTimeout` | `int` | Khong | Khong validate | `30` |
| `commandType` | `CommandType` | Khong | Truyen thang xuong Dapper. **Khong** co `switch` noi `READ UNCOMMITTED` nhu 2.1–2.3 | `CommandType.Text` |
| `cancellationToken` | `CancellationToken` | Khong | Chi dung o `ThrowIfCancellationRequested()` (`CoreSQL.cs:226`); **khong duoc truyen xuong Dapper** | `default` |

**Output** — `Task<bool>`.
- `scriptSQLQuery` rong/whitespace -> `false`, co log `FailLogic`.
- Thuc thi thanh cong va `rowsAffected > 0` -> `true`.
- Thuc thi thanh cong nhung `rowsAffected == 0` (vi du `UPDATE` khong match dong nao) -> `false`, **khong co log**.
- Loi SQL -> exception noi len (khong tra `false`).

-> `false` mang **hai nghia khac nhau**: "script rong" va "khong co dong nao bi anh huong".

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` — `CoreSQL.cs:226`.
2. Guard `string.IsNullOrWhiteSpace(scriptSQLQuery)` -> log + `return false` — `CoreSQL.cs:228-233`.
3. Goi `context.ExecuteAsync(param, sql, commandType, transaction, commandTimeout)` va so sanh `> 0` — `CoreSQL.cs:235-240`. Extension `ExecuteAsync` den tu `using static Dapper.SqlMapper;` (`CoreSQL.cs:10`).

**Side effect** — **Ghi du lieu vao DB** (bat ky cau lenh nao caller truyen vao). Su dung connection/transaction cua caller -> tham gia vao transaction dang mo cua caller. Ghi log khi guard chan. Khong mutate tham so dau vao. **Khong** mo/dong connection (caller chiu trach nhiem mo connection truoc — Dapper se tu mo neu connection dang `Closed`, nhung khi co `transaction` thi connection buoc phai dang mo).

**Error handling** — Khong co try/catch; moi `SqlException`/`InvalidOperationException`/`NullReferenceException` noi len caller. **Khong co Polly pipeline** -> khong retry, khong circuit breaker. Khong log exception. Khong rollback transaction (caller chiu trach nhiem).

**Khi nao NEN dung** — Can chay DML/DDL raw SQL ben trong mot transaction do `IUnitOfWork` hoac caller quan ly; cac thao tac bulk ma EF Core khong dien dat hieu qua; goi stored procedure ghi du lieu.

**Khi nao KHONG dung** — Khi can biet **so dong** bi anh huong (method chi tra `bool`, thong tin `rowsAffected` bi mat). Khi can huy tac vu giua duong (`cancellationToken` khong toi duoc Dapper). Khi can resilience (khong co retry/CB). Khi can domain event — method nay **bo qua hoan toan** `AuditModel` va co che `OnBeforeSaveChanges`/`DispatchDomainEvents` cua `WriteDbContext.SaveChangesAsync` (khong set `IsDeleted`/`CreatedDate`/`ModifiedDate`, khong publish domain event). Luu y: phan **ghi audit log** thi cac overload EF Core cung khong lam duoc, vi `DetectChangesAudit` luon tra `[]` (`WriteDbContext.cs:356`). Khi script duoc ghep tu input ben ngoai.

**Gioi han**
- **Nguy co SQL injection cao nhat trong class**: day la method ghi, `scriptSQLQuery` khong duoc validate noi dung, va cau lenh duoc truyen nguyen van xuong Dapper.
- `context` va `transaction` **khong duoc null-check** -> `NullReferenceException` thay vi loi nghiep vu ro rang.
- `cancellationToken` bi bo qua o lop Dapper: overload `SqlMapper.ExecuteAsync(IDbConnection, string sql, object param, IDbTransaction transaction, int? commandTimeout, CommandType? commandType)` duoc goi tai `CoreSQL.cs:235-240` **khong co** tham so `CancellationToken`.
- Khong co Polly pipeline (khac toan bo cac method con lai).
- XML doc tai `CoreSQL.cs:210` mo ta `context` la "Đối tượng DbContext để truy cập cơ sở dữ liệu", nhung kieu that la `DbConnection` — mau thuan giua tai lieu va code.
- Thu tu tham so trong XML doc cua `ICoreSQL.cs:69-72` (`scriptSQLQuery`, `parameters`, `context`, `transaction`) khac thu tu tham so that (`scriptSQLQuery`, `context`, `transaction`, `parameters`).
- Bo qua `OnBeforeSaveChanges` va `DispatchDomainEvents` -> thay doi du lieu qua method nay khong duoc gan cac field audit cua `IBaseEntitySQL` va khong phat domain event. (Khong xac dinh duoc tu source code rang co "snapshot audit" nao bi mat: `DetectChangesAudit` trong `WriteDbContext` luon tra `[]` nen duong EF Core cung khong ghi audit log.)

---

### 2.5 FindByIdAsync

**Signature** (`CoreSQL.cs:250-251`)

```csharp
public virtual async Task<TEntity> FindByIdAsync(
    object id, CancellationToken cancellationToken = default)
```

**Muc dich** — Tim entity theo **khoa chinh** bang EF Core `DbSet<TEntity>.FindAsync` tren `DBContextRead`, roi chuyen entity sang trang thai `Detached` khoi change tracker (`CoreSQL.cs:271-274`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `id` | `object` | Co | `id == null` -> log `FailLogic` + `return null` (`CoreSQL.cs:255-260`). **Khong** kiem tra kieu co khop khoa chinh | — |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`CoreSQL.cs:253`) | `default` |

**Output** — `Task<TEntity>`.
- `id` la `null` -> `null`, co log `FailLogic`.
- Khong tim thay -> `null` (tu `FindAsync`), khong co log.
- Tim thay -> instance `TEntity` da `Detached`.
- Kieu `id` khong khop kieu khoa chinh -> exception tu EF Core noi len.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` — `CoreSQL.cs:253`.
2. Guard `id == null` -> log + `return null` — `CoreSQL.cs:255-260`.
3. `_pipelineRead.ExecuteAsync`: tao `DBContextRead` qua factory (`CoreSQL.cs:265-266`), goi `Set<TEntity>().FindAsync(keyValues: [id], ct)` (`CoreSQL.cs:268-269`).
4. `if (entity is not null)` -> `createDbContext.Entry(entity).State = EntityState.Detached` (`CoreSQL.cs:271-274`).
5. `return entity`; context duoc dispose boi `await using` khi callback ket thuc.

**Side effect** — Tao va dispose mot `DBContextRead` cho moi lan goi. Thay doi `EntityState` cua entry trong change tracker cua context tam do (context bi dispose ngay sau nen tac dong khong thoat ra ngoai). Ghi log khi guard chan. Khong ghi DB.

**Error handling** — Khong co try/catch. Exception noi len caller sau khi `_pipelineRead` xu ly. Khong log exception.

**Khi nao NEN dung** — Lay nhanh mot ban ghi khi da biet khoa chinh va **khong** quan tam trang thai soft delete.

**Khi nao KHONG dung** — Khi can loai bo ban ghi da soft delete: method nay **khong** ap filter `IsDeleted`, nen van tra ve ban ghi co `IsDeleted = true`. Khi entity co **composite key**: code truyen `keyValues: [id]` — chi mot gia tri, nen khoa phuc hop se loi. Khi can entity co tracking de cap nhat (entity da bi detach va context da dispose).

**Gioi han**
- `id` kieu `object` -> khong co kiem tra kieu tai compile time; loi chi phat hien luc chay.
- Chi ho tro **khoa chinh mot cot** (`keyValues: [id]` tai `CoreSQL.cs:269`).
- Khong ap filter soft delete.
- `FindAsync` **khong** dung `AsNoTracking` (khac cac method `Find*` khac); code phai detach thu cong.
- Khong co projection sang DTO.

---

### 2.6 FindOneSortDeletedAsync&lt;TDto&gt; (filters, isDeleted, cancellationToken)

**Signature** (`CoreSQL.cs:288-290`)

```csharp
public virtual async Task<TDto> FindOneSortDeletedAsync<TDto>(
    Expression<Func<TEntity, bool>>[] filters,
    bool isDeleted = false, CancellationToken cancellationToken = default)
```

**Muc dich** — Lay entity **dau tien** thoa man dieu kien `IsDeleted == isDeleted` **va** toan bo `filters`, sau do map sang `TDto` bang reflection (`ProjectTo`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filters` | `Expression<Func<TEntity, bool>>[]` | Khong | `filters is not null && filters.Length > 0` moi ap dung (`CoreSQL.cs:305-308`). `null` hoac mang rong -> chi con dieu kien `IsDeleted`. **Khong** kiem tra phan tu `null` ben trong mang | — |
| `isDeleted` | `bool` | Khong | Khong validate; dung truc tiep trong `EF.Property<bool>(x, IsDeleted) == isDeleted` | `false` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`CoreSQL.cs:292`) | `default` |

**Output** — `Task<TDto>`.
- Khong tim thay -> `default(TDto)` (`CoreSQL.cs:314`: `result is null ? default : ...`).
- Tim thay -> `TDto` moi duoc tao boi `Activator.CreateInstance` va copy property cung ten.
- Loi (vi du `TEntity` khong co property `IsDeleted`) -> exception noi len.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` — `CoreSQL.cs:292`.
2. `_pipelineRead.ExecuteAsync`: tao `DBContextRead` (`CoreSQL.cs:298-299`).
3. `query = createDbContext.Set<TEntity>()` (`CoreSQL.cs:301`).
4. **Luon** them `query.Where(x => EF.Property<bool>(x, "IsDeleted") == isDeleted)` (`CoreSQL.cs:303`) — dieu kien soft delete duoc ap **truoc** cac filter.
5. Neu `filters` khong null va co phan tu -> `filters.Aggregate(query, (current, filter) => current.Where(filter))` (`CoreSQL.cs:305-308`) — moi filter la mot `Where` chong them -> **AND**.
6. `query.AsNoTracking().FirstOrDefaultAsync(ct)` (`CoreSQL.cs:310`) — **khong co `OrderBy`**.
7. Sau khi ra khoi pipeline: `result is null ? default : result.ProjectTo<TEntity, TDto>()` (`CoreSQL.cs:314`).

**Side effect** — Tao/dispose mot `DBContextRead`. Khong ghi DB, khong ghi log (method nay **khong** co `FailLogic`), khong mutate dau vao.

**Error handling** — Khong co try/catch. `ProjectTo` ben trong co `try/catch` rieng cho tung property va ghi log ra console qua `CommonBaseConstant.ConfigLoggerExceptionByConsole` (`Extensions/ProjectToExtensions.cs:56-59`) -> **loi map tung property bi nuot lang le**, property do giu gia tri mac dinh. Cac exception khac noi len caller.

**Khi nao NEN dung** — Lay 1 ban ghi chua bi xoa mem theo dieu kien nghiep vu va tra ve DTO cho tang tren, khi `TEntity` chac chan co property `bool IsDeleted`.

**Khi nao KHONG dung** — Khi `TEntity` khong co property `bool IsDeleted` (query se loi luc EF translate). Khi co nhieu ban ghi thoa dieu kien va can ban ghi **cu the** (khong co `OrderBy` -> thu tu do SQL Server quyet dinh, khong xac dinh) — dung overload 2.7 co `sorting`. Khi `TDto` khong co constructor khong tham so (xem "Gioi han"). Khi can hieu nang cao tren tap lon (mapping bang reflection).

**Gioi han**
- Ten property soft delete **hardcode** `"IsDeleted"` (`CoreSQL.cs:27`); khong co tham so cau hinh.
- `EF.Property<bool>` yeu cau property kieu `bool` **khong nullable** trong model EF Core (CLR property hoac shadow property deu duoc); `bool?` se khong khop.
- `ProjectTo` duoc goi **ngoai** `_pipelineRead` (`CoreSQL.cs:314`) -> loi mapping khong bi Polly coi la failure. Overload 2.7 lam nguoc lai (`CoreSQL.cs:368`, nam trong pipeline) — hai overload cung ten **khong nhat quan** o diem nay.
- **Khong co `OrderBy`** -> `FirstOrDefaultAsync` tra ban ghi khong xac dinh khi co nhieu ket qua. Ten method co chu "Sort" nhung than ham **khong sap xep** — "Sort" o day thuc chat chi mang nghia loc theo trang thai deleted.
- `ProjectTo` dung `Activator.CreateInstance(typeof(TDto))` (`Extensions/ProjectToExtensions.cs:29`) -> `TDto` phai co constructor public khong tham so, neu khong se nem exception luc chay.
- `ProjectTo` chay reflection moi lan goi, khong cache metadata -> chi phi CPU dang ke.
- Chi map property **cung ten**; property khac ten hoac nested object khong duoc map.
- Khong co tham so nao cho phep truyen `null` de "bo qua" dieu kien `IsDeleted` — luon phai chon `true` hoac `false`.

---

### 2.7 FindOneSortDeletedAsync&lt;TDto&gt; (filters, sorting, isDeleted, selector, cancellationToken)

**Signature** (`CoreSQL.cs:330-335`)

```csharp
public virtual async Task<TDto> FindOneSortDeletedAsync<TDto>(
    Expression<Func<TEntity, bool>>[] filters,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sorting,
    bool isDeleted = false,
    Expression<Func<TEntity, TDto>> selector = null,
    CancellationToken cancellationToken = default)
```

> [!NOTE]
> Overload nay **khong duoc khai bao trong `ICoreSQL`** — chi ton tai tren class. Ly do duoc ghi trong XML doc (`CoreSQL.cs:319-321`): `sorting` la delegate da bien dich nen khong the dich tham so kieu nhu `ReplaceParameters<TFrom,TTo>` de tai su dung cho lop `CoreSQL<TEntityFrom, TEntityTo, ...>`. He qua thuc te: code phu thuoc vao `ICoreSQL` (mock, DI theo interface) **khong goi duoc** overload nay.

**Muc dich** — Giong 2.6 nhung them hai kha nang: (a) **sap xep** query truoc khi lay ban ghi dau tien, (b) **chieu (projection) server-side** sang `TDto` qua `selector` thay vi map reflection.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filters` | `Expression<Func<TEntity, bool>>[]` | Khong | `filters is not null && filters.Length > 0` moi ap dung (`CoreSQL.cs:349-352`) | — |
| `sorting` | `Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>` | Khong | `sorting is not null` -> `query = sorting(query)` (`CoreSQL.cs:354-357`). Khong co default value -> caller **phai truyen** (co the truyen `null`) | — |
| `isDeleted` | `bool` | Khong | Dung trong `EF.Property<bool>` (`CoreSQL.cs:347`) | `false` |
| `selector` | `Expression<Func<TEntity, TDto>>` | Khong | `selector is not null` -> `readOnlyQuery.Select(selector).FirstOrDefaultAsync(ct)` (`CoreSQL.cs:361-364`) | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`CoreSQL.cs:337`) | `default` |

**Output** — `Task<TDto>`.
- Co `selector`: ket qua cua `Select(selector).FirstOrDefaultAsync(ct)` -> `default(TDto)` neu khong co dong (`CoreSQL.cs:363`).
- Khong co `selector`: lay `TEntity` roi `result is null ? default : result.ProjectTo<TEntity, TDto>()` (`CoreSQL.cs:366-368`).
- Loi -> exception noi len.

**Dieu kien xu ly** (theo thu tu)
1. `ThrowIfCancellationRequested()` — `CoreSQL.cs:337`.
2. Tao `DBContextRead` trong `_pipelineRead.ExecuteAsync` — `CoreSQL.cs:342-343`.
3. `query = Set<TEntity>()` — `CoreSQL.cs:345`.
4. Ap `Where(EF.Property<bool>(x, "IsDeleted") == isDeleted)` — `CoreSQL.cs:347`.
5. Ap `filters` bang `Aggregate` neu co — `CoreSQL.cs:349-352`.
6. Ap `sorting(query)` neu `sorting is not null` — `CoreSQL.cs:354-357`.
7. `readOnlyQuery = query.AsNoTracking()` — `CoreSQL.cs:359`.
8. Nhanh `selector is not null` -> `Select(selector).FirstOrDefaultAsync(ct)` va **ket thuc** — `CoreSQL.cs:361-364`.
9. Nhanh con lai -> `FirstOrDefaultAsync(ct)` roi `ProjectTo` — `CoreSQL.cs:366-368`.

**Side effect** — Tao/dispose mot `DBContextRead`. Khong ghi DB, khong ghi log, khong mutate dau vao. Luu y `sorting` la delegate cua caller duoc **thuc thi** tren `IQueryable` -> neu delegate do co side effect thi side effect do xay ra.

**Error handling** — Khong co try/catch. Neu `selector` chua bieu thuc EF Core khong dich duoc -> `InvalidOperationException` noi len. Neu `sorting` tra `null` -> `query` bi gan `null` va `query.AsNoTracking()` o `CoreSQL.cs:359` se nem `NullReferenceException`.

**Khi nao NEN dung** — Can "ban ghi moi nhat" / "ban ghi co gia tri lon nhat" (`sorting`), hoac can chi lay vai cot tu DB (`selector`) de giam bang thong va bo han chi phi reflection.

**Khi nao KHONG dung** — Khi code goi qua abstraction `ICoreSQL` (overload khong co trong interface). Khi can map toan bo property ma khong muon viet `selector` — dung 2.6. Khi `TEntity` khong co `bool IsDeleted`.

**Gioi han**
- Khong co trong `ICoreSQL` -> kho mock/unit test qua interface, va lop `CoreSQL<TEntityFrom, TEntityTo, ...>` khong co tuong duong.
- `sorting` la `Func<>` (delegate da bien dich), khong phai expression tree -> duoc ap truc tiep tren `IQueryable` nen van dich duoc sang SQL, nhung **khong kiem tra duoc** noi dung va khong the dich tham so sang entity khac.
- Khong kiem tra `sorting` tra `null`.
- `IsDeleted` van hardcode.
- Khi khong truyen `selector`, van dung `ProjectTo` reflection voi day du gioi han neu o 2.6.
- Khac 2.6, `ProjectTo` o day nam **ben trong** `_pipelineRead.ExecuteAsync` (`CoreSQL.cs:368`) -> neu `ProjectTo` nem exception (vi du `TDto` khong co ctor public khong tham so), Polly coi do la failure cua pipeline va **co the retry lai ca cau query**, dong thoi tinh vao ti le mo circuit breaker.

---

### 2.8 FindOneSortDeletedAsync (tra ve TEntity)

**Signature** (`CoreSQL.cs:381-384`)

```csharp
public virtual async Task<TEntity> FindOneSortDeletedAsync(
    Expression<Func<TEntity, bool>>[] filters,
    bool isDeleted = false,
    CancellationToken cancellationToken = default)
```

**Muc dich** — Giong 2.6 nhung tra ve **entity goc** `TEntity`, khong map DTO.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filters` | `Expression<Func<TEntity, bool>>[]` | Khong | `filters is not null && filters.Length > 0` (`CoreSQL.cs:398-401`) | — |
| `isDeleted` | `bool` | Khong | `EF.Property<bool>(x, IsDeleted) == isDeleted` (`CoreSQL.cs:396`) | `false` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`CoreSQL.cs:386`) | `default` |

**Output** — `Task<TEntity>`; `null` neu khong tim thay (`FirstOrDefaultAsync` tai `CoreSQL.cs:403`). Khong co buoc `ProjectTo` nen khong co chuyen doi nao.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` — `CoreSQL.cs:386`.
2. `_pipelineRead.ExecuteAsync` -> tao `DBContextRead` (`CoreSQL.cs:391-392`).
3. `Where(EF.Property<bool>(x, "IsDeleted") == isDeleted)` — `CoreSQL.cs:396`.
4. Ap `filters` neu co — `CoreSQL.cs:398-401`.
5. `AsNoTracking().FirstOrDefaultAsync(ct)` — `CoreSQL.cs:403`. **Khong co `OrderBy`**.

**Side effect** — Tao/dispose `DBContextRead`. Khong ghi DB, khong log, khong mutate dau vao.

**Error handling** — Khong co try/catch; exception noi len caller.

**Khi nao NEN dung** — Can entity day du (vi du de dua sang `UpdateAsync`), co xet trang thai soft delete.

**Khi nao KHONG dung** — Khi dinh dung entity tra ve de `UpdateAsync` **tren cung context** — entity nay den tu `DBContextRead` va o trang thai `AsNoTracking`, context lai da dispose. Khi `TEntity` khong co `bool IsDeleted`. Khi can ban ghi xac dinh trong nhieu ket qua (khong co `OrderBy`).

**Gioi han** — Giong 2.6 ve hardcode `IsDeleted`, thieu `OrderBy`, khong co paging. Entity tra ve la **detached / no-tracking** -> moi sua doi tren no khong tu dong duoc EF theo doi.

---

### 2.9 FindOneAsync&lt;TDto&gt;

**Signature** (`CoreSQL.cs:416-418`)

```csharp
public virtual async Task<TDto> FindOneAsync<TDto>(
    Expression<Func<TEntity, bool>>[] filters,
    CancellationToken cancellationToken = default)
```

**Muc dich** — Lay entity dau tien thoa `filters` (**khong** ap dieu kien `IsDeleted`) va map sang `TDto` bang `ProjectTo`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filters` | `Expression<Func<TEntity, bool>>[]` | Khong | `filters is not null && filters.Length > 0` (`CoreSQL.cs:431-434`); neu khong co -> query **khong co `Where` nao** | — |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`CoreSQL.cs:420`) | `default` |

**Output** — `Task<TDto>`; `default(TDto)` neu khong tim thay (`CoreSQL.cs:440`), nguoc lai DTO map bang reflection.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` — `CoreSQL.cs:420`.
2. `_pipelineRead.ExecuteAsync` -> tao `DBContextRead` (`CoreSQL.cs:426-427`).
3. `query = Set<TEntity>()` (`CoreSQL.cs:429`); ap `filters` neu co (`CoreSQL.cs:431-434`).
4. `AsNoTracking().FirstOrDefaultAsync(ct)` (`CoreSQL.cs:436`).
5. `result is null ? default : result.ProjectTo<TEntity, TDto>()` (`CoreSQL.cs:440`).

**Side effect** — Tao/dispose `DBContextRead`. Khong ghi DB, khong log, khong mutate dau vao.

**Error handling** — Khong co try/catch; exception noi len. Loi map tung property bi `ProjectTo` nuot va log ra console.

**Khi nao NEN dung** — Doc 1 ban ghi cho entity **khong co** cot soft delete, hoac khi can lay ca ban ghi da bi soft delete.

**Khi nao KHONG dung** — Khi entity co soft delete va nghiep vu **khong** duoc thay ban ghi da xoa: method nay khong tu loai chung ra, phai tu them filter `x => x.IsDeleted == false`. Khi `filters` rong/null ma bang lon (query quet toan bang lay dong dau, khong `OrderBy`).

**Gioi han** — Khong `OrderBy` -> ket qua khong xac dinh khi nhieu dong khop. Khong paging. `ProjectTo` reflection (yeu cau `TDto` co ctor khong tham so, khong cache metadata). Khong co guard log khi `filters` null (khac nhom raw SQL von co `FailLogic`).

---

### 2.10 FindOneAsync (tra ve TEntity)

**Signature** (`CoreSQL.cs:450-452`)

```csharp
public virtual async Task<TEntity> FindOneAsync(
    Expression<Func<TEntity, bool>>[] filters,
    CancellationToken cancellationToken = default)
```

**Muc dich** — Giong 2.9 nhung tra ve `TEntity` goc, khong map DTO, khong ap `IsDeleted`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filters` | `Expression<Func<TEntity, bool>>[]` | Khong | `filters is not null && filters.Length > 0` (`CoreSQL.cs:464-467`) | — |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`CoreSQL.cs:454`) | `default` |

**Output** — `Task<TEntity>`; `null` neu khong tim thay (`CoreSQL.cs:469`).

**Dieu kien xu ly** — `ThrowIfCancellationRequested()` (`:454`) -> `_pipelineRead.ExecuteAsync` (`:456`) -> tao context (`:459-460`) -> `Set<TEntity>()` (`:462`) -> ap `filters` neu co (`:464-467`) -> `AsNoTracking().FirstOrDefaultAsync(ct)` (`:469`).

**Side effect** — Tao/dispose `DBContextRead`. Khong ghi DB, khong log, khong mutate dau vao.

**Error handling** — Khong co try/catch; exception noi len.

**Khi nao NEN dung** — Can entity day du cho entity khong co soft delete, hoac can ca ban ghi da xoa mem.

**Khi nao KHONG dung** — Khi can tu dong loai ban ghi soft delete (dung 2.8). Khi can entity duoc EF theo doi de update (ket qua la no-tracking, context da dispose).

**Gioi han** — Khong `OrderBy`, khong paging, khong projection, entity tra ve detached.

---

### 2.11 FindAllSortDeletedAsync&lt;TDto&gt;

**Signature** (`CoreSQL.cs:483-486`)

```csharp
public virtual async Task<List<TDto>> FindAllSortDeletedAsync<TDto>(
    Expression<Func<TEntity, bool>>[] filters,
    bool isDeleted = false,
    CancellationToken cancellationToken = default)
```

**Muc dich** — Lay **toan bo** entity thoa `IsDeleted == isDeleted` va `filters`, map sang `List<TDto>`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filters` | `Expression<Func<TEntity, bool>>[]` | Khong | `filters is not null && filters.Length > 0` (`CoreSQL.cs:501-504`) | — |
| `isDeleted` | `bool` | Khong | `EF.Property<bool>(x, IsDeleted) == isDeleted` (`CoreSQL.cs:499`) | `false` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`CoreSQL.cs:488`) | `default` |

**Output** — `Task<List<TDto>>`.
- Khong co ban ghi -> `[]` (`CoreSQL.cs:510`: `result.IsNullOrEmpty() ? [] : ...`). **Khong bao gio tra `null`.**
- Co ban ghi -> `List<TDto>` tu overload `ProjectTo<TEntity, TDto>(this List<TEntity>)` (`Extensions/ProjectToExtensions.cs:76`).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` — `CoreSQL.cs:488`.
2. `_pipelineRead.ExecuteAsync` -> tao `DBContextRead` (`CoreSQL.cs:494-495`).
3. `Where(EF.Property<bool>(x, "IsDeleted") == isDeleted)` — `CoreSQL.cs:499`.
4. Ap `filters` neu co — `CoreSQL.cs:501-504`.
5. `AsNoTracking().ToListAsync(ct)` — `CoreSQL.cs:506`.
6. `result.IsNullOrEmpty() ? [] : result.ProjectTo<TEntity, TDto>()` — `CoreSQL.cs:510`.

**Side effect** — Tao/dispose `DBContextRead`. Khong ghi DB, khong log, khong mutate dau vao.

**Error handling** — Khong co try/catch trong `CoreSQL`. `ProjectTo` overload danh sach co **hai lop** try/catch: lop trong (`Extensions/ProjectToExtensions.cs:96-115`) nuot loi cua **tung property** — phan tu van duoc them vao danh sach voi property do o gia tri mac dinh; lop ngoai (`Extensions/ProjectToExtensions.cs:90-123`) nuot loi cua ca **mot phan tu** (ke ca loi `Activator.CreateInstance<TDto>()`) — phan tu do bi **loai khoi danh sach ket qua**. Ca hai chi log ra console, khong nem ra caller.

**Khi nao NEN dung** — Lay danh sach ban ghi con hieu luc (hoac da xoa mem) cho entity co `bool IsDeleted`, tap ket qua nho/vua.

**Khi nao KHONG dung** — Khi bang lon: **khong co `Skip`/`Take`/`TOP`**, toan bo dong khop filter duoc nap vao RAM roi map reflection -> rui ro OOM va CPU cao. Khi can thu tu cu the (khong co `OrderBy`). Khi `TEntity` khong co `bool IsDeleted`.

**Gioi han**
- **Khong co phan trang va khong co gioi han so dong.**
- Khong `OrderBy` -> thu tu tra ve khong xac dinh.
- `IsDeleted` hardcode `"IsDeleted"`, phai la `bool` khong nullable.
- `ProjectTo` overload danh sach chay reflection cho **tung phan tu** (`Extensions/ProjectToExtensions.cs:88-124`) -> chi phi ti le so dong × so property.
- Yeu cau `TDto` co ctor public khong tham so (`Activator.CreateInstance<TDto>()` — `Extensions/ProjectToExtensions.cs:92`). **Khac overload 1 phan tu:** loi goi nay nam **ben trong** khoi `try` cua vong lap (`ProjectToExtensions.cs:90-91`) va exception bi bat tai `:120-123` -> **khong nem ra ngoai**; phan tu do bi **bo qua lang le** va method tra ve danh sach **thieu phan tu** (neu moi phan tu deu loi thi tra ve `[]`, caller khong phan biet duoc voi "khong co du lieu"). Chi log ra console qua `CommonBaseConstant.ConfigLoggerExceptionByConsole`.

---

### 2.12 FindAllSortDeletedAsync (tra ve List&lt;TEntity&gt;)

**Signature** (`CoreSQL.cs:521-524`)

```csharp
public virtual async Task<List<TEntity>> FindAllSortDeletedAsync(
    Expression<Func<TEntity, bool>>[] filters,
    bool isDeleted = false,
    CancellationToken cancellationToken = default)
```

**Muc dich** — Giong 2.11 nhung tra ve `List<TEntity>` goc.

**Input hop le** — Giong 2.11: `filters` duoc ap khi `filters is not null && filters.Length > 0` (`CoreSQL.cs:539-542`); `isDeleted` mac dinh `false` (`CoreSQL.cs:537`); `cancellationToken` duoc kiem tra tai `CoreSQL.cs:526`.

**Output** — `Task<List<TEntity>>`; `[]` neu khong co ban ghi, nguoc lai chinh danh sach tu `ToListAsync` (`CoreSQL.cs:548`: `result.IsNullOrEmpty() ? [] : result`). Khong bao gio `null`.

**Dieu kien xu ly** — `ThrowIfCancellationRequested()` (`:526`) -> `_pipelineRead.ExecuteAsync` (`:529`) -> tao context (`:532-533`) -> `Where(EF.Property<bool>(x, "IsDeleted") == isDeleted)` (`:537`) -> ap `filters` (`:539-542`) -> `AsNoTracking().ToListAsync(ct)` (`:544`) -> chuan hoa rong (`:548`).

**Side effect** — Tao/dispose `DBContextRead`. Khong ghi DB, khong log, khong mutate dau vao.

**Error handling** — Khong co try/catch; exception noi len.

**Khi nao NEN dung** — Can danh sach entity day du (vi du de `UpdateAsync` hang loat) co xet soft delete.

**Khi nao KHONG dung** — Bang lon (khong paging, khong limit). Can thu tu xac dinh. Entity khong co `bool IsDeleted`. Can entity duoc tracking (ket qua la no-tracking tu context da dispose).

**Gioi han** — Khong paging, khong `OrderBy`, `IsDeleted` hardcode, entity tra ve detached.

---

### 2.13 FindAllAsync&lt;TDto&gt;

**Signature** (`CoreSQL.cs:559-561`)

```csharp
public virtual async Task<List<TDto>> FindAllAsync<TDto>(
    Expression<Func<TEntity, bool>>[] filters,
    CancellationToken cancellationToken = default)
```

**Muc dich** — Lay toan bo entity thoa `filters` (**khong** ap `IsDeleted`) va map sang `List<TDto>`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filters` | `Expression<Func<TEntity, bool>>[]` | Khong | `filters is not null && filters.Length > 0` (`CoreSQL.cs:574-577`); neu khong -> **lay toan bo bang** | — |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`CoreSQL.cs:563`) | `default` |

**Output** — `Task<List<TDto>>`; `[]` khi khong co dong, nguoc lai `List<TDto>` map reflection (`CoreSQL.cs:583`).

**Dieu kien xu ly** — `ThrowIfCancellationRequested()` (`:563`) -> `_pipelineRead.ExecuteAsync` (`:566`) -> tao context (`:569-570`) -> `Set<TEntity>()` (`:572`) -> ap `filters` neu co (`:574-577`) -> `AsNoTracking().ToListAsync(ct)` (`:579`) -> `result.IsNullOrEmpty() ? [] : result.ProjectTo<TEntity, TDto>()` (`:583`).

**Side effect** — Tao/dispose `DBContextRead`. Khong ghi DB, khong log, khong mutate dau vao.

**Error handling** — Khong co try/catch; loi map property bi `ProjectTo` nuot + log console.

**Khi nao NEN dung** — Doc danh sach cho entity khong co soft delete, hoac co y lay ca ban ghi da xoa mem; bang nho (danh muc, lookup table).

**Khi nao KHONG dung** — **Khi goi voi `filters` la `null` hoac mang rong tren bang lon: query tro thanh `SELECT * FROM table` khong gioi han.** Khi entity co soft delete va nghiep vu khong duoc thay ban ghi da xoa. Khi can thu tu hoac phan trang.

**Gioi han** — Khong paging, khong `OrderBy`, khong limit dong, `ProjectTo` reflection voi cac yeu cau ve `TDto` nhu da neu o 2.11 (bao gom viec phan tu map loi bi **loai khoi danh sach** thay vi nem exception).

---

### 2.14 FindAllAsync (tra ve List&lt;TEntity&gt;)

**Signature** (`CoreSQL.cs:593-595`)

```csharp
public virtual async Task<List<TEntity>> FindAllAsync(
    Expression<Func<TEntity, bool>>[] filters,
    CancellationToken cancellationToken = default)
```

**Muc dich** — Giong 2.13 nhung tra ve `List<TEntity>` goc.

**Input hop le** — `filters` ap khi `filters is not null && filters.Length > 0` (`CoreSQL.cs:608-611`); `cancellationToken` kiem tra tai `CoreSQL.cs:597`.

**Output** — `Task<List<TEntity>>`; `[]` khi rong, nguoc lai danh sach entity (`CoreSQL.cs:617`). Khong bao gio `null`.

**Dieu kien xu ly** — `ThrowIfCancellationRequested()` (`:597`) -> `_pipelineRead.ExecuteAsync` (`:600`) -> tao context (`:603-604`) -> `Set<TEntity>()` (`:606`) -> ap `filters` (`:608-611`) -> `AsNoTracking().ToListAsync(ct)` (`:613`) -> chuan hoa rong (`:617`).

**Side effect** — Tao/dispose `DBContextRead`. Khong ghi DB, khong log, khong mutate dau vao.

**Error handling** — Khong co try/catch; exception noi len.

**Khi nao NEN dung** — Can danh sach entity day du cho bang nho, khong quan tam soft delete.

**Khi nao KHONG dung** — `filters` rong tren bang lon (quet toan bang). Can paging/thu tu. Can entity tracking.

**Gioi han** — Khong paging, khong `OrderBy`, khong limit, entity detached.

---

### 2.15 CreateAsync(TEntity, AuditModel, CancellationToken)

**Signature** (`CoreSQL.cs:628-631`)

```csharp
public virtual async Task<int> CreateAsync(
    TEntity entity,
    AuditModel auditLog = null,
    CancellationToken cancellationToken = default)
```

**Muc dich** — Them **mot** entity: tu tao `DBContextWrite` tu factory, `AddAsync`, roi goi `SaveChangesAsync(audit: auditLog, ...)` ben trong `_pipelineWrite`. Tra ve **so ban ghi bi anh huong** (`int`), **khong phai `bool`**.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entity` | `TEntity` | Co | `entity is null` -> log `FailLogic` + `return 0` (`CoreSQL.cs:635-640`). Khong validate noi dung entity | — |
| `auditLog` | `AuditModel` | Khong | Khong validate; truyen thang xuong `SaveChangesAsync`. `null` -> `WriteDbContext` bo qua `DetectChangesAudit` (`WriteDbContext.cs:81-84`) | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`CoreSQL.cs:633`) | `default` |

**Output** — `Task<int>`.
- `entity` la `null` -> `0`, co log `FailLogic`.
- Ghi thanh cong -> so ban ghi `SaveChangesAsync` bao (thuong `1`, nhung co the lon hon neu entity co navigation keo theo ban ghi lien quan).
- `SaveChangesAsync` tra `0` -> `0` (khong co log).
- Loi DB (`DbUpdateException`, `SqlException`) -> exception noi len, **khong** tra `0`.

-> `0` mang hai nghia: "entity null" va "khong co ban ghi nao duoc ghi".

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` — `CoreSQL.cs:633`.
2. Guard `entity is null` -> log + `return 0` — `CoreSQL.cs:635-640`.
3. `int result = 0;` — `CoreSQL.cs:642`.
4. `await using DBContextWrite createDbContext = await _dbContextWrite.Value.CreateDbContextAsync(ct)` — `CoreSQL.cs:644-645`.
5. `createDbContext.Set<TEntity>().AddAsync(entity, ct)` — `CoreSQL.cs:647-648`. **Nam ngoai** `_pipelineWrite`.
6. `_pipelineWrite.ExecuteAsync(async ct => await createDbContext.SaveChangesAsync(audit: auditLog, cancellationToken: ct))` — `CoreSQL.cs:650-655`.
7. `return result` — `CoreSQL.cs:657`.

**Side effect** — **Ghi DB va commit ngay** (`SaveChangesAsync` duoc goi ben trong method, khong cho UnitOfWork). Tao va dispose mot `DBContextWrite`. `WriteDbContext.SaveChangesAsync` con goi `OnBeforeSaveChanges(audit)`, `DetectChangesAudit(audit)` khi `audit != null`, va `OnAfterSaveChanges(...)` khi `result > 0` (`WriteDbContext.cs:75-94`). **Mutate `entity`**: EF Core gan khoa sinh tu dong; ngoai ra `OnBeforeSaveChanges` **chi** mutate entity implement `IBaseEntitySQL` (`WriteDbContext.cs:131-132`) va voi `EntityState.Added` se **ep `IsDeleted = false`** cung `CreatedDate`/`CreatedUser`/`CreatedUserCode`/`CreatedUserOrganization` bang `??=` (`WriteDbContext.cs:152-166`, rieng cac `??=` o `:156-159`) — khi `auditLog` la `null` cac gia tri nay lay mac dinh `Anonymous`/`AnonymousCode`/`OrganizationForISC`. Sau khi luu thanh cong, `DispatchDomainEvents` goi **`ChangeTracker.Clear()`** (`WriteDbContext.cs:433`) -> entity tro ve trang thai `Detached`. **Khong co ban ghi audit log nao duoc tao**: `DetectChangesAudit` luon tra `[]` (`WriteDbContext.cs:356`). Ghi log khi guard chan.

**Error handling** — Khong co try/catch. `_pipelineWrite` co the retry `SaveChangesAsync` (theo cau hinh write policy: `MaxRetryAttempts = 1`, chi voi loi connection-level). Neu van that bai, exception noi len caller. Khong log exception. Khong rollback thu cong — vi `SaveChangesAsync` cua EF Core tu boc transaction ngam cho mot lan luu.

**Khi nao NEN dung** — Them mot ban ghi doc lap, khong can nam trong transaction chung voi thao tac khac, va chi can biet "da ghi may dong".

**Khi nao KHONG dung** — Khi can them entity trong **cung transaction** voi thao tac khac (method nay tu tao context rieng -> khong the tham gia transaction cua caller) — dung overload 2.16. Khi can lay lai entity sau khi ghi duoi dang gia tri tra ve (overload nay chi tra `int`; entity van duoc mutate nen caller co the doc tu bien goc). Khi can biet chac "loi" vs "khong co gi thay doi" (ca hai deu khong phan biet duoc bang `0` ma khong bat exception).

**Gioi han**
- Khong the tham gia transaction ben ngoai.
- Sau khi luu thanh cong, `ChangeTracker.Clear()` (`WriteDbContext.cs:433`) da detach entity, roi context bi dispose khi method ket thuc -> entity **khong con duoc EF theo doi**; moi lazy loading sau do se loi.
- `_pipelineWrite` boc `SaveChangesAsync` nhung source **khong kiem tra** co transaction dang mo hay khong; retry `SaveChangesAsync` trong ngu canh transaction do caller quan ly la rui ro chua duoc xu ly trong code.
- `AddAsync` nam ngoai pipeline -> neu retry, chi `SaveChangesAsync` duoc goi lai.
- Khong validate entity (khong goi FluentValidation du package co trong project).

---

### 2.16 CreateAsync(TEntity, DBContextWrite, AuditModel, CancellationToken)

**Signature** (`CoreSQL.cs:669-673`)

```csharp
public virtual async Task<(int Result, TEntity Data)> CreateAsync(
    TEntity entity,
    DBContextWrite context,
    AuditModel auditLog = null,
    CancellationToken cancellationToken = default)
```

**Muc dich** — Them mot entity vao `DBContextWrite` **do caller cung cap** (thuong lay tu `IUnitOfWork<DBContextWrite>.Context(...)`), goi `SaveChangesAsync` tren context do va tra ve tuple `(Result, Data)`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entity` | `TEntity` | Co | `entity is null` -> log + `return (Result: 0, Data: null)` (`CoreSQL.cs:677-682`) | — |
| `context` | `DBContextWrite` | Co (thuc te) | **Khong co null-check.** `null` -> `NullReferenceException` tai `CoreSQL.cs:686` | — |
| `auditLog` | `AuditModel` | Khong | Truyen thang xuong `SaveChangesAsync` | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`CoreSQL.cs:675`) | `default` |

**Output** — `Task<(int Result, TEntity Data)>`.
- `entity` la `null` -> `(Result: 0, Data: null)` — **day la truong hop duy nhat `Data` la `null`**.
- `SaveChangesAsync` tra `0` -> `(Result: 0, Data: entity)` (`CoreSQL.cs:695-697`).
- `SaveChangesAsync` tra `> 0` -> `(Result: result, Data: entity)`.
- Loi DB -> exception noi len.

> [!NOTE]
> Bieu thuc tai `CoreSQL.cs:695-697` la `result is 0 ? (Result: 0, Data: entity) : (Result: result, Data: entity)`. Hai nhanh tra **cung mot `Data`** va nhanh `true` chi tra lai chinh `result` (`= 0`) -> toan bo ternary tuong duong `return (Result: result, Data: entity);`. Day la code du thua, khong thay doi hanh vi.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` — `CoreSQL.cs:675`.
2. Guard `entity is null` -> log + tra `(0, null)` — `CoreSQL.cs:677-682`.
3. `int result = 0;` — `CoreSQL.cs:684`.
4. `context.Set<TEntity>().AddAsync(entity, ct)` — `CoreSQL.cs:686` (ngoai pipeline).
5. `_pipelineWrite.ExecuteAsync(... context.SaveChangesAsync(audit: auditLog, ct) ...)` — `CoreSQL.cs:688-693`.
6. Ternary tra tuple — `CoreSQL.cs:695-697`.

**Side effect** — **Ghi DB**: goi `SaveChangesAsync` **ngay trong method**, khong hoan cho UnitOfWork. Neu caller da mo transaction (`IUnitOfWork.CreateTransactionAsync`), du lieu duoc ghi vao transaction do nhung **chua commit** — commit van do `IUnitOfWork.CommitAsync` (`UnitOfWork/UnitOfWork.cs:70-102`). Mutate `entity` (khoa sinh tu dong, cac field `IBaseEntitySQL` do `OnBeforeSaveChanges` dat). Phat domain event qua `DispatchDomainEvents`; **khong** ghi audit log (`DetectChangesAudit` luon tra `[]` — `WriteDbContext.cs:356`). **Khong** dispose `context` (dung — context thuoc caller). Ghi log khi guard chan.

> [!WARNING]
> **Side effect tren state dung chung:** khi `SaveChangesAsync` tra `> 0`, `OnAfterSaveChanges` -> `DispatchDomainEvents` goi **`ChangeTracker.Clear()`** tren chinh `context` cua caller (`WriteDbContext.cs:433`) -> **toan bo entity ma caller dang cho context theo doi bi detach**, khong chi entity truyen vao method nay. Cac buoc tiep theo trong cung transaction phai tu attach/`Update` lai. Khong co tham so nao tat duoc hanh vi nay.

**Error handling** — Khong co try/catch; exception noi len caller sau khi `_pipelineWrite` retry. Khong rollback (thuoc trach nhiem caller / `IUnitOfWork.CommitAsync` von tu rollback khi commit loi — `UnitOfWork/UnitOfWork.cs:82-101`).

**Khi nao NEN dung** — Them entity nhu mot buoc trong transaction nghiep vu nhieu bang, dung chung `DBContextWrite` tu `IUnitOfWork`; khi can ca so dong va entity (da co khoa sinh) trong mot lan tra ve.

**Khi nao KHONG dung** — Khi muon **hoan** `SaveChanges` cho toi luc commit (method nay luon `SaveChanges` ngay -> moi lan goi la mot round-trip DB va mot lan `ChangeTracker.Clear()` tren context dung chung). Khi caller can giu tracking cho cac entity khac tren cung `context` (xem canh bao o Side effect). Khi khong co `DBContextWrite` san — dung 2.15. Khi caller co the truyen `null` cho `context`.

**Gioi han**
- `context` khong duoc null-check.
- `SaveChangesAsync` duoc goi ngay -> **khong** phai mau "UnitOfWork thuan" (khong gom nhieu thay doi vao mot lan luu).
- Ternary du thua nhu da neu; `Result: 0` cung `Data != null` khien caller kho dung `Result` lam co thanh cong.
- `Data` chi `null` khi guard chan -> caller muon phan biet "entity null" voi "ghi 0 dong" phai kiem tra `Data is null`, khong phai `Result`.
- Retry `SaveChangesAsync` ben trong transaction cua caller khong duoc kiem soat trong code.
- `ChangeTracker.Clear()` chay tren context cua caller sau moi lan luu thanh cong (`WriteDbContext.cs:433`) — mat tracking toan cuc tren context do.

---

### 2.17 CreateAsync(IEnumerable&lt;TEntity&gt;, AuditModel, CancellationToken)

**Signature** (`CoreSQL.cs:708-711`)

```csharp
public virtual async Task<int> CreateAsync(
    IEnumerable<TEntity> entities,
    AuditModel auditLog = null,
    CancellationToken cancellationToken = default)
```

**Muc dich** — Them **nhieu** entity: tu tao `DBContextWrite`, `AddRangeAsync`, `SaveChangesAsync` trong `_pipelineWrite`, tra ve so ban ghi bi anh huong.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entities` | `IEnumerable<TEntity>` | Co | `entities.IsNullOrEmpty()` -> log `FailLogic` + `return 0` (`CoreSQL.cs:715-720`). Extension xu ly duoc `null` (`Helpers/CollectionHelpers.cs:16-19`). **Khong** kiem tra phan tu `null` ben trong | — |
| `auditLog` | `AuditModel` | Khong | Truyen thang xuong `SaveChangesAsync` | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`CoreSQL.cs:713`) | `default` |

**Output** — `Task<int>`; `0` khi `entities` null/rong (co log) hoac khi `SaveChangesAsync` tra `0`; nguoc lai la so ban ghi bi anh huong. Loi -> exception.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` — `CoreSQL.cs:713`.
2. Guard `entities.IsNullOrEmpty()` -> log + `return 0` — `CoreSQL.cs:715-720`.
3. Tao `DBContextWrite` (`await using`) — `CoreSQL.cs:724-725`.
4. `Set<TEntity>().AddRangeAsync(entities, ct)` — `CoreSQL.cs:727-728` (ngoai pipeline).
5. `_pipelineWrite.ExecuteAsync(... SaveChangesAsync(audit: auditLog, ct) ...)` — `CoreSQL.cs:730-735`.
6. `return result` — `CoreSQL.cs:737`.

**Side effect** — Ghi DB va commit ngay. Tao/dispose `DBContextWrite`. Mutate tung entity trong `entities` (khoa sinh tu dong; cac field `IBaseEntitySQL` do `OnBeforeSaveChanges` dat, gom viec ep `IsDeleted = false`). Phat domain event; **khong** ghi audit log (`WriteDbContext.cs:356`). `ChangeTracker.Clear()` sau khi luu thanh cong (`WriteDbContext.cs:433`). **Enumerate `entities`**: `IsNullOrEmpty` co the goi `Any()` voi `IEnumerable` khong phai collection (`Helpers/CollectionHelpers.cs:36`), sau do `AddRangeAsync` enumerate lan nua -> **enumerate nhieu lan**. Ghi log khi guard chan.

**Error handling** — Khong co try/catch; exception noi len sau retry cua `_pipelineWrite`.

**Khi nao NEN dung** — Them mot lo ban ghi doc lap, kich thuoc vua phai, khong can transaction chung voi thao tac khac.

**Khi nao KHONG dung** — Khi `entities` la mot `IEnumerable` **lazy/deferred** hoac chi enumerate duoc mot lan (vi du ket qua `yield return`, stream reader): code enumerate nhieu lan va co the nem exception hoac mat du lieu. Khi lo rat lon: EF Core `AddRangeAsync` + `SaveChanges` sinh nhieu cau `INSERT` trong mot transaction, **khong** co bulk copy — hieu nang kem va transaction dai. Khi can transaction chung — dung 2.18.

**Gioi han**
- Khong co gioi han/batching kich thuoc lo trong code.
- Khong dung `SqlBulkCopy`; chi EF Core `AddRangeAsync`.
- Enumerate `entities` nhieu lan.
- Khong kiem tra phan tu `null` ben trong tap.
- Khong the tham gia transaction ben ngoai.

---

### 2.18 CreateAsync(IEnumerable&lt;TEntity&gt;, DBContextWrite, AuditModel, CancellationToken)

**Signature** (`CoreSQL.cs:749-753`)

```csharp
public virtual async Task<(int Result, IEnumerable<TEntity> Data)> CreateAsync(
    IEnumerable<TEntity> entities,
    DBContextWrite context,
    AuditModel auditLog = null,
    CancellationToken cancellationToken = default)
```

**Muc dich** — Them nhieu entity vao `DBContextWrite` cua caller, goi `SaveChangesAsync`, tra tuple `(Result, Data)`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entities` | `IEnumerable<TEntity>` | Co | `entities.IsNullOrEmpty()` -> log + `return (Result: 0, Data: null)` (`CoreSQL.cs:757-762`) | — |
| `context` | `DBContextWrite` | Co (thuc te) | **Khong co null-check** -> `NullReferenceException` tai `CoreSQL.cs:766` | — |
| `auditLog` | `AuditModel` | Khong | Truyen xuong `SaveChangesAsync` | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`CoreSQL.cs:755`) | `default` |

**Output** — `Task<(int Result, IEnumerable<TEntity> Data)>`.
- `entities` null/rong -> `(Result: 0, Data: null)`.
- `SaveChangesAsync` tra `0` -> `(Result: 0, Data: entities)` (`switch` nhanh `case true`, `CoreSQL.cs:778-781`).
- `> 0` -> `(Result: result, Data: entities)` (nhanh `case false`, `CoreSQL.cs:782-785`).
- Loi -> exception.

> [!NOTE]
> Method dung cau truc `switch (result is 0) { case true: ...; case false: ...; }` (`CoreSQL.cs:776-785`) — mot `switch` tren bieu thuc `bool`. Ca hai nhanh tra cung `Data: entities`, nen toan bo khoi tuong duong `return (Result: result, Data: entities);`. Cung mau code nay xuat hien o `UpdateAsync` tuple overload (`CoreSQL.cs:865-876` va `CoreSQL.cs:952-963`), con `CreateAsync(TEntity, DBContextWrite, ...)` lai dung ternary (`CoreSQL.cs:695-697`) -> hai style khac nhau cho cung mot logic.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` — `CoreSQL.cs:755`.
2. Guard `entities.IsNullOrEmpty()` -> log + `(0, null)` — `CoreSQL.cs:757-762`.
3. `context.Set<TEntity>().AddRangeAsync(entities, ct)` — `CoreSQL.cs:766-767` (ngoai pipeline).
4. `_pipelineWrite.ExecuteAsync(... context.SaveChangesAsync(audit: auditLog, ct) ...)` — `CoreSQL.cs:769-774`.
5. `switch (result is 0)` tra tuple — `CoreSQL.cs:776-785`.

**Side effect** — Ghi DB (`SaveChangesAsync` ngay trong method, chua commit transaction neu caller dang mo transaction). Mutate cac entity (khoa sinh tu dong + field `IBaseEntitySQL`). Phat domain event; **khong** ghi audit log (`WriteDbContext.cs:356`). **Goi `ChangeTracker.Clear()` tren `context` cua caller** khi luu thanh cong (`WriteDbContext.cs:433`) -> detach moi entity dang duoc context do theo doi. Enumerate `entities` nhieu lan (`IsNullOrEmpty` roi `AddRangeAsync`). Khong dispose `context`.

**Error handling** — Khong co try/catch; exception noi len sau retry.

**Khi nao NEN dung** — Them mot lo entity nhu mot buoc trong transaction nghiep vu dung chung context tu `IUnitOfWork`.

**Khi nao KHONG dung** — Khi `entities` la `IEnumerable` chi enumerate duoc mot lan. Khi lo rat lon (khong batching, khong bulk copy, transaction dai). Khi muon hoan `SaveChanges` toi luc commit.

**Gioi han** — `context` khong null-check; `switch (result is 0)` du thua; enumerate nhieu lan; khong batching; `Result: 0` khong phan biet duoc "ghi 0 dong" voi loi (loi thi nem exception); `Data` chi `null` khi guard chan.

---

### 2.19 UpdateAsync(TEntity, AuditModel, CancellationToken)

**Signature** (`CoreSQL.cs:799-802`)

```csharp
public virtual async Task<int> UpdateAsync(
    TEntity entity,
    AuditModel auditLog = null,
    CancellationToken cancellationToken = default)
```

**Muc dich** — Cap nhat mot entity: tu tao `DBContextWrite`, goi `Set<TEntity>().Update(entity)` (dong bo, khong co ban async), roi `SaveChangesAsync` trong `_pipelineWrite`. Tra ve so ban ghi bi anh huong.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entity` | `TEntity` | Co | `entity is null` -> log `FailLogic` + `return 0` (`CoreSQL.cs:806-811`). **Khong** kiem tra entity co khoa chinh hop le | — |
| `auditLog` | `AuditModel` | Khong | Truyen xuong `SaveChangesAsync` | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`CoreSQL.cs:804`) | `default` |

**Output** — `Task<int>`; `0` khi `entity` null (co log) hoac `SaveChangesAsync` tra `0`; nguoc lai so ban ghi cap nhat. Loi -> exception.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` — `CoreSQL.cs:804`.
2. Guard `entity is null` -> log + `return 0` — `CoreSQL.cs:806-811`.
3. Tao `DBContextWrite` (`await using`) — `CoreSQL.cs:815-816`.
4. `createDbContext.Set<TEntity>().Update(entity)` — `CoreSQL.cs:818` (ngoai pipeline).
5. `_pipelineWrite.ExecuteAsync(... SaveChangesAsync(audit: auditLog, ct) ...)` — `CoreSQL.cs:820-825`.
6. `return result` — `CoreSQL.cs:827`.

**Side effect** — **Ghi DB va commit ngay.** `DbSet.Update` danh dau **toan bo** property cua entity la `Modified` -> cau `UPDATE` sinh ra ghi **tat ca cac cot**, khong chi cot da doi. Tao/dispose `DBContextWrite`. Mutate entity: `OnBeforeSaveChanges` chi set `ModifiedDate`/`ModifiedUser`/`ModifiedUserCode`/`ModifiedUserOrganization` khi `entry.State` la `Modified`/`Detached` **va `audit is not null`** (`WriteDbContext.cs:168-180`) -> voi gia tri mac dinh `auditLog = null` thi **khong field audit nao duoc cap nhat**. Phat domain event; **khong** ghi audit log (`WriteDbContext.cs:356`). `ChangeTracker.Clear()` sau khi luu thanh cong (`WriteDbContext.cs:433`). Ghi log khi guard chan.

**Error handling** — Khong co try/catch. `DbUpdateConcurrencyException` (khi ban ghi khong ton tai hoac concurrency token lech) noi len caller. `_pipelineWrite` chi retry loi connection-level theo cau hinh write policy.

**Khi nao NEN dung** — Cap nhat toan bo mot entity da co du gia tri (ke ca cac cot khong thay doi), doc lap, khong can transaction chung.

**Khi nao KHONG dung** — Khi chi muon cap nhat vai cot (`Update` ghi tat ca cot — nguy co ghi de gia tri do nguoi khac vua sua, va ghi de bang `null`/`default` neu entity truyen vao thieu du lieu). Khi can transaction chung — dung 2.20. Khi entity duoc lay tu `DBContextRead` ma khong co day du moi cot. Khi can soft delete — **class nay khong co API xoa nao**; soft delete phai tu thuc hien bang cach set co `IsDeleted` roi goi `UpdateAsync`.

**Gioi han**
- `DbSet.Update` -> **full-row update**, khong partial update.
- Khong kiem tra entity co ton tai trong DB; `SaveChanges` se nem `DbUpdateConcurrencyException`.
- Khong the tham gia transaction ben ngoai.
- `Update` la API dong bo, nam ngoai pipeline.
- Khong co optimistic concurrency duoc xu ly o lop nay.
- Goi ma **khong** truyen `auditLog` -> `ModifiedDate`/`ModifiedUser`/`ModifiedUserCode`/`ModifiedUserOrganization` **khong** duoc gan (`WriteDbContext.cs:168-180`). Day la bay de mac vi tham so co default `null`.

---

### 2.20 UpdateAsync(TEntity, DBContextWrite, AuditModel, CancellationToken)

**Signature** (`CoreSQL.cs:839-843`)

```csharp
public virtual async Task<(int Result, TEntity Data)> UpdateAsync(
    TEntity entity,
    DBContextWrite context,
    AuditModel auditLog = null,
    CancellationToken cancellationToken = default)
```

**Muc dich** — Cap nhat mot entity tren `DBContextWrite` cua caller va tra tuple `(Result, Data)`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entity` | `TEntity` | Co | `entity is null` -> log + `return (Result: 0, Data: null)` (`CoreSQL.cs:847-852`) | — |
| `context` | `DBContextWrite` | Co (thuc te) | **Khong co null-check** -> `NullReferenceException` tai `CoreSQL.cs:856` | — |
| `auditLog` | `AuditModel` | Khong | Truyen xuong `SaveChangesAsync` | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`CoreSQL.cs:845`) | `default` |

**Output** — `Task<(int Result, TEntity Data)>`.
- `entity` null -> `(Result: 0, Data: null)`.
- `SaveChangesAsync` tra `0` -> `(Result: 0, Data: entity)` (`CoreSQL.cs:867-870`).
- `> 0` -> `(Result: result, Data: entity)` (`CoreSQL.cs:871-874`).
- Loi -> exception.

Khoi `switch (result is 0)` tai `CoreSQL.cs:865-876` tra cung `Data: entity` o ca hai nhanh -> du thua nhu da neu o 2.18.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` — `CoreSQL.cs:845`.
2. Guard `entity is null` -> log + `(0, null)` — `CoreSQL.cs:847-852`.
3. `context.Set<TEntity>().Update(entity)` — `CoreSQL.cs:856` (ngoai pipeline).
4. `_pipelineWrite.ExecuteAsync(... context.SaveChangesAsync(audit: auditLog, ct) ...)` — `CoreSQL.cs:858-863`.
5. `switch (result is 0)` tra tuple — `CoreSQL.cs:865-876`.

**Side effect** — Ghi DB (`SaveChangesAsync` ngay, chua commit transaction cua caller). Full-row update nhu 2.19. Mutate entity (chi khi truyen `auditLog` thi cac field `Modified*` moi duoc set — `WriteDbContext.cs:168-180`). Phat domain event; **khong** ghi audit log (`WriteDbContext.cs:356`). **Goi `ChangeTracker.Clear()` tren `context` cua caller** khi luu thanh cong (`WriteDbContext.cs:433`). Khong dispose `context`.

**Error handling** — Khong co try/catch; `DbUpdateConcurrencyException`/`DbUpdateException` noi len caller.

**Khi nao NEN dung** — Cap nhat entity nhu mot buoc trong transaction nghiep vu dung chung context.

**Khi nao KHONG dung** — Khi entity da duoc context nay **tracking** voi cung khoa: `Update` tren mot instance khac cung khoa se gay `InvalidOperationException` (EF Core khong cho hai instance cung khoa trong mot context). Khi chi muon partial update. Khi muon hoan `SaveChanges`.

**Gioi han** — `context` khong null-check; full-row update; `switch` du thua; `Result: 0` khong phan biet "0 dong" voi loi; retry `SaveChangesAsync` trong transaction cua caller khong duoc kiem soat.

---

### 2.21 UpdateAsync(IEnumerable&lt;TEntity&gt;, AuditModel, CancellationToken)

**Signature** (`CoreSQL.cs:886-889`)

```csharp
public virtual async Task<int> UpdateAsync(
    IEnumerable<TEntity> entities,
    AuditModel auditLog = null,
    CancellationToken cancellationToken = default)
```

**Muc dich** — Cap nhat nhieu entity: tu tao `DBContextWrite`, `UpdateRange(entities)`, `SaveChangesAsync` trong `_pipelineWrite`, tra ve so ban ghi bi anh huong.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entities` | `IEnumerable<TEntity>` | Co | `entities.IsNullOrEmpty()` -> log `FailLogic` + `return 0` (`CoreSQL.cs:893-898`) | — |
| `auditLog` | `AuditModel` | Khong | Truyen xuong `SaveChangesAsync` | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`CoreSQL.cs:891`) | `default` |

**Output** — `Task<int>`; `0` khi `entities` null/rong (co log) hoac `SaveChangesAsync` tra `0`; nguoc lai so ban ghi cap nhat. Loi -> exception.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` — `CoreSQL.cs:891`.
2. Guard `entities.IsNullOrEmpty()` -> log + `return 0` — `CoreSQL.cs:893-898`.
3. Tao `DBContextWrite` (`await using`) — `CoreSQL.cs:902-903`.
4. `createDbContext.Set<TEntity>().UpdateRange(entities)` — `CoreSQL.cs:905` (dong bo, ngoai pipeline).
5. `_pipelineWrite.ExecuteAsync(... SaveChangesAsync(audit: auditLog, ct) ...)` — `CoreSQL.cs:907-912`.
6. `return result` — `CoreSQL.cs:914`.

**Side effect** — Ghi DB va commit ngay. Full-row update cho **moi** entity. Tao/dispose `DBContextWrite`. Mutate cac entity (`Modified*` chi duoc set khi `auditLog` khac `null` — `WriteDbContext.cs:168-180`). Phat domain event; **khong** ghi audit log (`WriteDbContext.cs:356`). `ChangeTracker.Clear()` sau khi luu thanh cong. Enumerate `entities` nhieu lan (`IsNullOrEmpty` roi `UpdateRange`).

**Error handling** — Khong co try/catch; `DbUpdateConcurrencyException` khi mot trong cac ban ghi khong ton tai -> **toan bo lo that bai** (mot `SaveChanges` = mot transaction).

**Khi nao NEN dung** — Cap nhat mot lo entity da co day du gia tri, doc lap voi transaction khac, lo nho/vua.

**Khi nao KHONG dung** — Khi can partial update. Khi lo lon (khong batching, khong bulk update, transaction dai, moi entity mot cau `UPDATE`). Khi `entities` chi enumerate duoc mot lan. Khi can "cap nhat duoc bao nhieu thi cap nhat" — mot ban ghi loi lam ca lo rollback.

**Gioi han** — Full-row update; khong batching; enumerate nhieu lan; khong kiem tra phan tu `null`; khong the tham gia transaction ben ngoai; khong co `ExecuteUpdate` (set-based update).

---

### 2.22 UpdateAsync(IEnumerable&lt;TEntity&gt;, DBContextWrite, AuditModel, CancellationToken)

**Signature** (`CoreSQL.cs:926-930`)

```csharp
public virtual async Task<(int Result, IEnumerable<TEntity> Data)> UpdateAsync(
    IEnumerable<TEntity> entities,
    DBContextWrite context,
    AuditModel auditLog = null,
    CancellationToken cancellationToken = default)
```

**Muc dich** — Cap nhat nhieu entity tren `DBContextWrite` cua caller va tra tuple `(Result, Data)`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entities` | `IEnumerable<TEntity>` | Co | `entities.IsNullOrEmpty()` -> log + `return (Result: 0, Data: null)` (`CoreSQL.cs:934-939`) | — |
| `context` | `DBContextWrite` | Co (thuc te) | **Khong co null-check** -> `NullReferenceException` tai `CoreSQL.cs:943` | — |
| `auditLog` | `AuditModel` | Khong | Truyen xuong `SaveChangesAsync` | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (`CoreSQL.cs:932`) | `default` |

**Output** — `Task<(int Result, IEnumerable<TEntity> Data)>`.
- `entities` null/rong -> `(Result: 0, Data: null)`.
- `SaveChangesAsync` tra `0` -> `(Result: 0, Data: entities)` (`CoreSQL.cs:954-957`).
- `> 0` -> `(Result: result, Data: entities)` (`CoreSQL.cs:958-961`).
- Loi -> exception.

Khoi `switch (result is 0)` tai `CoreSQL.cs:952-963` du thua nhu 2.18/2.20.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` — `CoreSQL.cs:932`.
2. Guard `entities.IsNullOrEmpty()` -> log + `(0, null)` — `CoreSQL.cs:934-939`.
3. `context.Set<TEntity>().UpdateRange(entities)` — `CoreSQL.cs:943` (ngoai pipeline).
4. `_pipelineWrite.ExecuteAsync(... context.SaveChangesAsync(audit: auditLog, ct) ...)` — `CoreSQL.cs:945-950`.
5. `switch (result is 0)` tra tuple — `CoreSQL.cs:952-963`.

**Side effect** — Ghi DB (`SaveChangesAsync` ngay, chua commit transaction cua caller). Full-row update cho moi entity. Mutate cac entity (`Modified*` chi khi `auditLog` khac `null`). Phat domain event; **khong** ghi audit log (`WriteDbContext.cs:356`). **Goi `ChangeTracker.Clear()` tren `context` cua caller** khi luu thanh cong (`WriteDbContext.cs:433`). Enumerate `entities` nhieu lan. Khong dispose `context`.

**Error handling** — Khong co try/catch; exception noi len; mot ban ghi loi -> ca lo that bai.

**Khi nao NEN dung** — Cap nhat lo entity nhu mot buoc trong transaction nghiep vu dung chung context tu `IUnitOfWork`.

**Khi nao KHONG dung** — Khi trong lo co entity ma context dang tracking instance khac cung khoa (`InvalidOperationException`). Khi can partial update hoac set-based update. Khi lo lon. Khi `entities` chi enumerate mot lan.

**Gioi han** — `context` khong null-check; full-row update; `switch` du thua; enumerate nhieu lan; khong batching; `Result: 0` khong phan biet duoc cac tinh huong.

---

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `FindAllWithScriptAsync` tra **`null`** khi `scriptSQLQuery` rong, trong khi `FindAllAsync`, `FindAllAsync<TDto>`, `FindAllSortDeletedAsync`, `FindAllSortDeletedAsync<TDto>` deu tra `[]` | `CoreSQL.cs:129` so voi `CoreSQL.cs:510`, `:548`, `:583`, `:617` | Khong nhat quan trong cung mot class. Caller `foreach`/`.Count()` truc tiep tren ket qua se gap `NullReferenceException` |
| 2 | `scriptSQLQuery` cua ca 4 method raw SQL chi duoc kiem tra rong, **khong kiem tra noi dung** | `CoreSQL.cs:75`, `:125`, `:175`, `:228` | **Nguy co SQL injection** neu caller noi chuoi tu input ben ngoai. `DynamicParameters` chi bao ve phan gia tri, khong bao ve phan cau lenh |
| 3 | `IsExecuteNonQueryAsync` **khong null-check** `context` va `transaction` | `CoreSQL.cs:217-240` | `null` context -> `NullReferenceException` thay vi loi nghiep vu ro rang; khong co log |
| 4 | `IsExecuteNonQueryAsync` **khong truyen `cancellationToken`** xuong Dapper (overload `SqlMapper.ExecuteAsync` duoc goi khong co tham so token) | `CoreSQL.cs:235-240` | Khong the huy cau lenh ghi dang chay; token chi co tac dung o `ThrowIfCancellationRequested()` dau method (`:226`) |
| 5 | `IsExecuteNonQueryAsync` la method duy nhat **khong duoc boc Polly pipeline** | `CoreSQL.cs:235` (so voi 13 lan `_pipelineRead.ExecuteAsync` va 8 lan `_pipelineWrite.ExecuteAsync`) | Khong co retry/circuit breaker cho duong ghi raw SQL; hanh vi resilience khong dong nhat trong class |
| 6 | XML doc cua `IsExecuteNonQueryAsync` mo ta `context` la "Đối tượng DbContext" nhung kieu that la `DbConnection`; thu tu tham so trong doc cua interface khac thu tu that | `CoreSQL.cs:210`, `ICoreSQL.cs:69-72` | Tai lieu mau thuan code. Theo nguyen tac Source Code > Documentation, kieu dung la `DbConnection` va thu tu dung la `(scriptSQLQuery, context, transaction, parameters, ...)` |
| 7 | `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;` duoc ghep **cung** vao script khi `commandType == CommandType.Text` | `CoreSQL.cs:82-90`, `:132-140`, `:182-190` | Moi truy van doc raw SQL dang Text deu la **dirty read**; caller khong co tham so nao de tat |
| 8 | Ternary `result is 0 ? (Result: 0, Data: entity) : (Result: result, Data: entity)` va `switch (result is 0)` voi hai nhanh tra cung `Data` | `CoreSQL.cs:695-697`, `:776-785`, `:865-876`, `:952-963` | Code du thua (tuong duong `return (Result: result, Data: entities);`). Ngoai ra hai style khac nhau (ternary vs `switch`) cho cung mot logic trong cung file |
| 9 | Cac overload `CreateAsync`/`UpdateAsync` nhan `DBContextWrite context` van **tu goi `SaveChangesAsync`** ngay trong method | `CoreSQL.cs:688-693`, `:769-774`, `:858-863`, `:945-950` | Khong phai mau UnitOfWork thuan: khong gom nhieu thay doi vao mot lan luu; moi lan goi la mot round-trip va mot snapshot audit rieng. Viec commit transaction van do `IUnitOfWork.CommitAsync` (`UnitOfWork/UnitOfWork.cs:70`) |
| 10 | `_pipelineWrite` retry `SaveChangesAsync` nhung code **khong kiem tra** co transaction do caller dang mo hay khong | `CoreSQL.cs:650-655`, `:688-693`, `:730-735`, `:769-774`, `:820-825`, `:858-863`, `:907-912`, `:945-950` | Retry `SaveChangesAsync` ben trong mot transaction ben ngoai la rui ro chua duoc xu ly (EF Core khong tu khoi phuc trang thai transaction da bi abort) |
| 11 | `CreateAsync`/`UpdateAsync` tra **`int`** (so ban ghi) chu khong phai `bool`; gia tri `0` mang hai nghia: guard chan hoac `SaveChanges` tra 0 | `CoreSQL.cs:639` & `:657`; `:719` & `:737`; `:810` & `:827`; `:897` & `:914` | Caller khong phan biet duoc "dau vao khong hop le" voi "khong co gi thay doi" neu chi nhin gia tri tra ve. O cac overload tuple, chi co `Data is null` moi cho biet guard da chan |
| 12 | Ten property soft delete **hardcode** `"IsDeleted"` va bat buoc kieu `bool` khong nullable | `CoreSQL.cs:27`, dung tai `:303`, `:347`, `:396`, `:499`, `:537` | Entity khong co property nay -> query loi luc EF translate (chi phat hien khi chay). Khong cau hinh duoc ten cot khac |
| 13 | Nhom `*SortDeletedAsync` co chu "Sort" nhung **khong sap xep**; chi overload `CoreSQL.cs:330` co tham so `sorting` | `CoreSQL.cs:288`, `:381`, `:483`, `:521` | Ten method gay hieu sai. `FirstOrDefaultAsync` khong co `OrderBy` -> ban ghi tra ve **khong xac dinh** khi nhieu dong khop |
| 14 | **Khong co API xoa nao** trong class (khong `Delete`/`Remove`/`RemoveRange`/`ExecuteDelete`) | Toan bo `CoreSQL.cs` | Hard delete phai lam qua `IsExecuteNonQueryAsync` (raw SQL, khong qua `OnBeforeSaveChanges`, khong phat domain event); soft delete phai tu set co `IsDeleted` roi goi `UpdateAsync` |
| 15 | **Khong co phan trang, `OrderBy`, `Count`/`Any`, `Include`** trong bat ky method EF Core nao | `CoreSQL.cs:483-618` (nhom `FindAll*`) | `FindAllAsync(null, ct)` sinh query quet toan bang, nap het vao RAM. Khong co cach gioi han so dong o lop nay |
| 16 | Overload `FindOneSortDeletedAsync<TDto>(filters, sorting, isDeleted, selector, ct)` **khong co trong `ICoreSQL`** | `CoreSQL.cs:330` vs `ICoreSQL.cs` (khong khai bao) | Code phu thuoc abstraction `ICoreSQL` khong goi duoc overload nay; kho mock trong unit test |
| 17 | Overload `FindOneSortDeletedAsync<TDto>` khong kiem tra `sorting(query)` tra `null` | `CoreSQL.cs:354-359` | Neu delegate `sorting` tra `null` -> `NullReferenceException` tai `query.AsNoTracking()` (`:359`) |
| 18 | `ProjectTo` dung `Activator.CreateInstance` + reflection, **nuot loi**; va **hai overload xu ly loi khac nhau** | Overload 1 phan tu: `Extensions/ProjectToExtensions.cs:29`, `:56-59`. Overload danh sach: `:90-123` (`Activator.CreateInstance<TDto>()` o `:92` nam **trong** `try`) | Loi map mot property bi nuot lang le (chi log console) -> DTO tra ve thieu du lieu ma caller khong biet. `TDto` khong co ctor public khong tham so: overload **1 phan tu** nem exception ra caller (`:29` nam ngoai `try`), con overload **danh sach** thi exception bi bat o `:120-123` -> phan tu bi loai khoi ket qua, `FindAll*Async<TDto>` tra ve danh sach thieu hoac `[]` ma **khong co loi nao noi len**. Reflection khong cache -> chi phi CPU cao tren tap lon |
| 19 | `FindByIdAsync` chi ho tro khoa chinh **mot cot** (`keyValues: [id]`) va **khong ap filter `IsDeleted`** | `CoreSQL.cs:269` | Entity co composite key khong dung duoc. Ban ghi da soft delete van duoc tra ve |
| 20 | Cac overload nhan `IEnumerable<TEntity>` **enumerate tap dau vao nhieu lan** (`IsNullOrEmpty` roi `AddRangeAsync`/`UpdateRange`) | `CoreSQL.cs:715` & `:727`; `:757` & `:766`; `:893` & `:905`; `:934` & `:943` | `IEnumerable` lazy/one-shot (vi du tu `yield return` hoac reader) co the loi hoac mat du lieu |
| 21 | **Khong co mot khoi `try`/`catch` nao** trong `CoreSQL.cs` | Toan bo file (grep `catch` = 0 ket qua) | Moi exception noi len caller va **khong duoc ghi log** o lop repository. `_logger` chi duoc dung cho guard clause (`FailLogic`) |
| 22 | Constructor **khong null-check** tham so nao | `CoreSQL.cs:35-53` | Loi cau hinh DI (thieu pipeline, thieu factory) chi lo ra o lan goi method dau tien duoi dang `NullReferenceException` |
| 23 | Toan bo field la `private` | `CoreSQL.cs:21-33` | Lop con ke thua **khong truy cap duoc** `_logger`, `_dapperDbContext`, `_dbContextRead`, `_dbContextWrite`, `_pipelineRead`, `_pipelineWrite`; muon mo rong phai `override` method `virtual` hoac tu inject lai dependency |
| 24 | `SqlResiliencePolicyFactory` ton tai nhung **khong co code nao trong repo goi `ConfigureReadPolicy`/`ConfigureWritePolicy`** de tao `_pipelineRead`/`_pipelineWrite` | `Data/SQL/Helpers/Policies/SqlResiliencePolicyFactory.cs:59`, `:154`; `CoreSQL.cs:39` | Viec cau hinh pipeline phu thuoc hoan toan vao code dang ky DI **ben ngoai** repo nay. Neu ben ngoai truyen mot `ResiliencePipeline` rong, toan bo retry/circuit breaker cua `CoreSQL` se khong hoat dong — va dieu do khong the phat hien tu source trong repo |
| 25 | Trong repo khong co class nao ke thua `CoreSQL<,,>` va khong co dang ky DI cho no | grep `CoreSQL<` ngoai thu muc `Data/SQL/Core/` = 0 ket qua | Khong the xac minh cach su dung thuc te (gia tri `AuditModel`, cau hinh pipeline, entity nao co `IsDeleted`) **tu source code trong repo nay**; phai tra o cac repo API tieu thu DLL (xem `CopyToOtherLibs` trong `FTELSRCore.Shared.csproj`) |
| 26 | Tham so lambda cua Polly duoc dat ten trung tham so method (`cancellationToken`) trong 3 method raw SQL | `CoreSQL.cs:93`, `:143`, `:193` | De doc sai: token truyen xuong Dapper la token do Polly cap, khong phai truc tiep tham so method. Cac method EF Core dung ten `ct` cho cung vi tri -> style khong dong nhat |
| 27 | **`ChangeTracker.Clear()` duoc goi vo dieu kien sau moi lan luu thanh cong** — ke ca tren `DBContextWrite` do caller cung cap | `WriteDbContext.cs:433` (qua `OnAfterSaveChanges` -> `DispatchDomainEvents`), duoc kich hoat tu `CoreSQL.cs:653`, `:691`, `:733`, `:772`, `:823`, `:861`, `:910`, `:948` | Voi 4 overload nhan `context` (2.16, 2.18, 2.20, 2.22): **moi entity ma caller dang cho context theo doi deu bi detach** sau loi goi, khong chi entity truyen vao. Nhieu buoc nghiep vu dung chung mot context trong cung transaction se mat tracking giua cac buoc va phai attach/`Update` lai |
| 28 | **Co che audit log khong duoc cai dat**: `DetectChangesAudit` luon `return []`, toan bo than ham nam trong region `NOT SUPPORT` bi comment | `WriteDbContext.cs:356`; he qua tai `WriteDbContext.cs:375-378` (`DispatchAuditLog` thoat ngay) | `AuditModel` truyen vao `CreateAsync`/`UpdateAsync` **chi** dung de gan cac field `Created*`/`Modified*` tren entity `IBaseEntitySQL`; **khong co bang/ban ghi audit log nao duoc ghi**. Moi phat bieu kieu "mat audit khi dung raw SQL" chi dung o phan field audit tren entity, khong phai audit log |
| 29 | `OnBeforeSaveChanges` **chi** set cac field `Modified*` khi `audit is not null`; va **chi** ap dung cho entity implement `IBaseEntitySQL` | `WriteDbContext.cs:131-132`, `:168-180` | `UpdateAsync(entity)` / `UpdateAsync(entities)` goi voi gia tri mac dinh `auditLog = null` -> **khong** cap nhat `ModifiedDate`/`ModifiedUser`; entity khong implement `IBaseEntitySQL` thi khong co field audit nao duoc gan trong moi truong hop. Khong co guard/log nao canh bao |
| 30 | `IDapperSQLDBContext.ExecuteNonQueryAsync` — ban non-query **co** truyen `CancellationToken` qua `CommandDefinition` va tu quan connection — **khong bao gio duoc `CoreSQL` goi** | `Dapper/IDapperSQLDBContext.cs:18`, impl `Dapper/DapperSQLDBContext.cs:28-51`; `CoreSQL` chi dung `GetOne`/`GetAll`/`GetOneExecute` | Duong ghi raw SQL cua `CoreSQL` (`IsExecuteNonQueryAsync`) tu goi `SqlMapper.ExecuteAsync` nen mat ca `CancellationToken` lan Polly, trong khi thu vien da co san implementation ho tro token. `GetAllExecuteAsync` (`IDapperSQLDBContext.cs:82`) cung khong duoc dung -> capability chet |
| 31 | `_logger.FailLogic` ghi o muc **`LogLevel.Information`**, khong phai Warning/Error | `Extensions/Loggers/LoggerExtensions.cs:179-182` (EventId 107, category `BIZ_LOGIC`) | Guard clause bi kich hoat (script rong, `entity is null`, `entities` rong) rat de bi bo lot khi he thong cau hinh minimum level la `Warning`; day la **nguon thong tin duy nhat** ve cac truong hop tra `default`/`null`/`0`/`false` do guard |
| 32 | Nhom `FindOne*`/`FindAll*` EF Core **khong co guard clause va khong ghi log** nao, khac nhom raw SQL va nhom ghi | `CoreSQL.cs:288-618` (khong co `_logger` nao duoc goi) | `filters = null` duoc coi la hop le va sinh query khong co `Where` (ngoai `IsDeleted` o nhom `*SortDeleted*`); khong co dau vet log de phat hien loi goi thieu filter |
