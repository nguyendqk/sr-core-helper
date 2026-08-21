# CoreMongoDB&lt;TTable&gt; / ICoreMongoDB&lt;TTable&gt;

> Nguon: `FTELSRCore.Shared/Data/MongoDB/Core/CoreMongoDB.cs`, `FTELSRCore.Shared/Data/MongoDB/Core/ICoreMongoDB.cs`
> Loai: `ICoreMongoDB<TTable>` = interface; `CoreMongoDB<TTable>` = abstract class (implement `ICoreMongoDB<TTable>`), rang buoc `where TTable : class`
> Cap nhat theo commit: `2262829`

## 1. Tong quan

`CoreMongoDB<TTable>` la generic repository truu tuong cho MongoDB, nam o tang Data Access cua thu vien `FTELSRCore.Shared`. Class gom 32 method `public virtual async` bao boc cac lenh CRUD/aggregate cua `MongoDB.Driver` (v3.10.0) va chay chung qua hai `ResiliencePipeline` cua Polly (v8.7.0): `_pipelineRead` cho luong doc va `_pipelineWrite` cho luong ghi (CoreMongoDB.cs:18-20).

Class tach bach hai collection: `_dbReadContext` va `_dbWriteContext`, ca hai la `Lazy<IMongoCollection<TTable>>` duoc tao tu hai `IMongoDatabase` khac nhau truyen vao constructor (CoreMongoDB.cs:22-24, 42-48). Nho vay repository ho tro mo hinh read/write splitting.

Class khong the khoi tao truc tiep (`abstract`, constructor `protected`) — phai ke thua roi truyen `collectionName` va cac dependency vao constructor base.

