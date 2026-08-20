# Knowledge Base - FTELSRCore.Shared

> Chuan: Engineering Knowledge Documentation Standard v1.0
> Ngon ngu: Tieng Viet | Code identifier giu nguyen casing goc
> Cap nhat theo commit: `2262829`

---

## 1. Cach dung tai lieu nay

Day la tap tai lieu ky thuat cho thu vien dung chung `FTELSRCore.Shared` (repo `sr-core-helper`, target `net9.0`). Toan bo noi dung duoc **doc truc tiep tu source code** trong repo tai commit `2262829`, kem so dong tham chieu de nguoi doc tu kiem chung.

### 1.1 Doi tuong doc va cach doc

| Doi tuong | Doc gi truoc | Muc dich |
|---|---|---|
| **Developer** (dang code) | Muc `1.3 Danh muc API` cua file lien quan -> muc `2.x` cua dung API can goi | Nam signature, input hop le, gia tri tra ve o tung nhanh, side effect |
| **Developer** (dang debug) | Muc `Van de da biet` (muc cuoi cua moi file) -> muc 5 cua README | Doi chieu trieu chung voi cai bay da duoc ghi nhan |
| **Tech Lead / Reviewer** | Muc `1.1 Pham vi chuc nang` + `Van de da biet` | Danh gia rui ro truoc khi duyet PR dung cac API nay |
| **PO / BA** | Muc `1. Tong quan` + cot "Khong lam duoc" cua bang `1.1` | Hieu gioi han nang luc that cua thu vien, tranh cam ket sai voi khach hang |
| **AI / LLM agent** | README (muc 2, 3, 4) truoc, roi file chi tiet | Dinh huong dung file, dung muc; **khong** doan API |
| **NotebookLM / RAG** | Nap toan bo thu muc `docs/knowledge-base/` | Moi file la mot don vi ngu nghia doc lap, co header nguon va commit |

### 1.2 Nguyen tac "Source Code > Documentation"

Khi XML doc / comment trong `.cs` **mau thuan** voi than ham, tai lieu nay **luon lay than ham lam su that** va ghi ro cho mau thuan do o muc `Van de da biet`. Cac vi du da duoc ghi nhan:

- XML doc noi `data null nếu lỗi` nhung `EnsureSuccessOrException` khong nem voi 4xx/5xx (`Utilizes-CallApiWithHttp.md`, van de #2 va #24).
- XML doc noi `false nếu có lỗi` nhung than ham `IsCreateOneAsync` khong co `try/catch` nao (`Data-MongoDB-CoreMongoDB.md`, van de #7).
- XML doc noi `FindAll*` tra `null` khi khong tim thay nhung than ham tra `[]` (`Data-SQL-CoreSQL-TwoEntity.md`, van de #2).
- XML doc noi `ReadDbContext` "khong chiu chi phi change tracking" nhung khong co dong nao dat `QueryTrackingBehavior` (`Data-SQL-UnitOfWork-DbContexts.md`, van de #10).
- XML doc mo ta `context` la "Đối tượng DbContext" nhung kieu that la `DbConnection` (`Data-SQL-CoreSQL.md`, van de #6).

### 1.3 Luu y danh cho AI / LLM

> [!IMPORTANT]
> **Khong duoc suy dien capability ngoai pham vi tai lieu.** Cu the:
>
> - Khong gia dinh mot API ton tai neu no khong nam trong muc `1.3 Danh muc API` cua file tuong ung. Vi du: **khong co API Delete/Remove nao** trong `CoreSQL` (ca 2 arity) — xem `Data-SQL-CoreSQL.md` van de #14.
> - Khong gia dinh co retry / transaction / audit log / phan trang chi vi ten ham goi y nhu vay. Vi du: `*SortDeletedAsync` **khong** sap xep; `ExecuteScalar` **khong** dung `ExecuteScalarAsync`; `AuditModel` **khong** sinh ban ghi audit log; ten file `CoreSQLTenant.cs` **khong** co logic multi-tenant nao.
> - Khi tai lieu ghi "**khong xac dinh duoc tu source code**", phai giu nguyen ket luan do, khong duoc thay bang phong doan hay kien thuc chung ve .NET/EF Core/Polly.
> - Moi so dong (`File.cs:123`) chi dung cho commit `2262829`. Neu repo da thay doi, phai doc lai source truoc khi tra loi.
> - Code identifier (ten class, method, property, generic parameter) **giu nguyen casing goc**, khong dich sang tieng Viet, khong doi hoa/thuong.

---

## 2. Ban do module

| Tai lieu | Module | Tang kien truc | Cong nghe | So API |
|---|---|---|---|---|
| [`Utilizes-CallApiWithHttp.md`](Utilizes-CallApiWithHttp.md) | `CallApiWithHttp<TRequest, TResponse>` | Shared / Utilizes (outbound HTTP) | `HttpClient`, `System.Text.Json`, `Newtonsoft.Json` (log) | **11** public static + 1 private helper (`ParseModelToQueryString`) |
| [`Utilizes-CallApi.md`](Utilizes-CallApi.md) | `CallApi<TResponse>` | Shared / Utilizes (outbound HTTP) | `HttpClient`, `System.Text.Json`, `Newtonsoft.Json` (log) | **9** public static |
| [`Data-MongoDB-CoreMongoDB.md`](Data-MongoDB-CoreMongoDB.md) | `CoreMongoDB<TTable>` / `ICoreMongoDB<TTable>` | Data Access - repository (MongoDB) | `MongoDB.Driver` 3.10.0, `Polly` 8.7.0 | **32** public virtual async + 1 constructor = 33 muc |
| [`Data-SQL-CoreSQL.md`](Data-SQL-CoreSQL.md) | `CoreSQL<TEntity, DBContextRead, DBContextWrite>` / `ICoreSQL<...>` | Data Access - repository (SQL Server) | EF Core 9.0.18, `Dapper` 2.1.79, `Polly` 8.7.0 | **22** public method tren class (`ICoreSQL` khai bao **21**) |
| [`Data-SQL-CoreSQL-TwoEntity.md`](Data-SQL-CoreSQL-TwoEntity.md) | `CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>` (file `CoreSQLTenant.cs`) | Data Access - repository + mapping | EF Core 9.0.18, `Dapper` 2.1.79, `Polly` 8.7.0 | **22** public method |
| [`Data-SQL-UnitOfWork-DbContexts.md`](Data-SQL-UnitOfWork-DbContexts.md) | `IUnitOfWork` / `UnitOfWork` / `WriteDbContext<TContext>` / `ReadDbContext<TContext>` | Data Access - persistence (EF Core) | EF Core 9.0.18, `MediatR` 12.4.1 | **23** muc (4 hop dong interface + 7 `UnitOfWork` + 10 `WriteDbContext` + 2 `ReadDbContext`) |
| [`Data-SQL-Dapper.md`](Data-SQL-Dapper.md) | `IDapperSQLDBContext` / `DapperSQLDBContext` / `ExecuteSQLContext<TClass>` / `ConfigurationHelpers` | Data Access - raw SQL (duoi tang repository) | `Dapper` 2.1.79, `Microsoft.Data.SqlClient` 7.0.2 | **13** muc (6 `DapperSQLDBContext` + 6 `ExecuteSQLContext` + 1 helper) |
| [`Data-SQL-Resilience.md`](Data-SQL-Resilience.md) | `SqlResiliencePolicyFactory` / `ReadUncommittedConnectionInterceptor` | Data Access / cross-cutting (helpers) | `Polly` 8.7.0, EF Core `DbConnectionInterceptor`, `System.Diagnostics.ActivitySource` | **9** muc trong danh muc (2 public static + 3 private static + 3 private field + 1 public override); **6** muc chi tiet cap method |

> [!NOTE]
> **Phan biet danh tinh type.** `CoreSQL.cs:16` khai bao `CoreSQL<TEntity, DBContextRead, DBContextWrite>` (3 type parameter); `CoreSQLTenant.cs:17` khai bao `CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>` (4 type parameter). Khac arity nen day la **hai generic type doc lap**, khong phai hai phan `partial` cua cung mot class. **Trong repo khong ton tai type nao ten `CoreSQLTenant`** — do chi la ten file.

---

## 3. Chon dung component

| Neu can lam | Dung module / API | Luu y bat buoc doc truoc |
|---|---|---|
| **Goi HTTP API co body JSON** (POST/PUT/PATCH) | `CallApiWithHttp<TRequest, TResponse>`: `PostAsJSonAsync`, `PutAsJSonAsync`, `PatchAsJSonAsync`, `PostWithHeadersAsJSonAsync` | Body lay tu `option.Value`, **khong** build query string. Bat buoc kiem tra **ca** `ErrorModel.Succeeded` **va** `data != null` (van de #2, #23). `PostWithHeadersAsJSonAsync` ghi header vao `client.DefaultRequestHeaders` va **khong xoa** -> ro ri sang request khac (van de #5) |
| **Goi HTTP API khong body** (GET/DELETE) | `CallApiWithHttp<TRequest, TResponse>`: `GetAsJSonAsync`, `GetAsJSonAndHeaderAsync`, `GetAsJSonCustomHeaderAsync`, `DeleteAsJSonAsync` | Query string duoc build bang **hai co che khac nhau** -> cung mot model sinh ra hai URL khac nhau (van de #13, #28). `GetAsJSonCustomHeaderAsync` khong null-check `headers` (van de #15) |
| **Goi HTTP API khi khong co model request** | `CallApi<TResponse>` | **Canh bao lon nhat:** `PostAsJSonAsync`, `PostWithHeadersAsJSonAsync`, `PutAsJSonAsync`, `PatchAsJSonAsync` cua lop nay **gui request KHONG CO BODY** (khong set `HttpRequestMessage.Content`) du ten co `AsJSon`. Chi `PostFormDataAsJSonAsync` co body (van de #1) |
| **Upload file qua HTTP** | `CallApiWithHttp<TRequest, TResponse>`: `PostAsFileV2Async` (uu tien) hoac `PostAsFileAsync` | `PostAsFileAsync` goi `ReadAsync` **mot lan** va bo qua so byte doc duoc -> **file gui len co the bi thieu** (van de #8). `PostAsFileV2Async` ep `HttpVersion.Version10`, khong doi duoc (van de #12). Ca hai: `cancellationTokenTime` **khong** bao trum buoc doc/mo file (van de #26) |
| **Truy van MongoDB (CRUD / aggregate)** | Ke thua `CoreMongoDB<TTable>` | Hai overload `FindAllPagingAsync(QueryContext<...>)` dung `Skip(pageNumber)` — **skip theo SO TRANG**, sai phan trang (van de #1); thu tu tham so cua chung cung nguoc `(pageSize, pageNumber)` (van de #2). **Moi ham update hardcode `IsUpsert = true`**, khong tat duoc (van de #3). `IsUpdateOneAsync(filter, entity)` co the ghi `IsDeleted = false` len document -> **hoi sinh ban ghi da xoa mem** (van de #4) |
| **Truy van SQL qua EF Core (1 entity)** | Ke thua `CoreSQL<TEntity, DBContextRead, DBContextWrite>` | Nhom `*SortDeletedAsync` **khong sap xep** du co chu "Sort" (van de #13). **Khong co API Delete** (van de #14). **Khong co phan trang / `OrderBy` / `Count` / `Include`** (van de #15). `FindByIdAsync` khong ap filter `IsDeleted` (van de #19) |
| **Truy van SQL qua EF Core khi entity doc khac entity ghi** | Ke thua `CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>` (file `CoreSQLTenant.cs`) | **Khong co logic multi-tenant nao** du ten file (van de #1). `MapUsingExpression` chi bind khi **trung ten + trung kieu + thuoc tinh nguon `CanWrite`**; ket hop `DbSet.Update` (full-row update) gay **ghi de `null`/`0` len cot dang co du lieu** — rui ro mat du lieu cao nhat cua KB (van de #5, #34) |
| **Truy van SQL raw qua Dapper (co repository)** | `CoreSQL.FindOneWithScriptAsync` / `FindAllWithScriptAsync` / `FindOneWithScalarScriptAsync` | Duoc boc `_pipelineRead` (co resilience). Nhung `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;` bi **ghep cung** vao script khi `CommandType.Text` -> **dirty read**, khong co tham so tat (van de #7). `FindAllWithScriptAsync` tra **`null`** khi script rong, trong khi cac `FindAll*` khac tra `[]` (van de #1) |
| **Truy van SQL raw truc tiep (khong qua repository)** | `IDapperSQLDBContext`: `ExecuteNonQueryAsync`, `GetOne<T>`, `GetAll<T>`, `GetOneExecute<T>` | **Khong co resilience, khong co log, khong co transaction** o tang nay (van de #1, #10, #13). `GetAll<T>` tra **`null`** (khong phai empty) khi query rong (van de #7). `GetAllExecuteAsync<T>` **trung hoan toan** hanh vi voi `GetAll<T>` (van de #9) |
| **Goi stored procedure co output parameter** | Ke thua `ExecuteSQLContext<TClass>` | `CommandType` bi hardcode `StoredProcedure` (khong doi duoc). Ten output parameter `"P_RESULT"` hardcode, khong kiem tra ton tai (van de #3). `ExecuteScalar` dung `QueryAsync` chu khong phai `ExecuteScalarAsync` -> buffer toan bo result set (van de #5). Khong doc duoc `ReturnValue` cua SP (van de #23) |
| **Ghi SQL raw (INSERT/UPDATE/DELETE)** | `CoreSQL.IsExecuteNonQueryAsync(scriptSQLQuery, context, transaction, parameters, ...)` | Day la **ngoai le duy nhat** trong `CoreSQL`: **khong** boc Polly, **khong** truyen `cancellationToken` xuong Dapper, **khong** goi `SaveChangesAsync` (nen khong dien cot audit, khong publish domain event) — `Data-SQL-CoreSQL.md` van de #4, #5; `Data-SQL-CoreSQL-TwoEntity.md` van de #7. Khong null-check `context`/`transaction` (van de #3) |
| **Can transaction (nhieu buoc ghi nguyen tu)** | `IUnitOfWork<DBContextWrite>`: `CreateTransactionAsync` -> ... -> `CommitAsync` | **Domain event duoc publish TRUOC khi commit** (`UnitOfWork.cs:74` truoc `:76`) -> handler chay tren du lieu chua commit (van de #1). `RollbackAsync` **khong** null-check `_transaction` -> `NullReferenceException` neu goi khi chua co transaction (van de #2). `IUnitOfWork<T>` **khong** ke thua `IAsyncDisposable` -> khong `await using` duoc (van de #13). `UnitOfWork` **khong co** cach truyen `AuditModel` (van de #8) |
| **Can transaction cho raw SQL** | `CoreSQL.IsExecuteNonQueryAsync` (nhan `DbConnection` + `DbTransaction` tu ngoai) | Tang Dapper thuan (`IDapperSQLDBContext`, `ExecuteSQLContext`) **khong ho tro transaction**: `CommandDefinition` khong bao gio duoc truyen `transaction` va moi lan goi mo connection rieng (`Data-SQL-Dapper.md` van de #13) |
| **Can resilience / retry cho SQL** | `SqlResiliencePolicyFactory.ConfigureReadPolicy` / `ConfigureWritePolicy` (ung dung tieu thu tu goi khi dang ky DI) | Hai ham la `static void`, **chi mutate `ResiliencePipelineBuilder`**, khong tra ve pipeline. **Trong repo nay khong co noi nao goi chung** (van de #21) -> pipeline truyen vao `CoreSQL` co retry hay khong phu thuoc hoan toan code DI ben ngoai. Hai ham **khong idempotent** (van de #25) |
| **Can resilience / retry cho MongoDB** | `MongoResiliencePolicyFactory` -> truyen `ResiliencePipeline` vao constructor `CoreMongoDB` | `CoreMongoDB` **khong tu tao policy**; khong co gi bao dam pipeline truyen vao la pipeline do factory nay tao. Buoc duyet cursor cua aggregate nam **ngoai** pipeline -> loi khi duyet cursor khong duoc retry (van de #6) |
| **Can resilience / retry cho HTTP** | **Khong co trong thu vien** | Ca `CallApi<TResponse>` va `CallApiWithHttp<TRequest, TResponse>` **khong co retry / circuit breaker / fallback**. Neu tang `IHttpClientFactory` ben ngoai gan pipeline retry, `cancellationTokenTime` se **bao trum toan bo cac lan retry** (`Utilizes-CallApiWithHttp.md` van de #20) |
| **Can dirty read cho toan bo luong doc EF Core** | `ReadUncommittedConnectionInterceptor` (ung dung tieu thu tu `AddInterceptors`) | Chi override `ConnectionOpenedAsync` -> connection mo **dong bo** khong duoc set isolation level (van de #18). **Khong reset** isolation level khi dong / tra connection ve pool (van de #19). Trong repo khong co dong `AddInterceptors` nao (van de #21) |
| **Can audit log chi tiet (old/new values)** | **Khong co trong thu vien** | `WriteDbContext.DetectChangesAudit` **luon `return []`** (`WriteDbContext.cs:356`); toan bo than ham nam trong `#region NOT SUPPORT` bi comment (`:201-353`). `AuditModel` **chi** dien cac cot audit tren chinh ban ghi, va **chi** voi entity implement `IBaseEntitySQL` |
| **Can publish domain event** | Entity ke thua **`Aggregate`** (khong phai chi implement `IAggregate`) + `WriteDbContext.SaveChangesAsync` | `DispatchDomainEvents` chi thu event tu `ChangeTracker.Entries<Aggregate>()` -> entity chi implement `IAggregate` se **mat event am tham** (`Data-SQL-CoreSQL-TwoEntity.md` van de #28). Khong goi `ClearDomainEvents()` -> event co the publish lai (`Data-SQL-UnitOfWork-DbContexts.md` van de #17). Mot handler nem exception lam **dut** vong publish (van de #19) |

---

## 4. Canh bao xuyen suot (cross-cutting)

Day la cac cai bay trai deu tren nhieu module. Doc muc nay truoc khi doc bat ky file chi tiet nao.

### 4.1 Audit va thoi gian

| # | Canh bao | Module bi anh huong |
|---|---|---|
| CC-1 | **`AuditModel` KHONG sinh audit log.** `DetectChangesAudit` luon `return []` (`WriteDbContext.cs:356`), nen `DispatchAuditLog` luon thoat ngay. `AuditModel` **chi** dien cac cot `Created*`/`Modified*` tren chinh ban ghi. Moi phat bieu kieu "mat audit trail khi dung raw SQL" chi dung o phan **cot audit tren entity**, khong phai audit log | `CoreSQL` (2 arity), `UnitOfWork`/`WriteDbContext` |
| CC-2 | **`CommonBaseConstant.DateTimeUtc()` mac dinh `addHour = 7`** -> `CreatedDate`/`ModifiedDate` luu **UTC+7**, khong phai UTC, du ten ham la `DateTimeUtc`. Khong cau hinh duoc mui gio | `CoreMongoDB`, `WriteDbContext` (nen ca `CoreSQL` 2 arity) |
| CC-3 | **Bat doi xung create vs update khi `audit` la `null`.** Luong create van dong dau `"Anonymous"`/`"0"`/`"FTEL"`; luong update **bo qua hoan toan**, khong stamp `Modified*`, khong canh bao | `CoreMongoDB` (`SetDataUpdatedDefault`), `WriteDbContext` (`OnBeforeSaveChanges`) |
| CC-4 | **Cac cot cua `IEntityFullCreatedAndModifiedBase<T>` khong bao gio duoc stamp** (`CreatedUserRegionId`/`LocationId`/`BranchId` va ban `Modified*`), du `CreatorInfo` da mang san `RegionId`/`BranchId`/`LocationId` | `WriteDbContext` -> anh huong `CoreSQL` (2 arity), `UnitOfWork` |

### 4.2 Toan ven du lieu

| # | Canh bao | Module bi anh huong |
|---|---|---|
| CC-5 | **Ghi de `null`/gia tri mac dinh len cot dang co du lieu.** Mongo: `MapUpdateDefinition` khong bo qua value type khong nullable -> `$set` luon chua `IsDeleted = false`, `int = 0`, `DateTime = default`. SQL 2-entity: `MapUsingExpression` bo qua thuoc tinh khong trung ten/kieu + `DbSet.Update` full-row -> cot tuong ung bi xoa | `CoreMongoDB` (#4), `CoreSQL<TFrom,TTo,...>` (#5, #34) |
| CC-6 | **`ChangeTracker.Clear()` chay vo dieu kien sau moi lan luu thanh cong**, ke ca tren `DBContextWrite` do caller truyen vao. Moi entity dang duoc context theo doi bi detach, thay doi chua luu bi mat | `WriteDbContext` (#7) -> `CoreSQL` (2 arity, cac overload nhan `context`), `UnitOfWork` |
| CC-7 | **Soft delete phu thuoc ten property `"IsDeleted"` hardcode dang chuoi**, khong cau hinh duoc. Entity thieu property nay -> loi **tai runtime**, khong phai loi bien dich | `CoreMongoDB` (#18), `CoreSQL` (#12), `CoreSQL<TFrom,TTo,...>` (#12) |
| CC-8 | **Khong co API Delete nao** trong tang repository SQL. Xoa cung phai qua raw SQL; xoa mem phai tu set co roi `UpdateAsync` (chiu rui ro full-row update) | `CoreSQL` (#14), `CoreSQL<TFrom,TTo,...>` (#13) |
| CC-9 | **`IEnumerable` dau vao bi enumerate nhieu lan** (`IsNullOrEmpty()` roi `foreach`/`AddRangeAsync`/`UpdateRange`). Nguon lazy khong buffer se bi thuc thi lai hoac cho ket qua khac | `CoreMongoDB` (#23), `CoreSQL` (#20), `CoreSQL<TFrom,TTo,...>` (#20), `WriteDbContext` (#24, #36) |

### 4.3 Resilience va cau hinh

| # | Canh bao | Module bi anh huong |
|---|---|---|
| CC-10 | **Repo khong wire resilience.** `SqlResiliencePolicyFactory.ConfigureReadPolicy`/`ConfigureWritePolicy` khong duoc goi o dau; `ReadUncommittedConnectionInterceptor` khong duoc `AddInterceptors`; khong co dang ky DI cho `IDapperSQLDBContext`, `IUnitOfWork`, `IDbContextFactory`. **Muc do hieu qua thuc te khong xac dinh duoc tu source code trong repo nay** | `SqlResiliencePolicyFactory` (#21), `CoreSQL` (#24, #25), `Dapper` (#18), `UnitOfWork` (#31) |
| CC-11 | **Circuit breaker duoc `Add` TRUOC retry** o ca hai factory (SQL va Mongo) -> thu tu thuc thi la `CircuitBreaker -> Retry -> lenh DB`. CB chi dem **mot** outcome cho moi lan goi pipeline (sau khi retry da can), khong dem tung attempt -> `MinimumThroughput` dat cham hon nhieu so voi truc giac | `SqlResiliencePolicyFactory` (#3), `CoreMongoDB` (#43) |
| CC-12 | **Toan bo tham so resilience la hardcode**, khong doc `IConfiguration`/`IOptions`/`appsettings`. Muon tinh chinh theo moi truong phai sua code va deploy lai | `SqlResiliencePolicyFactory` (#10), `CoreMongoDB` (`_aggregateOptions`, #27) |
| CC-13 | **Khong co retry noi tai cho HTTP.** Polly trong repo chi dung cho SQL va MongoDB; khong tim thay `AddPolicyHandler` nao cho `HttpClient` | `CallApi<TResponse>` (#17), `CallApiWithHttp<TRequest,TResponse>` (#20) |
| CC-14 | **Constructor khong null-check tham so nao** o hau het cac lop. Loi cau hinh DI (thieu `ILogger`, thieu `ResiliencePipeline`) chi bung ra o lan goi API dau tien duoi dang `NullReferenceException` kho truy nguyen | `CoreMongoDB` (#31), `CoreSQL` (#22), `Dapper` (#11), `UnitOfWork` (#34), `SqlResiliencePolicyFactory` (#14) |

### 4.4 Xu ly loi va quan sat

| # | Canh bao | Module bi anh huong |
|---|---|---|
| CC-15 | **Khong co `try/catch` o tang repository / tang Dapper.** Moi exception noi thang len caller va **khong duoc ghi log** tai lop do. `_logger` chi duoc dung cho guard clause | `CoreMongoDB`, `CoreSQL` (#21), `CoreSQL<TFrom,TTo,...>` (#21), toan bo 4 file Dapper |
| CC-16 | **Guard clause tra ve `null`/`[]`/`0`/`false` im lang**, va **khong nhat quan giua cac ham cung nhom**: `FindAllWithScriptAsync` tra `null` con `FindAllAsync` tra `[]`; `GetAll<T>` tra `null`; `FindAllWithAggregateAsync` tra `[]` khi `pipeline` la `null` ma khong log | `CoreSQL` (#1), `CoreSQL<TFrom,TTo,...>` (#3), `Dapper` (#6, #7), `CoreMongoDB` (#28) |
| CC-17 | **`FailLogic` ghi o `LogLevel.Information`** (`LoggerExtensions.cs:179-182`, EventId 107, category `BIZ_LOGIC`) — khong phai `Warning`/`Error`. He thong dat minimum level tu `Warning` se **mat hoan toan** dau vet cua cac truong hop guard chan | `CoreSQL` (#31), `CoreSQL<TFrom,TTo,...>` (#32), `CoreMongoDB` |
| CC-18 | **Nguoc lai: `UnitOfWork` ghi MOI su kien binh thuong o muc `Warning`** ("Create transaction.", "Commit transaction.", "Rollback transaction."), va khong truyen doi tuong `Exception` vao log loi -> mat stack trace | `UnitOfWork` (#20, #33), `SqlResiliencePolicyFactory` (#12, #13) |
| CC-19 | **`ProjectTo` (reflection) nuot loi va log ra `Console`, khong qua `ILogger`.** Hai overload xu ly loi **khac nhau**: overload doi tuong don nem `MissingMethodException` ra caller; overload `List<T>` bat loi va **loai phan tu khoi ket qua** -> `FindAll*Async<TDto>` tra ve danh sach thieu hoac `[]` ma khong co loi nao noi len | `CoreMongoDB` (#33), `CoreSQL` (#18), `CoreSQL<TFrom,TTo,...>` (#15, #31) |
| CC-20 | **Khoi `finally` khong duoc bao ve bang `try/catch`** trong ca hai lop HTTP: no doc `option.*` va serialize `option` + `result`. Bat ky exception phat sinh trong `finally` se **thay the** gia tri tra ve / exception goc | `CallApi<TResponse>` (#21), `CallApiWithHttp<TRequest,TResponse>` (#1, #21) |

### 4.5 Bao mat

| # | Canh bao | Module bi anh huong |
|---|---|---|
| CC-21 | **Token va toan bo payload response bi ghi vao log muc `Information` o MOI lan goi**, ke ca khi thanh cong: `option` (chua `Token`, `Client`) duoc serialize bang `System.Text.Json`, `result` (chua `data` da deserialize) duoc serialize bang `Newtonsoft.Json`. Khong co mask, khong gioi han kich thuoc | `CallApi<TResponse>` (#5, #5b), `CallApiWithHttp<TRequest,TResponse>` (#1) |
| CC-22 | **`Authorization` chi duoc gan khi `option.Token` khac rong va KHONG BAO GIO duoc xoa/reset.** Goi voi `Token` rong tren `HttpClient` da tung mang `Authorization` -> request van gui **token cu cua luong/nguoi dung truoc** | `CallApi<TResponse>` (#6b), `CallApiWithHttp<TRequest,TResponse>` (#3, #4) |
| CC-23 | **Header tuy chinh ghi vao `client.DefaultRequestHeaders` khong duoc remove** trong `PostWithHeadersAsJSonAsync` -> ro ri sang moi request sau tren cung `HttpClient`, khong thread-safe. Doi lap voi `GetAsJSonCustomHeaderAsync` (ghi vao `HttpRequestMessage.Headers`, dung pham vi) | `CallApi<TResponse>` (#2), `CallApiWithHttp<TRequest,TResponse>` (#5) |
| CC-24 | **Raw SQL khong duoc validate ngoai kiem tra rong/trang.** `DynamicParameters` chi bao ve **phan gia tri**, khong bao ve phan cau lenh. Neu caller ghep chuoi tu input nguoi dung -> **SQL injection** truoc khi vao tang Dapper | `CoreSQL` (#2), `CoreSQL<TFrom,TTo,...>` (#10), `Dapper` (muc 6.1) |
| CC-25 | **Dirty read bi ap dat cung.** `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;` duoc ghep cung vao moi truy van raw SQL dang `CommandType.Text`, **khong co tham so tat**. Khong phu hop nghiep vu tai chinh / doi soat | `CoreSQL` (#7), `CoreSQL<TFrom,TTo,...>` (#11), `ReadUncommittedConnectionInterceptor` |

### 4.6 Ten goi gay hieu nham

| # | Canh bao | Module bi anh huong |
|---|---|---|
| CC-26 | `*SortDeletedAsync` **khong sap xep** (chi loc `IsDeleted`); `FirstOrDefaultAsync` khong co `OrderBy` -> ban ghi tra ve **khong xac dinh** khi nhieu dong khop | `CoreSQL` (#13), `CoreSQL<TFrom,TTo,...>` |
| CC-27 | Ten file `CoreSQLTenant.cs` **khong co logic multi-tenant nao** (grep `enant` tren toan file = 0 ket qua). Vai tro thuc te chi la mapping `TEntityFrom` -> `TEntityTo` | `CoreSQL<TFrom,TTo,...>` (#1) |
| CC-28 | `ExecuteScalar` dung `QueryAsync` chu khong phai `ExecuteScalarAsync`; `GetAllExecuteAsync` khong ep `CommandType.StoredProcedure` du ten co `...Execute...`; `ConfigurationHelpers` **khong doc/build gia tri config nao**; `TypeConnection` la member vestigial khong duoc tham chieu o dau | `Dapper` (#2, #5, #9, #15) |
| CC-29 | 4 method `Post/Put/Patch...AsJSonAsync` cua `CallApi<TResponse>` **khong gui body** du ten co `AsJSon`; `IsCreateOneAsync`/`IsCreateManyAsync` **luon** tra `true`; `desiredTime` **khong** la nguong timeout (chi sinh log `Warning`) | `CallApi<TResponse>` (#1), `CoreMongoDB` (#7), ca hai lop HTTP |

---

## 5. Van de da biet toan he thong

Tong hop **251** van de da ghi nhan tren 8 tai lieu. Bang duoi la cac van de **muc do Critical va High**, sap xep Critical truoc. Danh sach day du nam o muc `Van de da biet` cua tung file.

| # | Van de | Module | Vi tri | Muc do |
|---|---|---|---|---|
| 1 | Hai overload `FindAllPagingAsync(QueryContext<...>)` dung `Skip(pageNumber)` — **skip theo SO TRANG**, khong nhan voi `pageSize`; cac overload khac dung `Skip((pageNumber - 1) * pageSize)`. Cung mot ten ham co hai ngu nghia phan trang | `CoreMongoDB<TTable>` | `CoreMongoDB.cs:149`, `:194` | **Critical** |
| 2 | Toan bo 6 ham update hardcode **`IsUpsert = true`**, khong co cach tat. `filter` sai -> MongoDB **tao document moi** va ham tra **`true`** | `CoreMongoDB<TTable>` | `CoreMongoDB.cs:638`, `715`, `792`, `874`, `947`, `1024` | **Critical** |
| 3 | `MapUpdateDefinition` chi bo qua property `null`; value type khong nullable **luon** vao `$set` -> `IsUpdateOneAsync(filter, entity)` voi entity dien mot phan ghi `IsDeleted = false` len document, **"hoi sinh" ban ghi da xoa mem** | `CoreMongoDB<TTable>` | `ProjectToExtensions.cs:293-304`; goi tai `CoreMongoDB.cs:622`, `863`, `940` | **Critical** |
| 4 | `MapUsingExpression` chi bind khi trung ten + trung kieu + thuoc tinh nguon `CanWrite`; thuoc tinh khong thoa bi bo qua **am tham**. Ket hop `DbSet.Update` (full-row update) -> **ghi de `null`/`0` len cot dang co du lieu** | `CoreSQL<TFrom,TTo,...>` | `ProjectToExtensions.cs:157-160`; update tai `CoreSQLTenant.cs:939`, `997`, `1076`, `1144` | **Critical** |
| 5 | `MapUsingExpression` doi hoi thuoc tinh **nguon** phai `CanWrite` moi map -> moi computed property / `{ get; }` cua `TEntityFrom` bi bo qua, cot tuong ung nhan gia tri mac dinh va bi ghi de len DB | `CoreSQL<TFrom,TTo,...>` | `ProjectToExtensions.cs:157-163` | **Critical** |
| 6 | **Domain event duoc publish TRUOC khi transaction commit.** `CommitAsync` goi `SaveChangeAsync` (-> `DispatchDomainEvents`) o `:74` roi moi `CommitAsync` o `:76`. Commit that bai + rollback -> event **da phat** cho du lieu khong ton tai | `UnitOfWork<DBContextWrite>` | `UnitOfWork.cs:74-76`; `WriteDbContext.cs:88-91` | **Critical** |
| 7 | Log tracing serialize **toan bo `option`** (gom `Token`) bang `System.Text.Json` va **toan bo `result`** (payload response) bang `Newtonsoft.Json`, o muc `Information`, **moi** lan goi | `CallApiWithHttp<,>`, `CallApi<TResponse>` | `CallApiWithHttp.cs:131-133` va 10 vi tri tuong tu; `:1577`, `:1689`, ... | **Critical** (bao mat) |
| 8 | `PostAsJSonAsync`, `PostWithHeadersAsJSonAsync`, `PutAsJSonAsync`, `PatchAsJSonAsync` tao `HttpRequestMessage` **khong set `Content`** -> gui request **khong co body**, khong co `Content-Type`, du ten chua `AsJSon` | `CallApi<TResponse>` | `CallApiWithHttp.cs:1847`, `:2098`, `:2212`, `:2440` | **Critical** |
| 9 | `PostAsFileAsync` goi `ReadAsync` **mot lan** va bo qua so byte doc duoc; ep `(int)fileStream.Length` -> **noi dung file gui len co the bi thieu**; file > 2 GB tran `int` | `CallApiWithHttp<,>` | `CallApiWithHttp.cs:663`, `665-666` | **Critical** |
| 10 | **`ChangeTracker.Clear()` duoc goi vo dieu kien** sau moi lan luu thanh cong, ke ca tren `DBContextWrite` do caller cung cap -> detach **moi** entity context dang theo doi, mat thay doi chua luu, pha vo unit-of-work nhieu buoc | `WriteDbContext<TContext>`, `CoreSQL` (2 arity) | `WriteDbContext.cs:433` (guard o `:435-438`) | **High** |
| 11 | **`AuditModel` khong sinh audit log.** `DetectChangesAudit` luon `return []`; than ham nam trong `#region NOT SUPPORT` bi comment. `DispatchAuditLog` luon thoat ngay | `WriteDbContext<TContext>`, `CoreSQL` (2 arity) | `WriteDbContext.cs:356` (dead code `:201-353`); `:373-378` | **High** |
| 12 | `EnsureSuccessOrException` **khong nem** voi 4xx/5xx (doan `EnsureSuccessStatusCode()` bi comment) -> luong van deserialize body loi thanh `TResponse`. Co the nhan `data != null` trong khi `Succeeded = false` | `CallApiWithHttp<,>`, `CallApi<TResponse>` | `HttpClientUtilizes.cs:401-416`, dac biet `:412-415` | **High** |
| 13 | `ReadAsStreamAsync` **khong nem** khi deserialize that bai — chi log `Error` va tra `default` -> co the `(data == null, Succeeded == true)` voi HTTP 200 body sai schema | `CallApiWithHttp<,>`, `CallApi<TResponse>` | `HttpClientUtilizes.cs:317-341` | **High** |
| 14 | Header ghi vao `client.DefaultRequestHeaders` **khong bao gio duoc remove**; goi lan hai cung key -> cong don gia tri hoac `InvalidOperationException` -> `Code = 500`. Khong thread-safe | `CallApi<TResponse>` (#2), `CallApiWithHttp<,>` (#5) | `CallApiWithHttp.cs:2091`; `:975-982` | **High** |
| 15 | `Authorization` chi gan khi `Token` khac rong va **khong bao gio duoc xoa** -> request "anonymous" tren `HttpClient` dung chung van gui token cu | `CallApi<TResponse>`, `CallApiWithHttp<,>` | `HttpClientUtilizes.cs:354-357`; `CallApiWithHttp.cs:1967-1971` | **High** (bao mat) |
| 16 | `ConfigHttpClient` mutate `HttpClient` dung chung moi lan goi: gan lai `BaseAddress` (-> `InvalidOperationException` sau khi client da gui request), `Add` them `Accept` (tich luy vo han), ghi de `Authorization` | `CallApi<TResponse>`, `CallApiWithHttp<,>` | `HttpClientUtilizes.cs:343-360` | **High** |
| 17 | Query string duoc build bang **hai co che khac nhau** (`ParseModelToQueryString` vs `HttpClientUtilizes.ToQueryString`): khac ve doc `JsonPropertyNameAttribute`, ve cach encode, ve `BindingFlags`, ve xu ly gia tri whitespace -> **cung mot model sinh ra hai URL khac nhau** | `CallApiWithHttp<,>` | `CallApiWithHttp.cs:1429-1463` vs `HttpClientUtilizes.cs:111-132` | **High** |
| 18 | Response khong co `Content-Type` (dien hinh `204 No Content`) bi `ResponseResult` nem `CustomException` -> bao loi sai du thao tac da thanh cong | `CallApiWithHttp<,>`, `CallApi<TResponse>` | `HttpClientUtilizes.cs:364-372` | **High** |
| 19 | `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;` ghep **cung** vao moi truy van raw SQL `CommandType.Text`, khong co tham so tat -> **dirty read** toan bo duong doc raw SQL | `CoreSQL` (2 arity) | `CoreSQL.cs:82-90`, `:132-140`, `:182-190`; `CoreSQLTenant.cs:92`, `144`, `194` | **High** |
| 20 | Raw SQL khong duoc validate ngoai kiem tra rong; script duoc noi suy truc tiep vao chuoi SQL -> **SQL injection** neu caller ghep input nguoi dung | `CoreSQL` (2 arity), tang Dapper | `CoreSQL.cs:75`, `:125`, `:175`, `:228`; `CoreSQLTenant.cs:82`, `134`, `184`, `237` | **High** |
| 21 | `IsExecuteNonQueryAsync` la method **duy nhat khong duoc boc Polly pipeline**, **khong** truyen `cancellationToken` xuong Dapper, va **khong** null-check `context`/`transaction` | `CoreSQL` (2 arity) | `CoreSQL.cs:217-240`, `:235`; `CoreSQLTenant.cs:244-249` | **High** |
| 22 | `FindAllWithScriptAsync` tra **`null`** khi script rong, trong khi 4 ham `FindAll*` con lai tra `[]` -> `NullReferenceException` khi caller dung chung mot cach xu ly | `CoreSQL` (2 arity) | `CoreSQL.cs:129` vs `:510`, `548`, `583`, `617` | **High** |
| 23 | `GetAll<T>` va `GetAllExecuteAsync<T>` tra **`null`** (khong phai empty collection) khi query rong; XML doc lai noi tra `Enumerable.Empty{T}` | tang Dapper | `DapperSQLDBContext.cs:110`, `:185`; doc `IDapperSQLDBContext.cs:79` | **High** |
| 24 | Tang Dapper **khong co resilience nao** (khong Polly, khong retry, khong CB) va **khong ghi log o bat ky nhanh nao**. Goi truc tiep -> loi transient (deadlock `1205`, connection broken `-1`, timeout `-2`) that bai ngay | tang Dapper | `DapperSQLDBContext.cs:28-200`; `ExecuteSQLContext.cs:33-85` | **High** |
| 25 | `ConnectionLevelSqlErrors` la tap con thuc su cua `RetryableSqlErrors`: `-2`, `1205`, `20000` **khong** co trong tap connection-level -> DB qua tai gay timeout/deadlock lien tuc van tieu ton tron bo retry ma **circuit breaker khong bao gio mo** | `SqlResiliencePolicyFactory` | `SqlResiliencePolicyFactory.cs:24-36` vs `:42-51` | **High** |
| 26 | `MinimumThroughput = 10` + `SamplingDuration = 15s` o `ConfigureWritePolicy` -> service co tan suat ghi thap hon 10 lan goi/15 giay **khong bao gio** mo circuit breaker | `SqlResiliencePolicyFactory` | `SqlResiliencePolicyFactory.cs:162`, `:164` | **High** |
| 27 | `IsConnectionLevel` khong nhan dien `TimeoutException`; `SocketException` chi duoc kiem tra tren exception ngoai cung; `UnwrapSqlException` khong duyet `AggregateException.InnerExceptions` -> nhieu loi that su khong duoc retry va khong lam mo CB | `SqlResiliencePolicyFactory` | `:251-259`, `:234`, `:258`, `:278` | **High** |
| 28 | `RollbackAsync` **khong null-check `_transaction`** -> goi trong `catch`/`finally` phong ve khi chua co transaction nem `NullReferenceException`, **che mat exception nghiep vu goc**. Mau thuan voi `CommitAsync` (noi **co** null-check tai `:89`) | `UnitOfWork<DBContextWrite>` | `UnitOfWork.cs:109-116` (cu the `:111`) | **High** |
| 29 | `CommitAsync` khi chua co transaction: `SaveChanges` **da thanh cong** (`:74`) roi `NullReferenceException` o `:76`, khong rollback duoc (guard `:89` chan), cuoi cung `throw;` -> **du lieu da persist** nhung caller nhan exception -> de retry/ghi trung | `UnitOfWork<DBContextWrite>` | `UnitOfWork.cs:70-101` | **High** |
| 30 | `ModifiedUser`/`ModifiedDate`/`ModifiedUserCode`/`ModifiedUserOrganization` **khong bao gio** duoc gan khi luu qua `UnitOfWork` hoac `SaveChangesAsync(bool, ct)` / `SaveChanges(bool)`. `UnitOfWork` **khong co** cach truyen `AuditModel` | `UnitOfWork`, `WriteDbContext` | `WriteDbContext.cs:170-173`; `UnitOfWork.cs:155` | **High** |
| 31 | `ReadDbContext<TContext>` **khong chan ghi**: khong ghi de `SaveChanges*`, khong dat `QueryTrackingBehavior.NoTracking`, khong gan interceptor -> ve ky thuat co the ghi that vao DB qua context "read", khong qua audit stamping, khong dispatch domain event | `ReadDbContext<TContext>` | `ReadDbContext.cs:12-32` (toan file) | **High** |
| 32 | `DispatchDomainEvents` chi thu event tu `ChangeTracker.Entries<Aggregate>()` (**lop truu tuong `Aggregate`**), trong khi `CoreSQL<TFrom,TTo,...>` chuyen tiep event khi `entityConvert is IAggregate` -> entity implement `IAggregate` ma khong ke thua `Aggregate` **mat su kien am tham** | `WriteDbContext`, `CoreSQL<TFrom,TTo,...>` | `WriteDbContext.cs:421`; `CoreSQLTenant.cs:651-654` va 7 vi tri khac | **High** |
| 33 | Exception trong mot domain event handler lam **dut** vong publish — cac event con lai bi bo. Khong retry, khong outbox, khong dead-letter | `WriteDbContext<TContext>` | `WriteDbContext.cs:444-447` | **High** |
| 34 | `BulkWriteAsync` chi xet `IsAcknowledged`, `MatchedCount`, `Upserts.Count`; **bo qua `InsertedCount` va `DeletedCount`** -> lo bulk chi gom insert hoac chi gom delete tra **`false`** du da thuc thi thanh cong | `CoreMongoDB<TTable>` | `CoreMongoDB.cs:1407-1416` | **High** |
| 35 | Nhom `FindAllAsync` / `FindAllSortDeletedAsync` (Mongo) va toan bo nhom `FindAll*` (SQL) **khong co `Limit`/`OrderBy`/`Skip`/phan trang** -> nap toan bo tap ket qua vao bo nho, rui ro `OutOfMemoryException` va command timeout | `CoreMongoDB`, `CoreSQL` (2 arity) | `CoreMongoDB.cs:403-404`, `433-434`, `457-458`, `484-485`; `CoreSQL.cs:483-618` | **High** |
| 36 | Hai `Lazy<IMongoCollection<TTable>>` dung `LazyThreadSafetyMode.ExecutionAndPublication` (mac dinh) — **cache ca exception**. Loi o lan truy cap dau tien lam instance repository **hong vinh vien** | `CoreMongoDB<TTable>` | `CoreMongoDB.cs:42-48` | **High** |
| 37 | Doc va ghi di qua **hai `IMongoDatabase` khac nhau**, khong co `IClientSessionHandle` / causal consistency -> `FindOneAsync` ngay sau `IsUpdateOneAsync` **co the khong thay** du lieu vua ghi (read-your-write khong duoc bao dam) | `CoreMongoDB<TTable>` | `CoreMongoDB.cs:42-48` | **High** |
| 38 | 3 overload `FindAllWithAggregateAsync` chay tren `_dbReadContext` -> pipeline chua `$out`/`$merge` se **ghi du lieu qua ket noi doc**; class khong kiem tra noi dung pipeline | `CoreMongoDB<TTable>` | `CoreMongoDB.cs:1274`, `1318`, `1364` | **High** |
| 39 | `ProjectTo` reflection **nuot loi** va log ra `Console`, khong qua `ILogger`; hai overload xu ly loi **khac nhau** -> `FindOneAsync<TDto>` nem exception con `FindAllAsync<TDto>` tra list rong nhu the "khong co du lieu" | `CoreMongoDB`, `CoreSQL` (2 arity) | `ProjectToExtensions.cs:29` vs `:90-123` | **High** |
| 40 | `FailLogic` ghi o **`LogLevel.Information`** — la nguon thong tin **duy nhat** ve cac truong hop tra `default`/`null`/`0`/`false` do guard. He thong dat minimum level tu `Warning` se mat hoan toan dau vet nay | `CoreSQL` (2 arity), `CoreMongoDB` | `LoggerExtensions.cs:179-182` (EventId 107, `BIZ_LOGIC`) | **High** |
| 41 | Ten file `CoreSQLTenant.cs` gay hieu nham: **khong co bat ky logic multi-tenant nao** (grep `enant` = 0 ket qua) -> developer/AI co the bo qua viec tu loc theo tenant, gay ro ri du lieu giua cac don vi | `CoreSQL<TFrom,TTo,...>` | `CoreSQLTenant.cs` (toan file); class khai bao tai `:17` la `CoreSQL` | **High** |
| 42 | `_pipelineWrite` retry bao boc `SaveChangesAsync` ma **khong co idempotency key** va **khong kiem tra** co transaction do caller dang mo hay khong -> co the insert/update trung, hoac retry ben trong mot transaction da bi abort | `CoreSQL` (2 arity) | `CoreSQL.cs:650-655` va 7 vi tri khac; `SqlResiliencePolicyFactory.cs:194-210` | **High** |
| 43 | `ReadUncommittedConnectionInterceptor` chi override `ConnectionOpenedAsync` (khong override ban dong bo) va **khong reset** isolation level khi dong / tra connection ve pool | `ReadUncommittedConnectionInterceptor` | `ReadUncommittedConnectionInterceptor.cs:23`; toan file | **High** |
| 44 | Ten output parameter `"P_RESULT"` hardcode, khong constant, khong cau hinh, khong kiem tra ton tai truoc khi doc; khong co dong nao doc `ReturnValue` cua stored procedure | `ExecuteSQLContext<TClass>` | `ExecuteSQLContext.cs:82` | **High** |
| 45 | `cancellationTokenTime` **khong bao trum** giai doan chuan bi noi dung file trong hai method upload — `CancellationTokenSource` chi duoc tao **sau** khi doc/mo file xong | `CallApiWithHttp<,>` | `CallApiWithHttp.cs:655-673` roi `702-703`; `817-834` roi `859-860` | **High** |
| 46 | `cancellationToken.ThrowIfCancellationRequested()` va viec build URL/body nam **ngoai `try`** -> pha vo hop dong "khong bao gio nem": `OperationCanceledException`, loi serialize JSON, loi reflection query string **nem thang ra caller** | `CallApiWithHttp<,>`, `CallApi<TResponse>` | `CallApiWithHttp.cs:38`, `154`, `272`, ... | **High** |
| 47 | `IsCreateOneAsync` / `IsCreateManyAsync` **luon** `return true` sau khi goi driver, khong kiem tra ket qua nao -> `bool` tra ve khong mang thong tin nghiep vu | `CoreMongoDB<TTable>` | `CoreMongoDB.cs:1112`, `:1166` | **High** |
| 48 | Vong lap duyet cursor cua aggregate nam **ngoai** `_pipelineRead` va **thieu `.ConfigureAwait(false)`** -> loi khi duyet cursor khong duoc retry, va rui ro deadlock khi co `SynchronizationContext` | `CoreMongoDB<TTable>` | `CoreMongoDB.cs:1280`, `1324`, `1370` | **High** |
| 49 | **Repo khong wire resilience/interceptor/DI**: khong co noi goi `ConfigureReadPolicy`/`ConfigureWritePolicy`, khong co `AddInterceptors`, khong dang ky `IDapperSQLDBContext`/`IUnitOfWork`/`IDbContextFactory`, khong co lop con nao ke thua `CoreSQL<,,>`/`CoreSQL<,,,>`/`ExecuteSQLContext<>` -> **hieu qua thuc te khong xac dinh duoc tu source code trong repo nay** | Toan he thong | grep toan repo (tru `.claude/worktrees`) | **High** |
| 50 | Constructor **khong null-check** tham so nao o hau het cac lop -> loi cau hinh DI chi lo ra o lan goi API dau tien duoi dang `NullReferenceException` kho truy nguyen | Toan he thong | `CoreMongoDB.cs:32-53`; `CoreSQL.cs:35-53`; `UnitOfWork.cs:7-9`; `DapperSQLDBContext.cs:14-16` | **High** |

### 5.1 Phan bo van de theo tai lieu

| Tai lieu | So van de da ghi nhan |
|---|---|
| `Data-MongoDB-CoreMongoDB.md` | 43 |
| `Data-SQL-UnitOfWork-DbContexts.md` | 36 |
| `Data-SQL-CoreSQL-TwoEntity.md` | 34 |
| `Data-SQL-CoreSQL.md` | 32 |
| `Utilizes-CallApiWithHttp.md` | 29 |
| `Data-SQL-Resilience.md` | 28 |
| `Utilizes-CallApi.md` | 26 |
| `Data-SQL-Dapper.md` | 23 |
| **Tong** | **251** |