> [!IMPORTANT]
> Toan bo file `CoreMongoDB.cs` KHONG co bat ky tu khoa `try` / `catch` / `finally` nao (kiem tra toan file, khong tim thay tu khoa nao trong so ba tu khoa nay) — tuc **khong co diem bat exception nao**. Ba khai bao `using IAsyncCursor<...> cursor = ...` (dong 1269, 1313, 1359) van sinh ra `try` / `finally` an de `Dispose` cursor, nhung day chi la giai phong tai nguyen, khong phai bat loi. Moi exception phat sinh tu `MongoDB.Driver` se di qua policy cua Polly roi **nem lai** cho caller. Cac gia tri `false` ma cac ham `Is*Async` tra ve chi den tu **validate dau vao** hoac **ket qua nghiep vu** (`MatchedCount`, `DeletedCount`, `IsAcknowledged`), khong bao gio den tu viec bat exception.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Dem document theo `FilterDefinition<TTable>` hoac `Expression<Func<TTable, bool>>` (`CountAllAsync`) | Khong tra ve tong so ban ghi kem theo danh sach phan trang — phai goi `CountAllAsync` rieng |
| Dem/tim theo trang thai xoa mem qua nhom `*SortDeletedAsync` (AND them dieu kien `IsDeleted == isDeleted`) | Khong co ham xoa mem (soft delete). `IsDeleteOneAsync` / `IsDeleteManyAsync` la xoa vat ly (`DeleteOneAsync` / `DeleteManyAsync`) |
| Phan trang qua 7 overload `FindAllPagingAsync` | Khong chuan hoa cong thuc `Skip` giua cac overload (xem muc 3) va khong validate `pageNumber` / `pageSize` |
| Anh xa entity sang DTO bang reflection (`ProjectTo<TTable, TDto>`) | Khong dung AutoMapper hay expression-tree co cache; anh xa bang `PropertyInfo.SetValue` moi lan goi |
| Insert 1 hoac nhieu document (`IsCreateOneAsync`, `IsCreateManyAsync`) | Khong tra ve `_id` cua document vua tao; chi tra `bool` |
| Update 1 hoac nhieu document, **luon bat upsert** (`IsUpdateOneAsync`, `IsUpdateManyAsync`) | Khong co overload nao cho phep TAT upsert. Muon update khong upsert phai tu dung `BulkWriteAsync` |
| Bulk write hon hop qua `BulkWriteAsync` (caller tu tao `WriteModel<TTable>`) | `BulkWriteAsync` tra `false` khi bulk chi gom insert/delete (xem muc 3, #10) |
| Aggregation pipeline dang `PipelineDefinition<TTable, TResult>` va `BsonDocument[]`, loai bo phan tu `null` khoi ket qua | Khong ho tro transaction / `IClientSessionHandle` (khong co overload nao nhan session) |
| Tu dong dong dau audit khi tao/sua (`SetDataCreatedDefault` / `SetDataUpdatedDefault`) | Khong dong dau audit khi `audit` la `null` o luong **update** (xem muc 1.4) |
| Chay moi lenh driver qua `ResiliencePipeline` **duoc inject tu ben ngoai** (`ExecuteAsync`) | **Khong tu cau hinh retry / circuit breaker**: trong `CoreMongoDB.cs` khong co dong code nao tao policy (khong co `AddRetry` / `AddCircuitBreaker`). Co retry hay khong hoan toan phu thuoc pipeline ma caller truyen vao ctor. Ngoai ra khong bao boc buoc duyet cursor cua aggregate (`cursor.MoveNextAsync`) trong pipeline (xem muc 3, #6) |
| Huy tac vu qua `CancellationToken` (`ThrowIfCancellationRequested` o dau moi ham — 32 lan) | Khong tu dinh nghia timeout tong; `MaxTime` chi ap dung cho aggregate (30 giay mac dinh) |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `MongoDB.Driver` (3.10.0) | `IMongoCollection<TTable>`, `IMongoDatabase`, `FilterDefinition<TTable>`, `SortDefinition<TTable>`, `UpdateDefinition<TTable>`, `UpdateOptions`, `UpdateOneModel<TTable>`, `WriteModel<TTable>`, `BulkWriteOptions`, `AggregateOptions`, `PipelineDefinition<,>`, `IAsyncCursor<>`, `UpdateResult`, `DeleteResult`, `BulkWriteResult<TTable>` |
| `MongoDB.Bson` | `BsonDocument` cho cac overload aggregate nhan `BsonDocument[]` |
| `Polly` (8.7.0) | `ResiliencePipeline` — `_pipelineRead` / `_pipelineWrite`, goi qua `ExecuteAsync(callback, cancellationToken)` |
| `ILogger<CoreMongoDB<TTable>>` | Ghi log nghiep vu that bai qua extension `FailLogic` (`FTELSRCore.Shared/Extensions/Loggers/LoggerExtensions.cs:358`) |
| `FTELSRCore.Extensions.ProjectToExtensions` | `QueryContext<TTable, TDto>` (record), `ProjectTo<TEntity, TDto>`, `MapUpdateDefinition`, `SetDataCreatedDefault`, `SetDataUpdatedDefault` |
| `FTELSRCore.Extensions.PrecateBuilderExtensions` | `AddIsDeleted<T>(bool)` va `And<T>(...)` de gan dieu kien xoa mem vao filter |
| `FTELSRCore.Helpers.CollectionHelpers` | `IsNullOrEmpty<T>(this IEnumerable<T>)` — `null` cung tra `true` (`CollectionHelpers.cs:14-37`) |
| `FTELSRCore.Helpers.JSonParseHelpers` | `ToJSon<T>()` de serialize doi tuong vao message log; tra `string.Empty` khi doi tuong `null` (`JSonParseHelpers.cs:21`) |
| `FTELSRCore.Data.MongoDB.Helpers.Policies.MongoResiliencePolicyFactory` | Factory cau hinh san policy doc/ghi. **Luu y**: `CoreMongoDB` nhan `ResiliencePipeline` qua constructor, khong tu goi factory — khong co gi bao dam pipeline duoc truyen vao la pipeline do factory nay tao |
| `System.Linq.Expressions` | `Expression<Func<TTable, bool>>` cho cac overload dung LINQ filter |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `CoreMongoDB(...)` ctor | Khoi tao | Nhan `collectionName`, 2 `IMongoDatabase`, `ILogger`, 2 `ResiliencePipeline`; khong validate null |
| `CountAllAsync(FilterDefinition<TTable>, CancellationToken)` | Doc / Count | Dem document; `filter == null` quy ve `Filter.Empty` |
| `CountAllAsync(Expression<Func<TTable,bool>>, CancellationToken)` | Doc / Count | Dem document theo LINQ predicate; khong guard null |
| `CountAllSortDeletedAsync(Expression, bool, CancellationToken)` | Doc / Count | Dem document, AND them `IsDeleted == isDeleted` |
| `FindAllPagingAsync<TDto>(FilterDefinition, SortDefinition, int, int, CancellationToken)` | Doc / Paging | `Skip((pageNumber - 1) * pageSize)`, sau do `ProjectTo` sang `TDto` |
| `FindAllPagingAsync<TDto>(QueryContext<TTable,TDto>, int, int?, CancellationToken)` | Doc / Paging | Projection server-side; **`Skip(pageNumber)`** — skip theo SO TRANG |
| `FindAllPagingAsync(QueryContext<TTable,TTable>, int, int?, CancellationToken)` | Doc / Paging | Khong projection; **`Skip(pageNumber)`** — skip theo SO TRANG |
| `FindAllPagingAsync<TDto>(Expression, int, int, CancellationToken)` | Doc / Paging | `Skip((pageNumber - 1) * pageSize)`, khong sort |
| `FindAllPagingAsync<TDto>(Expression, SortDefinition, int, int, CancellationToken)` | Doc / Paging | `Skip((pageNumber - 1) * pageSize)`, co sort |
| `FindAllPagingAsync(Expression, int, int, CancellationToken)` | Doc / Paging | Tra `List<TTable>` truc tiep tu driver |
| `FindAllPagingAsync(Expression, SortDefinition, int, int, CancellationToken)` | Doc / Paging | Tra `List<TTable>` truc tiep tu driver, co sort |
| `FindAllAsync<TDto>(Expression, CancellationToken)` | Doc / List | Lay tat ca ban ghi khop filter, map sang `TDto` |
| `FindAllAsync(Expression, CancellationToken)` | Doc / List | Lay tat ca ban ghi khop filter, kieu `TTable` |
| `FindAllSortDeletedAsync<TDto>(Expression, bool, CancellationToken)` | Doc / List | Nhu tren, AND `IsDeleted == isDeleted`, map `TDto` |
| `FindAllSortDeletedAsync(Expression, bool, CancellationToken)` | Doc / List | Nhu tren, AND `IsDeleted == isDeleted`, kieu `TTable` |
| `FindOneAsync<TDto>(Expression, CancellationToken)` | Doc / Single | `FirstOrDefaultAsync` roi map `TDto`; `null` neu khong tim thay |
| `FindOneAsync(Expression, CancellationToken)` | Doc / Single | `FirstOrDefaultAsync`; `null` neu khong tim thay |
| `FindOneSortDeletedAsync<TDto>(Expression, bool, CancellationToken)` | Doc / Single | Nhu tren, AND `IsDeleted == isDeleted` |
| `FindOneSortDeletedAsync(Expression, bool, CancellationToken)` | Doc / Single | Nhu tren, AND `IsDeleted == isDeleted` |
| `IsCreateOneAsync(TTable, AuditModel, CancellationToken)` | Ghi / Create | `InsertOneAsync`; **luon tra `true`** neu khong nem exception |
| `IsCreateManyAsync(IEnumerable<TTable>, AuditModel, CancellationToken)` | Ghi / Create | `InsertManyAsync`; **luon tra `true`** neu khong nem exception |
| `IsUpdateOneAsync(Expression, TTable, AuditModel, CancellationToken)` | Ghi / Update | `UpdateOneAsync` va **`IsUpsert = true`**; tu map entity thanh `$set` |
| `IsUpdateOneAsync(Expression, UpdateDefinition, AuditModel, CancellationToken)` | Ghi / Update | `UpdateOneAsync` va **`IsUpsert = true`**; dung `UpdateDefinition` caller cung cap |
| `IsUpdateManyAsync(Expression, TTable, AuditModel, CancellationToken)` | Ghi / Update | `UpdateManyAsync` va **`IsUpsert = true`** |
| `IsUpdateManyAsync(Expression, UpdateDefinition, AuditModel, CancellationToken)` | Ghi / Update | `UpdateManyAsync` va **`IsUpsert = true`** |
| `IsUpdateManyAsync(List<(Expression, TTable)>, AuditModel, CancellationToken)` | Ghi / Bulk | `BulkWriteAsync` voi `UpdateOneModel<TTable> { IsUpsert = true }` moi phan tu |
| `IsUpdateManyAsync(List<(Expression, UpdateDefinition)>, CancellationToken)` | Ghi / Bulk | Nhu tren; **khong co tham so `audit`** |
| `IsDeleteOneAsync(Expression, CancellationToken)` | Ghi / Delete | `DeleteOneAsync`; `false` khi `filter == null` hoac `DeletedCount == 0` |
| `IsDeleteManyAsync(Expression, CancellationToken)` | Ghi / Delete | `DeleteManyAsync`; `false` khi `filter == null` hoac `DeletedCount == 0` |
| `FindAllWithAggregateAsync<TResult>(PipelineDefinition<TTable,TResult>, AggregateOptions, CancellationToken)` | Doc / Aggregate | `[]` khi `pipeline == null`; loc bo phan tu `null` |
| `FindAllWithAggregateAsync(BsonDocument[], AggregateOptions, CancellationToken)` | Doc / Aggregate | Ket qua map ve `TTable` |
| `FindAllWithAggregateAsync<TResult>(BsonDocument[], AggregateOptions, CancellationToken)` | Doc / Aggregate | Ket qua map ve `TResult` tuy y |
| `BulkWriteAsync(IEnumerable<WriteModel<TTable>>, BulkWriteOptions, CancellationToken)` | Ghi / Bulk | Caller tu tao `WriteModel`; **khong guard null/empty**; `true` chi khi `MatchedCount > 0` hoac co `Upserts` |

### 1.4 Hanh vi cac ham ho tro (doc tu source, khong suy dien)

Nguon: `FTELSRCore.Shared/Extensions/ProjectToExtensions.cs`, `FTELSRCore.Shared/Extensions/PrecateBuilderExtensions.cs`, `FTELSRCore.Shared/Abstractions/Entities/BaseEntityMongoDB.cs`.

#### `SetDataCreatedDefault<TTable>(TTable entity, AuditModel audit = default)` — `ProjectToExtensions.cs:478-510`

- Guard duy nhat: `if (entity is null) return entity;` (dong 480). **Khong guard `audit is null`**.
- Lay `userName` = `audit?.CreatorInfo?.Name`, fallback `CommonBaseConstant.Anonymous` = `"Anonymous"`; `userCode` fallback `AnonymousCode` = `"0"`; `organization` fallback `OrganizationForISC` = `"FTEL"` (`CommonBaseConstant.cs:29-33`).
- Dung reflection `typeof(TTable).GetProperties()` va ham noi bo `SetPropertyIfNull` — **chi gan khi `prop.GetValue(entity) is null`** (dong 496).
- Gan theo thu tu: `IsDeleted` = `false`, `CreatedUser`, `CreatedUserCode`, `CreatedUserOrganization`, `CreatedDate` = `CommonBaseConstant.DateTimeUtc()`.
- `CommonBaseConstant.DateTimeUtc(int addHour = 7)` = `TimeProvider.System.GetUtcNow().DateTime.AddHours(7)` (`CommonBaseConstant.cs:47-50`) — tuc la **gio UTC+7 duoc luu vao truong `DateTime`**, khong phai UTC thuan.
- **Side effect**: mutate truc tiep object `entity` do caller truyen vao.
- **Diem can luu y**: `BaseEntityMongoDB.IsDeleted` la `bool` khong nullable, mac dinh `false` (`BaseEntityMongoDB.cs:17`). Vi vay `prop.GetValue(entity) is null` khong bao gio dung cho truong nay, nen dong `SetPropertyIfNull(nameof(BaseEntityMongoDB.IsDeleted), false)` (dong 502) **khong bao gio thuc su gan gia tri**. Ket qua cuoi cung van la `false` do gia tri khoi tao cua property.

#### `SetDataUpdatedDefault<TTable>(TTable entity, AuditModel audit = default)` — `ProjectToExtensions.cs:428-458`

- Guard: `if (entity is null || audit is null) return entity;` (dong 430). **Neu `audit` la `null`, ham tra ve entity NGUYEN VEN — khong dong dau `ModifiedUser` / `ModifiedDate` nao.**
- Neu `audit` khong null: gan `ModifiedUser`, `ModifiedUserCode`, `ModifiedUserOrganization`, `ModifiedDate` — cung theo co che `SetPropertyIfNull` (chi gan khi dang null).
- **Ham nay khong bao gio tra `null` khi dau vao khong `null`.** Cac lenh `if (entity is null)` / `if (updateDefinition is null)` ngay sau khi goi ham (CoreMongoDB.cs:700, 777, 854) la nhanh khong bao gio chay.

#### `SetDataUpdatedDefault<TTable>(UpdateDefinition<TTable>, AuditModel)` — `ProjectToExtensions.cs:460-476`

- Guard: `if (updateDefinition is null || audit is null) return updateDefinition;` (dong 462) — **`audit == null` thi khong them stage `$set` nao**.
- Neu `audit` khong null: **luon ghi de** (`.Set(...)`, khong kiem tra null) 4 truong `ModifiedUser`, `ModifiedUserCode`, `ModifiedUserOrganization`, `ModifiedDate`.

#### `MapUpdateDefinition<TTableTo>(TTableTo request)` — `ProjectToExtensions.cs:276-311`

- Duyet `typeof(TTableTo).GetProperties()` (chi property `public`). `private string Id` trong `BaseEntityMongoDB` (dong 13) **khong** duoc lay, nen `_id` khong bao gio bi dua vao `$set`.
- Bo qua property khong doc duoc (`!property.CanRead`), property co gia tri `null`, va property gan attribute `[NoMapUpdateDefinition]` (dong 288-301).
- Voi moi property con lai: `updateBuilder.Set(property.Name, value)` (dong 304).
- Tra `null` khi khong co property nao duoc gom (`entitys.Count <= 0`, dong 307); nguoc lai tra `updateBuilder.Combine(entitys)`.
- **Diem can luu y (quan trong)**: dieu kien loai bo la `value is null`. Cac property kieu **value type khong nullable** (`bool`, `int`, `DateTime`, enum...) sau khi boxing **luon khac null**, nen **luon** duoc dua vao `$set`. Voi entity ke thua `BaseEntityMongoDB`, `IsDeleted` (kieu `bool`) se **luon** co trong `$set`. Hau qua: goi `IsUpdateOneAsync(filter, entity)` voi mot entity chi dien vai truong se **ghi `IsDeleted = false` len document**, tuc la co the "hoi sinh" mot ban ghi da xoa mem.

#### `PrecateBuilderExtensions.AddIsDeleted<T>(bool isDeleted = false)` — `PrecateBuilderExtensions.cs:68-74`

- Xay expression tree `x => x.IsDeleted == isDeleted`, dung `Expression.Property(param, "IsDeleted")` voi **ten property hardcode la chuoi `"IsDeleted"`**.
- Neu `T` khong co property ten `IsDeleted`, `Expression.Property` nem exception ngay khi goi (truoc khi cham DB). Do do moi ham `*SortDeletedAsync` **chi dung duoc cho `TTable` co property `IsDeleted`**.
- `filter.And(addIsDeleted)` (`PrecateBuilderExtensions.cs:37-40`) gop hai predicate bang `Expression.AndAlso` sau khi rebind parameter. Neu `filter` la `null` thi `NullReferenceException` (khong co guard).

#### `ProjectTo<TEntity, TDto>` — `ProjectToExtensions.cs:27-63` (single) va `76-127` (list)

- Ban single: `Activator.CreateInstance(typeof(TDto))` duoc goi **truoc** khi kiem tra `entity is null` (dong 29-31). Copy property cung ten (`BindingFlags.Public | Instance | FlattenHierarchy`), bo qua property `!CanWrite` hoac gan `[NoMap]` (dong 47-52). Neu `entity is null` tra ve **instance TDto rong** (khong phai `null`) — nhung trong `CoreMongoDB` luon goi qua `result?.ProjectTo<...>()` nen truong hop nay khong xay ra.
- Ban single **cung** bat exception cho tung property va ghi ra Console (dong 56-59), khong chi ban list.
- Ban list: bat exception cho tung property va tung phan tu, ghi log ra **Console** qua `CommonBaseConstant.ConfigLoggerExceptionByConsole` (dong 114, 122) — **khong** di qua `ILogger`. Loi anh xa mot property se bi "im lang" o goc do `ILogger`.
- Anh xa bang `PropertyInfo.SetValue` moi lan goi — khong co cache expression hay delegate.

### 1.5 `_pipelineRead` / `_pipelineWrite` bao quanh nhung gi

`_pipelineRead.ExecuteAsync(...)` bao boc **dung mot lenh driver** trong moi ham doc:

| Ham | Lenh duoc bao boc |
|---|---|
| `CountAllAsync` (ca 2 overload), `CountAllSortDeletedAsync` | `CountDocumentsAsync` |
| Toan bo 7 overload `FindAllPagingAsync` | `Find(...)....ToListAsync` |
| `FindAllAsync`, `FindAllSortDeletedAsync` (ca 4 overload) | `Find(...).ToListAsync` |
| `FindOneAsync`, `FindOneSortDeletedAsync` (ca 4 overload) | `Find(...).FirstOrDefaultAsync` |
| 3 overload `FindAllWithAggregateAsync` | **Chi** `AggregateAsync` (buoc lay cursor) |

`_pipelineWrite.ExecuteAsync(...)` bao boc:

| Ham | Lenh duoc bao boc |
|---|---|
| `IsCreateOneAsync` | `InsertOneAsync` |
| `IsCreateManyAsync` | `InsertManyAsync` |
| `IsUpdateOneAsync` (2 overload) | `UpdateOneAsync` |
| `IsUpdateManyAsync(Expression, ...)` (2 overload) | `UpdateManyAsync` |
| `IsUpdateManyAsync(List<...>, ...)` (2 overload) | `BulkWriteAsync` |
| `IsDeleteOneAsync` | `DeleteOneAsync` |
| `IsDeleteManyAsync` | `DeleteManyAsync` |
| `BulkWriteAsync` | `BulkWriteAsync` |

**KHONG duoc bao boc trong pipeline nao:**

| Doan code | Vi tri |
|---|---|
| `cancellationToken.ThrowIfCancellationRequested()` dau ham | Dau moi ham (32 lan) |
| Vong lap duyet cursor `cursor.MoveNextAsync(...)` cua aggregate | `CoreMongoDB.cs:1280`, `1324`, `1370` |
| Buoc xay `IFindFluent` cho 2 overload `QueryContext` (`Find`, `Project`, `Sort`, `Limit`, `Skip`) | `CoreMongoDB.cs:133-150`, `179-195` |
| `ProjectTo<TTable, TDto>()` (anh xa DTO) | Sau moi lenh `ExecuteAsync` cua cac overload tra `TDto` |
| `SetDataCreatedDefault` / `SetDataUpdatedDefault` / `MapUpdateDefinition` | Truoc khoi `ExecuteAsync` cua cac ham ghi |
| Cac lenh `_logger.FailLogic` o phan validate dau vao | Truoc khoi `ExecuteAsync` |

> [!NOTE]
> Cac lenh `_logger.FailLogic` nam **ben trong** lambda cua `ExecuteAsync` (`CoreMongoDB.cs:643`, `666`, `720`, `743`, `797`, `820`, `967`, `990`, `1044`, `1067`, `1198`, `1238`). **Ca 12 vi tri nay deu thuoc cac ham GHI, tuc chay trong `_pipelineWrite`** (khong phai `_pipelineRead`).
> Cac dong log nay **khong bi ghi trung khi retry**: chung chi chay **sau khi** lenh driver da tra ve binh thuong va ngay sau do la `return`, trong khi retry cua `MongoResiliencePolicyFactory` chi kich hoat khi callback **nem exception** (`ShouldHandle` chi xet `args.Outcome.Exception` — `MongoResiliencePolicyFactory.cs:84-85`, `182-183`). Neu callback nem exception thi exception den tu lenh driver, xay ra **truoc** cac dong log nay. Cai thuc su duoc chay lai khi retry la **lenh ghi cua driver**, khong phai cac dong log.

### 1.6 Cau hinh policy mac dinh (`MongoResiliencePolicyFactory`)

`CoreMongoDB` **khong** tu tao pipeline — nhan tu constructor. Factory duoc cung cap san trong repo (`FTELSRCore.Shared/Data/MongoDB/Helpers/Policies/MongoResiliencePolicyFactory.cs`) co cau hinh:

| Thuoc tinh | `ConfigureReadPolicy` (dong 20-108) | `ConfigureWritePolicy` (dong 116-207) |
|---|---|---|
| Circuit breaker `FailureRatio` | `0.6` | `0.5` |
| `MinimumThroughput` | `5` | `10` |
| `SamplingDuration` | 10 giay | 15 giay |
| `BreakDuration` | 20 giay | 60 giay |
| Retry `MaxRetryAttempts` | `3` | `1` |
| Retry `Delay` goc | 150 ms | 300 ms |
| `BackoffType` / `UseJitter` | `Exponential` / `true` | `Exponential` / `true` |
| Exception duoc retry | `MongoNotPrimaryException`, `MongoNodeIsRecoveringException`, `MongoConnectionException`, `SocketException`, `MongoExecutionTimeoutException`, `TimeoutException` | **Chi** `MongoNotPrimaryException`, `MongoNodeIsRecoveringException` |

Ly do khac biet o luong ghi duoc ghi ro trong comment (`MongoResiliencePolicyFactory.cs:218-221`): `MongoConnectionException` / `SocketException` co the xay ra sau khi server da xu ly lenh ghi, retry se tao ban ghi trung hoac ap update hai lan.

Ca hai ham deu goi `AddCircuitBreaker(...)` **truoc** `AddRetry(...)` (`MongoResiliencePolicyFactory.cs:22-107` va `120-206`). Trong Polly v8, chien luoc duoc `Add` truoc la lop **ngoai cung**, nen thu tu thuc te la `CircuitBreaker -> Retry -> lenh driver`: circuit breaker chi thay **ket qua cuoi** cua ca chuoi retry (mot lan that bai cho moi lan goi `ExecuteAsync`), khong dem tung lan retry. Cac `ShouldHandle` cua ca retry va circuit breaker chi xet `args.Outcome.Exception` (`MongoResiliencePolicyFactory.cs:26-27`, `84-85`, `124-125`, `182-183`) — nghia la **khong** co chien luoc nao kich hoat dua tren gia tri tra ve (`false`) cua cac ham `Is*Async`.

> [!NOTE]
> Trong repo nay khong tim thay doan code nao goi `ConfigureReadPolicy` / `ConfigureWritePolicy` de dang ky vao DI. Viec noi day pipeline vao `CoreMongoDB` nam ngoai pham vi repo — **khong xac dinh duoc tu source code**.

---
## 2. Chi tiet API

### 2.1 Constructor `CoreMongoDB`

**Signature**

```csharp
protected CoreMongoDB(
    string collectionName,
    IMongoDatabase dbContextRead,
    IMongoDatabase dbContextWrite,
    ILogger<CoreMongoDB<TTable>> logger,
    ResiliencePipeline pipelineRead,
    ResiliencePipeline pipelineWrite)
```

**Muc dich** - Gan `_logger`, `_pipelineRead`, `_pipelineWrite` va tao hai `Lazy<IMongoCollection<TTable>>` tro ten collection `collectionName` tren hai database khac nhau (CoreMongoDB.cs:32-53).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `collectionName` | `string` | Co | Khong validate. Duoc capture vao closure cua `Lazy`, chi dung khi collection duoc truy cap lan dau | Khong co |
| `dbContextRead` | `IMongoDatabase` | Co | Khong validate | Khong co |
| `dbContextWrite` | `IMongoDatabase` | Co | Khong validate | Khong co |
| `logger` | `ILogger<CoreMongoDB<TTable>>` | Co | Khong validate | Khong co |
| `pipelineRead` | `ResiliencePipeline` | Co | Khong validate | Khong co |
| `pipelineWrite` | `ResiliencePipeline` | Co | Khong validate | Khong co |

**Output** - Khong co gia tri tra ve (constructor). Sau khi chay, object o trang thai san sang; **chua** ket noi MongoDB (do `Lazy`).

**Dieu kien xu ly** - Khong co nhanh re. Thu tu thuc thi: gan `_logger` (dong 40), tao `_dbWriteContext` (42-44), tao `_dbReadContext` (46-48), gan `_pipelineRead` (50), gan `_pipelineWrite` (52).

**Side effect** - Khong ghi DB. Khong goi API ngoai. Khong ghi log.

**Error handling** - Khong co. Neu truyen `null` vao bat ky tham so nao, constructor van thanh cong; loi se bung ra o lan su dung dau tien (`_dbReadContext.Value` -> `NullReferenceException`, `_pipelineRead.ExecuteAsync` -> `NullReferenceException`, `_logger.FailLogic` -> `NullReferenceException`).

**Khi nao NEN dung** - Trong constructor cua class repository cu the ke thua `CoreMongoDB<TTable>`, truyen ten collection cua entity va cac dependency lay tu DI.

**Khi nao KHONG dung** - Khong the goi truc tiep tu ngoai (class `abstract`, ctor `protected`).

**Gioi han**
- Khong co guard `ArgumentNullException` cho bat ky tham so nao — loi bao tre va kho truy nguyen.
- `_aggregateOptions` la `private static readonly` (CoreMongoDB.cs:26-30) nen **dung chung cho moi instance cua cung mot `TTable`**; gia tri: `BatchSize = 500`, `MaxTime = TimeSpan.FromSeconds(30)`.
- Khong co cach doi `collectionName` sau khi khoi tao.

---

### 2.2 `CountAllAsync` (overload `FilterDefinition<TTable>`)

**Signature**

```csharp
public virtual async Task<long> CountAllAsync(
    FilterDefinition<TTable> filter = null, CancellationToken cancellationToken = default)
```

**Muc dich** - Dem so document khop `filter` bang `CountDocumentsAsync` tren `_dbReadContext` (CoreMongoDB.cs:64-80).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `FilterDefinition<TTable>` | Khong | `filter ??= Builders<TTable>.Filter.Empty;` (dong 71) — `null` nghia la dem toan bo collection | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 67) | `default` |

**Output** - `long`: so document khop filter. Khong co document nao khop thi tra `0`. Khong bao gio tra gia tri am.

**Dieu kien xu ly**
1. `cancellationToken.ThrowIfCancellationRequested()` (dong 67).
2. `filter ??= Builders<TTable>.Filter.Empty` (dong 71).
3. `_pipelineRead.ExecuteAsync(...)` goi `CountDocumentsAsync(filter, ct)` (dong 73-79). Lambda dat ten tham so la `ct` (khac voi cac ham con lai dung `cancellationToken`).

**Side effect** - Khong co (chi doc).

**Error handling** - Khong co `try`/`catch`. Exception tu driver di qua policy cua `_pipelineRead` roi nem lai. Khong ghi log.

**Khi nao NEN dung** - Khi da co `FilterDefinition` (vi du dung `Builders<TTable>.Filter` de gop nhieu dieu kien phuc tap), hoac khi can dem toan bo collection (`CountAllAsync()`).

**Khi nao KHONG dung**
- Khi collection rat lon va chi can biet "co ban ghi nao khong": `CountDocumentsAsync` phai quet/duyet index, ton kem hon so voi mot truy van `FindOneAsync` co gioi han.
- Khi muon dem theo `IsDeleted` — dung `CountAllSortDeletedAsync` thay vi tu them dieu kien.

**Gioi han**
- Goi `CountAllAsync(null)` **khong bien dich duoc** vi lai overload voi `CountAllAsync(Expression<Func<TTable, bool>>, CancellationToken)` (ca hai deu la reference type, khong co overload nao "cu the hon"). Phai ep kieu ro rang, vi du `CountAllAsync((FilterDefinition<TTable>)null)`, hoac goi `CountAllAsync()` khong tham so.
- Khong co timeout rieng; phu thuoc `MaxTime` cua driver hoac policy Polly.
- XML doc trong `ICoreMongoDB.cs:26-28` (cua overload `Expression`) noi ham chay voi "chinh sach thu lai (`_retryPolicy`)". Field ten `_retryPolicy` **khong ton tai** trong `CoreMongoDB.cs`; field that la `_pipelineRead`.

---

### 2.3 `CountAllAsync` (overload `Expression<Func<TTable, bool>>`)

**Signature**

```csharp
public virtual async Task<long> CountAllAsync(
    Expression<Func<TTable, bool>> filter, CancellationToken cancellationToken = default)
```

**Muc dich** - Dem so document khop LINQ predicate `filter` bang `CountDocumentsAsync` (CoreMongoDB.cs:370-383).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `Expression<Func<TTable, bool>>` | Co | **Khong co guard null.** Truyen thang cho driver | Khong co |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 373) | `default` |

**Output** - `long`: so document khop filter; `0` khi khong co document nao khop.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 373).
2. `_pipelineRead.ExecuteAsync(...)` goi `CountDocumentsAsync(filter, cancellationToken)` (dong 376-382).

**Side effect** - Khong co.

**Error handling** - Khong co `try`/`catch`; exception nem lai cho caller. Khong ghi log.

**Khi nao NEN dung** - Khi dieu kien dem viet duoc bang LINQ lambda, vi du `CountAllAsync(x => x.Status == 1)`.

**Khi nao KHONG dung**
- Khi predicate chua phep toan LINQ ma driver khong dich duoc sang MongoDB query (se nem exception tai thoi diem thuc thi).
- Khi can dem toan bo collection: overload nay khong nhan `null` an toan (khong guard) — dung overload `FilterDefinition` khong tham so.

**Gioi han**
- Khong guard `filter == null` -> loi den tu driver, thong bao it ngu canh, khong co log nghiep vu.
- Xem ghi chu ve xung dot overload o muc 2.2.

---

### 2.4 `CountAllSortDeletedAsync`

**Signature**

```csharp
public virtual async Task<long> CountAllSortDeletedAsync(
    Expression<Func<TTable, bool>> filter,
    bool isDeleted = false, CancellationToken cancellationToken = default)
```

**Muc dich** - Dem so document khop `filter` **VA** co `IsDeleted == isDeleted` (CoreMongoDB.cs:339-358).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `Expression<Func<TTable, bool>>` | Co | **Khong guard null.** `filter.And(...)` se nem `NullReferenceException` neu `null` | Khong co |
| `isDeleted` | `bool` | Khong | Khong validate (kieu `bool` chi co 2 gia tri) | `false` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 343) | `default` |

**Output** - `long`: so document thoa ca hai dieu kien; `0` neu khong co.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 343).
2. `PrecateBuilderExtensions.AddIsDeleted<TTable>(isDeleted)` dung expression `x => x.IsDeleted == isDeleted` (dong 345-346).
3. **Ghi de tham so dau vao**: `filter = filter.And(addIsDeleted);` (dong 348) — bien local `filter` bi gan lai.
4. `_pipelineRead.ExecuteAsync(...)` goi `CountDocumentsAsync(filter, cancellationToken)` (dong 351-357).

**Side effect** - Khong ghi DB. **Gan lai bien tham so `filter` trong pham vi ham** (khong anh huong doi tuong `Expression` goc cua caller vi `Expression` la immutable va `And` tao expression moi).

**Error handling** - Khong co `try`/`catch`. Neu `TTable` khong co property `IsDeleted`, `Expression.Property` trong `AddIsDeleted` nem exception **truoc khi** goi DB. Khong ghi log.

**Khi nao NEN dung** - Khi entity ke thua `BaseEntityMongoDB` (co `IsDeleted`) va can dem chi ban ghi con hieu luc (`isDeleted: false`) hoac chi ban ghi da xoa mem (`isDeleted: true`).

**Khi nao KHONG dung**
- Khi `TTable` khong co property `IsDeleted` — ham nem exception, khong tra `0`.
- Khi muon dem CA hai trang thai — ham nay luon rang buoc dung mot gia tri; dung `CountAllAsync` thay the.

**Gioi han**
- Ten property `"IsDeleted"` bi hardcode dang chuoi trong `AddIsDeleted` (`PrecateBuilderExtensions.cs:71`) — doi ten property se lam ham nay vo hieu ma compiler khong canh bao.
- Khong guard `filter == null`.
- XML doc (`ICoreMongoDB.cs:40-43`) nhac den `_retryPolicy` — field nay khong ton tai.

---

### 2.5 `FindAllPagingAsync<TDto>` (overload `FilterDefinition` + `SortDefinition`)

**Signature**

```csharp
public virtual async Task<List<TDto>> FindAllPagingAsync<TDto>(
    FilterDefinition<TTable> filter,
    SortDefinition<TTable> sortDefinition,
    int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default) where TDto : class
```

**Muc dich** - Truy van `TTable` co sort + phan trang, sau do anh xa sang `List<TDto>` bang reflection (CoreMongoDB.cs:93-114).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `FilterDefinition<TTable>` | Co | **Khong guard null** (khac voi `CountAllAsync` — khong quy ve `Filter.Empty`) | Khong co |
| `sortDefinition` | `SortDefinition<TTable>` | Co | **Khong guard null**; truyen thang vao `.Sort(...)` | Khong co |
| `pageNumber` | `int` | Khong | **Khong validate**; dung trong `(pageNumber - 1) * pageSize` | `1` |
| `pageSize` | `int` | Khong | **Khong validate**; truyen vao `.Limit(pageSize)` | `10` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 98) | `default` |

**Output** - `List<TDto>`:
- Co du lieu: danh sach DTO da anh xa qua `result.ProjectTo<TTable, TDto>()`.
- Khong co du lieu (`result.IsNullOrEmpty()`): **`[]`** (list rong), khong bao gio `null` (dong 113).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 98).
2. `_pipelineRead.ExecuteAsync(...)`: `Find(filter).Sort(sortDefinition).Skip((pageNumber - 1) * pageSize).Limit(pageSize).ToListAsync(...)` (dong 105-110). **Cong thuc Skip: `(pageNumber - 1) * pageSize`** — dung nghia phan trang thong thuong.
3. `return !result.IsNullOrEmpty() ? result.ProjectTo<TTable, TDto>() : [];` (dong 113).

**Side effect** - Khong ghi DB. `ProjectTo` co the ghi log **ra Console** neu anh xa mot property that bai (`ProjectToExtensions.cs:114`).

**Error handling** - Khong co `try`/`catch` trong ham. Exception tu driver duoc nem lai. Loi anh xa tung property duoc `ProjectTo` bat va ghi Console, khong nem len.

**Khi nao NEN dung** - Khi can phan trang chuan (trang 1, 2, 3...) voi sort xac dinh va can DTO cho tang API.

**Khi nao KHONG dung**
- Khi `pageSize` co the la `0` hoac so am: `(pageNumber - 1) * pageSize` cho ket qua sai/am. Gia tri `.Limit(0)` / `.Limit(so am)` duoc truyen thang cho driver, ham khong xu ly gi them — hanh vi cuoi cung do `MongoDB.Driver` / server quyet dinh, **khong xac dinh duoc tu source code cua repo nay**.
- Khi can biet tong so ban ghi de hien thi so trang — ham khong tra `totalCount`, phai goi `CountAllAsync` rieng.
- Khi `TDto` khong co constructor khong tham so: `ProjectTo` dung `Activator.CreateInstance` -> exception.
- Voi collection rat lon va `pageNumber` lon: `Skip` lon la mo hinh phan trang ton kem (server phai duyet qua so document da skip).

**Gioi han**
- Khong validate `pageNumber` / `pageSize`; `pageNumber = 0` cho `Skip(-pageSize)` (gia tri am).
- Anh xa DTO chay tren client bang reflection cho **tung** ban ghi (khong co projection server-side) -> toan bo document duoc keo ve roi moi loc truong.
- Tham so lambda ben trong `ExecuteAsync` duoc dat cung ten `cancellationToken` voi tham so cua ham (dong 102) — kho doc, va bien duoc dung ben trong la bien cua lambda.

---

### 2.6 `FindAllPagingAsync<TDto>` (overload `QueryContext<TTable, TDto>`)

**Signature**

```csharp
public virtual async Task<List<TDto>> FindAllPagingAsync<TDto>(
    QueryContext<TTable, TDto> queryContext,
    int pageSize = 10, int? pageNumber = null,
    CancellationToken cancellationToken = default) where TDto : class
```

`QueryContext` la record khai bao tai `ProjectToExtensions.cs:10-14`:

```csharp
public record QueryContext<TTable, TDto>(
    FilterDefinition<TTable> Predicate,
    SortDefinition<TTable> Sorting = null,
    Expression<Func<TTable, TDto>> Selector = null) where TDto : class
                                                    where TTable : class;
```

**Muc dich** - Truy van co **projection server-side** (`.Project(queryContext.Selector)`) va tra ve `List<TDto>` truc tiep tu driver, khong qua reflection (CoreMongoDB.cs:126-161).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `queryContext` | `QueryContext<TTable, TDto>` | Co | **Khong guard null**; `queryContext.Predicate` duoc doc ngay o dong 134 | Khong co |
| `pageSize` | `int` | Khong | Chi ap `.Limit(pageSize)` khi `pageSize > 0` (dong 142-145) | `10` |
| `pageNumber` | `int?` | Khong | Chi ap `.Skip(pageNumber)` khi `pageNumber.HasValue is true` (dong 147-150) | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 131) | `default` |

**Output** - `List<TDto>`: danh sach DTO do MongoDB tra ve sau projection. Khi rong tra **`[]`** (dong 160). Khong bao gio `null`.

**Dieu kien xu ly** (theo dung thu tu trong code)
1. `ThrowIfCancellationRequested()` (dong 131).
2. `findFluent = _dbReadContext.Value.Find(queryContext.Predicate).Project(queryContext.Selector)` (dong 133-135) — **khong** kiem tra `Selector` co `null` khong.
3. `if (queryContext is { Sorting: not null })` -> `findFluent.Sort(queryContext.Sorting)` (dong 137-140).
4. `if (pageSize > 0)` -> `findFluent.Limit(pageSize)` (dong 142-145). Neu `pageSize <= 0` thi **khong co Limit** -> tra ve toan bo ket qua khop filter.
5. `if (pageNumber.HasValue is true)` -> `findFluent.Skip(pageNumber)` (dong 147-150). **Cong thuc Skip: `Skip(pageNumber)`** — skip dung bang **so trang**, KHONG nhan voi `pageSize`.
6. `_pipelineRead.ExecuteAsync(...)` goi `findFluent.ToListAsync(...)` (dong 152-158).
7. `return !result.IsNullOrEmpty() ? result : [];` (dong 160).

> [!CAUTION]
> Voi `pageNumber = 3, pageSize = 10`, ham nay skip **3 document** (khong phai 20) roi lay 10 document tiep theo. Cac trang lien tiep se **trung lap gan het du lieu**. Xem muc 3, #1.

**Side effect** - Khong ghi DB, khong ghi log.

**Error handling** - Khong `try`/`catch`. `queryContext == null` -> `NullReferenceException` tai dong 134 (truoc khi vao pipeline). `queryContext.Selector == null` (gia tri mac dinh cua record) duoc truyen thang vao `.Project(...)`; hanh vi cu the do `MongoDB.Driver` quyet dinh — **khong xac dinh duoc tu source code cua repo nay**.

**Khi nao NEN dung** - Khi muon MongoDB tra ve **dung** cac truong can thiet (giam bang thong va bo nhoi) va chap nhan tu tinh gia tri `pageNumber` truyen vao la "so document can skip".

**Khi nao KHONG dung**
- Khi muon phan trang theo dung nghia trang: cong thuc `Skip(pageNumber)` sai. Dung overload `Expression` (muc 2.8, 2.9) hoac overload `FilterDefinition` (muc 2.5).
- Khi khong truyen `Selector`: record cho phep `Selector = null` nhung ham khong co nhanh xu ly.
- Khi `pageSize <= 0`: se lay toan bo ket qua, co the gay tran bo nho.

**Gioi han**
- Thu tu tham so **nguoc** so voi cac overload khac: day la `(pageSize, pageNumber)`, cac overload khac la `(pageNumber, pageSize)`. Rat de truyen sai neu goi bang tham so vi tri.
- `IFindFluent` duoc xay **ngoai** pipeline; chi `ToListAsync` nam trong pipeline.
- Khong ghi log khi dau vao khong hop le.

---

### 2.7 `FindAllPagingAsync` (overload `QueryContext<TTable, TTable>`)

**Signature**

```csharp
public virtual async Task<List<TTable>> FindAllPagingAsync(
    QueryContext<TTable, TTable> queryContext,
    int pageSize = 10, int? pageNumber = null,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Giong muc 2.6 nhung tra ve `TTable` va **khong goi `.Project(...)`** (CoreMongoDB.cs:172-206).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `queryContext` | `QueryContext<TTable, TTable>` | Co | **Khong guard null**; `queryContext.Predicate` doc o dong 180 | Khong co |
| `pageSize` | `int` | Khong | `.Limit(pageSize)` chi khi `pageSize > 0` (dong 187-190) | `10` |
| `pageNumber` | `int?` | Khong | `.Skip(pageNumber)` chi khi `pageNumber.HasValue is true` (dong 192-195) | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 177) | `default` |

**Output** - `List<TTable>`; `[]` khi rong (dong 205). Khong bao gio `null`.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 177).
2. `findFluent = _dbReadContext.Value.Find(queryContext.Predicate)` (dong 179-180). **`queryContext.Selector` bi bo qua hoan toan** — truyen `Selector` vao overload nay khong co tac dung.
3. `Sorting` khac null -> `.Sort(...)` (dong 182-185).
4. `pageSize > 0` -> `.Limit(pageSize)` (dong 187-190).
5. `pageNumber.HasValue` -> **`.Skip(pageNumber)`** (dong 192-195) — skip theo **so trang**, giong loi o muc 2.6.
6. `ToListAsync` trong `_pipelineRead` (dong 197-203).
7. `return !result.IsNullOrEmpty() ? result : [];` (dong 205).

**Side effect** - Khong co.

**Error handling** - Khong `try`/`catch`. `queryContext == null` -> `NullReferenceException` tai dong 180.

**Khi nao NEN dung** - Khi can lay entity day du (khong projection) voi filter dang `FilterDefinition` va sort tuy chon, va tu quan ly gia tri skip.

**Khi nao KHONG dung**
- Khi can phan trang dung nghia trang (xem canh bao muc 2.6).
- Khi ky vong `Selector` co tac dung — o overload nay `Selector` bi bo qua.

**Gioi han**
- Cung 2 nhuoc diem cua muc 2.6: `Skip(pageNumber)` va thu tu tham so `(pageSize, pageNumber)`.
- `pageSize <= 0` -> khong Limit -> co the tra ve toan bo collection.

---

### 2.8 `FindAllPagingAsync<TDto>` (overload `Expression`, khong sort)

**Signature**

```csharp
public virtual async Task<List<TDto>> FindAllPagingAsync<TDto>(
    Expression<Func<TTable, bool>> filter, int pageNumber = 1, int pageSize = 10,
    CancellationToken cancellationToken = default) where TDto : class
```

**Muc dich** - Phan trang theo LINQ predicate, **khong sort**, roi anh xa sang `List<TDto>` (CoreMongoDB.cs:218-237).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `Expression<Func<TTable, bool>>` | Co | Khong guard null | Khong co |
| `pageNumber` | `int` | Khong | Khong validate | `1` |
| `pageSize` | `int` | Khong | Khong validate | `10` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 222) | `default` |

**Output** - `List<TDto>`; `[]` khi khong co du lieu (dong 236).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 222).
2. `_pipelineRead.ExecuteAsync(...)`: `Find(filter).Skip((pageNumber - 1) * pageSize).Limit(pageSize).ToListAsync(...)` (dong 229-233). **Cong thuc Skip: `(pageNumber - 1) * pageSize`**.
3. `ProjectTo<TTable, TDto>()` neu co du lieu, nguoc lai `[]` (dong 236).

**Side effect** - Khong ghi DB. `ProjectTo` co the ghi Console khi anh xa loi.

**Error handling** - Khong `try`/`catch`.

**Khi nao NEN dung** - Phan trang don gian ma thu tu ban ghi khong quan trong.

**Khi nao KHONG dung**
- **Bat cu khi nao thu tu ban ghi quan trong**: khong co `Sort`, MongoDB khong bao dam thu tu on dinh giua cac lan truy van, nen `Skip`/`Limit` co the tra ve ban ghi trung hoac bo sot giua cac trang. Dung overload co `SortDefinition` (muc 2.9).
- Khi `pageSize <= 0` (xem muc 2.5).

**Gioi han**
- Khong sort -> phan trang khong on dinh.
- Anh xa DTO bang reflection tren client.
- Khong validate `pageNumber` / `pageSize`.

---

### 2.9 `FindAllPagingAsync<TDto>` (overload `Expression` + `SortDefinition`)

**Signature**

```csharp
public virtual async Task<List<TDto>> FindAllPagingAsync<TDto>(
    Expression<Func<TTable, bool>> filter, SortDefinition<TTable> sort,
    int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default) where TDto : class
```

**Muc dich** - Phan trang theo LINQ predicate, **co sort**, roi anh xa sang `List<TDto>` (CoreMongoDB.cs:250-270).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `Expression<Func<TTable, bool>>` | Co | Khong guard null | Khong co |
| `sort` | `SortDefinition<TTable>` | Co | Khong guard null | Khong co |
| `pageNumber` | `int` | Khong | Khong validate | `1` |
| `pageSize` | `int` | Khong | Khong validate | `10` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 254) | `default` |

**Output** - `List<TDto>`; `[]` khi khong co du lieu (dong 269).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 254).
2. `_pipelineRead.ExecuteAsync(...)`: `Find(filter).Skip((pageNumber - 1) * pageSize).Limit(pageSize).Sort(sort).ToListAsync(...)` (dong 261-266). **Cong thuc Skip: `(pageNumber - 1) * pageSize`**. Luu y `.Sort(...)` duoc goi **sau** `.Skip` va `.Limit` trong chuoi fluent (khac thu tu voi muc 2.5) — day la cach dat option nen thu tu goi khong lam doi ket qua.
3. `ProjectTo<TTable, TDto>()` hoac `[]` (dong 269).

**Side effect** - Khong ghi DB.

**Error handling** - Khong `try`/`catch`.

**Khi nao NEN dung** - Lua chon mac dinh cho endpoint danh sach co phan trang + sort + DTO.

**Khi nao KHONG dung**
- Khi can projection server-side (giam luong du lieu keo ve) — dung muc 2.6 nhung phai tu tinh skip.
- Khi `pageSize <= 0`.

**Gioi han**
- Anh xa DTO bang reflection tren client, toan bo document van duoc keo ve.
- Khong validate `pageNumber` / `pageSize`, khong guard `sort == null`.

---

### 2.10 `FindAllPagingAsync` (overload `Expression`, tra `TTable`, khong sort)

**Signature**

```csharp
public virtual async Task<List<TTable>> FindAllPagingAsync(
    Expression<Func<TTable, bool>> filter,
    int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
```

**Muc dich** - Phan trang theo LINQ predicate, khong sort, **tra thang ket qua cua driver** khong qua `ProjectTo` (CoreMongoDB.cs:281-297).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `Expression<Func<TTable, bool>>` | Co | Khong guard null | Khong co |
| `pageNumber` | `int` | Khong | Khong validate | `1` |
| `pageSize` | `int` | Khong | Khong validate | `10` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 285) | `default` |

**Output** - `List<TTable>` do `ToListAsync` tra ve. **Khong co buoc chuan hoa `[]`** nhu cac overload `TDto` — ket qua la list rong khi khong co document khop (theo hanh vi `ToListAsync` cua driver).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 285).
2. `return await _pipelineRead.ExecuteAsync(...)`: `Find(filter).Skip((pageNumber - 1) * pageSize).Limit(pageSize).ToListAsync(...)` (dong 288-296). **Cong thuc Skip: `(pageNumber - 1) * pageSize`**.

**Side effect** - Khong co.

**Error handling** - Khong `try`/`catch`.

**Khi nao NEN dung** - Khi tang goi can chinh entity `TTable` (vi du xu ly nghiep vu noi bo) va thu tu khong quan trong.

**Khi nao KHONG dung** - Khi thu tu ban ghi quan trong (khong co `Sort`); khi `pageSize <= 0`.

**Gioi han**
- Khong sort -> phan trang khong on dinh.
- Khong validate tham so phan trang.

---

### 2.11 `FindAllPagingAsync` (overload `Expression` + `SortDefinition`, tra `TTable`)

**Signature**

```csharp
public virtual async Task<List<TTable>> FindAllPagingAsync(
    Expression<Func<TTable, bool>> filter, SortDefinition<TTable> sort,
    int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
```

**Muc dich** - Phan trang theo LINQ predicate, co sort, tra thang `List<TTable>` (CoreMongoDB.cs:309-325).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `Expression<Func<TTable, bool>>` | Co | Khong guard null | Khong co |
| `sort` | `SortDefinition<TTable>` | Co | Khong guard null | Khong co |
| `pageNumber` | `int` | Khong | Khong validate | `1` |
| `pageSize` | `int` | Khong | Khong validate | `10` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 313) | `default` |

**Output** - `List<TTable>` tu `ToListAsync`; khong co buoc chuan hoa `[]`.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 313).
2. `_pipelineRead.ExecuteAsync(...)`: `Find(filter).Skip((pageNumber - 1) * pageSize).Limit(pageSize).Sort(sort).ToListAsync(...)` (dong 319-323). **Cong thuc Skip: `(pageNumber - 1) * pageSize`**.

**Side effect** - Khong co.

**Error handling** - Khong `try`/`catch`.

**Khi nao NEN dung** - Lua chon mac dinh khi tang goi can entity day du + phan trang + sort.

**Khi nao KHONG dung** - Khi can DTO (dung muc 2.9); khi `pageSize <= 0`.

**Gioi han** - Khong validate tham so; khong guard `sort == null`.

---

### 2.12 `FindAllAsync<TDto>`

**Signature**

```csharp
public virtual async Task<List<TDto>> FindAllAsync<TDto>(
    Expression<Func<TTable, bool>> filter,
    CancellationToken cancellationToken = default) where TDto : class
```

**Muc dich** - Lay **toan bo** ban ghi khop `filter` (khong Skip, khong Limit, khong Sort) roi anh xa sang `List<TDto>` (CoreMongoDB.cs:392-408).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `Expression<Func<TTable, bool>>` | Co | Khong guard null | Khong co |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 396) | `default` |

**Output** - `List<TDto>`: danh sach DTO; **`[]`** khi khong tim thay ban ghi nao (dong 407). Khong bao gio `null`.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 396).
2. `_pipelineRead.ExecuteAsync(...)`: `Find(filter).ToListAsync(...)` (dong 403-404).
3. `!result.IsNullOrEmpty() ? result.ProjectTo<TTable, TDto>() : []` (dong 407).

**Side effect** - Khong ghi DB. `ProjectTo` co the ghi Console khi anh xa loi.

**Error handling** - Khong `try`/`catch`.

**Khi nao NEN dung** - Khi biet chac tap ket qua nho (danh muc, cau hinh, master data theo ma).

**Khi nao KHONG dung**
- Khi filter co the khop hang nghin/hang trieu document: **khong co gioi han so luong**, toan bo du lieu duoc nap vao bo nho roi anh xa DTO bang reflection -> rui ro `OutOfMemoryException` va tang dot bien do tre.
- Khi can thu tu on dinh (khong co `Sort`).

**Gioi han**
- Khong `Limit`, khong `Sort`, khong `Skip`.
- Anh xa reflection tuan tu tren client.

---

### 2.13 `FindAllAsync`

**Signature**

```csharp
public virtual async Task<List<TTable>> FindAllAsync(
    Expression<Func<TTable, bool>> filter, CancellationToken cancellationToken = default)
```

**Muc dich** - Lay toan bo ban ghi khop `filter`, tra thang ket qua `ToListAsync` (CoreMongoDB.cs:447-460).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `Expression<Func<TTable, bool>>` | Co | Khong guard null | Khong co |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 450) | `default` |

**Output** - `List<TTable>` tu `ToListAsync`; **khong co buoc chuan hoa `[]`** trong code ham nay.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 450).
2. `_pipelineRead.ExecuteAsync(...)`: `Find(filter).ToListAsync(...)` (dong 457-458).

**Side effect** - Khong co.

**Error handling** - Khong `try`/`catch`.

**Khi nao NEN dung** - Khi can entity day du cua mot tap nho ban ghi.

**Khi nao KHONG dung** - Khi tap ket qua co the lon (khong co Limit) hoac khi can thu tu on dinh (khong co Sort).

**Gioi han** - Khong `Limit`/`Sort`/`Skip`; khong guard `filter == null`.

---

### 2.14 `FindAllSortDeletedAsync<TDto>`

**Signature**

```csharp
public virtual async Task<List<TDto>> FindAllSortDeletedAsync<TDto>(
    Expression<Func<TTable, bool>> filter, bool isDeleted = false, CancellationToken cancellationToken = default) where TDto : class
```

**Muc dich** - Nhu muc 2.12 nhung AND them dieu kien `IsDeleted == isDeleted` (CoreMongoDB.cs:418-438).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `Expression<Func<TTable, bool>>` | Co | Khong guard null; `filter.And(...)` nem `NullReferenceException` neu `null` | Khong co |
| `isDeleted` | `bool` | Khong | Khong validate | `false` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 421) | `default` |

**Output** - `List<TDto>`; **`[]`** khi khong co ban ghi (dong 437).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 421).
2. `AddIsDeleted<TTable>(isDeleted)` (dong 423-424).
3. `filter = filter.And(addIsDeleted)` (dong 426).
4. `_pipelineRead.ExecuteAsync(...)`: `Find(filter).ToListAsync(...)` (dong 433-434).
5. `ProjectTo<TTable, TDto>()` hoac `[]` (dong 437).

**Side effect** - Khong ghi DB.

**Error handling** - Khong `try`/`catch`. `TTable` khong co `IsDeleted` -> exception tu `Expression.Property`.

**Khi nao NEN dung** - Lay tap nho ban ghi con hieu luc (`isDeleted: false`) va tra DTO.

**Khi nao KHONG dung** - Tap ket qua lon (khong Limit); `TTable` khong co `IsDeleted`.

**Gioi han** - Khong `Limit`/`Sort`; ten property `"IsDeleted"` hardcode dang chuoi.

---

### 2.15 `FindAllSortDeletedAsync`

**Signature**

```csharp
public virtual async Task<List<TTable>> FindAllSortDeletedAsync(
   Expression<Func<TTable, bool>> filter, bool isDeleted = false, CancellationToken cancellationToken = default)
```

**Muc dich** - Nhu muc 2.13 nhung AND them `IsDeleted == isDeleted` (CoreMongoDB.cs:469-487).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `Expression<Func<TTable, bool>>` | Co | Khong guard null | Khong co |
| `isDeleted` | `bool` | Khong | Khong validate | `false` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 472) | `default` |

**Output** - `List<TTable>` tu `ToListAsync`; khong co buoc chuan hoa `[]`.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 472).
2. `AddIsDeleted<TTable>(isDeleted)` (dong 474-475), `filter = filter.And(addIsDeleted)` (dong 477).
3. `_pipelineRead.ExecuteAsync(...)`: `Find(filter).ToListAsync(...)` (dong 484-485).

**Side effect** - Khong co.

**Error handling** - Khong `try`/`catch`.

**Khi nao NEN dung** - Can entity day du cua tap nho ban ghi theo trang thai xoa mem.

**Khi nao KHONG dung** - Tap ket qua lon; `TTable` khong co `IsDeleted`.

**Gioi han** - Khong `Limit`/`Sort`.

---

### 2.16 `FindOneAsync<TDto>`

**Signature**

```csharp
public virtual async Task<TDto> FindOneAsync<TDto>(
    Expression<Func<TTable, bool>> filter, CancellationToken cancellationToken = default) where TDto : class
```

**Muc dich** - Lay ban ghi **dau tien** khop `filter` (`FirstOrDefaultAsync`) roi anh xa sang `TDto` (CoreMongoDB.cs:496-511).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `Expression<Func<TTable, bool>>` | Co | Khong guard null | Khong co |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 499) | `default` |

**Output** - `TDto`:
- Tim thay: DTO da anh xa tu ban ghi dau tien.
- **Khong tim thay: `null`** — vi `result` la `null` va code dung `result?.ProjectTo<TTable, TDto>()` (dong 510).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 499).
2. `_pipelineRead.ExecuteAsync(...)`: `Find(filter).FirstOrDefaultAsync(...)` (dong 506-507).
3. `return result?.ProjectTo<TTable, TDto>();` (dong 510).

**Side effect** - Khong ghi DB.

**Error handling** - Khong `try`/`catch`.

**Khi nao NEN dung** - Lay chi tiet mot ban ghi theo khoa nghiep vu va tra DTO cho API.

**Khi nao KHONG dung**
- Khi can bao dam chi co dung 1 ban ghi khop: `FirstOrDefaultAsync` **khong** bao loi khi co nhieu ban ghi khop, no lay ban dau tien. Khong co `Sort` nen "ban dau tien" khong xac dinh.
- Khi can phan biet "khong tim thay" voi "tim thay nhung tat ca truong deu null": ca hai deu co the cho ket qua kho phan biet o phia caller.

**Gioi han**
- Khong `Sort` -> ban ghi duoc chon khong tat dinh khi nhieu ban ghi khop.
- Anh xa DTO bang reflection.

---

### 2.17 `FindOneAsync`

**Signature**

```csharp
public virtual async Task<TTable> FindOneAsync(
    Expression<Func<TTable, bool>> filter, CancellationToken cancellationToken = default)
```

**Muc dich** - Lay ban ghi dau tien khop `filter`, tra thang `TTable` (CoreMongoDB.cs:552-565).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `Expression<Func<TTable, bool>>` | Co | Khong guard null | Khong co |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 555) | `default` |

**Output** - `TTable` neu tim thay; **`null`** neu khong (`FirstOrDefaultAsync` tra `default(TTable)`, va `TTable : class`).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 555).
2. `_pipelineRead.ExecuteAsync(...)`: `Find(filter).FirstOrDefaultAsync(...)` (dong 562-563).

**Side effect** - Khong co.

**Error handling** - Khong `try`/`catch`.

**Khi nao NEN dung** - Kiem tra ton tai, hoac lay entity day du de xu ly nghiep vu tiep.

**Khi nao KHONG dung** - Khi can bao dam tinh duy nhat (xem muc 2.16).

**Gioi han** - Khong `Sort`; khong guard `filter == null`.

---

### 2.18 `FindOneSortDeletedAsync<TDto>`

**Signature**

```csharp
public virtual async Task<TDto> FindOneSortDeletedAsync<TDto>(
   Expression<Func<TTable, bool>> filter,
   bool isDeleted = false, CancellationToken cancellationToken = default) where TDto : class
```

**Muc dich** - Nhu muc 2.16 nhung AND them `IsDeleted == isDeleted` (CoreMongoDB.cs:522-543).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `Expression<Func<TTable, bool>>` | Co | Khong guard null | Khong co |
| `isDeleted` | `bool` | Khong | Khong validate | `false` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 526) | `default` |

**Output** - `TDto` neu tim thay; **`null`** neu khong (dong 542 dung `result?.ProjectTo<...>()`).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 526).
2. `AddIsDeleted<TTable>(isDeleted)` (dong 528-529), `filter = filter.And(addIsDeleted)` (dong 531).
3. `_pipelineRead.ExecuteAsync(...)`: `Find(filter).FirstOrDefaultAsync(...)` (dong 538-539).
4. `return result?.ProjectTo<TTable, TDto>();` (dong 542).

**Side effect** - Khong ghi DB.

**Error handling** - Khong `try`/`catch`. `TTable` khong co `IsDeleted` -> exception tu `Expression.Property`.

**Khi nao NEN dung** - Lay chi tiet mot ban ghi con hieu luc de tra ve API.

**Khi nao KHONG dung** - `TTable` khong co `IsDeleted`; khi can bao dam tinh duy nhat.

**Gioi han** - Khong `Sort`; ten property `"IsDeleted"` hardcode.

---

### 2.19 `FindOneSortDeletedAsync`

**Signature**

```csharp
public virtual async Task<TTable> FindOneSortDeletedAsync(
   Expression<Func<TTable, bool>> filter, bool isDeleted = false, CancellationToken cancellationToken = default)
```

**Muc dich** - Nhu muc 2.17 nhung AND them `IsDeleted == isDeleted` (CoreMongoDB.cs:575-593).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `Expression<Func<TTable, bool>>` | Co | Khong guard null | Khong co |
| `isDeleted` | `bool` | Khong | Khong validate | `false` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 578) | `default` |

**Output** - `TTable` neu tim thay; **`null`** neu khong.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 578).
2. `AddIsDeleted<TTable>(isDeleted)` (dong 580-581), `filter = filter.And(addIsDeleted)` (dong 583).
3. `_pipelineRead.ExecuteAsync(...)`: `Find(filter).FirstOrDefaultAsync(...)` (dong 590-591).

**Side effect** - Khong co.

**Error handling** - Khong `try`/`catch`.

**Khi nao NEN dung** - Lay entity con hieu luc de xu ly nghiep vu.

**Khi nao KHONG dung** - `TTable` khong co `IsDeleted`; khi can bao dam tinh duy nhat.

**Gioi han** - Khong `Sort`.

---
### 2.20 `IsCreateOneAsync`

**Signature**

```csharp
public virtual async Task<bool> IsCreateOneAsync(
    TTable entity, AuditModel audit = null, CancellationToken cancellationToken = default)
```

**Muc dich** - Chen mot document moi bang `InsertOneAsync`, sau khi dong dau cac truong `Created*` qua `SetDataCreatedDefault` (CoreMongoDB.cs:1082-1113).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entity` | `TTable` | Co | `if (entity is null)` -> log `FailLogic` + tra `false` (dong 1087-1093) | Khong co |
| `audit` | `AuditModel` | Khong | Khong validate. `SetDataCreatedDefault` **khong** guard `audit == null` nen van dong dau gia tri fallback `"Anonymous"` / `"0"` / `"FTEL"` | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 1085) | `default` |

**Output** - `bool`:
- `false` khi `entity is null` (dong 1092).
- `false` khi `SetDataCreatedDefault` tra `null` (dong 1102) — nhung theo `ProjectToExtensions.cs:480` ham chi tra `null` khi dau vao la `null`, ma truong hop do da bi guard o buoc truoc, nen **nhanh nay khong bao gio chay**.
- **`true` trong moi truong hop con lai** (dong 1112) — gia tri tra ve **khong** phu thuoc ket qua tu MongoDB, vi `InsertOneAsync` tra `Task` (khong co result object).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 1085).
2. `if (entity is null)` -> log + `false` (1087-1093).
3. `entity = SetDataCreatedDefault(entity, audit)` (dong 1095) — **mutate object caller truyen vao**.
4. `if (entity is null)` -> log + `false` (1097-1103) — nhanh khong the xay ra.
5. `_pipelineWrite.ExecuteAsync(...)` goi `InsertOneAsync(entity, ct)` (dong 1105-1110).
6. `return true;` (dong 1112).

**Side effect**
- **Ghi DB**: chen 1 document vao `_dbWriteContext`.
- **Mutate tham so dau vao**: `SetDataCreatedDefault` gan gia tri vao cac property `null` cua `entity` (`CreatedUser`, `CreatedUserCode`, `CreatedUserOrganization`, `CreatedDate`).
- **Ghi log**: `_logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsCreateOneAsync), ...)` khi validate that bai.

**Error handling** - Khong `try`/`catch`. Neu MongoDB tra loi (vi du trung khoa `_id`, vi pham unique index), exception di qua policy `_pipelineWrite` roi **nem lai cho caller** — ham **khong** tra `false`. XML doc noi "false nếu có lỗi" (`ICoreMongoDB.cs:234`) khong dung voi than ham.

**Khi nao NEN dung** - Tao moi mot ban ghi va khong can biet `_id` sinh ra.

**Khi nao KHONG dung**
- Khi can biet ban ghi thuc su duoc chen hay chua thong qua gia tri tra ve: ham luon tra `true`, chi exception moi phan anh that bai.
- Khi can `_id` cua document vua tao: ham khong tra ve. `BaseEntityMongoDB.Id` la `private` (`BaseEntityMongoDB.cs:13`) nen caller cung khong doc duoc tu entity.
- Khi khong muon entity dau vao bi thay doi.
- Khi da co san `_id` va muon "insert neu chua co, cap nhat neu co": dung `IsUpdateOneAsync` (co upsert).

**Gioi han**
- Gia tri tra ve khong mang thong tin: `true` la "khong nem exception", khong phai "server da xac nhan ghi".
- `CreatedDate` duoc gan `UTC + 7 gio` (`CommonBaseConstant.DateTimeUtc()`), khong phai UTC thuan — de sai lech khi so sanh voi du lieu luu UTC.
- Chi gan cac truong `Created*` dang `null`; neu caller da dien san thi khong ghi de.

---

### 2.21 `IsCreateManyAsync`

**Signature**

```csharp
public virtual async Task<bool> IsCreateManyAsync(
    IEnumerable<TTable> entities,
    AuditModel audit = null, CancellationToken cancellationToken = default)
```

> [!NOTE]
> Interface khai bao tham so nay ten la **`entites`** (`ICoreMongoDB.cs:248`), con implementation dat ten **`entities`** (`CoreMongoDB.cs:1125`). Goi bang named argument se phai dung ten khac nhau tuy theo kieu bien la interface hay class cu the.

**Muc dich** - Chen nhieu document bang `InsertManyAsync`, sau khi dong dau `Created*` cho tung phan tu (CoreMongoDB.cs:1124-1167).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entities` | `IEnumerable<TTable>` | Co | `if (entities.IsNullOrEmpty())` -> log + `false` (dong 1130-1136). `IsNullOrEmpty` xu ly an toan ca truong hop `null` (`CollectionHelpers.cs:16`) | Khong co |
| `audit` | `AuditModel` | Khong | Khong validate; fallback `"Anonymous"` / `"0"` / `"FTEL"` | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 1128) | `default` |

**Output** - `bool`:
- `false` khi `entities` la `null` hoac rong (dong 1135).
- `false` khi sau khi loc, `result` rong (dong 1156) — chi xay ra neu **moi** phan tu deu la `null`, vi `SetDataCreatedDefault` chi tra `null` khi dau vao `null`.
- **`true`** trong moi truong hop con lai (dong 1166), khong phu thuoc ket qua MongoDB.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 1128).
2. `if (entities.IsNullOrEmpty())` -> log + `false` (1130-1136).
3. Duyet tung `entity`, goi `SetDataCreatedDefault(entity, audit)`, chi them vao `result` khi `data is not null` (dong 1140-1149) — **phan tu `null` bi bo qua im lang, khong log**.
4. `if (result.IsNullOrEmpty())` -> log + `false` (1151-1157).
5. `_pipelineWrite.ExecuteAsync(...)` goi `InsertManyAsync(result, ct)` (dong 1159-1164).
6. `return true;` (dong 1166).

**Side effect**
- **Ghi DB**: chen nhieu document.
- **Mutate tham so dau vao**: moi entity khong `null` bi gan cac truong `Created*`.
- **Duyet `IEnumerable` hai lan**: mot lan trong `IsNullOrEmpty` (neu khong phai `ICollection`/`IReadOnlyCollection`/`List` thi goi `.Any()` — `CollectionHelpers.cs:36`) va mot lan trong `foreach`. Voi `IEnumerable` sinh du lieu mot lan (lazy, khong buffer) day la rui ro thuc thi lai truy van nguon.
- **Ghi log**: `FailLogic` voi `methodName = nameof(IsCreateManyAsync)` khi validate that bai.

**Error handling** - Khong `try`/`catch`. Exception tu `InsertManyAsync` (vi du `MongoBulkWriteException` khi vi pham unique index) duoc nem lai.

**Khi nao NEN dung** - Nap mot lo ban ghi moi (batch import) khi so luong vua phai.

**Khi nao KHONG dung**
- Khi can biet chinh xac bao nhieu ban ghi da chen: ham chi tra `bool`.
- Khi `entities` la truy van lazy khong buffer (rui ro duyet hai lan).
- Khi lo qua lon: khong co chia batch; `InsertManyAsync` gui toan bo trong mot lenh, co gioi han kich thuoc request cua MongoDB.
- Khi can bo qua ban ghi trung ma van chen phan con lai: khong co tuy chon `InsertManyOptions { IsOrdered = false }` (ham khong nhan `options`).

**Gioi han**
- Khong nhan `InsertManyOptions` -> mac dinh cua driver la ordered; mot loi se chan cac ban ghi phia sau.
- Gia tri tra ve khong phan anh so luong chen thanh cong.
- Phan tu `null` trong danh sach bi bo qua im lang.

---

### 2.22 `IsUpdateOneAsync` (overload `TTable entity`)

**Signature**

```csharp
public virtual async Task<bool> IsUpdateOneAsync(
    Expression<Func<TTable, bool>> filter,
    TTable entity, AuditModel audit = null, CancellationToken cancellationToken = default)
```

**Muc dich** - Cap nhat mot document: tu dong sinh `UpdateDefinition` dang `$set` tu cac property khac `null` cua `entity` (qua `MapUpdateDefinition`) roi goi `UpdateOneAsync` voi **`UpdateOptions { IsUpsert = true }`** (CoreMongoDB.cs:605-671, dong 638).

> [!WARNING]
> **Upsert luon bat.** Neu `filter` khong khop document nao, MongoDB **tu tao mot document moi** (dua tren cac dieu kien dang so sanh bang trong filter cong voi cac truong trong `$set`), va ham tra ve **`true`** (nhanh `MatchedCount: 0, ModifiedCount: 0, UpsertedId: not null` -> `true`, dong 657-660). **Khong** tra `false`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `Expression<Func<TTable, bool>>` | Co | `if (entity is null || filter is null)` -> log + `false` (dong 611-617) | Khong co |
| `entity` | `TTable` | Co | Cung guard nhu tren | Khong co |
| `audit` | `AuditModel` | Khong | Khong validate. **Neu `null`, `SetDataUpdatedDefault` KHONG dong dau `Modified*`** (`ProjectToExtensions.cs:430`) | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 609) | `default` |

**Output** - `bool`:
- `false` khi `entity` hoac `filter` la `null` (dong 616).
- `false` khi `MapUpdateDefinition` tra `null` (dong 629) — thuc te rat kho xay ra voi entity ke thua `BaseEntityMongoDB` vi property `IsDeleted` kieu `bool` luon khac `null`.
- `false` khi `result is null` (dong 646).
- **`true`** khi `result.MatchedCount > 0` (dong 652-653) — **ke ca khi `ModifiedCount == 0`** (du lieu khong doi, no-op).
- **`true`** khi `MatchedCount == 0 && ModifiedCount == 0 && UpsertedId != null`, tuc la **da tao moi document** (dong 657-660).
- `false` cho moi truong hop con lai, kem log (dong 666-669).

**Dieu kien xu ly** (theo thu tu thuc thi)
1. `ThrowIfCancellationRequested()` (dong 609).
2. Guard `entity is null || filter is null` -> log + `false`.
3. `entity = SetDataUpdatedDefault(entity, audit)` (dong 619) — **mutate entity**; khong lam gi neu `audit == null`.
4. `mapUpdateDefinition = MapUpdateDefinition(entity)` (dong 621-622).
5. Guard `mapUpdateDefinition is null` -> log + `false` (dong 624-630).
6. Trong `_pipelineWrite`: `UpdateOneAsync(filter, mapUpdateDefinition, new UpdateOptions { IsUpsert = true }, ct)` (dong 636-639).
7. Guard `result is null` -> log + `false` (dong 641-647).
8. `switch (result.MatchedCount > 0)`: `true` -> `true`; `false` -> kiem tra pattern upsert -> `true` (dong 649-664).
9. Log + `false` (dong 666-669).

**Side effect**
- **Ghi DB**: cap nhat 1 document, **hoac tao moi 1 document (upsert)**.
- **Mutate tham so dau vao**: `entity` co the bi gan cac truong `Modified*` (khi `audit != null`).
- **Ghi log**: `FailLogic` voi `methodName = nameof(IsUpdateOneAsync)` — dung ten ham dang chay o ca 4 vi tri (dong 613, 626, 643, 666). Hai vi tri cuoi nam **trong** lambda pipeline, nhung chi chay sau khi `UpdateOneAsync` tra ve va ngay sau do la `return`, nen retry **khong** lam chung bi ghi trung (xem ghi chu muc 1.5).
- `$set` sinh ra bao gom **tat ca** property khac `null` cua entity, ke ca property kieu value type nhu `IsDeleted` (`false`), `int` (`0`), `DateTime` (`default`) — xem muc 1.4.

**Error handling** - Khong `try`/`catch`. Exception tu MongoDB duoc nem lai. Cac truong hop that bai nghiep vu duoc ghi log `FailLogic` (category `BIZ_LOGIC`).

**Khi nao NEN dung** - Khi muon "cap nhat neu ton tai, tao moi neu chua co" (semantic upsert) cho **mot** document, va entity chua day du gia tri can ghi.

**Khi nao KHONG dung**
- **Khi khong muon tao moi ban ghi.** Vi upsert luon bat, mot `filter` sai (vi du sai ma dinh danh) se **sinh ra ban ghi rac** thay vi tra `false`. Dung `BulkWriteAsync` voi `UpdateOneModel<TTable> { IsUpsert = false }` neu can update thuan.
- **Khi entity chi dien mot phan cac truong**: cac property kieu value type khong nullable van bi ghi de bang gia tri mac dinh, dac biet `IsDeleted = false` co the "hoi sinh" ban ghi da xoa mem. Dung overload `UpdateDefinition` (muc 2.23) de kiem soat chinh xac truong nao duoc ghi.
- Khi `filter` chua toan tu khong phai so sanh bang (`>`, `<`, `$in`, regex...) va co kha nang khong khop: document upsert sinh ra co the thieu truong dinh danh mong doi.
- Khi can biet co dung "sua" hay "tao moi": ham tra `true` cho ca hai truong hop.

**Gioi han**
- Khong co cach tat upsert.
- `true` khi `MatchedCount > 0` du `ModifiedCount == 0` — khong phan biet duoc "co thay doi" va "khong thay doi".
- Nhanh `mapUpdateDefinition is null` gan nhu khong the xay ra voi entity ke thua `BaseEntityMongoDB`.
- Khi retry (voi pipeline do `MongoResiliencePolicyFactory.ConfigureWritePolicy` cau hinh: retry toi da 1 lan, chi cho `MongoNotPrimaryException` / `MongoNodeIsRecoveringException`), lenh `$set` la idempotent nen an toan — `mapUpdateDefinition` (ke ca `ModifiedDate`) duoc tinh **truoc** khi vao `ExecuteAsync` (dong 619-622) nen moi lan retry gui **dung cung mot** lenh update. Luu y: pipeline that su duoc dung la pipeline caller inject qua ctor, khong bao dam la pipeline cua factory nay.

---

### 2.23 `IsUpdateOneAsync` (overload `UpdateDefinition<TTable>`)

**Signature**

```csharp
public virtual async Task<bool> IsUpdateOneAsync(
    Expression<Func<TTable, bool>> filter, UpdateDefinition<TTable> updateDefinition,
    AuditModel audit = null, CancellationToken cancellationToken = default)
```

**Muc dich** - Cap nhat mot document theo `UpdateDefinition` do caller tu xay, co bo sung stage `$set` cho cac truong `Modified*` neu `audit != null`, va goi `UpdateOneAsync` voi **`UpdateOptions { IsUpsert = true }`** (CoreMongoDB.cs:683-748, dong 715).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `Expression<Func<TTable, bool>>` | Co | `if (updateDefinition is null || filter is null)` -> log + `false` (dong 689-695) | Khong co |
| `updateDefinition` | `UpdateDefinition<TTable>` | Co | Cung guard nhu tren | Khong co |
| `audit` | `AuditModel` | Khong | Khong validate. **Neu `null` thi khong them stage `Modified*` nao** (`ProjectToExtensions.cs:462`) | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 687) | `default` |

**Output** - `bool`:
- `false` khi `updateDefinition` hoac `filter` la `null` (dong 694).
- `false` khi `updateDefinition` la `null` sau khi goi `SetDataUpdatedDefault` (dong 705) — **nhanh khong the xay ra**, vi ham chi tra `null` khi dau vao `null` (da bi guard).
- `false` khi `result is null` (dong 723).
- **`true`** khi `MatchedCount > 0` (ke ca `ModifiedCount == 0`).
- **`true`** khi `MatchedCount == 0 && ModifiedCount == 0 && UpsertedId != null` (**da upsert tao moi**).
- `false` + log cho cac truong hop con lai (dong 743-746).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 687).
2. Guard `updateDefinition is null || filter is null` -> log + `false`.
3. `updateDefinition = SetDataUpdatedDefault(updateDefinition, audit)` (dong 697-698) — **ghi de** (khong kiem tra null) 4 truong `ModifiedUser`, `ModifiedUserCode`, `ModifiedUserOrganization`, `ModifiedDate` khi `audit != null`.
4. Guard `updateDefinition is null` -> log + `false` (dong 700-706) — nhanh chet.
5. Trong `_pipelineWrite`: `UpdateOneAsync(filter, updateDefinition, new UpdateOptions { IsUpsert = true }, ct)` (dong 712-716).
6. Guard `result is null` -> log + `false`.
7. `switch (result.MatchedCount > 0)` nhu muc 2.22.

**Side effect**
- **Ghi DB**: cap nhat 1 document hoac **tao moi 1 document (upsert)**.
- **Khong** mutate object cua caller: `UpdateDefinition` la immutable, `.Set(...)` tra dinh nghia moi; bien local duoc gan lai.
- **Ghi log**: `FailLogic` voi `methodName = nameof(IsUpdateOneAsync)` (dung ten ham) tai dong 691, 702, 720, 743.

**Error handling** - Khong `try`/`catch`; exception nem lai.

**Khi nao NEN dung**
- Khi can kiem soat chinh xac cac truong duoc ghi (`$set`, `$inc`, `$push`, `$addToSet`, `$unset`...) — **day la overload duoc uu tien** cho update ban phan.
- Khi muon tranh viec `MapUpdateDefinition` ghi de cac truong value type ngoai y muon.

**Khi nao KHONG dung**
- Khi khong muon tao moi ban ghi: upsert luon bat (xem canh bao muc 2.22).
- Khi `audit != null` nhung ban **khong** muon ghi de `Modified*`: ham luon `.Set(...)` 4 truong nay.
- Khi can biet phan biet "sua" hay "tao moi".

**Gioi han**
- Khong co cach tat upsert.
- Neu `updateDefinition` da chua `$set` cho `ModifiedDate` va `audit != null`, gia tri se bi ghi de bang `CommonBaseConstant.DateTimeUtc()` (UTC+7).
- Nhanh kiem tra null sau `SetDataUpdatedDefault` la dead code.

---

### 2.24 `IsUpdateManyAsync` (overload `TTable entity`)

**Signature**

```csharp
public virtual async Task<bool> IsUpdateManyAsync(
    Expression<Func<TTable, bool>> filter,
    TTable entity, AuditModel audit = null, CancellationToken cancellationToken = default)
```

**Muc dich** - Cap nhat **nhieu** document khop `filter` bang `UpdateManyAsync`, `UpdateDefinition` sinh tu entity qua `MapUpdateDefinition`, voi **`UpdateOptions { IsUpsert = true }`** (CoreMongoDB.cs:838-907, dong 874).

> [!WARNING]
> Upsert bat tren `UpdateManyAsync`: neu `filter` khong khop document nao, MongoDB tao **mot** document moi (upsert cua `updateMany` chi tao toi da 1 document), va ham tra `true`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `Expression<Func<TTable, bool>>` | Co | `if (entity is null || filter is null)` -> log + `false` (dong 844-850) | Khong co |
| `entity` | `TTable` | Co | Cung guard nhu tren | Khong co |
| `audit` | `AuditModel` | Khong | Khong validate; `audit == null` thi khong dong dau `Modified*` | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 842) | `default` |

**Output** - `bool`:
- `false` khi `entity` hoac `filter` la `null`.
- `false` khi `entity is null` sau `SetDataUpdatedDefault` (dong 859) — nhanh chet.
- **`false` (khong log)** khi `mapUpdateDefinition is null` (dong 865) — **day la vi tri duy nhat trong cac ham update tra `false` ma KHONG ghi log**.
- `false` khi `result is null` (dong 882).
- **`true`** khi `MatchedCount > 0`.
- **`true`** khi `MatchedCount == 0 && ModifiedCount == 0 && UpsertedId != null` (upsert).
- `false` + log cho cac truong hop con lai (dong 902-905).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 842).
2. Guard `entity is null || filter is null` -> log + `false`.
3. `entity = SetDataUpdatedDefault(entity, audit)` (dong 852) — mutate entity.
4. Guard `entity is null` -> log message `"MapUpdateDefinition ... is null"` (dong 856-857) — **noi dung log khong khop voi dieu kien that** (dieu kien la entity null, khong lien quan `MapUpdateDefinition`); ngoai ra nhanh nay khong the xay ra.
5. `mapUpdateDefinition = MapUpdateDefinition(entity)` (dong 862-863).
6. `if (mapUpdateDefinition is null) return false;` (dong 865) — khong log.
7. Trong `_pipelineWrite`: `UpdateManyAsync(filter, mapUpdateDefinition, new UpdateOptions { IsUpsert = true }, ct)` (dong 871-875).
8. Guard `result is null` -> log + `false`.
9. `switch (result.MatchedCount > 0)` nhu muc 2.22.

**Side effect**
- **Ghi DB**: cap nhat nhieu document, hoac tao moi 1 document (upsert).
- **Mutate tham so dau vao**: `entity` bi gan cac truong `Modified*` khi `audit != null`.
- **Ghi log**: `FailLogic` voi `methodName = nameof(IsUpdateManyAsync)` (dung ten ham) tai dong 846, 856, 879, 902.

**Error handling** - Khong `try`/`catch`; exception nem lai.

**Khi nao NEN dung** - Cap nhat cung mot bo gia tri cho nhieu ban ghi khop mot dieu kien, khi entity da du gia tri can ghi.

**Khi nao KHONG dung**
- **Khi filter co the khong khop gi**: upsert se tao ban ghi rac (rui ro cao hon overload `UpdateOne` vi day thuong la lenh hang loat).
- Khi entity chi dien mot phan truong: cac value type se bi ghi de bang mac dinh cho **tat ca** document khop — dac biet `IsDeleted = false`.
- Khi can biet so document da doi: ham chi tra `bool`.

**Gioi han**
- Khong co cach tat upsert.
- Truong hop `mapUpdateDefinition is null` tra `false` khong co log -> kho debug.
- Message log tai dong 856-857 mo ta sai nguyen nhan.

---

### 2.25 `IsUpdateManyAsync` (overload `UpdateDefinition<TTable>`)

**Signature**

```csharp
public virtual async Task<bool> IsUpdateManyAsync(
    Expression<Func<TTable, bool>> filter,
    UpdateDefinition<TTable> updateDefinition, AuditModel audit = null, CancellationToken cancellationToken = default)
```

**Muc dich** - Cap nhat nhieu document khop `filter` theo `UpdateDefinition` caller cung cap, co bo sung `Modified*` neu `audit != null`, voi **`UpdateOptions { IsUpsert = true }`** (CoreMongoDB.cs:760-825, dong 792).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `Expression<Func<TTable, bool>>` | Co | `if (updateDefinition is null || filter is null)` -> log + `false` (dong 766-772) | Khong co |
| `updateDefinition` | `UpdateDefinition<TTable>` | Co | Cung guard nhu tren | Khong co |
| `audit` | `AuditModel` | Khong | Khong validate; `audit == null` thi khong them `Modified*` | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 764) | `default` |

**Output** - `bool`:
- `false` khi `updateDefinition` hoac `filter` la `null`.
- `false` khi `updateDefinition is null` sau `SetDataUpdatedDefault` (dong 782) — nhanh chet.
- `false` khi `result is null` (dong 800).
- **`true`** khi `MatchedCount > 0` (ke ca `ModifiedCount == 0`).
- **`true`** khi `MatchedCount == 0 && ModifiedCount == 0 && UpsertedId != null` (upsert).
- `false` + log cho cac truong hop con lai (dong 820-823).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 764).
2. Guard `updateDefinition is null || filter is null` -> log + `false`.
3. `updateDefinition = SetDataUpdatedDefault(updateDefinition, audit)` (dong 774-775).
4. Guard `updateDefinition is null` -> log + `false` (dong 777-783) — nhanh chet.
5. Trong `_pipelineWrite`: `UpdateManyAsync(filter, updateDefinition, new UpdateOptions { IsUpsert = true }, ct)` (dong 789-793).
6. Guard `result is null` -> log + `false`.
7. `switch (result.MatchedCount > 0)` nhu muc 2.22.

**Side effect**
- **Ghi DB**: cap nhat nhieu document hoac tao moi 1 document (upsert).
- Khong mutate object cua caller (`UpdateDefinition` immutable).
- **Ghi log**: `FailLogic` voi `methodName = nameof(IsUpdateManyAsync)` (dung ten ham) tai dong 768, 779, 797, 820.

**Error handling** - Khong `try`/`catch`; exception nem lai.

**Khi nao NEN dung** - Cap nhat hang loat co kiem soat chinh xac truong ghi, vi du `Builders<T>.Update.Set(x => x.Status, 2)` cho moi ban ghi khop.

**Khi nao KHONG dung**
- Khi khong muon tao moi ban ghi (upsert luon bat).
- Khi can biet so ban ghi bi anh huong.

**Gioi han** - Khong tat duoc upsert; khong tra `MatchedCount` / `ModifiedCount`.

---

### 2.26 `IsUpdateManyAsync` (overload `List<(Expression, TTable)>`)

**Signature**

```csharp
public virtual async Task<bool> IsUpdateManyAsync(
    List<(Expression<Func<TTable, bool>> filter, TTable entity)> entities,
    AuditModel audit = null, CancellationToken cancellationToken = default)
```

**Muc dich** - Cap nhat hang loat, **moi phan tu co filter va entity rieng**, gom thanh mot lenh `BulkWriteAsync` gom nhieu `UpdateOneModel<TTable> { IsUpsert = true }` (CoreMongoDB.cs:918-995, dong 947).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entities` | `List<(Expression<Func<TTable,bool>> filter, TTable entity)>` | Co | `if (entities is null \|\| entities.IsNullOrEmpty())` -> log + `false` (dong 924-930). Dieu kien nay trung lap vi `IsNullOrEmpty` da xu ly `null` | Khong co |
| `audit` | `AuditModel` | Khong | Khong validate; `audit == null` thi khong dong dau `Modified*` cho phan tu nao | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 922) | `default` |

**Output** - `bool`:
- `false` khi `entities` `null`/rong (dong 929).
- `false` khi sau khi duyet, `writeModels` rong (dong 955) — tuc **moi** phan tu deu cho `MapUpdateDefinition` tra `null`.
- `false` khi `result is null` (dong 970).
- **`true`** khi `result.MatchedCount > 0` (dong 975-978).
- **`true`** khi `result.Upserts != null && result.Upserts.Any()` (dong 981-984) — **da tao moi it nhat 1 document**.
- `false` + log cho cac truong hop con lai (dong 990-993).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 922).
2. Guard `entities is null || entities.IsNullOrEmpty()` -> log + `false`.
3. `foreach` tung cap `(Filter, Entity)` (dong 934-948):
   - `SetDataUpdatedDefault(Entity, audit)` (dong 936-937);
   - `MapUpdateDefinition(entity)` (dong 939-940);
   - `if (mapUpdateDefinition is null) continue;` (dong 942-945) — **bo qua phan tu do IM LANG, khong log**;
   - `writeModels.Add(new UpdateOneModel<TTable>(Filter, mapUpdateDefinition) { IsUpsert = true });` (dong 947).
   - **Khong kiem tra `Filter` co `null` khong** truoc khi tao `UpdateOneModel`.
   - **Khong kiem tra `Entity` co `null` khong**: `SetDataUpdatedDefault` tra ve `null` (guard `entity is null`), roi `MapUpdateDefinition(null)` goi `PropertyInfo.GetValue(null)` tren property instance (`ProjectToExtensions.cs:291`) — **nem exception, khong phai bo qua phan tu im lang**. Chi khi `TTable` khong co property `public` nao thi vong lap moi khong chay va `MapUpdateDefinition` tra `null` (`ProjectToExtensions.cs:307`).
4. Guard `writeModels is null || writeModels.IsNullOrEmpty()` -> log + `false` (dong 950-956).
5. Trong `_pipelineWrite`: `BulkWriteAsync(writeModels, ct)` — **khong truyen `BulkWriteOptions`** (dong 961-963).
6. Guard `result is null` -> log + `false`.
7. `switch (result.MatchedCount > 0)` -> `true`; nguoc lai kiem tra `Upserts` -> `true` (dong 973-988).
8. Log + `false` (dong 990-993).

**Side effect**
- **Ghi DB**: nhieu lenh update trong mot bulk; moi lenh co the tao moi document (upsert).
- **Mutate tham so dau vao**: tung `Entity` trong list bi gan cac truong `Modified*` khi `audit != null`.
- **Ghi log**: `FailLogic` voi `methodName = nameof(IsUpdateManyAsync)` (dung ten ham) tai dong 926, 952, 967, 990.

**Error handling** - Khong `try`/`catch`. Vi khong truyen `BulkWriteOptions`, thu tu thuc thi la mac dinh cua driver; loi bulk (vi du `MongoBulkWriteException`) duoc nem lai cho caller.

**Khi nao NEN dung** - Cap nhat nhieu ban ghi voi gia tri **khac nhau** cho tung ban ghi trong mot round-trip.

**Khi nao KHONG dung**
- **Khi khong muon tao moi ban ghi**: `IsUpsert = true` duoc gan cho **tung** `UpdateOneModel`, nen moi filter khong khop se sinh mot document moi. Voi lo lon, mot loi map dinh danh co the sinh ra rat nhieu ban ghi rac.
- Khi entity chi dien mot phan truong (xem muc 1.4 — `IsDeleted` bi ghi de).
- Khi can biet phan tu nao bi bo qua: viec `continue` khong duoc ghi log.
- Khi can kiem soat `IsOrdered`: ham khong nhan `BulkWriteOptions`.

**Gioi han**
- Khong nhan `BulkWriteOptions`.
- Bo qua phan tu loi im lang.
- Khong guard tung `Filter` null.
- Khong guard tung `Entity` null -> nem exception tu reflection giua vong lap, cac phan tu da xu ly truoc do **khong** duoc ghi (chua goi `BulkWriteAsync`).
- `true` khi `MatchedCount > 0` — khong cho biet bao nhieu phan tu thanh cong, bao nhieu bi bo.

---

### 2.27 `IsUpdateManyAsync` (overload `List<(Expression, UpdateDefinition)>`)

**Signature**

```csharp
public virtual async Task<bool> IsUpdateManyAsync(
    List<(Expression<Func<TTable, bool>> filter, UpdateDefinition<TTable> entity)> entities,
    CancellationToken cancellationToken = default)
```

**Muc dich** - Cap nhat hang loat bang `UpdateDefinition` co san cho tung phan tu, gom thanh mot `BulkWriteAsync` gom nhieu `UpdateOneModel<TTable> { IsUpsert = true }` (CoreMongoDB.cs:1006-1072, dong 1024).

> [!IMPORTANT]
> Day la overload update **duy nhat KHONG co tham so `audit`**. Cac truong `ModifiedUser` / `ModifiedUserCode` / `ModifiedUserOrganization` / `ModifiedDate` **khong** duoc dong dau tu dong — caller phai tu them vao `UpdateDefinition`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entities` | `List<(Expression<Func<TTable,bool>> filter, UpdateDefinition<TTable> entity)>` | Co | `if (entities is null \|\| entities.IsNullOrEmpty())` -> log + `false` (dong 1012-1018) | Khong co |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 1010) | `default` |

**Output** - `bool`:
- `false` khi `entities` `null`/rong (dong 1017).
- `false` khi `writeModels` rong (dong 1032) — **khong the xay ra neu `entities` khong rong**, vi vong lap them mot `UpdateOneModel` cho **moi** phan tu, khong co dieu kien loc nao.
- `false` khi `result is null` (dong 1047).
- **`true`** khi `result.MatchedCount > 0` (dong 1052-1055).
- **`true`** khi `result.Upserts != null && result.Upserts.Any()` (dong 1058-1061).
- `false` + log cho cac truong hop con lai (dong 1067-1070).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 1010).
2. Guard `entities is null || entities.IsNullOrEmpty()` -> log + `false`.
3. `foreach` tung cap `(Filter, Entity)`: `writeModels.Add(new UpdateOneModel<TTable>(Filter, Entity) { IsUpsert = true });` (dong 1022-1025). **Khong co bat ky kiem tra null nao cho `Filter` hoac `Entity`.**
4. Guard `writeModels is null || writeModels.IsNullOrEmpty()` -> log + `false` (dong 1027-1033) — nhanh chet.
5. Trong `_pipelineWrite`: `BulkWriteAsync(writeModels, ct)` — **khong truyen `BulkWriteOptions`** (dong 1038-1040).
6. Guard `result is null` -> log + `false`.
7. `switch (result.MatchedCount > 0)` -> `true`; nguoc lai kiem tra `Upserts` -> `true`.
8. Log + `false`.

**Side effect**
- **Ghi DB**: nhieu lenh update trong mot bulk; moi lenh co the tao moi document (upsert).
- Khong mutate object cua caller.
- **Ghi log**: `FailLogic` voi `methodName = nameof(IsUpdateManyAsync)` (dung ten ham) tai dong 1014, 1029, 1044, 1067.

**Error handling** - Khong `try`/`catch`. Neu mot phan tu co `Filter` hoac `Entity` la `null`, hanh vi do `MongoDB.Driver` quyet dinh khi khoi tao `UpdateOneModel<TTable>` — **khong xac dinh duoc tu source code cua repo nay**.

**Khi nao NEN dung** - Bulk update voi cac lenh cap nhat khac nhau cho tung ban ghi (`$inc`, `$push`, `$set` cuc bo), khi khong can dong dau audit tu dong.

**Khi nao KHONG dung**
- Khi can dong dau `Modified*` tu dong: overload nay khong ho tro `audit`.
- Khi khong muon tao moi ban ghi (upsert luon bat cho tung phan tu).
- Khi can kiem soat `IsOrdered`.

**Gioi han**
- Khong co tham so `audit`.
- Khong nhan `BulkWriteOptions`.
- Khong validate tung phan tu.
- Nhanh `writeModels.IsNullOrEmpty()` la dead code.

---

### 2.28 `IsDeleteOneAsync`

**Signature**

```csharp
public virtual async Task<bool> IsDeleteOneAsync(
    Expression<Func<TTable, bool>> filter, CancellationToken cancellationToken = default)
```

**Muc dich** - Xoa **vat ly** mot document khop `filter` bang `DeleteOneAsync` (CoreMongoDB.cs:1176-1206).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `Expression<Func<TTable, bool>>` | Co | `if (filter is null)` -> log + `false` (dong 1181-1187) | Khong co |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 1179) | `default` |

**Output** - `bool`:
- `false` khi `filter is null` (dong 1186).
- **`false` khi `result is null` HOAC `result.DeletedCount is 0`** (dong 1196-1202) — tuc la **khong tim thay ban ghi nao de xoa cung tra `false`**, kem log.
- `true` khi `DeletedCount > 0` (dong 1204).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 1179).
2. Guard `filter is null` -> log + `false`.
3. Trong `_pipelineWrite`: `DeleteOneAsync(filter, ct)` (dong 1192-1194).
4. `if (result is null || result.DeletedCount is 0)` -> log + `false` (dong 1196-1202).
5. `return true;` (dong 1204).

**Side effect**
- **Ghi DB**: xoa vinh vien 1 document (khong the phuc hoi tu ung dung).
- **Ghi log**: `FailLogic` voi `methodName = nameof(IsDeleteOneAsync)` (dung ten ham) tai dong 1183, 1198. Message tai dong 1199 dung `filter.ToJSon()` — serialize object `Expression` bang `System.Text.Json`, ket qua co the rat dai hoac roi vao nhanh fallback `Newtonsoft` / thong bao loi cua `ToJSon` (`JSonParseHelpers.cs:33-40`).

**Error handling** - Khong `try`/`catch`; exception tu MongoDB nem lai. Truong hop "khong co gi de xoa" duoc coi la that bai nghiep vu: log `FailLogic` + `false`.

**Khi nao NEN dung** - Xoa han mot ban ghi khi nghiep vu yeu cau xoa vat ly (vi du du lieu tam, cache, ban ghi ky thuat).

**Khi nao KHONG dung**
- **Khi nghiep vu can xoa mem**: khong co ham xoa mem trong class. Phai dung `IsUpdateOneAsync` de dat `IsDeleted = true`.
- Khi coi "khong tim thay" la thanh cong (idempotent delete): ham tra `false` va ghi log loi.
- Khi `filter` co the khop nhieu ban ghi va ban khong ro ban ghi nao bi xoa: `DeleteOneAsync` xoa **ban dau tien** tim duoc, khong co `Sort` de xac dinh.
- Voi cac collection co du lieu can luu vet/audit.

**Gioi han**
- Xoa vat ly, khong the hoan tac.
- Khong phan biet duoc "loi" va "khong co ban ghi khop" tu gia tri tra ve.
- Serialize `Expression` vao log co the ton kem — va chi phi nay chi phat sinh o duong loi (khi `DeletedCount == 0`), tuc dung luc he thong dang co su co.
- Log `FailLogic` tai dong 1198 nam trong lambda pipeline, nhung chi chay sau khi `DeleteOneAsync` tra ve va ngay sau do la `return`, nen **khong** bi ghi trung khi retry (xem ghi chu muc 1.5).

---

### 2.29 `IsDeleteManyAsync`

**Signature**

```csharp
public virtual async Task<bool> IsDeleteManyAsync(
    Expression<Func<TTable, bool>> filter, CancellationToken cancellationToken = default)
```

**Muc dich** - Xoa **vat ly** tat ca document khop `filter` bang `DeleteManyAsync` (CoreMongoDB.cs:1216-1246).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `filter` | `Expression<Func<TTable, bool>>` | Co | `if (filter is null)` -> log + `false` (dong 1221-1227) | Khong co |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 1219) | `default` |

**Output** - `bool`:
- `false` khi `filter is null` (dong 1226).
- **`false` khi `result is null` HOAC `result.DeletedCount is 0`** (dong 1236-1242), kem log.
- `true` khi `DeletedCount > 0` (dong 1244).

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 1219).
2. Guard `filter is null` -> log + `false`.
3. Trong `_pipelineWrite`: `DeleteManyAsync(filter, ct)` (dong 1232-1234).
4. `if (result is null || result.DeletedCount is 0)` -> log + `false`.
5. `return true;` (dong 1244).

**Side effect**
- **Ghi DB**: xoa vinh vien **nhieu** document.
- **Ghi log**: `FailLogic` voi `methodName = nameof(IsDeleteManyAsync)` (dung ten ham) tai dong 1223, 1238.

**Error handling** - Khong `try`/`catch`; exception nem lai. "Khong co gi de xoa" -> log + `false`.

**Khi nao NEN dung** - Don du lieu tam theo dieu kien ro rang (vi du xoa ban ghi cua mot phien lam viec da ket thuc).

**Khi nao KHONG dung**
- **Khi filter co the rong hoac qua rong**: ham khong kiem tra pham vi filter, mot predicate `x => true` se xoa toan bo collection. Khong co xac nhan, khong co gioi han so luong.
- Khi nghiep vu can xoa mem.
- Khi coi "khong co ban ghi khop" la thanh cong.
- Khi can xoa theo lo co kiem soat (khong co batching / khong tra so luong da xoa).

**Gioi han**
- Khong gioi han so document bi xoa.
- Khong tra ve `DeletedCount`.
- Khong the hoan tac.

---

### 2.30 `FindAllWithAggregateAsync<TResult>` (overload `PipelineDefinition<TTable, TResult>`)

**Signature**

```csharp
public virtual async Task<List<TResult>> FindAllWithAggregateAsync<TResult>(
    PipelineDefinition<TTable, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default)
```

**Muc dich** - Chay aggregation pipeline kieu manh, duyet toan bo cursor, loai bo phan tu `null`, tra `List<TResult>` (CoreMongoDB.cs:1257-1292).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `pipeline` | `PipelineDefinition<TTable, TResult>` | Co | `if (pipeline is null) return [];` (dong 1262-1265) — **tra list rong, KHONG nem exception, KHONG ghi log** | Khong co |
| `options` | `AggregateOptions` | Khong | `options ??= _aggregateOptions;` (dong 1267) — mac dinh `BatchSize = 500`, `MaxTime = 30 giay` (CoreMongoDB.cs:26-30) | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 1260) | `default` |

**Output** - `List<TResult>`:
- **`[]`** khi `pipeline is null` (dong 1264).
- **`[]`** khi pipeline chay nhung khong co ket qua (bien `result` khoi tao la `[]` va khong duoc bo sung — dong 1278, 1291).
- Danh sach `TResult` da **loai bo cac phan tu `null`** (`cursor.Current.Where(item => item is not null)` — dong 1283).
- Khong bao gio tra `null`.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 1260).
2. `if (pipeline is null) return [];` (dong 1262-1265).
3. `options ??= _aggregateOptions;` (dong 1267).
4. Trong `_pipelineRead`: `AggregateAsync(pipeline, options, ct)` -> lay `IAsyncCursor<TResult>`, dat trong `using` (dong 1269-1276).
5. `while (await cursor.MoveNextAsync(cancellationToken))` — **NAM NGOAI pipeline Polly** (dong 1280).
6. Voi moi batch: loc `item is not null`, neu `!data.IsNullOrEmpty()` thi `result.AddRange(data)` (dong 1282-1288).
7. `return result;` (dong 1291).

**Side effect**
- Khong ghi DB **tru khi** pipeline chua stage `$out` hoac `$merge` — trong truong hop do lenh ghi do MongoDB thuc hien, class khong kiem tra.
- Khong ghi log.
- Giai phong cursor qua `using` (dong 1269).

**Error handling** - Khong `try`/`catch`. Exception khi lay cursor duoc `_pipelineRead` xu ly; exception khi **duyet** cursor (`MoveNextAsync`) **khong** duoc bao ve boi policy nao va nem thang cho caller.

**Khi nao NEN dung** - Bao cao, gom nhom, `$lookup`, tinh toan phia server ma `Find` khong lam duoc, khi muon kieu manh (compile-time checked) cho pipeline.

**Khi nao KHONG dung**
- Khi ket qua co the rat lon: ham nap **toan bo** cursor vao `List<TResult>` trong bo nho, khong co streaming, khong co `$limit` tu dong.
- Khi can phan biet "pipeline null do lap trinh sai" voi "khong co du lieu": ca hai deu tra `[]` va khong co log.
- Khi can `MaxTime` dai hon 30 giay: phai truyen `options` rieng, neu khong pipeline se bi ngat theo `MaxTime` mac dinh.
- Khi can retry cho toan bo qua trinh doc du lieu (buoc duyet cursor khong duoc retry).

**Gioi han**
- `pipeline is null` bi "nuot" im lang.
- `_aggregateOptions` la `static readonly`, gia tri co dinh trong code (`BatchSize = 500`, `MaxTime = 30s`) — khong cau hinh duoc tu ngoai.
- `options ??= _aggregateOptions` la phep **thay the toan bo**, khong phai gop: neu caller truyen `options` cua rieng minh thi `BatchSize = 500` va `MaxTime = 30s` **mat hoan toan**, chi con nhung gia tri caller tu dat.
- Chay tren `_dbReadContext` (dong 1274). Neu pipeline chua stage `$out` / `$merge` thi lenh **ghi** se di qua ket noi/database **doc**, khong qua `_dbWriteContext`.
- Buoc duyet cursor khong nam trong resilience pipeline.
- `await cursor.MoveNextAsync(...)` **khong** co `.ConfigureAwait(false)` (dong 1280), khac voi toan bo cac `await` con lai trong file.
- Loc `item is not null` lam so phan tu tra ve co the it hon so document pipeline sinh ra, ma khong co canh bao nao.

---

### 2.31 `FindAllWithAggregateAsync` (overload `BsonDocument[]`, tra `TTable`)

**Signature**

```csharp
public virtual async Task<List<TTable>> FindAllWithAggregateAsync(
    BsonDocument[] pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default)
```

**Muc dich** - Chay aggregation pipeline dang mang `BsonDocument`, anh xa ket qua ve `TTable` (CoreMongoDB.cs:1301-1336).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `pipeline` | `BsonDocument[]` | Co | `if (pipeline is null) return [];` (dong 1306-1309). **Mang rong `[]` KHONG bi guard** — se chay aggregate voi pipeline rong | Khong co |
| `options` | `AggregateOptions` | Khong | `options ??= _aggregateOptions;` (dong 1311) — `BatchSize = 500`, `MaxTime = 30 giay` | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 1304) | `default` |

**Output** - `List<TTable>`:
- **`[]`** khi `pipeline is null` (dong 1308).
- **`[]`** khi khong co ket qua (dong 1322, 1335).
- Danh sach `TTable` da loai bo phan tu `null` (dong 1327).
- Khong bao gio tra `null`.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 1304).
2. `if (pipeline is null) return [];` (dong 1306-1309).
3. `options ??= _aggregateOptions;` (dong 1311).
4. Trong `_pipelineRead`: `AggregateAsync<TTable>(pipeline, options, ct)` (dong 1313-1320).
5. `while (await cursor.MoveNextAsync(cancellationToken))` — **NGOAI pipeline Polly** (dong 1324).
6. Loc `item is not null`, `AddRange` (dong 1326-1332).
7. `return result;` (dong 1335).

**Side effect** - Nhu muc 2.30 (khong ghi log; `$out` / `$merge` trong pipeline se ghi DB).

**Error handling** - Nhu muc 2.30: exception khi duyet cursor khong duoc policy bao ve.

**Khi nao NEN dung** - Khi pipeline duoc viet san dang JSON/BSON (vi du lay tu cau hinh) va ket qua co cau truc trung voi `TTable`.

**Khi nao KHONG dung**
- Khi ket qua **khong** co cau truc `TTable`: phai dung overload `TResult` (muc 2.32), neu khong se loi deserialize.
- Khi ket qua co the rat lon (nap toan bo vao bo nho).
- Khi can biet pipeline sai (null) — bi nuot im lang.

**Gioi han**
- Mat kiem tra kieu tai compile time (pipeline la `BsonDocument`).
- `pipeline` la mang rong khong bi guard.
- Cung cac gioi han ve `_aggregateOptions` va cursor nhu muc 2.30.

---

### 2.32 `FindAllWithAggregateAsync<TResult>` (overload `BsonDocument[]`)

**Signature**

```csharp
public virtual async Task<List<TResult>> FindAllWithAggregateAsync<TResult>(
    BsonDocument[] pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default)
```

**Muc dich** - Chay aggregation pipeline dang `BsonDocument[]`, anh xa ket qua ve kieu `TResult` tuy y (CoreMongoDB.cs:1347-1382).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `pipeline` | `BsonDocument[]` | Co | `if (pipeline is null) return [];` (dong 1352-1355). Mang rong khong bi guard | Khong co |
| `options` | `AggregateOptions` | Khong | `options ??= _aggregateOptions;` (dong 1357) | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 1350) | `default` |

**Output** - `List<TResult>`: `[]` khi `pipeline is null` hoac khong co ket qua; nguoc lai la danh sach da loc bo phan tu `null` (dong 1373). Khong bao gio `null`.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 1350).
2. `if (pipeline is null) return [];` (dong 1352-1355).
3. `options ??= _aggregateOptions;` (dong 1357).
4. Trong `_pipelineRead`: `AggregateAsync<TResult>(pipeline, options, ct)` (dong 1359-1366).
5. `while (await cursor.MoveNextAsync(cancellationToken))` — **NGOAI pipeline Polly** (dong 1370).
6. Loc `item is not null`, `AddRange` (dong 1372-1378).
7. `return result;` (dong 1381).

**Side effect** - Nhu muc 2.30.

**Error handling** - Nhu muc 2.30.

**Khi nao NEN dung** - Bao cao/thong ke co cau truc ket qua rieng (`$group`, `$project`, `$facet`), voi `TResult` la mot DTO chuyen dung.

**Khi nao KHONG dung**
- Khi ket qua rat lon.
- Khi `TResult` khong khop cau truc document sinh ra tu pipeline (loi deserialize se nem exception khi duyet cursor — ngoai vong bao ve cua policy).
- Khi can phat hien pipeline `null` (bi nuot im lang).

**Gioi han** - Giong muc 2.31: mat kiem tra kieu, mang rong khong bi guard, `_aggregateOptions` co dinh, cursor khong duoc retry.

---

### 2.33 `BulkWriteAsync`

**Signature**

```csharp
public virtual async Task<bool> BulkWriteAsync(
    IEnumerable<WriteModel<TTable>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default)
```

**Muc dich** - Thuc thi mot lo lenh ghi tuy y (`InsertOneModel`, `UpdateOneModel`, `UpdateManyModel`, `ReplaceOneModel`, `DeleteOneModel`, `DeleteManyModel`) do caller tu xay, trong mot lan goi `BulkWriteAsync` (CoreMongoDB.cs:1393-1418). **Day la API duy nhat cho phep caller tu quyet dinh `IsUpsert`.**

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `requests` | `IEnumerable<WriteModel<TTable>>` | Co | **Khong co guard null, khong guard rong** — truyen thang cho driver | Khong co |
| `options` | `BulkWriteOptions` | Khong | Khong validate; truyen thang (`null` = mac dinh cua driver) | `null` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` (dong 1396) | `default` |

**Output** - `bool`, quyet dinh bang `switch` expression (dong 1407-1416):

| Dieu kien | Ket qua |
|---|---|
| `result` co `IsAcknowledged: false` | `false` |
| `result.MatchedCount > 0` | `true` |
| `result.Upserts.Count > 0` | `true` |
| Con lai (bao gom `result is null`, hoac bulk chi gom **insert** / **delete** thanh cong) | **`false`** |

> [!CAUTION]
> `BulkWriteResult` cua mot lo **chi gom `InsertOneModel`** co `MatchedCount = 0` va `Upserts` rong -> ham tra **`false`** du cac ban ghi da duoc chen thanh cong. Tuong tu voi lo **chi gom `DeleteOneModel` / `DeleteManyModel`** (`DeletedCount` khong duoc xet den). Xem muc 3, #10.

**Dieu kien xu ly**
1. `ThrowIfCancellationRequested()` (dong 1396).
2. Trong `_pipelineWrite`: `BulkWriteAsync(requests, options, ct)` (dong 1401-1405).
3. `switch` tren `result` theo bang tren (dong 1407-1416).

**Side effect**
- **Ghi DB**: thuc thi toan bo cac lenh trong `requests` (insert / update / replace / delete).
- **Khong ghi log** — day la ham ghi duy nhat khong goi `_logger.FailLogic` o bat ky nhanh nao.

**Error handling** - Khong `try`/`catch`. `requests` la `null` -> loi den tu driver, khong co log. Loi bulk (`MongoBulkWriteException`) duoc nem lai cho caller cung voi chi tiet tung lenh.

**Khi nao NEN dung**
- **Khi can update ma KHONG muon upsert**: tao `UpdateOneModel<TTable>(filter, update) { IsUpsert = false }`. Day la duong duy nhat trong class de lam viec do.
- Khi can tron nhieu loai lenh ghi trong mot round-trip.
- Khi can kiem soat `IsOrdered` qua `BulkWriteOptions`.

**Khi nao KHONG dung**
- **Khi lo chi gom insert hoac chi gom delete**: gia tri tra ve `false` khong phan anh ket qua thuc. Dung `IsCreateManyAsync` / `IsDeleteManyAsync`, hoac bo qua gia tri tra ve va chi dua vao exception.
- Khi can biet chi tiet ket qua (`InsertedCount`, `ModifiedCount`, `DeletedCount`): ham chi tra `bool`.
- Khi caller khong the tu bao dam `requests` khac `null` va khong rong.

**Gioi han**
- Khong guard `requests` null/rong.
- Khong ghi log o bat ky nhanh nao.
- Logic tra ve bo qua hoan toan `InsertedCount` va `DeletedCount`.
- `requests` duoc capture vao lambda; neu la `IEnumerable` lazy khong buffer thi moi lan retry se duyet lai nguon.

---

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | **Hai overload `QueryContext` dung `Skip(pageNumber)` — skip theo SO TRANG, khong nhan voi `pageSize`.** Cac overload con lai dung `Skip((pageNumber - 1) * pageSize)`. Cung mot ten ham `FindAllPagingAsync` co hai ngu nghia phan trang khac nhau | `CoreMongoDB.cs:149`, `CoreMongoDB.cs:194` (sai ngu nghia) so voi `CoreMongoDB.cs:107`, `230`, `262`, `293`, `320` | **Nghiem trong.** Voi `pageSize = 10`, trang 2 chi skip 2 document thay vi 10 -> cac trang lien tiep trung lap gan het du lieu; nguoi dung thay ban ghi lap lai va khong bao gio den duoc cuoi danh sach |
| 2 | Thu tu tham so cua hai overload `QueryContext` la `(pageSize, pageNumber)`, nguoc voi 5 overload con lai `(pageNumber, pageSize)` | `CoreMongoDB.cs:128`, `174` so voi `96`, `219`, `252`, `283`, `311` | Goi bang tham so vi tri rat de hoan doi hai gia tri ma compiler khong bao loi (ca hai deu la `int`); ket hop voi #1 gay loi phan trang kho phat hien |
| 3 | **Toan bo 6 ham update deu hardcode `IsUpsert = true`**, khong co cach tat | `CoreMongoDB.cs:638`, `715`, `792`, `874`, `947`, `1024` | Khi `filter` khong khop ban ghi nao, MongoDB **tao document moi** va ham tra **`true`**. Mot `filter` sai (sai ma dinh danh, sai kieu du lieu) sinh ra du lieu rac thay vi bao loi. Voi overload bulk (`947`, `1024`), moi phan tu khong khop tao mot document rac |
| 4 | `MapUpdateDefinition` chi bo qua property co gia tri `null`; property **value type khong nullable** luon duoc dua vao `$set`, ke ca `IsDeleted` (kieu `bool`) | `ProjectToExtensions.cs:293-304`; `BaseEntityMongoDB.cs:17`; duoc goi tai `CoreMongoDB.cs:622`, `863`, `940` | Goi `IsUpdateOneAsync(filter, entity)` voi entity dien mot phan se ghi `IsDeleted = false`, `int = 0`, `DateTime = default` len document -> **co the "hoi sinh" ban ghi da xoa mem** va lam mat du lieu cac truong value type khac |
| 5 | `SetDataUpdatedDefault` bo qua hoan toan khi `audit == null` (guard `entity is null \|\| audit is null`), trong khi `SetDataCreatedDefault` chi guard `entity is null` | `ProjectToExtensions.cs:430` so voi `ProjectToExtensions.cs:480` | Bat doi xung: goi update **khong** truyen `audit` -> `ModifiedUser` / `ModifiedDate` khong duoc cap nhat, khong co canh bao nao. Goi create khong truyen `audit` -> van dong dau `"Anonymous"` / `"0"` / `"FTEL"` |
| 6 | Vong lap duyet cursor cua aggregate nam **ngoai** `_pipelineRead` | `CoreMongoDB.cs:1280`, `1324`, `1370` | Loi mang/timeout xay ra trong qua trinh duyet cursor **khong** duoc retry va khong duoc tinh vao circuit breaker; exception nem thang cho caller. Chi buoc lay cursor duoc bao ve |
| 7 | `IsCreateOneAsync` / `IsCreateManyAsync` **luon** `return true` sau khi goi driver, khong kiem tra ket qua nao | `CoreMongoDB.cs:1112`, `1166` | `bool` tra ve khong mang thong tin nghiep vu; caller khong the phan biet thanh cong that su. XML doc "false nếu có lỗi" (`ICoreMongoDB.cs:234`, `246`) **mau thuan voi than ham** (khong co `try`/`catch` nao trong file) |
| 8 | Nhieu nhanh kiem tra `null` sau khi goi `SetDataUpdatedDefault` / `SetDataCreatedDefault` la **dead code** — cac ham nay khong bao gio bien gia tri khac `null` thanh `null` | `CoreMongoDB.cs:700-706`, `777-783`, `854-860`, `1097-1103`; can cu `ProjectToExtensions.cs:430`, `462`, `480` | Gay nham lan khi doc code; message log tai `CoreMongoDB.cs:857` con mo ta sai nguyen nhan (`"MapUpdateDefinition ... is null"` cho dieu kien `entity is null`) |
| 9 | `IsUpdateManyAsync(Expression, TTable, ...)` tra `false` khi `mapUpdateDefinition is null` **ma khong ghi log** — vi tri duy nhat trong nhom update lam vay | `CoreMongoDB.cs:865` | Mot lenh update im lang tra `false`, khong co dau vet trong log -> rat kho debug tren moi truong that |
| 10 | `BulkWriteAsync` chi xet `IsAcknowledged`, `MatchedCount`, `Upserts.Count`; **bo qua `InsertedCount` va `DeletedCount`** | `CoreMongoDB.cs:1407-1416` | Lo bulk chi gom insert hoac chi gom delete se tra **`false`** du da thuc thi thanh cong -> caller co the retry hoac bao loi sai |
| 11 | `IsDeleteOneAsync` / `IsDeleteManyAsync` coi `DeletedCount == 0` la that bai: ghi log `FailLogic` va tra `false` | `CoreMongoDB.cs:1196-1202`, `1236-1242` | Xoa idempotent (ban ghi da bi xoa truoc do) bi bao la loi, sinh log nhieu. Caller khong phan biet duoc "loi that" va "khong co gi de xoa" |
| 12 | Cac lenh `_logger.FailLogic` nam **ben trong** lambda `ExecuteAsync`. **Chung KHONG bi ghi trung khi retry**: moi dong log chi chay sau khi lenh driver da tra ve va ngay sau do la `return`, con retry chi kich hoat khi callback **nem exception** (`ShouldHandle` chi xet `args.Outcome.Exception`). Ca 12 vi tri deu thuoc `_pipelineWrite`, khong phai pipeline doc | `CoreMongoDB.cs:643`, `666`, `720`, `743`, `797`, `820`, `967`, `990`, `1044`, `1067`, `1198`, `1238`; `MongoResiliencePolicyFactory.cs:84-85`, `182-183` | Khong co van de log trung. Dieu **thuc su** chay lai khi retry la lenh ghi cua driver (`UpdateOneAsync` / `UpdateManyAsync` / `InsertOneAsync` / `BulkWriteAsync` / `Delete*Async`); vi vay factory da co y **khong** retry `MongoConnectionException` / `SocketException` o luong ghi (`MongoResiliencePolicyFactory.cs:218-225`) de tranh ghi hai lan |
| 13 | Overload `IsUpdateManyAsync(List<(Expression, UpdateDefinition)>)` la ham update duy nhat **khong co tham so `audit`** | `CoreMongoDB.cs:1006-1008`, `ICoreMongoDB.cs:311-312` | Bulk update qua overload nay khong dong dau `Modified*`; du lieu audit khong nhat quan giua cac duong ghi |
| 14 | Ten tham so khong khop giua interface va implementation: `entites` (interface) va `entities` (implementation) | `ICoreMongoDB.cs:248` so voi `CoreMongoDB.cs:1125` | Goi bang named argument phai dung ten khac nhau tuy theo bien duoc khai bao la `ICoreMongoDB<T>` hay class cu the -> loi bien dich kho hieu |
| 15 | Hai overload `CountAllAsync` (`FilterDefinition` va `Expression`) khong the phan giai khi truyen `null` truc tiep | `ICoreMongoDB.cs:17`, `30`; `CoreMongoDB.cs:64`, `370` | `CountAllAsync(null)` khong bien dich duoc; phai ep kieu ro rang hoac goi `CountAllAsync()` |
| 16 | XML doc nhac den field `_retryPolicy` — field nay **khong ton tai** trong `CoreMongoDB.cs` (field that la `_pipelineRead` / `_pipelineWrite`) | `ICoreMongoDB.cs:27`, `41`; `CoreMongoDB.cs:335`, `367` | Tai lieu XML lech so voi hien thuc; nguoi doc/AI agent co the tim mot field khong ton tai |
| 17 | `SetDataCreatedDefault` co dong `SetPropertyIfNull(nameof(BaseEntityMongoDB.IsDeleted), false)` nhung `IsDeleted` la `bool` khong nullable nen dieu kien `is null` **khong bao gio dung** | `ProjectToExtensions.cs:502`; `BaseEntityMongoDB.cs:17` | Dong code khong co tac dung (ket qua van la `false` do gia tri khoi tao). Neu sau nay `IsDeleted` doi thanh `bool?`, hanh vi se thay doi bat ngo |
| 18 | Cac ham `*SortDeletedAsync` phu thuoc ten property `"IsDeleted"` **hardcode dang chuoi** trong expression tree | `PrecateBuilderExtensions.cs:71`; duoc goi tai `CoreMongoDB.cs:346`, `424`, `475`, `529`, `581` | `TTable` khong co property `IsDeleted` -> exception tai runtime (khong phai loi bien dich). Doi ten property se lam 5 ham nay hong ma compiler khong canh bao |
| 19 | Nhom `FindAllAsync` / `FindAllSortDeletedAsync` khong co `Limit`, khong co `Sort`, khong co `Skip` | `CoreMongoDB.cs:403-404`, `433-434`, `457-458`, `484-485` | Nap toan bo tap ket qua vao bo nho; voi collection lon co the gay `OutOfMemoryException` va do tre cao. Voi nhom `<TDto>` con them chi phi reflection cho tung ban ghi |
| 20 | Hai overload `QueryContext` khong guard `queryContext == null` va khong guard `queryContext.Selector == null` (gia tri mac dinh cua record la `null`) | `CoreMongoDB.cs:133-135`, `179-180`; `ProjectToExtensions.cs:13` | `queryContext == null` -> `NullReferenceException` truoc khi vao pipeline. `Selector == null` duoc truyen thang vao `.Project(...)`; hanh vi do driver quyet dinh, khong xac dinh duoc tu source code cua repo |
| 21 | Hai overload `QueryContext` khong ap `Limit` khi `pageSize <= 0` | `CoreMongoDB.cs:142-145`, `187-190` | Truyen `pageSize = 0` (hoac so am) se lay **toan bo** ket qua khop filter, khong co gioi han |
| 22 | Khong ham nao validate `pageNumber` / `pageSize` (am, `0`, tran so) | `CoreMongoDB.cs:107`, `230`, `262`, `293`, `320` | `pageNumber = 0` -> `Skip(-pageSize)` (gia tri am); `pageSize` lon -> `(pageNumber - 1) * pageSize` co the tran `int`. Hanh vi cuoi do driver/server quyet dinh — khong xac dinh duoc tu source code cua repo |
| 23 | `IsCreateManyAsync` duyet `IEnumerable<TTable>` **hai lan**: mot lan trong `IsNullOrEmpty` va mot lan trong `foreach` | `CoreMongoDB.cs:1130`, `1140`; `CollectionHelpers.cs:36` | Voi `IEnumerable` lazy khong buffer (vi du truy van LINQ tren nguon khac), nguon du lieu bi thuc thi lai; ket qua co the khac nhau giua hai lan duyet |
| 24 | `IsUpdateManyAsync(List<(Expression, TTable)>)` bo qua phan tu co `mapUpdateDefinition is null` bang `continue` ma **khong ghi log** | `CoreMongoDB.cs:942-945` | Mot phan cua lo bi bo im lang; ham van co the tra `true` -> caller tuong toan bo lo da duoc cap nhat |
| 25 | `IsUpdateManyAsync(List<(Expression, UpdateDefinition)>)` khong kiem tra `Filter` / `Entity` cua tung phan tu truoc khi tao `UpdateOneModel<TTable>` | `CoreMongoDB.cs:1022-1025` | Phan tu `null` khien viec khoi tao `UpdateOneModel` that bai; hanh vi cu the do `MongoDB.Driver` quyet dinh — khong xac dinh duoc tu source code cua repo nay |
| 26 | Hai overload bulk `IsUpdateManyAsync(List<...>)` goi `BulkWriteAsync` **khong truyen `BulkWriteOptions`** | `CoreMongoDB.cs:962-963`, `1039-1040` | Khong the chon `IsOrdered = false`; theo mac dinh cua driver, mot lenh loi se chan cac lenh phia sau |
| 27 | `_aggregateOptions` la `private static readonly` voi gia tri co dinh trong code: `BatchSize = 500`, `MaxTime = TimeSpan.FromSeconds(30)` | `CoreMongoDB.cs:26-30` | Khong cau hinh duoc tu ngoai. Aggregate chay lau hon 30 giay bi ngat, tru khi caller nho truyen `options` rieng cho **tung** lan goi |
| 28 | 3 overload `FindAllWithAggregateAsync` tra `[]` khi `pipeline is null` — **khong log, khong nem exception** | `CoreMongoDB.cs:1262-1265`, `1306-1309`, `1352-1355` | Loi lap trinh (quen tao pipeline) bi che giau thanh "khong co du lieu"; rat kho phat hien tren moi truong that |
| 29 | 3 overload `FindAllWithAggregateAsync` loc bo phan tu `null` khoi ket qua (`Where(item => item is not null)`) ma khong bao hieu | `CoreMongoDB.cs:1283`, `1327`, `1373` | So phan tu tra ve co the it hon so document pipeline sinh ra; sai lech khi dung ket qua de dem |
| 30 | `BulkWriteAsync` khong ghi log o bat ky nhanh nao va khong guard `requests` null/rong | `CoreMongoDB.cs:1393-1418` | Ham ghi duy nhat khong co dau vet log; `requests` null/rong -> loi tu driver, khong co ngu canh nghiep vu |
| 31 | Constructor khong validate `null` cho bat ky tham so nao | `CoreMongoDB.cs:32-53` | Loi cau hinh DI (thieu `ILogger` hoac `ResiliencePipeline`) chi bung ra o lan goi API dau tien, dang `NullReferenceException` kho truy nguyen |
| 32 | `CommonBaseConstant.DateTimeUtc()` mac dinh `addHour = 7` nen `CreatedDate` / `ModifiedDate` luu **UTC+7** vao truong `DateTime` | `CommonBaseConstant.cs:47-50`; duoc goi tai `ProjectToExtensions.cs:455`, `473`, `507`, `526` | Ten ham la `DateTimeUtc` nhung gia tri khong phai UTC -> de so sanh sai voi du lieu luu UTC (vi du `$currentDate` cua MongoDB) hoac sai lech 7 gio khi doc lai |
| 33 | `ProjectTo` (ban `List`) bat exception cho tung property va ghi log ra **Console**, khong qua `ILogger` | `ProjectToExtensions.cs:112-115`, `120-123` | Loi anh xa DTO khong xuat hien trong he thong log tap trung; du lieu tra ve co the thieu truong ma khong co canh bao |
| 34 | Class khong co overload nao nhan `IClientSessionHandle` | Toan bo `CoreMongoDB.cs` | Khong the tham gia MongoDB transaction qua repository nay; cac lenh ghi lien quan khong the nguyen tu hoa |
| 35 | `IsDeleteOneAsync` / `IsDeleteManyAsync` serialize `Expression` vao message log qua `filter.ToJSon()` | `CoreMongoDB.cs:1199`, `1239` | `System.Text.Json` co the khong serialize duoc `Expression` va roi vao nhanh fallback `Newtonsoft` hoac nhanh bao loi (`JSonParseHelpers.cs:33-40`) -> log dai/kho doc va ton CPU tren duong loi |
| 36 | Trong hau het cac ham, tham so cua lambda `ExecuteAsync` duoc dat **cung ten** `cancellationToken` voi tham so cua ham; rieng `CountAllAsync(FilterDefinition)` dat la `ct` | `CoreMongoDB.cs:74` (dung `ct`) so voi `102`, `154`, `199`, `226`, `258`, `289`, `316`, ... | Khong nhat quan ve phong cach; khi doc code de nham lan bien nao dang duoc dung ben trong lambda |
| 37 | 3 vong lap duyet cursor cua aggregate **thieu `.ConfigureAwait(false)`**, khac voi 100% cac `await` con lai trong file | `CoreMongoDB.cs:1280`, `1324`, `1370` | Khi duoc goi tu moi truong co `SynchronizationContext` (WinForms/WPF, hoac code dong bo `.Result` / `.Wait()`), phan tiep sau moi `MoveNextAsync` phai quay ve context goc -> rui ro deadlock va tang do tre; khong nhat quan voi phan con lai cua file |
| 38 | 3 overload `FindAllWithAggregateAsync` chay tren `_dbReadContext` | `CoreMongoDB.cs:1274`, `1318`, `1364` | Pipeline chua stage `$out` / `$merge` se **ghi du lieu qua database/ket noi doc**, khong qua `_dbWriteContext`; class khong kiem tra noi dung pipeline nen khong co canh bao nao |
| 39 | Doc va ghi di qua **hai `IMongoDatabase` khac nhau**, khong co `IClientSessionHandle` / causal consistency | `CoreMongoDB.cs:42-48`; toan bo file | Neu `dbContextRead` tro vao secondary (hoac cluster khac), mot lenh `FindOneAsync` ngay sau `IsUpdateOneAsync` / `IsCreateOneAsync` **co the khong thay** du lieu vua ghi (read-your-write khong duoc bao dam). Mo hinh read/write splitting nay khong co co che nao bu lai o tang repository |
| 40 | Hai collection duoc boc trong `new Lazy<IMongoCollection<TTable>>(Func<...>)` — che do mac dinh `LazyThreadSafetyMode.ExecutionAndPublication` **cache ca exception** | `CoreMongoDB.cs:42-48` | Neu `GetCollection` nem exception o lan truy cap dau tien (vi du `dbContextRead` la `null`, `collectionName` khong hop le), moi lan `.Value` sau do se nem **lai dung exception cu**: instance repository hong vinh vien, khong the tu phuc hoi ma khong tao lai object |
| 41 | `IsUpdateManyAsync(List<(Expression, TTable)>)` khong guard `Entity is null` cua tung phan tu | `CoreMongoDB.cs:936-940`; `ProjectToExtensions.cs:291` | `SetDataUpdatedDefault(null, ...)` tra `null`, roi `MapUpdateDefinition(null)` goi `PropertyInfo.GetValue(null)` -> **nem exception giua vong lap**, khong phai `continue` im lang. Toan bo lo bi huy truoc khi goi `BulkWriteAsync` (chua co lenh nao duoc ghi) |
| 42 | `options ??= _aggregateOptions` la phep **thay the**, khong phai gop cau hinh | `CoreMongoDB.cs:1267`, `1311`, `1357` | Caller chi muon doi `MaxTime` van phai tu dat lai `BatchSize`; neu quen, `BatchSize = 500` bi mat va quay ve mac dinh cua driver |
| 43 | `MongoResiliencePolicyFactory` `Add` circuit breaker **truoc** retry, nen thu tu thuc thi la `CircuitBreaker -> Retry -> lenh driver` | `MongoResiliencePolicyFactory.cs:22-107`, `120-206` | Circuit breaker chi dem **mot** that bai cho moi lan `ExecuteAsync` (sau khi retry da can), khong dem tung lan retry -> `MinimumThroughput` (`5` doc / `10` ghi) dat cham hon nhieu so voi truc giac; nguoc lai, khi circuit dang `Open` thi ca chuoi retry bi chan ngay (hanh vi mong doi) |

**Doi chieu Reconcile (module moi):** Phat hien #17 da duoc doi chieu doc lap lan 3 voi `BaseEntityMongoDB.cs:17` va `ProjectToExtensions.cs:502` — xac nhan noi dung hien tai cua bang tren la CHINH XAC (khong phai loi, khong sua noi dung).
