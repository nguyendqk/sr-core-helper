# UnitOfWork & DbContexts (EF Core)

> Nguon:
> - `FTELSRCore.Shared/Data/SQL/UnitOfWork/IUnitOfWork.cs`
> - `FTELSRCore.Shared/Data/SQL/UnitOfWork/UnitOfWork.cs`
> - `FTELSRCore.Shared/Data/SQL/DbContexts/Read/ReadDbContext.cs`
> - `FTELSRCore.Shared/Data/SQL/DbContexts/Write/WriteDbContext.cs`
>
> Loai:
> - `IUnitOfWork<DBContextWrite>` - interface (generic)
> - `UnitOfWork<DBContextWrite>` - partial class (generic, primary constructor)
> - `ReadDbContext<TContext>` - class (generic, ke thua `DbContext`)
> - `WriteDbContext<TContext>` - partial class (generic, ke thua `DbContext`, chia thanh 4 khoi partial trong cung 1 file)
>
> Cap nhat theo commit: `2262829`
> Target framework: `net9.0`; `Microsoft.EntityFrameworkCore` 9.0.18; `MediatR` 12.4.1 (theo `FTELSRCore.Shared/FTELSRCore.Shared.csproj`)

---

## 1. Tong quan

Module nay la tang persistence EF Core cua thu vien `FTELSRCore.Shared`. No cung cap ba thanh phan:

1. `ReadDbContext<TContext>` - `DbContext` danh cho luong doc.
2. `WriteDbContext<TContext>` - `DbContext` danh cho luong ghi, ghi de `SaveChanges`/`SaveChangesAsync` de tu dong gan (stamp) thong tin audit truoc khi luu va phat tan (dispatch) domain event qua MediatR `IPublisher` sau khi luu thanh cong.
3. `UnitOfWork<DBContextWrite>` + `IUnitOfWork<DBContextWrite>` - lop quan ly vong doi cua mot `WriteDbContext` va mot transaction EF Core (`IDbContextTransaction`).

Day la thu vien dung chung (helper library), khong phai ung dung chay truc tiep. Trong pham vi repo nay `IUnitOfWork`/`UnitOfWork` **khong duoc tham chieu boi bat ky file `.cs` nao khac** (kiem tra bang `grep -rn "UnitOfWork" --include="*.cs"` - chi khop hai file trong `Data/SQL/UnitOfWork/`), va cung **khong co dong `AddDbContext`/`AddDbContextFactory`/`services.AddScoped<IUnitOfWork...>` nao trong repo**. Viec dang ky DI thuoc trach nhiem cua ung dung tieu thu thu vien.

> [!IMPORTANT]
> `WriteDbContext<TContext>` la lop duoc su dung thuc te trong repo: `FTELSRCore.Shared/Data/SQL/Core/CoreSQL.cs` goi `context.SaveChangesAsync(audit: auditLog, cancellationToken: ct)` (vi du tai `CoreSQL.cs:653`, `CoreSQL.cs:691`) - tuc la overload co tham so `AuditModel`.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Tao va cache mot `DBContextWrite` tu `IDbContextFactory<DBContextWrite>` (`UnitOfWork.cs:24-34`) | Khong tao/quan ly context doc (`ReadDbContext`) - `UnitOfWork` chi lam viec voi context ghi |
| Mo transaction, commit, rollback, dispose transaction (`UnitOfWork.cs:43-151`) | Khong co API `SaveChanges` cong khai tren `UnitOfWork` - `SaveChangeAsync` la `private` (`UnitOfWork.cs:153`) |
| Tu dong rollback transaction cu con treo khi tao transaction moi (`UnitOfWork.cs:47-53`) | Khong ho tro transaction long nhau (nested) hay savepoint - transaction cu bi rollback, khong duoc giu lai |
| Tu dong rollback khi `CommitAsync` that bai roi nem lai ngoai le goc (`UnitOfWork.cs:82-101`) | `RollbackAsync` khong co null-check `_transaction` (`UnitOfWork.cs:111`) - goi truc tiep khi chua co transaction se nem `NullReferenceException` |
| Gan audit `CreatedUser*`/`CreatedDate` cho entity `Added` (`WriteDbContext.cs:152-167`) | Khong gan audit cho entity `Modified` khi `audit` la `null` (`WriteDbContext.cs:170-173` `break` som) |
| Gan audit `ModifiedUser*`/`ModifiedDate` cho entity `Modified` khi truyen `AuditModel` (`WriteDbContext.cs:175-178`) | Khong ghi audit log chi tiet truoc/sau (old/new values) - toan bo khoi code do bi comment "NOT SUPPORT" (`WriteDbContext.cs:201-353`) |
| Phat tan domain event tu cac `Aggregate` dang duoc theo doi, qua `IPublisher` (`WriteDbContext.cs:416-448`) | Khong dam bao domain event chi duoc publish sau khi transaction commit (xem muc 6, van de #1) |
| Nap entity configuration tu assembly cua `TContext` (`ReadDbContext.cs:30`, `WriteDbContext.cs:39`) | `ReadDbContext` **khong** ghi de `SaveChanges`/`SaveChangesAsync`, **khong** cau hinh `QueryTrackingBehavior`, **khong** gan interceptor - tuc la ve ky thuat van ghi duoc DB qua context doc |
| `DispatchDomainEvents` la `public`, co the goi thu cong (`WriteDbContext.cs:416`) | Khong co retry/resilience trong module nay (retry nam o `CoreSQL`, khong nam o day) |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `Microsoft.EntityFrameworkCore.DbContext` | Lop co so cua `ReadDbContext<TContext>` va `WriteDbContext<TContext>` |
| `Microsoft.EntityFrameworkCore.IDbContextFactory<DBContextWrite>` (boc trong `Lazy<>`) | `UnitOfWork` dung de tao context: `dbContext.Value.CreateDbContextAsync(...)` (`UnitOfWork.cs:31`) |
| `Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction` | Kieu transaction do `BeginTransactionAsync` tra ve (`UnitOfWork.cs:55`) |
| `Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry` | Duyet cac entry trong `OnBeforeSaveChanges` (`WriteDbContext.cs:131`) |
| `Microsoft.Extensions.DependencyInjection.IServiceScopeFactory` (boc trong `Lazy<>`) | `WriteDbContext` tao scope DI moi de resolve `IPublisher` (`WriteDbContext.cs:440`) |
| `MediatR.IPublisher` (global using `MediatR` tai `FTELSRCore.Shared/GlobalUsing.cs:11`) | Publish domain event (`WriteDbContext.cs:442-447`) |
| `ILogger<UnitOfWork<DBContextWrite>>` + extension `LoggerExtensions.Warning` (`Extensions/Loggers/LoggerExtensions.cs:254`) | Ghi log vong doi transaction. Extension nay map vao `LogLevel.Warning` (`LoggerExtensions.cs:134-137`) |
| `FTELSRCore.Abstractions.Entities.IBaseEntitySQL` | Bo loc entity duoc gan audit (`WriteDbContext.cs:132`); dinh nghia tai `Abstractions/Entities/BaseEntitySQL.cs:21-34` |
| `FTELSRCore.Abstractions.Aggregate` / `IDomainEvent` | Nguon domain event (`WriteDbContext.cs:421`); dinh nghia tai `Abstractions/Aggregate.cs:12`, `Abstractions/IDomainEvent.cs:3` |
| `FTELSRCore.Models.Audits.AuditModel` / `CreatorInfo` / `SnapshotAuditModel` | Kieu tham so audit va kieu tra ve cua `DetectChangesAudit` (`Models/Audits/AuditModel.cs:3`, `Models/Audits/SnapshotAuditModel.cs:5`) |
| `FTELSRCore.Constants.CommonBaseConstant` | Gia tri mac dinh cho audit: `Anonymous` = `"Anonymous"`, `AnonymousCode` = `"0"`, `OrganizationForISC` = `"FTEL"`, `DateTimeUtc(int addHour = 7)` (`Constants/CommonBaseConstant.cs:29-51`) |
| `FTELSRCore.Helpers.CollectionHelpers.IsNullOrEmpty<T>` | Kiem tra rong (`WriteDbContext.cs:134`, `WriteDbContext.cs:426`); dinh nghia tai `Helpers/CollectionHelpers.cs:14` |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `IUnitOfWork<DBContextWrite>.Context` | Interface | Hop dong lay context ghi |
| `IUnitOfWork<DBContextWrite>.CreateTransactionAsync` | Interface | Hop dong mo transaction |
| `IUnitOfWork<DBContextWrite>.CommitAsync` | Interface | Hop dong commit |
| `IUnitOfWork<DBContextWrite>.RollbackAsync` | Interface | Hop dong rollback |
| `UnitOfWork<DBContextWrite>.Context` | UnitOfWork - public | Tao (hoac tra ve cache) `DBContextWrite` |
| `UnitOfWork<DBContextWrite>.CreateTransactionAsync` | UnitOfWork - public | Mo transaction moi, tu rollback transaction cu neu con |
| `UnitOfWork<DBContextWrite>.CommitAsync` | UnitOfWork - public | `SaveChanges` + commit + dispose transaction; loi thi rollback roi rethrow |
| `UnitOfWork<DBContextWrite>.RollbackAsync` | UnitOfWork - public | Rollback + dispose transaction (khong null-check) |
| `UnitOfWork<DBContextWrite>.DisposeAsync` | UnitOfWork - public | Dispose transaction + context, idempotent qua `_disposed` |
| `UnitOfWork<DBContextWrite>.DisposeTransactionAsync` | UnitOfWork - private | Dispose transaction va gan `null` |
| `UnitOfWork<DBContextWrite>.SaveChangeAsync` | UnitOfWork - private | Goi `_context.SaveChangesAsync(cancellationToken)` |
| `WriteDbContext<TContext>` (constructor) | WriteDbContext - public | Nhan `DbContextOptions<TContext>` + `Lazy<IServiceScopeFactory>` |
| `WriteDbContext<TContext>.OnModelCreating` | WriteDbContext - protected override | `ApplyConfigurationsFromAssembly(typeof(TContext).Assembly)` |
| `WriteDbContext<TContext>.SaveChanges(bool)` | WriteDbContext - public override | Audit mac dinh -> luu dong bo -> fire-and-forget `OnAfterSaveChanges()` |
| `WriteDbContext<TContext>.SaveChangesAsync(AuditModel, bool, CancellationToken)` | WriteDbContext - public (overload moi) | Audit theo `AuditModel` -> luu -> dispatch domain event |
| `WriteDbContext<TContext>.SaveChangesAsync(bool, CancellationToken)` | WriteDbContext - public override | Audit mac dinh -> luu -> dispatch domain event |
| `WriteDbContext<TContext>.DispatchDomainEvents` | WriteDbContext - public | Gom domain event, `ChangeTracker.Clear()`, publish tuan tu |
| `WriteDbContext<TContext>.OnBeforeSaveChanges` | WriteDbContext - private | Gan audit vao entity `IBaseEntitySQL` |
| `WriteDbContext<TContext>.DetectChangesAudit` | WriteDbContext - private | Goi `ChangeTracker.DetectChanges()`, luon tra ve list rong |
| `WriteDbContext<TContext>.OnAfterSaveChanges` | WriteDbContext - private | Goi `DispatchAuditLog` roi `DispatchDomainEvents` |
| `WriteDbContext<TContext>.DispatchAuditLog` | WriteDbContext - private | Guard rong roi `return Task.CompletedTask`; nhanh con lai la `return base.SaveChangesAsync();` (`:404`) - khong the toi duoc voi cac call-site hien tai |
| `ReadDbContext<TContext>` (constructor) | ReadDbContext - public | Nhan `DbContextOptions<TContext>` + `Lazy<IServiceScopeFactory>` (khong luu tru) |
| `ReadDbContext<TContext>.OnModelCreating` | ReadDbContext - protected override | `ApplyConfigurationsFromAssembly(typeof(TContext).Assembly)` |

---

## 2. `IUnitOfWork<DBContextWrite>`

**Khai bao**

```csharp
public interface IUnitOfWork<DBContextWrite> where DBContextWrite : WriteDbContext<DBContextWrite>, IAsyncDisposable
```

Nguon: `IUnitOfWork.cs:6`.

Interface khai bao 4 thanh vien, tat ca deu la method (khong co property). Generic constraint yeu cau `DBContextWrite` vua ke thua `WriteDbContext<DBContextWrite>` (self-referencing / F-bounded) vua trien khai `IAsyncDisposable`.

> [!NOTE]
> Constraint tren **class** `UnitOfWork<DBContextWrite>` (`UnitOfWork.cs:9`) chi la `where DBContextWrite : WriteDbContext<DBContextWrite>` - **thieu** `IAsyncDisposable` so voi interface. Xem muc 6, van de #12.

### 2.1 `Context` (hop dong)

**Signature**

```csharp
Task<DBContextWrite> Context(CancellationToken cancellationToken = default);
```

Nguon: `IUnitOfWork.cs:14`.

**Muc dich** - Khai bao hop dong lay context ghi. Hanh vi thuc te xem muc 3.1.

**Input hop le** - `cancellationToken` tuy chon (`default` = `CancellationToken.None`). Interface khong dat rang buoc nao khac tren tham so.

**Output** - `Task<DBContextWrite>`. Interface **khong** ghi nhan (bang XML doc hay attribute) rang gia tri co the la `null`; hop dong duy nhat doc duoc tu khai bao la kieu tra ve.

**Dieu kien xu ly** - Khong co (interface, khong co than ham). Toan bo nhanh re nam o cai dat, xem muc 3.1.

**Side effect** - Khai bao khong co side effect. Cai dat `UnitOfWork<DBContextWrite>` **co** side effect (tao va cache `DbContext`) - xem 3.1.

**Error handling** - Khong duoc dinh nghia o cap interface: khong co XML doc `<exception>`, khong co hop dong ve loai exception. Xem 3.1.

**Khi nao NEN dung** - Khi code nghiep vu can `DBContextWrite` de goi `DbSet`/`SaveChangesAsync` ma van muon phu thuoc vao abstraction `IUnitOfWork<T>` (de mock trong unit test).

**Khi nao KHONG dung** - Khi chi can doc du lieu (dung `ReadDbContext`, muc 5). Khi muon dispose context theo `await using` tren bien kieu interface - `IUnitOfWork<T>` khong ke thua `IAsyncDisposable` (xem canh bao cuoi muc 2 va van de #13).

**Gioi han** - XML doc tai `IUnitOfWork.cs:8-10` viet *"The following Property is going to hold the context object"* nhung day la mot **method** tra ve `Task<DBContextWrite>`, khong phai property. Doc khong khop khai bao (xem van de #29).

### 2.2 `CreateTransactionAsync` (hop dong)

**Signature**

```csharp
Task<IDbContextTransaction> CreateTransactionAsync(CancellationToken cancellationToken = default);
```

Nguon: `IUnitOfWork.cs:22`. Hanh vi thuc te xem muc 3.2.

**Muc dich** - Khai bao hop dong mo transaction cua database.

**Input hop le** - `cancellationToken` tuy chon (`default`). Khong co tham so chon isolation level, khong co tham so savepoint.

**Output** - `Task<IDbContextTransaction>` - doi tuong transaction cua EF Core.

**Dieu kien xu ly** - Khong co (interface). Xem 3.2.

**Side effect** - Khai bao khong co side effect. Cai dat co: rollback transaction cu con treo roi mo transaction moi - xem 3.2.

**Error handling** - Khong duoc dinh nghia o cap interface. Xem 3.2.

**Khi nao NEN dung** - Khi mot use case can gom nhieu buoc ghi vao mot transaction duy nhat truoc khi `CommitAsync`.

**Khi nao KHONG dung** - Khi can transaction long nhau (nested) hay savepoint: hop dong khong co API cho hai truong hop nay, va cai dat **rollback** transaction cu thay vi long vao (xem 3.2). Khi thao tac ghi chi gom mot lenh - cac overload `CreateAsync`/`UpdateAsync` cua `CoreSQL` da tu goi `SaveChangesAsync`.

**Gioi han** - Khong chon duoc isolation level tu hop dong nay; khong co API tao savepoint.

### 2.3 `CommitAsync` (hop dong)

**Signature**

```csharp
Task CommitAsync(CancellationToken cancellationToken = default);
```

Nguon: `IUnitOfWork.cs:30`. Hanh vi thuc te xem muc 3.3.

**Muc dich** - Khai bao hop dong commit transaction dang mo.

**Input hop le** - `cancellationToken` tuy chon (`default`).

**Output** - `Task` (khong co gia tri). Hop dong **khong** tra ve so ban ghi bi anh huong (xem van de #25).

**Dieu kien xu ly** - Khong co (interface). Xem 3.3.

**Side effect** - Khai bao khong co side effect. Cai dat co: `SaveChanges`, commit, dispose transaction, va rollback khi loi - xem 3.3.

**Error handling** - Khong duoc dinh nghia o cap interface. Cai dat nem lai exception goc sau khi da co gang rollback - xem 3.3.

**Khi nao NEN dung** - Khi tat ca buoc ghi cua use case da hoan tat va can persist + commit mot lan.

**Khi nao KHONG dung** - Khi chua goi `CreateTransactionAsync`: cai dat van `SaveChanges` thanh cong roi nem `NullReferenceException` (xem van de #3). Khi can biet so ban ghi da luu.

**Gioi han** - Hop dong khong bao hieu ket qua (khong tra `int`, khong tra `bool`); cach duy nhat biet that bai la exception.

### 2.4 `RollbackAsync` (hop dong)

**Signature**

```csharp
Task RollbackAsync();
```

Nguon: `IUnitOfWork.cs:37`.

**Muc dich** - Khai bao hop dong rollback transaction dang mo.

**Input hop le** - Khong co tham so.

**Output** - `Task` (khong co gia tri).

**Dieu kien xu ly** - Khong co (interface). Xem 3.4.

**Side effect** - Khai bao khong co side effect. Cai dat co: rollback + dispose transaction + ghi log - xem 3.4.

**Error handling** - Khong duoc dinh nghia o cap interface. Xem 3.4.

**Khi nao NEN dung** - Khi luong nghiep vu quyet dinh huy toan bo thay doi cua transaction **da duoc mo** truoc do.

**Khi nao KHONG dung** - Trong `catch`/`finally` phong ve ma khong chac transaction da duoc tao: cai dat khong null-check `_transaction` nen se nem `NullReferenceException` che mat exception goc (xem van de #2). Khi can huy tac vu theo `CancellationToken` - hop dong khong nhan token.

**Gioi han** - Khong nhan `CancellationToken` (khac voi 3 method con lai). Hanh vi thuc te xem muc 3.4.

> [!WARNING]
> Interface `IUnitOfWork<DBContextWrite>` **khong** ke thua `IAsyncDisposable`. Lop `UnitOfWork<DBContextWrite>` co trien khai `IAsyncDisposable` (`UnitOfWork.cs:9`) nhung consumer resolve theo interface `IUnitOfWork<T>` se **khong thay** `DisposeAsync` - `await using` tren bien kieu interface se khong bien dich duoc. Muon dispose phai cast sang `IAsyncDisposable` hoac dang ky/resolve theo kieu class.

---

## 3. `UnitOfWork<DBContextWrite>` - Chi tiet API

**Khai bao**

```csharp
public partial class UnitOfWork<DBContextWrite>(
    ILogger<UnitOfWork<DBContextWrite>> logger, Lazy<IDbContextFactory<DBContextWrite>> dbContext)
    : IUnitOfWork<DBContextWrite>, IAsyncDisposable where DBContextWrite : WriteDbContext<DBContextWrite>
```

Nguon: `UnitOfWork.cs:7-9`. Dung **primary constructor** (C# 12): `logger` va `dbContext` la tham so primary constructor, duoc dung truc tiep trong than cac method.

**State noi bo**

| Field | Kieu | Khoi tao | Vai tro |
|---|---|---|---|
| `_disposed` | `bool` | `false` (`UnitOfWork.cs:11`) | Co danh dau da dispose, dam bao `DisposeAsync` idempotent |
| `_context` | `DBContextWrite` | `null` (`UnitOfWork.cs:13`) | Context duoc cache trong vong doi cua instance |
| `_transaction` | `IDbContextTransaction` | `null` (`UnitOfWork.cs:15`) | Transaction hien tai; `null` = khong co transaction |

`partial` chi co **mot** phan duy nhat trong repo (kiem tra bang `grep -rn "partial class UnitOfWork"` - chi khop `UnitOfWork.cs:7`).

### 3.1 `Context`

**Signature**

```csharp
public async Task<DBContextWrite> Context(CancellationToken cancellationToken = default)
```

Nguon: `UnitOfWork.cs:24`.

**Muc dich** - Tra ve context ghi dang duoc cache; neu chua co thi tao moi qua `IDbContextFactory<DBContextWrite>.CreateDbContextAsync` va luu vao `_context` (`UnitOfWork.cs:31`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `cancellationToken` | `CancellationToken` | Khong | Khong validate. Chi truyen thang xuong `CreateDbContextAsync(cancellationToken: cancellationToken)` (`UnitOfWork.cs:31`) | `default` |

**Output** - `Task<DBContextWrite>`:
- Neu `_context` da khac `null`: tra ve dung instance da cache (`UnitOfWork.cs:26-29`).
- Neu `_context` la `null`: tra ve instance vua tao tu factory (`UnitOfWork.cs:31-33`).
- Khong co nhanh nao tra ve `null` tru khi factory tra ve `null` (khong duoc kiem tra trong code).

**Dieu kien xu ly** (theo thu tu thuc thi)
1. `if (_context is not null) return _context;` - `UnitOfWork.cs:26-29`.
2. `_context = await dbContext.Value.CreateDbContextAsync(cancellationToken: cancellationToken);` - `UnitOfWork.cs:31`.
3. `return _context;` - `UnitOfWork.cs:33`.

Khong co guard `_disposed`.

**Side effect** - Gan `_context` (thay doi state cua object). Tao mot `DbContext` moi (mo tai nguyen); viec dispose phu thuoc vao `DisposeAsync` cua `UnitOfWork`. Khong ghi log.

**Error handling** - Khong co `try/catch`. Moi exception tu `CreateDbContextAsync` (vi du `ObjectDisposedException` khi factory da dispose, hoac `OperationCanceledException`) duoc nem thang ra ngoai. Neu exception xay ra thi `_context` van la `null` (phep gan chua hoan thanh), lan goi sau se thu tao lai.

**Khi nao NEN dung**
- Truoc khi truy cap `DbSet`/`Database` cua context ghi trong pham vi mot UnitOfWork.
- Khi can chac chan cac lenh ghi trong cung mot business operation dung **cung mot** `DbContext` instance (vi du de chung transaction).

**Khi nao KHONG dung**
- Cho truy van doc: `UnitOfWork` chi tao context ghi, khong tao `ReadDbContext`.
- Sau khi da goi `DisposeAsync`: khong co guard, ham se tao mot context moi ma `UnitOfWork` **khong bao gio dispose** nua (vi `_disposed` da la `true`) -> ro ri tai nguyen.
- Trong ngu canh nhieu luong truy cap song song cung instance `UnitOfWork` (xem "Gioi han").

**Gioi han**
- **Khong thread-safe**: hai luong goi `Context()` dong thoi khi `_context` con `null` co the tao hai context, mot trong hai bi ghi de va khong duoc dispose (`UnitOfWork.cs:26-33`, khong co `lock`/`SemaphoreSlim`).
- Khong kiem tra `_disposed`.
- Khong co API de thay/reset context - mot khi `_context` da duoc gan, chi `DisposeAsync` moi dua no ve `null`.

### 3.2 `CreateTransactionAsync`

**Signature**

```csharp
public async Task<IDbContextTransaction> CreateTransactionAsync(CancellationToken cancellationToken = default)
```

Nguon: `UnitOfWork.cs:43`.

**Muc dich** - Dam bao context da ton tai, rollback transaction cu neu con treo, roi mo transaction moi bang `_context.Database.BeginTransactionAsync(cancellationToken)` (`UnitOfWork.cs:55`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `cancellationToken` | `CancellationToken` | Khong | Khong validate. Truyen cho `Context(...)` (`UnitOfWork.cs:45`) va `BeginTransactionAsync(...)` (`UnitOfWork.cs:55`) | `default` |

**Output** - `Task<IDbContextTransaction>`: tra ve dung doi tuong transaction vua tao, doi tuong nay **cung duoc luu vao** `_transaction`. Khong co nhanh tra ve `null`.

**Dieu kien xu ly** (theo thu tu thuc thi)
1. `await Context(cancellationToken);` - dam bao `_context` khac `null` (`UnitOfWork.cs:45`).
2. `if (_transaction is not null)`: ghi log Warning `"[TRANSACTION] - Phát hiện transaction cũ chưa commit/rollback, tự rollback trước khi tạo transaction mới."` roi `await RollbackAsync();` (`UnitOfWork.cs:47-53`).
3. `_transaction = await _context.Database.BeginTransactionAsync(cancellationToken);` (`UnitOfWork.cs:55`).
4. Ghi log Warning `"[TRANSACTION] - Create transaction."` (`UnitOfWork.cs:57`).
5. `return _transaction;` (`UnitOfWork.cs:59`).

**Side effect**
- Co the tao `DbContext` (qua buoc 1).
- **Rollback ngam** transaction cu neu con - moi thay doi chua commit trong transaction cu bi huy (buoc 2).
- Mo transaction DB moi (giu connection).
- Gan `_transaction`.
- Ghi 1-2 dong log muc `Warning` (ke ca truong hop binh thuong - xem "Gioi han").

**Error handling** - Khong co `try/catch`. Exception tu `Context`, `RollbackAsync` hoac `BeginTransactionAsync` deu nem thang ra ngoai. Neu `BeginTransactionAsync` that bai, `_transaction` van giu gia tri truoc do (`null` sau `RollbackAsync`, hoac `null` neu chua tung co).

**Khi nao NEN dung**
- Khi mot business operation can ghi nhieu bang/nhieu buoc va phai atomic.
- Khi can lay `IDbContextTransaction` de truyen sang doan code khac (vi du chia se transaction voi Dapper qua `transaction.GetDbTransaction()`).

**Khi nao KHONG dung**
- Khi chi ghi mot lan duy nhat: EF Core da tu boc `SaveChanges` trong transaction noi bo, mo transaction tuong minh la du thua.
- Khi can transaction long nhau hoac savepoint: goi lai ham nay se **rollback** transaction dang mo, khong tao transaction con.
- Khi ban dang tu quan ly transaction ben ngoai (`TransactionScope`, ambient transaction) - code khong xu ly truong hop nay.

**Gioi han**
- Toan bo log trong lop dung `logger.Warning` (`LoggerExtensions.cs:254`, map `LogLevel.Warning` tai `LoggerExtensions.cs:134`), ke ca thong bao thanh cong `"Create transaction."`. Log noise o muc Warning.
- Khong co tham so `IsolationLevel` - luon dung isolation mac dinh cua provider.
- Neu `_transaction` cu da bi rollback/commit **ben ngoai** ma khong qua `RollbackAsync`/`CommitAsync` (nen `_transaction` chua ve `null`), buoc 2 se goi `RollbackAsync` tren transaction da ket thuc -> exception tu provider, nem ra ngoai.

### 3.3 `CommitAsync`

**Signature**

```csharp
public async Task CommitAsync(CancellationToken cancellationToken = default)
```

Nguon: `UnitOfWork.cs:70`.

**Muc dich** - Goi `SaveChangeAsync` (luu thay doi cua `_context`), commit transaction, dispose transaction. Neu bat ky buoc nao nem exception thi co gang rollback roi nem lai exception goc.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `cancellationToken` | `CancellationToken` | Khong | Khong validate. Truyen cho `SaveChangeAsync` (`UnitOfWork.cs:74`) va `_transaction.CommitAsync` (`UnitOfWork.cs:76`) | `default` |

Khong co tham so `AuditModel`. Xem "Gioi han".

**Output** - `Task` (khong co gia tri tra ve). So ban ghi bi anh huong tu `SaveChangeAsync` bi **loai bo** bang `_ = ...` (`UnitOfWork.cs:74`) - caller khong biet co bao nhieu row bi tac dong.

**Dieu kien xu ly** (theo thu tu thuc thi)
1. Trong `try`: `_ = await SaveChangeAsync(cancellationToken: cancellationToken);` (`UnitOfWork.cs:74`).
2. `await _transaction.CommitAsync(cancellationToken);` (`UnitOfWork.cs:76`) - **khong** null-check `_transaction`.
3. `await DisposeTransactionAsync();` -> dispose va gan `_transaction = null` (`UnitOfWork.cs:78`).
4. Log Warning `"[TRANSACTION] - Commit transaction."` (`UnitOfWork.cs:80`).
5. `catch (Exception exception)` (`UnitOfWork.cs:82`):
   - Log Warning `"[TRANSACTION] - Lỗi khi commit, tiến hành rollback. Ex: {exception.Message}"` (`UnitOfWork.cs:84-85`).
   - `try { if (_transaction is not null) await RollbackAsync(); }` (`UnitOfWork.cs:87-92`) - **co** null-check o day.
   - `catch (Exception rollbackException)`: chi log Warning `"[TRANSACTION] - Rollback sau lỗi commit cũng thất bại..."`, **khong** nem tiep (`UnitOfWork.cs:94-98`).
   - `throw;` - nem lai exception goc, giu nguyen stack trace (`UnitOfWork.cs:100`).

**Side effect**
- Ghi DB: `SaveChangeAsync` -> `_context.SaveChangesAsync(cancellationToken)`. Vi `_context` la `WriteDbContext`, chuoi nay **keo theo** `OnBeforeSaveChanges()` (audit mac dinh) va `OnAfterSaveChanges()` (dispatch domain event, bao gom `ChangeTracker.Clear()`) - xem muc 4.
- Commit transaction DB.
- Dispose transaction, gan `_transaction = null`.
- Ghi log muc `Warning` trong ca duong thanh cong va duong loi.

> [!WARNING]
> **Thu tu quan trong**: `SaveChangeAsync` (buoc 1) chay **truoc** `_transaction.CommitAsync` (buoc 2). Vi `WriteDbContext.SaveChangesAsync` dispatch domain event ngay sau khi luu (`WriteDbContext.cs:88-91`, `WriteDbContext.cs:110-113`), cac handler domain event da chay **truoc khi transaction duoc commit**. Neu buoc commit that bai va rollback, du lieu bi huy nhung domain event **da duoc publish**.

**Error handling** - Bat `Exception` (chung nhat). Log roi co gang rollback, nuot exception cua rollback, cuoi cung `throw;` nem lai exception goc. Khong bao boc thanh custom exception, khong swallow exception goc.

**Khi nao NEN dung**
- Sau khi da goi `CreateTransactionAsync` va thuc hien xong cac thao tac ghi tren `_context`.

**Khi nao KHONG dung**
- **Khi chua goi `CreateTransactionAsync`**: `_transaction` la `null` -> buoc 1 **van chay va van luu du lieu vao DB**, sau do buoc 2 nem `NullReferenceException`; `catch` bat duoc, khong rollback duoc (do null-check), roi `throw;`. Ket qua: du lieu **da persist** nhung caller nhan exception -> de bi hieu sai la "khong luu duoc".
- Khi chua goi `Context()` va cung chua goi `CreateTransactionAsync`: `_context` la `null` -> `SaveChangeAsync` nem `NullReferenceException` (`UnitOfWork.cs:155`), rollback bi bo qua, `throw;`.
- Khi can gan audit `ModifiedUser`/`ModifiedDate`: xem "Gioi han".
- Khi can biet so row bi anh huong: gia tri tra ve bi bo, hay goi `(await Context()).SaveChangesAsync(...)` truc tiep.

**Gioi han**
- Khong null-check `_transaction` truoc `CommitAsync` (`UnitOfWork.cs:76`).
- **Khong truyen `AuditModel`**: `SaveChangeAsync` goi `_context.SaveChangesAsync(cancellationToken)`. Xet theo kieu tham so, loi goi mot doi so `CancellationToken` **khong** khop overload `SaveChangesAsync(AuditModel, bool, CancellationToken)` (tham so dau la `AuditModel`) va **khong** khop `SaveChangesAsync(bool, CancellationToken)`; no khop `DbContext.SaveChangesAsync(CancellationToken)`. Hau qua theo `WriteDbContext.cs:170-173`: nhanh `EntityState.Modified` `break` som khi `audit is null`, nen **`ModifiedDate`/`ModifiedUser`/`ModifiedUserCode`/`ModifiedUserOrganization` khong duoc gan** khi luu qua `UnitOfWork`. Muon co audit Modified phai goi truc tiep `(await uow.Context()).SaveChangesAsync(audit: myAudit, cancellationToken: ct)`.
- Sau khi commit, `_context` **khong** bi dispose - context van con song va con dung duoc cho transaction tiep theo.
- Log loi **khong mang theo doi tuong exception**: `LoggerExtensions.Warning` co tham so `Exception e = null` (`Extensions/Loggers/LoggerExtensions.cs:254`) nhung ca hai loi goi trong khoi `catch` (`:84-85`, `:96-97`) chi noi `exception.Message` vao chuoi message va **khong** truyen `e:`. Stack trace, inner exception va cac thuoc tinh chan doan (vi du `SqlException.Number`) bi mat khoi log.
- `ChangeTracker` bi `Clear()` trong `DispatchDomainEvents` (`WriteDbContext.cs:433`) **truoc khi** commit (chi khi buoc 1 tra ve `result >= 1`), nen sau buoc 1 context khong con theo doi entity nao. Goi lai `CommitAsync` sau khi that bai o buoc 2 **khong** phai la retry an toan: (a) khong con change nao de luu, va (b) khoi `catch` da goi `RollbackAsync` -> `_transaction` da ve `null` (`:113`, `:149`), nen buoc 2 cua lan goi thu hai nem `NullReferenceException` tai `:76`.

### 3.4 `RollbackAsync`

**Signature**

```csharp
public async Task RollbackAsync()
```

Nguon: `UnitOfWork.cs:109`.

**Muc dich** - Rollback transaction hien tai roi dispose no.

**Input hop le** - Khong co tham so. Khong nhan `CancellationToken`.

**Output** - `Task`, khong co gia tri tra ve, khong bao hieu thanh cong/that bai ngoai viec nem exception.

**Dieu kien xu ly** (theo thu tu thuc thi)
1. `await _transaction.RollbackAsync();` (`UnitOfWork.cs:111`) - **khong co guard clause nao**.
2. `await DisposeTransactionAsync();` -> dispose + `_transaction = null` (`UnitOfWork.cs:113`).
3. Log Warning `"[TRANSACTION] - Rollback transaction."` (`UnitOfWork.cs:115`).

**Side effect** - Rollback transaction DB (huy moi thay doi chua commit); dispose transaction; gan `_transaction = null`; ghi 1 dong log Warning.

**Error handling** - Khong co `try/catch`. Moi exception nem thang ra ngoai. Neu buoc 1 nem exception thi buoc 2 **khong chay** -> `_transaction` van giu doi tuong cu (khong ve `null`), lan `CreateTransactionAsync` sau se lai co gang `RollbackAsync` tren transaction do.

**Khi nao NEN dung**
- Sau khi `CreateTransactionAsync` thanh cong va business logic quyet dinh huy thay doi.

**Khi nao KHONG dung**
- **Khi chua chac chan da co transaction**: `_transaction` `null` -> `NullReferenceException` tai `UnitOfWork.cs:111`. Hay tu kiem tra o phia caller (`UnitOfWork` khong expose thuoc tinh nao cho biet co transaction hay khong) hoac boc `try/catch`.
- Trong `finally` cua mot khoi ma khong biet transaction da bi `CommitAsync`/`CreateTransactionAsync` dispose chua - de nem `NullReferenceException` che mat exception goc.

**Gioi han**
- Thieu null-check `_transaction` (van de #2 muc 6).
- Khong ho tro `CancellationToken`.
- Khong co API cong khai de kiem tra su ton tai cua transaction -> caller khong the phong ve mot cach an toan ngoai `try/catch`.

### 3.5 `DisposeAsync`

**Signature**

```csharp
public async ValueTask DisposeAsync()
```

Nguon: `UnitOfWork.cs:124`.

**Muc dich** - Giai phong transaction (neu con) va context; chi thuc hien mot lan.

**Input hop le** - Khong co tham so.

**Output** - `ValueTask`. Tra ve ngay (khong lam gi) neu `_disposed` da la `true`.

**Dieu kien xu ly** (theo thu tu thuc thi)
1. `if (_disposed) return;` (`UnitOfWork.cs:126-129`).
2. `_disposed = true;` (`UnitOfWork.cs:131`) - dat co **truoc** khi giai phong.
3. `await DisposeTransactionAsync();` (`UnitOfWork.cs:133`).
4. `if (_context is not null) { await _context.DisposeAsync(); _context = null; }` (`UnitOfWork.cs:135-140`).

**Side effect** - Dispose transaction va context; gan `_transaction = null`, `_context = null`, `_disposed = true`.

**Error handling** - Khong co `try/catch`. Neu buoc 3 nem exception thi buoc 4 khong chay -> **context bi ro ri** va `_disposed` da la `true` nen khong the dispose lai.

> [!WARNING]
> `DisposeAsync` **khong** rollback transaction dang mo truoc khi dispose. Theo hanh vi tieu chuan cua EF Core, dispose mot `IDbContextTransaction` chua commit se rollback transaction; tuy nhien dieu do la hanh vi cua EF Core, **khong** doc duoc tu source code trong repo nay.

**Khi nao NEN dung**
- Trong `await using` (voi bien kieu `UnitOfWork<T>` hoac `IAsyncDisposable`) hoac cuoi mot scope DI ma DI container tu goi.

**Khi nao KHONG dung**
- Khi con y dinh tiep tuc dung `UnitOfWork`: sau `DisposeAsync`, goi `Context()` se tao context moi nhung se **khong bao gio** duoc dispose (buoc 1 chan).

**Gioi han**
- Khong co `GC.SuppressFinalize` (lop khong co finalizer, nen khong bat buoc).
- Khong co ban `Dispose()` dong bo -> khong dung duoc voi `using` (dong bo).
- Khong co guard `ObjectDisposedException` cho cac method khac sau khi dispose.
- `_disposed` la `bool` thuong, khong dung `Interlocked` -> khong an toan khi dispose dong thoi tu nhieu luong.

### 3.6 `DisposeTransactionAsync` (private)

**Signature**

```csharp
private async Task DisposeTransactionAsync()
```

Nguon: `UnitOfWork.cs:143`.

**Muc dich** - Dispose `_transaction` neu khac `null` roi gan lai `null`.

**Input hop le** - Khong co tham so. Khong nhan `CancellationToken`.

**Output** - `Task` (khong co gia tri tra ve, khong bao hieu da co transaction de dispose hay khong).

**Dieu kien xu ly** - `if (_transaction is not null) { await _transaction.DisposeAsync(); _transaction = null; }` (`UnitOfWork.cs:145-150`). Idempotent: goi nhieu lan khong loi.

**Side effect** - Dispose transaction; gan `_transaction = null`.

**Error handling** - Khong co `try/catch`; exception tu `DisposeAsync()` nem ra ngoai va `_transaction` khi do **khong** duoc dat ve `null`.

**Khi nao NEN dung** - Khong ap dung (`private`). Duoc goi noi bo tu `CommitAsync` (`UnitOfWork.cs:78`), `RollbackAsync` (`:113`) va `DisposeAsync` (`:133`); `CreateTransactionAsync` goi gian tiep qua `RollbackAsync` (`:52`).

**Khi nao KHONG dung** - Khong ap dung (`private`). Lop dan xuat **khong** the override (khong `virtual`) va **khong** the goi.

**Gioi han** - `private`, khong goi duoc tu ben ngoai.

### 3.7 `SaveChangeAsync` (private)

**Signature**

```csharp
private Task<int> SaveChangeAsync(CancellationToken cancellationToken = default)
```

Nguon: `UnitOfWork.cs:153`.

**Muc dich** - Uy quyen cho `_context.SaveChangesAsync(cancellationToken)` (`UnitOfWork.cs:155`). Khong `async`/`await`, tra ve truc tiep `Task<int>`.

**Input hop le** - `cancellationToken` tuy chon (`default`), duoc truyen nguyen ven xuong `SaveChangesAsync`. Ham **khong** validate `_context`.

**Dieu kien xu ly** - Mot lenh duy nhat, khong co nhanh re va khong co guard: `return _context.SaveChangesAsync(cancellationToken);` (`UnitOfWork.cs:155`). Vi khong `await`, exception dong bo phat sinh ben trong `SaveChangesAsync` van duoc goi lai qua `Task` cho caller `CommitAsync`; rieng `NullReferenceException` do `_context is null` duoc nem **dong bo** ngay tai day.

**Output** - `Task<int>`: so ban ghi bi anh huong theo EF Core. Chi duoc goi tu `CommitAsync` va gia tri bi bo (`_ = ...`).

**Side effect** - Ghi DB, keo theo audit stamping va dispatch domain event cua `WriteDbContext` (muc 4).

**Error handling** - Khong co. `NullReferenceException` neu `_context` la `null`.

**Khi nao NEN dung** - Khong ap dung (`private`). Chi duoc goi tu `CommitAsync` (`UnitOfWork.cs:74`).

**Khi nao KHONG dung** - Khong ap dung (`private`). Muon luu ma **khong** commit thi phai di qua context: `(await uow.Context(ct)).SaveChangesAsync(audit: ..., cancellationToken: ct)`.

**Gioi han** - `private`: khong co cach nao goi `SaveChanges` qua `IUnitOfWork`/`UnitOfWork` ma khong commit transaction. Muon vay phai lay context: `(await uow.Context()).SaveChangesAsync(...)`.

---

## 4. `WriteDbContext<TContext>` - Chi tiet API

**Khai bao**

```csharp
public partial class WriteDbContext<TContext> : DbContext where TContext : DbContext
```

Nguon: `WriteDbContext.cs:16`. Lop duoc chia thanh 4 khoi `partial` trong cung file: khai bao co ban (`:16`), nhom `SaveChanges` (`:43`), nhom "Before Saving" (`:122`), nhom "After Saving" (`:364`).

**State noi bo**

| Field | Kieu | Vai tro |
|---|---|---|
| `_serviceScopeFactory` | `readonly Lazy<IServiceScopeFactory>` | Tao scope DI de resolve `IPublisher` khi dispatch domain event (`WriteDbContext.cs:18`, `:440`) |

### 4.1 Constructor

**Signature**

```csharp
public WriteDbContext(DbContextOptions<TContext> options, Lazy<IServiceScopeFactory> serviceScopeFactory) : base(options)
```

Nguon: `WriteDbContext.cs:27`.

**Muc dich** - Truyen `options` cho `DbContext` va luu `serviceScopeFactory` vao `_serviceScopeFactory` (`WriteDbContext.cs:29`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `options` | `DbContextOptions<TContext>` | Co | Khong validate trong lop nay; chuyen cho `base(options)` | Khong co |
| `serviceScopeFactory` | `Lazy<IServiceScopeFactory>` | Co | **Khong** null-check (`WriteDbContext.cs:29`) | Khong co |

**Output** - Instance `WriteDbContext<TContext>`.

**Dieu kien xu ly** - Khong co nhanh re: `base(options)` chay truoc, sau do than constructor gan `_serviceScopeFactory = serviceScopeFactory;` (`WriteDbContext.cs:29`). Khong guard, khong log.

**Side effect** - Gan `_serviceScopeFactory`.

**Error handling** - Khong co `try/catch`. `ArgumentNullException` tu `DbContext` neu `options` la `null` (hanh vi cua EF Core, khong doc duoc tu file nay).

**Khi nao NEN dung** - Qua DI: dinh nghia lop context cu the `public class MyDbContext : WriteDbContext<MyDbContext>` roi dang ky `AddDbContext<MyDbContext>` / `AddDbContextFactory<MyDbContext>` o ung dung tieu thu.

**Khi nao KHONG dung** - Khoi tao thu cong bang `new` khi khong the cung cap `Lazy<IServiceScopeFactory>` hop le: `DispatchDomainEvents` se nem `NullReferenceException` tai `WriteDbContext.cs:440` khi co domain event can publish.

**Gioi han**
- Kieu tham so la `DbContextOptions<TContext>` (khong phai `DbContextOptions<WriteDbContext<TContext>>`), nen `TContext` bat buoc phai la chinh lop context ke thua (pattern self-referencing) de DI phan giai duoc.
- `Lazy<IServiceScopeFactory>` khong duoc DI .NET dang ky mac dinh - ung dung phai tu dang ky `Lazy<T>`. Khong co dong code nao trong repo nay lam viec do.

### 4.2 `OnModelCreating`

**Signature**

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
```

Nguon: `WriteDbContext.cs:36`.

**Muc dich** - `modelBuilder.ApplyConfigurationsFromAssembly(typeof(TContext).Assembly);` (`WriteDbContext.cs:39`) - nap moi `IEntityTypeConfiguration<>` trong assembly chua `TContext`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `modelBuilder` | `ModelBuilder` | Co | Khong validate | Khong co |

**Output** - `void`.

**Dieu kien xu ly** - Mot lenh duy nhat, khong co nhanh re. **Khong** goi `base.OnModelCreating(modelBuilder)`.

**Side effect** - Thay doi model duoc build.

**Error handling** - Khong co.

**Khi nao NEN dung** - Khong goi truc tiep. EF Core tu goi mot lan khi build model cho `TContext`; lop dan xuat chi override khi can bo sung cau hinh **va** phai goi `base.OnModelCreating(modelBuilder)`.

**Khi nao KHONG dung** - Neu lop dan xuat ghi de `OnModelCreating` ma khong goi `base.OnModelCreating(...)` thi cac entity configuration se **khong** duoc nap.

**Gioi han**
- Khong ap dung global query filter (vi du loc `IsDeleted == false`) du `IBaseEntitySQL` co `IsDeleted` - soft delete **khong** duoc filter tu dong.
- Khong goi `base.OnModelCreating`.

### 4.3 `SaveChanges(bool acceptAllChangesOnSuccess)`

**Signature**

```csharp
public override int SaveChanges(bool acceptAllChangesOnSuccess)
```

Nguon: `WriteDbContext.cs:52`.

**Muc dich** - Gan audit voi gia tri mac dinh, luu dong bo, roi **khong doi** (`_ =`) goi `OnAfterSaveChanges()`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `acceptAllChangesOnSuccess` | `bool` | Co | Khong validate; truyen cho `base.SaveChanges(...)` (`WriteDbContext.cs:56`) | Khong co (override, EF khai bao mac dinh o overload khac) |

**Output** - `int`: gia tri tra ve cua `base.SaveChanges(acceptAllChangesOnSuccess)`, tuc so ban ghi bi anh huong. `0` khi khong co change.

**Dieu kien xu ly** (theo thu tu thuc thi)
1. `OnBeforeSaveChanges();` - goi **khong** tham so, nen `audit` la `null` -> chi entity `Added` duoc stamp (`WriteDbContext.cs:54`).
2. `int result = base.SaveChanges(acceptAllChangesOnSuccess);` (`WriteDbContext.cs:56`).
3. `if (result > 0) { _ = OnAfterSaveChanges(); }` (`WriteDbContext.cs:58-61`).
4. `return result;` (`WriteDbContext.cs:63`).

**Side effect**
- Mutate cac entity dang duoc theo doi (gan `IsDeleted`, `CreatedDate`, `CreatedUser`, ... - xem 4.7).
- Ghi DB.
- Khoi chay `OnAfterSaveChanges()` (async) ma **khong await** (`WriteDbContext.cs:60`). Luu y phan doan chinh xac theo than ham: `DispatchAuditLog(null)` tra ve `Task.CompletedTask` ngay (`:375-378`) nen `await` o `:368` **khong** nhuong luong; `DispatchDomainEvents` chay dong bo tu `:418` den `ChangeTracker.Clear()` (`:433`) va guard `:435-438`. Do do `ChangeTracker.Clear()` **luon hoan thanh dong bo truoc khi `SaveChanges(bool)` tra ve**. Diem `await` that su dau tien chi co the xuat hien o `publisher.Publish(...)` (`:446`) - tuc **chi phan publish domain event** moi co the chay tiep sau khi caller da nhan ket qua, va chi khi co it nhat mot domain event.
- **Khong** goi `DetectChangesAudit` (khac voi overload 4.4).

**Error handling** - Khong co `try/catch`. Exception tu `base.SaveChanges` nem thang ra ngoai. Exception xay ra **ben trong** `OnAfterSaveChanges()` (buoc 3) **khong** duoc quan sat: `Task` bi bo, tro thanh unobserved task exception; caller khong nhan duoc loi.

**Khi nao NEN dung** - Chi nen dung khi bat buoc phai luu dong bo va **khong** phu thuoc vao domain event (vi thoi diem event duoc publish khong xac dinh).

**Khi nao KHONG dung**
- Khi co domain event can publish: fire-and-forget lam mat kha nang cho doi va mat exception - handler co the con dang chay (hoac chua chay xong) sau khi `SaveChanges` da tra ve, va neu caller dispose `DbContext` ngay sau do thi handler van tiep tuc chay tren mot scope DI rieng, ngoai tam kiem soat cua caller.
- Khi can gan `ModifiedUser`/`ModifiedDate`: `audit` la `null` nen nhanh `Modified` bi `break` (`WriteDbContext.cs:170-173`).
- Trong moi truong async - dung 4.4 hoac 4.5.

**Gioi han**
- `_ = OnAfterSaveChanges();` (`WriteDbContext.cs:60`) la fire-and-forget: khong ordering, khong error propagation cho phan publish domain event (van de #4 muc 6). Rieng `ChangeTracker.Clear()` thi **khong** bi anh huong - no chay dong bo truoc khi ham tra ve (xem "Side effect").
- Khong the truyen `AuditModel`.
- Khong ghi de `SaveChanges()` (khong tham so); theo hanh vi cua EF Core, `SaveChanges()` goi `SaveChanges(true)` nen se di vao override nay - day la hanh vi cua EF Core, **khong** doc duoc tu source trong repo nay.

### 4.4 `SaveChangesAsync(AuditModel audit, bool acceptAllChangesOnSuccess, CancellationToken cancellationToken)`

**Signature**

```csharp
public async Task<int> SaveChangesAsync(AuditModel audit = null, bool acceptAllChangesOnSuccess = true, CancellationToken cancellationToken = default)
```

Nguon: `WriteDbContext.cs:75`. Day la **overload moi** (them method, khong phai `override`).

**Muc dich** - Gan audit theo `AuditModel` truyen vao, goi `DetectChangesAudit` khi `audit` khac `null`, luu bat dong bo, roi await `OnAfterSaveChanges(detectChangesAudit, cancellationToken)` khi co ban ghi thay doi.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `audit` | `AuditModel` | Khong | Cho phep `null`. Trong `OnBeforeSaveChanges` chi doc `audit?.CreatorInfo?.Name/.Code/.Organization`; chuoi trang/rong bi thay bang gia tri mac dinh (`WriteDbContext.cs:139-146`). `audit is not null` quyet dinh co goi `DetectChangesAudit` (`WriteDbContext.cs:81-84`) | `null` |
| `acceptAllChangesOnSuccess` | `bool` | Khong | Khong validate; truyen cho `base.SaveChangesAsync` (`WriteDbContext.cs:86`) | `true` |
| `cancellationToken` | `CancellationToken` | Khong | Khong `ThrowIfCancellationRequested` tuong minh; truyen cho `base.SaveChangesAsync` va `OnAfterSaveChanges` | `default` |

Cac truong `Ip`, `Device`, `Method`, `Address` cua `AuditModel` (`Models/Audits/AuditModel.cs:5-11`) **khong** duoc doc o bat ky dong code dang hoat dong nao - chung chi xuat hien trong khoi comment "NOT SUPPORT" (`WriteDbContext.cs:215-219`: `Address` `:215`, `Ip` `:217`, `Device` `:218`, `Method` `:219`).

**Output** - `Task<int>`: so ban ghi bi anh huong tu `base.SaveChangesAsync`. `0` khi khong co change (khi do `OnAfterSaveChanges` khong chay, domain event khong duoc publish).

**Dieu kien xu ly** (theo thu tu thuc thi)
1. `OnBeforeSaveChanges(audit);` (`WriteDbContext.cs:77`).
2. `List<SnapshotAuditModel> detectChangesAudit = [];` (`WriteDbContext.cs:79`).
3. `if (audit is not null) detectChangesAudit = DetectChangesAudit(audit);` (`WriteDbContext.cs:81-84`) - luon nhan lai list rong, xem 4.8.
4. `int result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);` (`WriteDbContext.cs:86`).
5. `if (result > 0) await OnAfterSaveChanges(detectChangesAudit, cancellationToken);` (`WriteDbContext.cs:88-91`).
6. `return result;` (`WriteDbContext.cs:93`).

**Side effect**
- Mutate entity dang theo doi (audit stamping).
- Khi `audit` khac `null`: goi `ChangeTracker.DetectChanges()` (`WriteDbContext.cs:199`).
- Ghi DB.
- `OnAfterSaveChanges` -> `DispatchAuditLog` (khong lam gi, xem 4.10) -> `DispatchDomainEvents`: **`ChangeTracker.Clear()`** (`WriteDbContext.cs:433`) va publish domain event qua `IPublisher` trong mot scope DI moi.

**Error handling** - Khong co `try/catch`. Moi exception (validate cua EF, `DbUpdateException`, `DbUpdateConcurrencyException`, exception tu handler domain event) nem thang ra ngoai. Neu exception xay ra trong `OnAfterSaveChanges` thi du lieu **da duoc luu** nhung caller nhan exception.

**Khi nao NEN dung**
- Duong luu chinh cho luong ghi khi co thong tin nguoi thuc hien. Day la overload duoc `CoreSQL` su dung: `context.SaveChangesAsync(audit: auditLog, cancellationToken: ct)` (`CoreSQL.cs:653-654`, `CoreSQL.cs:691-692`, `CoreSQL.cs:733`, `CoreSQL.cs:772`, `CoreSQL.cs:823`, `CoreSQL.cs:861`, `CoreSQL.cs:910`, `CoreSQL.cs:948`).
- Khi can `ModifiedUser`/`ModifiedDate` duoc gan tu dong (bat buoc truyen `audit`).

**Khi nao KHONG dung**
- Khi con can tiep tuc dung cac entity dang theo doi sau khi luu: `ChangeTracker.Clear()` se detach het (`WriteDbContext.cs:433`), moi thao tac tiep theo phai `Attach`/query lai.
- Trong vong retry (Polly...) ma retry lai chinh loi goi nay, **chi khi** lan chay truoc da qua duoc `base.SaveChangesAsync` voi `result > 0` (`:86-88`): khi do `OnAfterSaveChanges` da chay va `ChangeTracker` da bi `Clear()` (`:433`), lan retry se khong con change nao va tra ve `0`. Nguoc lai - neu exception phat sinh **ngay trong** `base.SaveChangesAsync` (truong hop transient DB error ma retry duoc thiet ke cho) - `OnAfterSaveChanges` chua he chay, `ChangeTracker` con nguyen va retry hoat dong binh thuong.
- Khi can domain event chi phat sau khi transaction commit - xem canh bao o muc 3.3.

**Gioi han**
- `DetectChangesAudit` **luon** tra ve list rong (`WriteDbContext.cs:356`), nen `detectChangesAudit` luon rong va `DispatchAuditLog` luon khong lam gi. Tinh nang audit chi tiet (old/new values) khong ton tai.
- Cac truong `Ip`, `Device`, `Method`, `Address` cua `AuditModel` bi bo qua hoan toan.
- `ChangeTracker.Clear()` chay ke ca khi khong co domain event nao (`WriteDbContext.cs:433` nam **truoc** guard `Count is 0` o `:435-438`).
- Domain event khong duoc xoa khoi entity (`Aggregate.ClearDomainEvents()` - `Abstractions/Aggregate.cs:30` - khong duoc goi o day).
- Publish tuan tu trong `foreach` (`WriteDbContext.cs:444-447`); handler cham lam cham ca loi goi save.
- **Khong** null-check `audit.CreatorInfo` truoc khi truy cap - da dung `?.` nen an toan, nhung nghia la `AuditModel` co `CreatorInfo = null` se roi ve gia tri mac dinh `Anonymous` mot cach am tham.

### 4.5 `SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken)`

**Signature**

```csharp
public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
```

Nguon: `WriteDbContext.cs:104`. Day la `override` cua `DbContext`.

**Muc dich** - Gan audit voi gia tri mac dinh (`audit` = `null`), luu bat dong bo, roi await `OnAfterSaveChanges(cancellationToken: cancellationToken)`.

**Khac biet so voi 4.4** (quan trong):

| Diem khac | 4.4 `SaveChangesAsync(AuditModel, bool, CancellationToken)` | 4.5 `SaveChangesAsync(bool, CancellationToken)` |
|---|---|---|
| Tham so `AuditModel` | Co | Khong |
| `OnBeforeSaveChanges` goi voi | `audit` (`:77`) | khong tham so -> `null` (`:106`) |
| Stamp `ModifiedUser`/`ModifiedDate` | Co (khi `audit` khac `null`) | **Khong** (bi `break` tai `:170-173`) |
| Goi `DetectChangesAudit` | Co khi `audit` khac `null` (`:83`) | **Khong** |
| `ChangeTracker.DetectChanges()` | Co khi `audit` khac `null` (`:199`) | Khong goi tuong minh |
| Dieu kien dispatch | `result > 0` (`:88`) | `result >= 1` (`:110`) - tuong duong ve nghia voi `int` |
| Tham so truyen `OnAfterSaveChanges` | `detectChangesAudit` (`:90`) | khong truyen -> `null` (`:112`) |

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `acceptAllChangesOnSuccess` | `bool` | Co | Khong validate | Khong co |
| `cancellationToken` | `CancellationToken` | Khong | Khong validate | `default` |

**Output** - `Task<int>`: so ban ghi bi anh huong.

**Dieu kien xu ly** (theo thu tu thuc thi)
1. `OnBeforeSaveChanges();` (`WriteDbContext.cs:106`).
2. `int result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);` (`WriteDbContext.cs:108`).
3. `if (result >= 1) await OnAfterSaveChanges(cancellationToken: cancellationToken);` (`WriteDbContext.cs:110-113`).
4. `return result;` (`WriteDbContext.cs:115`).

**Side effect** - Nhu 4.4 tru phan `DetectChangesAudit`. Van co `ChangeTracker.Clear()` va publish domain event.

**Error handling** - Khong co `try/catch`; exception nem thang ra ngoai.

**Khi nao NEN dung**
- Khi khong co thong tin nguoi thuc hien (job nen, migration data, seeding) va chi can stamp `CreatedUser = "Anonymous"`, `CreatedUserCode = "0"`, `CreatedUserOrganization = "FTEL"`.
- Day cung la duong ma cac loi goi `SaveChangesAsync(cancellationToken)` cua EF Core di qua (theo cach EF Core dieu huong noi bo tu `SaveChangesAsync(CancellationToken)` sang overload `bool`) - vi du `UnitOfWork.SaveChangeAsync` (`UnitOfWork.cs:155`).

**Khi nao KHONG dung**
- Khi can `ModifiedUser`/`ModifiedDate`: **khong bao gio duoc gan** qua duong nay.
- Khi can `AuditModel`: dung 4.4.

**Gioi han**
- Khong the truyen `AuditModel` -> mat toan bo audit cho `Modified`.
- `result >= 1` (`:110`) khong dong nhat ve hinh thuc voi `result > 0` (`:58`, `:88`) - cung nghia nhung khong nhat quan style.
- Cung cac gioi han ve `ChangeTracker.Clear()`, domain event khong duoc clear khoi entity, publish tuan tu (nhu 4.4).

### 4.6 `DispatchDomainEvents`

**Signature**

```csharp
public async Task DispatchDomainEvents(List<IDomainEvent> domainEvents = null, CancellationToken cancellationToken = default)
```

Nguon: `WriteDbContext.cs:416`. Day la method `public` duy nhat cua nhom "After Saving".

**Muc dich** - Gom domain event tu cac entity `Aggregate` dang duoc `ChangeTracker` theo doi, hop voi danh sach truyen vao, loai trung, **clear `ChangeTracker`**, roi publish tuan tu tung event qua `IPublisher`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `domainEvents` | `List<IDomainEvent>` | Khong | `domainEvents ??= [];` (`WriteDbContext.cs:418`) - `null` duoc thay bang list rong. **Khong** validate phan tu `null` ben trong | `null` |
| `cancellationToken` | `CancellationToken` | Khong | Khong validate; truyen cho `publisher.Publish(...)` (`WriteDbContext.cs:446`) | `default` |

**Output** - `Task` (khong co gia tri). Khong bao hieu da publish bao nhieu event.

**Dieu kien xu ly** (theo thu tu thuc thi)
1. `domainEvents ??= [];` (`:418`).
2. Truy van lazy: `ChangeTracker.Entries<Aggregate>().Select(x => x.Entity).Where(x => x.DomainEvents.Count > 0).SelectMany(x => x.DomainEvents)` (`:420-424`).
3. `if (!domainEventsInSaveChange.IsNullOrEmpty()) domainEvents.AddRange(domainEventsInSaveChange);` (`:426-429`) - **mutate** list dau vao.
4. `List<IDomainEvent> domainEventsToPublish = [.. domainEvents.Distinct()];` (`:431`) - `Distinct()` khong truyen comparer, dung equality mac dinh cua tung kieu event.
5. `ChangeTracker.Clear();` (`:433`) - **luon chay**, ke ca khi khong co event nao.
6. `if (domainEventsToPublish.Count is 0) return;` (`:435-438`).
7. `await using var scope = _serviceScopeFactory.Value.CreateAsyncScope();` (`:440`).
8. `IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();` (`:442`).
9. `foreach (IDomainEvent domainEvent in domainEventsToPublish) await publisher.Publish(domainEvent, cancellationToken: cancellationToken).ConfigureAwait(false);` (`:444-447`).

**Side effect**
- **Mutate tham so dau vao** `domainEvents` (buoc 3): caller truyen list cua minh vao se thay list bi them phan tu.
- **`ChangeTracker.Clear()`**: detach toan bo entity dang duoc theo doi - thay doi state cua `DbContext` dung chung.
- Tao/huy mot scope DI moi.
- Goi handler MediatR - cac handler nay co the ghi DB, goi API ngoai, ghi log (khong xac dinh duoc tu source code trong module nay).
- Khong ghi log trong ham nay.

**Error handling** - Khong co `try/catch`.
- `GetRequiredService<IPublisher>()` nem `InvalidOperationException` neu `IPublisher` chua duoc dang ky.
- Exception tu bat ky handler nao lam dut vong `foreach` -> cac event **con lai khong duoc publish**, va exception noi len caller (`SaveChangesAsync`).
- Neu `_serviceScopeFactory` la `null`: `NullReferenceException` tai `:440`.

**Khi nao NEN dung**
- Tu dong qua `SaveChanges`/`SaveChangesAsync` (khong can goi thu cong).
- Goi thu cong khi can publish mot danh sach event **khong** gan vao entity nao (truyen qua tham so `domainEvents`) - luu y buoc 5 se clear `ChangeTracker`.

**Khi nao KHONG dung**
- Truoc khi luu du lieu: goi thu cong se `ChangeTracker.Clear()` -> **mat toan bo thay doi chua luu**.
- Khi con can `ChangeTracker` giu entity sau do.
- Khi can publish song song hoac can outbox pattern (bao dam at-least-once): module nay publish in-process, dong bo, khong luu event vao DB, khong retry.

**Gioi han**
- `ChangeTracker.Clear()` dat truoc guard rong (`:433` vs `:435-438`) -> **moi** loi goi thanh cong deu clear tracker.
- Khong goi `Aggregate.ClearDomainEvents()` (`Abstractions/Aggregate.cs:30`) -> event van nam tren instance entity. Neu cung instance duoc `Attach` lai va luu lan nua, cac event cu co the duoc publish lai (`Distinct()` chi loai trung trong pham vi mot loi goi).
- `Distinct()` khong co comparer: hai event khac reference nhung "giong nhau" ve noi dung se **khong** bi loai trung neu kieu event la `class` thuong; con neu la `record` thi value equality se loai trung ca event that su khac nhau ve nghiep vu. Kieu event cu the do ung dung tieu thu dinh nghia - khong xac dinh duoc tu source code trong module nay.
- Chi lay event tu entity ke thua `Aggregate` (`ChangeTracker.Entries<Aggregate>()` - `:421`); entity chi trien khai `IAggregate` ma khong ke thua `Aggregate` se bi bo qua.
- Khong co `IsolationLevel`/idempotency: nhu canh bao muc 3.3, khi dung `UnitOfWork` event chay truoc commit.
- `DomainEvents` la `List<IDomainEvent>` cong khai co the ghi (`Abstractions/Aggregate.cs:18`), khong co bao ve concurrency.

### 4.7 `OnBeforeSaveChanges` (private)

**Signature**

```csharp
private void OnBeforeSaveChanges(AuditModel audit = null)
```

Nguon: `WriteDbContext.cs:129`.

**Muc dich** - Gan cac truong audit cua `IBaseEntitySQL` cho cac entry dang duoc theo doi, theo `EntityState`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `audit` | `AuditModel` | Khong | Cho phep `null`; truy cap bang `audit?.CreatorInfo?.X` (`:140`, `:143`, `:146`). `audit is null` quyet dinh nhanh `Modified` co chay hay khong (`:170-173`) | `null` |

**Output** - `void`.

**Dieu kien xu ly** (theo thu tu thuc thi)
1. `IEnumerable<EntityEntry> filtered = ChangeTracker.Entries()?.Where(x => x.Entity is IBaseEntitySQL);` (`:131-132`) - truy van **lazy**.
2. `if (filtered.IsNullOrEmpty()) return;` (`:134-137`) - dung extension `CollectionHelpers.IsNullOrEmpty<T>` (`Helpers/CollectionHelpers.cs:14`), chiu duoc `null`.
3. Tinh gia tri mac dinh:
   - `userName` = `audit?.CreatorInfo?.Name` neu khong trang, nguoc lai `CommonBaseConstant.Anonymous` = `"Anonymous"` (`:139-140`, `Constants/CommonBaseConstant.cs:33`).
   - `userCode` = `audit?.CreatorInfo?.Code` neu khong trang, nguoc lai `CommonBaseConstant.AnonymousCode` = `"0"` (`:142-143`, `CommonBaseConstant.cs:29`).
   - `organization` = `audit?.CreatorInfo?.Organization` neu khong trang, nguoc lai `CommonBaseConstant.OrganizationForISC` = `"FTEL"` (`:145-146`, `CommonBaseConstant.cs:31`).
4. `foreach (var entry in filtered)` + `switch (entry.State)` (`:148-183`):
   - **`EntityState.Added`** (`:152-167`):
     - `IsDeleted = false` - **gan de vo dieu kien**.
     - `CreatedDate ??= CommonBaseConstant.DateTimeUtc()` - `DateTimeUtc(int addHour = 7)` = `TimeProvider.System.GetUtcNow().DateTime.AddHours(7)` (`CommonBaseConstant.cs:47-50`), tuc **UTC+7**, khong phai UTC thuan.
     - `CreatedUser ??= userName`; `CreatedUserCode ??= userCode`; `CreatedUserOrganization ??= organization` - dung `??=`, **giu nguyen** gia tri caller da set.
     - `ModifiedDate`/`ModifiedUser`/`ModifiedUserCode`/`ModifiedUserOrganization` = `null` - **gan de vo dieu kien**.
   - **`EntityState.Modified or EntityState.Detached`** (`:168-181`):
     - `if (audit is null) break;` - **khong stamp gi** khi khong co `audit`.
     - `ModifiedDate = CommonBaseConstant.DateTimeUtc()`; `ModifiedUser = userName`; `ModifiedUserCode = userCode`; `ModifiedUserOrganization = organization` - gan **de** vo dieu kien (khong dung `??=`).
   - `EntityState.Unchanged` va `EntityState.Deleted`: **khong co case** -> khong xu ly. Xoa cung (hard delete) khong duoc stamp gi.

**Side effect** - **Mutate truc tiep cac entity dang duoc theo doi** (state cua object dung chung). Khong ghi log, khong ghi DB.

**Error handling** - Khong co `try/catch`. Cac phep `((IBaseEntitySQL)entry.Entity)` an toan vi da loc `x.Entity is IBaseEntitySQL` o buoc 1.

**Khi nao NEN dung** - Khong ap dung (`private`). Duoc goi tu ca 3 duong save: `SaveChanges(bool)` (`WriteDbContext.cs:54`), `SaveChangesAsync(AuditModel, bool, CancellationToken)` (`:77`) va `SaveChangesAsync(bool, CancellationToken)` (`:106`).

**Khi nao KHONG dung** - Khong ap dung (`private`, khong `virtual`): lop dan xuat **khong** override va **khong** goi duoc. Muon doi quy tac stamp audit (them truong cua `IEntityFullCreatedAndModifiedBase<T>`, doi mui gio, bo viec gan de `Modified*` = `null`) thi phai gan truoc khi goi `SaveChanges*` hoac sua chinh `WriteDbContext`.

**Gioi han**
- `filtered` la `IEnumerable` lazy va bi **enumerate hai lan** (buoc 2 va buoc 4). Chinh xac hon: `ChangeTracker.Entries()` la mot loi goi ham thuong, duoc thuc thi **dung mot lan** tai `:132`; chi phep chieu `.Where(...)` la lazy va bi duyet lai. Lan duyet thu nhat nam trong `IsNullOrEmpty` -> `!enumerable.Any()` (`Helpers/CollectionHelpers.cs:36`) nen **dung o phan tu khop dau tien**, khong duyet het. Chi phi trung lap vi the la mot lan quet toi phan tu khop dau tien, khong phai hai lan quet toan bo. Viec `Entries()` co kich hoat lai change detection hay khong la hanh vi noi bo cua EF Core, **khong** xac dinh duoc tu source code trong repo nay.
- `?.` sau `ChangeTracker.Entries()` (`:132`) la du thua neu `Entries()` khong bao gio tra ve `null`; tuy nhien `IsNullOrEmpty` da bao ve nen khong gay loi.
- `EntityState.Detached` xuat hien trong case (`:168`) nhung `ChangeTracker.Entries()` chi tra ve cac entry dang duoc theo doi. Khong xac dinh duoc tu source code trong repo nay lieu nhanh `Detached` co bao gio chay.
- Khong xu ly `EntityState.Deleted` -> khong co ho tro soft-delete tu dong (khong tu gan `IsDeleted = true`).
- Gan de `IsDeleted = false` o nhanh `Added` -> **khong the** insert mot ban ghi da o trang thai da xoa.
- Gan de `Modified*` = `null` o nhanh `Added` -> mat gia tri neu ai do co tinh set truoc.
- Thoi gian dung UTC+7 hardcode qua tham so mac dinh `addHour = 7` (`CommonBaseConstant.cs:47`) - khong cau hinh duoc tu day.
- Chi 3 truong cua `CreatorInfo` duoc doc: `Name`, `Code`, `Organization` (`:140`, `:143`, `:146`). Cac truong con lai (`Email`, `Role`, `RegionId`, `BranchId`, `LocationId`, `TitleCode`, `RolesSR`, `RolesFTel`, `ConcurrentAreas` - `Models/Audits/AuditModel.cs:22-88`) **khong** duoc su dung o day.
- Chi gan cac thanh vien cua `IBaseEntitySQL` (`Abstractions/Entities/BaseEntitySQL.cs:21-34`). Cac entity trien khai `IEntityFullCreatedAndModifiedBase<T>` co them `CreatedUserRegionId`/`CreatedUserLocationId`/`CreatedUserBranchId` va `ModifiedUserRegionId`/`ModifiedUserLocationId`/`ModifiedUserBranchId` (`BaseEntitySQL.cs:12-18`) - **khong co dong code nao** trong `OnBeforeSaveChanges` gan cac truong nay, mac du `CreatorInfo` co san `RegionId`/`BranchId`/`LocationId` (`AuditModel.cs:52-64`). Ung dung tieu thu phai tu gan.
- `IsNullOrEmpty` tren mot `IEnumerable` lazy khong phai `ICollection`/`IReadOnlyCollection`/`List` roi vao nhanh cuoi `return !enumerable.Any();` (`Helpers/CollectionHelpers.cs:14-37`, nhanh `Any()` o `:36`).

### 4.8 `DetectChangesAudit` (private)

**Signature**

```csharp
private List<SnapshotAuditModel> DetectChangesAudit(AuditModel audit = null)
```

Nguon: `WriteDbContext.cs:192`.

**Muc dich** - Theo ten ham va XML doc (`WriteDbContext.cs:186-190`): thu thap snapshot cac truong da thay doi de ghi audit log. **Theo than ham (nguon su that): khong lam viec do** - xem ngay duoi.

**Muc dich thuc te theo than ham** - Goi `ChangeTracker.DetectChanges()` roi **luon tra ve list rong**.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `audit` | `AuditModel` | Khong | `if (audit is null) return [];` (`:194-197`). Ngoai guard nay, `audit` **khong duoc dung o dau nua** trong code dang hoat dong | `null` |

**Output** - `List<SnapshotAuditModel>`:
- `audit` la `null` -> `[]` (`:196`).
- `audit` khac `null` -> `[]` (`:356`, co comment `//  auditEntries.Where(_ => _.HasTemporaryProperties).ToList();`).

Tuc la **moi truong hop deu tra ve list rong**.

**Dieu kien xu ly** (theo thu tu thuc thi)
1. `if (audit is null) return [];` (`:194-197`).
2. `ChangeTracker.DetectChanges();` (`:199`) - **dong code duy nhat co tac dung**.
3. Khoi `#region NOT SUPPORT` tu `:201` den `:353` - **toan bo bi comment out**.
4. `return [];` (`:356`).

**Side effect** - `ChangeTracker.DetectChanges()` (`:199`): buoc EF Core do tim thay doi tren cac entity dang theo doi. Khong ghi DB, khong ghi log.

**Error handling** - Khong co `try/catch`.

**Khi nao NEN dung** - Khong ap dung (`private`, chi goi tu 4.4).

**Khi nao KHONG dung** - Khong ap dung (`private`). Khong duoc coi gia tri tra ve cua ham nay la nguon audit log: no **luon** rong. Neu can audit log chi tiet (old/new values) thi phai tu cai dat o ung dung tieu thu, khong dua vao module nay.

**Gioi han**
- Ten ham va XML doc (`:186-190` - *"Lấy thông tin các trường thay đổi"*) **khong khop** than ham: ham khong tra ve thong tin truong thay doi nao.
- ~150 dong dead code trong `#region NOT SUPPORT` (`:201-353`) tham chieu cac kieu khong ton tai trong scope (`AuditEntry`, `ActivityTypeEnum`, `AuditLog`, `auditEntry.ToAudit()`) - khong the bo comment ra dung ngay.
- `SnapshotAuditModel` (`Models/Audits/SnapshotAuditModel.cs:5`) duoc dinh nghia day du (`KeyValues`, `OldValues`, `NewValues`, `ChangedColumns`, `TemporaryProperties`, `HasTemporaryProperties`) nhung **khong bao gio duoc khoi tao** trong module nay.
- Tac dung phu duy nhat con lai (`DetectChanges()`) khien hanh vi cua 4.4 khac 4.5 mot cach kho doan: chi khi truyen `audit` thi `DetectChanges()` moi duoc goi tuong minh.

### 4.9 `OnAfterSaveChanges` (private)

**Signature**

```csharp
private async Task OnAfterSaveChanges(List<SnapshotAuditModel> auditEntries = null, CancellationToken cancellationToken = default)
```

Nguon: `WriteDbContext.cs:366`.

**Muc dich** - Goi lan luot `DispatchAuditLog(auditEntries)` roi `DispatchDomainEvents(cancellationToken: cancellationToken)`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `auditEntries` | `List<SnapshotAuditModel>` | Khong | Khong validate tai day; guard nam trong `DispatchAuditLog` (`:375`) | `null` |
| `cancellationToken` | `CancellationToken` | Khong | Khong validate; truyen cho `DispatchDomainEvents` (`:370`) | `default` |

**Output** - `Task`.

**Dieu kien xu ly** - Tuan tu, khong co nhanh re: `await DispatchAuditLog(auditEntries);` (`:368`) roi `await DispatchDomainEvents(cancellationToken: cancellationToken);` (`:370`).

**Side effect** - Xem 4.6 va 4.10. Dac biet: `ChangeTracker.Clear()`.

**Error handling** - Khong co `try/catch`; exception noi len caller (`SaveChanges`/`SaveChangesAsync`). Rieng o duong `SaveChanges(bool)` (`:60`), `Task` bi bo -> exception khong duoc quan sat.

**Khi nao NEN dung** - Khong ap dung (`private`). Duoc goi tu `SaveChanges(bool)` (`WriteDbContext.cs:60`, fire-and-forget), `SaveChangesAsync(AuditModel, bool, CancellationToken)` (`:90`) va `SaveChangesAsync(bool, CancellationToken)` (`:112`) - chi khi so ban ghi bi anh huong lon hon 0.

**Khi nao KHONG dung** - Khong ap dung (`private`, khong `virtual`). Muon publish domain event ma **khong** `ChangeTracker.Clear()` thi khong co duong nao qua ham nay; phai goi `DispatchDomainEvents` (4.6, `public`) va chap nhan rang chinh no cung clear tracker.

**Gioi han** - Khong truyen `cancellationToken` cho `DispatchAuditLog` (`:368`). Khong co co che bu tru (compensation) neu `DispatchDomainEvents` that bai sau khi du lieu da luu.

### 4.10 `DispatchAuditLog` (private)

**Signature**

```csharp
private Task DispatchAuditLog(List<SnapshotAuditModel> auditEntries)
```

Nguon: `WriteDbContext.cs:373`.

**Muc dich** - Theo ten ham: phat tan (ghi) audit log da thu thap. **Theo than ham: khong ghi audit log nao** - xem ngay duoi.

**Muc dich thuc te theo than ham** - Neu `auditEntries` la `null` hoac rong: tra ve `Task.CompletedTask`. Nguoc lai: `return base.SaveChangesAsync();`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `auditEntries` | `List<SnapshotAuditModel>` | Co (nhung cho phep `null`) | `if (auditEntries is null || auditEntries.Count is 0) return Task.CompletedTask;` (`:375-378`) | Khong co |

**Output** - `Task`: luon la `Task.CompletedTask` trong thuc te (xem "Gioi han").

**Dieu kien xu ly** (theo thu tu thuc thi)
1. Guard rong -> `Task.CompletedTask` (`:375-378`).
2. `#region NOT SUPPORT` tu `:380` den `:402` - **toan bo bi comment out**.
3. `return base.SaveChangesAsync();` (`:404`).

**Side effect** - Trong thuc te khong co (luon dung o buoc 1).

**Error handling** - Khong co `try/catch`.

**Khi nao NEN dung** - Khong ap dung (`private`). Chi duoc goi tu `OnAfterSaveChanges` (`WriteDbContext.cs:368`).

**Khi nao KHONG dung** - Khong ap dung (`private`). Khong duoc dua vao ham nay de ghi audit log: voi moi call-site hien tai no chi tra `Task.CompletedTask`. Dac biet **khong** nen khoi phuc phan code bi comment nhu hien trang, vi dong `:404` se goi `base.SaveChangesAsync()` **ngay trong** luong `OnAfterSaveChanges` cua mot lan save dang chay (save long nhau).

**Gioi han**
- Dong `:404` la **dead code khong the toi duoc**. Chung minh: `DispatchAuditLog` chi duoc goi tu `OnAfterSaveChanges` (`:368`); ba loi goi `OnAfterSaveChanges` truyen (a) `null` (`:60`), (b) `detectChangesAudit` - luon rong vi `DetectChangesAudit` luon tra ve `[]` (`:196`, `:356`) - (`:90`), (c) `null` (`:112`). Do do guard `:375` luon dung.
- Neu dong `:404` co the toi duoc, no se goi `base.SaveChangesAsync()` **ngay trong** luong `OnAfterSaveChanges` cua mot loi goi save dang chay - rui ro save long nhau / de quy. Day la mot cai bay tiem an neu ai do khoi phuc phan audit log.
- Ten ham (`DispatchAuditLog`) va `#region NOT SUPPORT` cho thay y dinh la ghi audit log, nhung khong co dong code hoat dong nao ghi audit log.

---

## 5. `ReadDbContext<TContext>` - Chi tiet API

**Khai bao**

```csharp
public class ReadDbContext<TContext> : DbContext where TContext : DbContext
```

Nguon: `ReadDbContext.cs:12`. **Khong** phai `partial`, khong co field noi bo, chi 33 dong.

> [!IMPORTANT]
> **Khac biet thuc te so voi `WriteDbContext<TContext>`** - kiem chung bang toan bo noi dung file `ReadDbContext.cs` (33 dong):
>
> | Tieu chi | `ReadDbContext<TContext>` | `WriteDbContext<TContext>` |
> |---|---|---|
> | Ghi de `SaveChanges` / `SaveChangesAsync` | **Khong co dong nao** | Co 3 phuong thuc (`:52`, `:75`, `:104`) |
> | Chan ghi DB | **Khong** | Khong ap dung |
> | Cau hinh `QueryTrackingBehavior` / `AsNoTracking` mac dinh | **Khong co** | Khong ap dung |
> | Interceptor | **Khong dang ky trong lop** | Khong |
> | Audit stamping | **Khong** | Co (`OnBeforeSaveChanges`) |
> | Dispatch domain event | **Khong** | Co (`DispatchDomainEvents`) |
> | Luu `serviceScopeFactory` | **Khong** (nhan roi bo) | Co (`_serviceScopeFactory`) |
> | `OnModelCreating` | `ApplyConfigurationsFromAssembly(typeof(TContext).Assembly)` (`:30`) | Giong het (`:39`) |
> | `OnConfiguring` | **Khong ghi de** | Khong ghi de |
>
> Ket luan: **`ReadDbContext<TContext>` va `WriteDbContext<TContext>` khac nhau duy nhat o phan `SaveChanges`/audit/domain event va viec luu `serviceScopeFactory`. Ve `OnModelCreating` chung giong nhau hoan toan.**

> [!WARNING]
> `ReadDbContext<TContext>` **khong chan ghi**. Vi khong ghi de `SaveChanges`/`SaveChangesAsync` va khong dat `QueryTrackingBehavior.NoTracking`, ve ky thuat consumer hoan toan co the goi `context.Add(...)` / `context.SaveChanges()` tren context "read" va **du lieu se duoc ghi that vao DB** (voi dieu kien connection string tro toi DB co quyen ghi). Su tach biet read/write o day chi la **quy uoc**, khong co rang buoc ky thuat nao thi hanh.

**Ghi chu ve `ReadUncommittedConnectionInterceptor`** - Repo co lop `FTELSRCore.Shared/Data/SQL/Helpers/ReadUncommittedConnectionInterceptor.cs:12` chay `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED` sau khi connection mo (`:29-30`), va XML doc cua no noi la "thuong gan vao `ReadDbContext`" (`:7-8`). Tuy nhien **khong co dong `AddInterceptors` nao trong repo** (kiem tra bang `grep -rn "AddInterceptors"` - khong khop). Viec gan interceptor thuoc trach nhiem ung dung tieu thu.

### 5.1 Constructor

**Signature**

```csharp
public ReadDbContext(DbContextOptions<TContext> options, Lazy<IServiceScopeFactory> serviceScopeFactory) : base(options)
```

Nguon: `ReadDbContext.cs:20`.

**Muc dich** - Truyen `options` cho `DbContext`. **Than constructor rong** (`ReadDbContext.cs:21-22`).

**Dieu kien xu ly** - Khong co nhanh re va khong co lenh nao: chi `base(options)` chay. `serviceScopeFactory` **khong** duoc gan vao field nao (dead parameter - xem van de #30).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `options` | `DbContextOptions<TContext>` | Co | Khong validate; chuyen cho `base(options)` | Khong co |
| `serviceScopeFactory` | `Lazy<IServiceScopeFactory>` | Co (theo signature) | **Khong duoc dung o bat ky dong nao** trong lop; XML doc `:18` tu xac nhan *"hiện chưa được sử dụng trong lớp này"* | Khong co |

**Output** - Instance `ReadDbContext<TContext>`.

**Side effect** - Khong co (ngoai viec khoi tao `DbContext` base).

**Error handling** - Khong co `try/catch` trong lop nay.

**Khi nao NEN dung** - Qua DI, voi lop cu the `public class MyReadDbContext : ReadDbContext<MyReadDbContext>`.

**Khi nao KHONG dung** - Khong dung `ReadDbContext` cho luong ghi (mac du ky thuat cho phep): se mat audit stamping va domain event.

**Gioi han**
- Tham so `serviceScopeFactory` la **dead parameter**: bat buoc phai cung cap (nen DI phai dang ky `Lazy<IServiceScopeFactory>`) nhung khong co tac dung nao.
- Kieu `DbContextOptions<TContext>` buoc `TContext` phai la chinh lop dan xuat (self-referencing).
- **Khong** co overload constructor chi nhan `options`.

### 5.2 `OnModelCreating`

**Signature**

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
```

Nguon: `ReadDbContext.cs:28`.

**Muc dich** - `modelBuilder.ApplyConfigurationsFromAssembly(typeof(TContext).Assembly);` (`ReadDbContext.cs:30`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc / Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `modelBuilder` | `ModelBuilder` | Co | Khong validate | Khong co |

**Output** - `void`.

**Dieu kien xu ly** - Mot lenh duy nhat, khong nhanh re. **Khong** goi `base.OnModelCreating(modelBuilder)`.

**Side effect** - Thay doi model duoc build.

**Error handling** - Khong co.

**Khi nao NEN dung** - Tu dong, do EF Core goi mot lan khi build model.

**Khi nao KHONG dung** - Khong goi thu cong.

**Gioi han**
- Giong het `WriteDbContext.OnModelCreating` (`WriteDbContext.cs:39`) - **cung nap moi entity configuration**, ke ca configuration cua cac bang chi dung cho ghi. Model read va model write la giong nhau.
- Khong ap dung global query filter cho `IsDeleted` -> query tren `ReadDbContext` **khong** tu dong loc ban ghi da soft-delete.
- Khong dat `QueryTrackingBehavior.NoTracking` -> query mac dinh **van** tracking. `CoreSQL` phai tu goi `.AsNoTracking()` o tung query (vi du `CoreSQL.cs:310`, `:359`, `:403`, `:436`, `:469`, `:506`, `:544`, `:579`, `:613`).
- Khong goi `base.OnModelCreating`.

---

## 6. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | **Domain event duoc publish TRUOC khi transaction commit.** `CommitAsync` goi `SaveChangeAsync` (dan tao `OnAfterSaveChanges` -> `DispatchDomainEvents`) o dong 74, roi moi `_transaction.CommitAsync` o dong 76. | `UnitOfWork.cs:74-76`; `WriteDbContext.cs:88-91`, `:110-113` | Handler domain event chay tren du lieu chua commit. Neu commit that bai va rollback, event **da phat** nhung du lieu bi huy -> he thong o trang thai khong nhat quan (email/notification/API ngoai da duoc goi cho mot thay doi khong ton tai). |
| 2 | **`RollbackAsync` khong null-check `_transaction`.** Goi truc tiep khi chua `CreateTransactionAsync` -> `NullReferenceException`. | `UnitOfWork.cs:109-116` (cu the `:111`) | Code phong ve kieu `try { ... } catch { await uow.RollbackAsync(); }` hoac `finally { await uow.RollbackAsync(); }` co the nem `NullReferenceException` che mat exception nghiep vu goc. Mau thuan voi `CommitAsync` - noi **co** null-check tai `:89`. |
| 3 | **`CommitAsync` khong null-check `_transaction` nhung `SaveChanges` van chay truoc.** Neu goi `CommitAsync` khi chua co transaction: du lieu duoc luu thanh cong (`:74`) roi `NullReferenceException` o `:76`, khong rollback duoc (guard `:89` chan), cuoi cung `throw;` (`:100`). | `UnitOfWork.cs:70-101` | Du lieu **da persist** nhung caller nhan exception -> de dan den retry/ghi trung. Neu ca `_context` cung `null` thi NRE xay ra ngay o `:155` truoc khi ghi gi. |
| 4 | **`SaveChanges(bool)` goi `OnAfterSaveChanges()` theo kieu fire-and-forget (`_ =`), khong await.** | `WriteDbContext.cs:60` | (a) Exception phat sinh tu `publisher.Publish` (`:446`) bi mat (unobserved task exception) - caller khong bao gio thay. (b) Thoi diem publish domain event khong xac dinh: `Task` co the con dang cho khi caller da tra ve va da dispose context. (c) **Khong** co race condition tren `ChangeTracker`: `DispatchAuditLog` tra ve `Task.CompletedTask` (`:375-378`) nen `await` `:368` khong nhuong luong, va `ChangeTracker.Clear()` (`:433`) nam truoc diem `await` that su dau tien (`:446`) -> Clear() luon hoan thanh dong bo truoc khi `SaveChanges(bool)` tra ve. |
| 5 | **`DetectChangesAudit` luon tra ve list rong**, tac dung duy nhat con lai la `ChangeTracker.DetectChanges()`. Khoi code that su (~150 dong) bi comment out trong `#region NOT SUPPORT`. | `WriteDbContext.cs:192-357` (dead code `:201-353`, `return []` tai `:196` va `:356`) | Tinh nang audit log chi tiet (old/new values, changed columns, ip/device/method) **khong ton tai**. XML doc `:186-190` ("Lấy thông tin các trường thay đổi") mau thuan voi than ham - theo nguyen tac Source Code > Documentation, tin than ham. Dead code tham chieu cac kieu khong ton tai (`AuditEntry`, `ActivityTypeEnum`, `AuditLog`, `ToAudit()`) nen khong bo comment ra la dung duoc. |
| 6 | **`DispatchAuditLog` co dead code khong the toi duoc tai `return base.SaveChangesAsync();`.** Guard `:375` luon dung vi moi caller truyen `null` hoac list rong. | `WriteDbContext.cs:373-405` (dong khong the toi: `:404`; dead code `:380-402`) | Neu ai do khoi phuc phan audit, dong `:404` se goi `base.SaveChangesAsync()` ngay trong `OnAfterSaveChanges` cua mot loi goi save dang chay -> save long nhau, hanh vi kho luong. |
| 7 | **`ChangeTracker.Clear()` chay tren MOI loi goi save thanh cong**, ke ca khi khong co domain event nao (dat truoc guard `Count is 0`). | `WriteDbContext.cs:433` (guard o `:435-438`) | Sau moi `SaveChanges*` thanh cong, toan bo entity bi detach. (a) Code doc lai entity sau khi luu se query lai DB. (b) Vong retry cua `CoreSQL` (`_pipelineWrite.ExecuteAsync(...)` boc `SaveChangesAsync` tai `CoreSQL.cs:651-656`) neu retry **sau khi** lan chay truoc da luu thanh cong (`result > 0`) se khong con change -> tra ve `0`, du du lieu da persist. Kich ban nay hep: pipeline ghi chi retry loi connection-level (`SqlResiliencePolicyFactory.cs:194-199`, `IsRetryable(ex, false)` -> `ConnectionLevelSqlErrors` hoac `SocketException`, `MaxRetryAttempts = 1`), nen phai la mot `SqlException` connection-level / `SocketException` nem ra **sau** khi save thanh cong (vi du tu handler domain event). Neu loi xay ra ngay trong `base.SaveChangesAsync` thi `ChangeTracker` con nguyen va retry hoat dong dung. (c) Retry `CommitAsync` sau loi commit se khong luu lai gi va con nem `NullReferenceException` (xem muc 3.3). |
| 8 | **`ModifiedUser`/`ModifiedDate`/`ModifiedUserCode`/`ModifiedUserOrganization` khong bao gio duoc gan khi luu qua `UnitOfWork` hoac qua `SaveChangesAsync(bool, ct)` / `SaveChanges(bool)`.** Nhanh `Modified` `break` som khi `audit is null`. | `WriteDbContext.cs:170-173`; `UnitOfWork.cs:155`; `WriteDbContext.cs:106`, `:54` | Update qua cac duong nay mat hoan toan **cac cot audit `Modified*` tren ban ghi** (audit trail dang bang audit log thi khong duong nao co — xem van de #5). Chi overload `SaveChangesAsync(AuditModel, bool, CancellationToken)` (`:75`) voi `audit` khac `null` moi stamp Modified. `UnitOfWork` **khong co** cach truyen `AuditModel`. |
| 9 | **`ReadDbContext<TContext>` khong chan ghi** - khong ghi de `SaveChanges`/`SaveChangesAsync`, khong dat `QueryTrackingBehavior.NoTracking`, khong gan interceptor. | `ReadDbContext.cs:12-32` (toan bo file) | Ve ky thuat co the `Add`/`Update`/`Remove` + `SaveChanges()` tren context read va du lieu **duoc ghi that** (khong qua audit stamping, khong dispatch domain event). Phan tach read/write chi la quy uoc. |
| 10 | **XML doc cua `ReadDbContext` mau thuan voi code**: doc noi lop nay "khong chiu chi phi theo doi thay doi (change tracking)" nhung khong co dong nao dat `QueryTrackingBehavior.NoTracking` hay `ChangeTracker.QueryTrackingBehavior`. | `ReadDbContext.cs:6-9` vs toan bo than lop | Doc gay hieu sai. Thuc te query mac dinh **van tracking**; muon NoTracking phai goi `.AsNoTracking()` o tung query nhu `CoreSQL` dang lam (`CoreSQL.cs:310`, `:359`, `:403`, ...). Theo Source Code > Documentation: tin code. |
| 11 | **`ReadUncommittedConnectionInterceptor` ton tai nhung khong duoc gan vao dau trong repo.** | `Helpers/ReadUncommittedConnectionInterceptor.cs:12`; khong co ket qua nao cho `grep -rn "AddInterceptors"` | Y dinh thiet ke (READ UNCOMMITTED cho luong doc) **khong duoc thi hanh** tu thu vien. Neu ung dung tieu thu khong dang ky interceptor, luong doc chay o isolation level mac dinh. |
| 12 | **Constraint tren interface va tren class khong dong nhat**: interface yeu cau `where DBContextWrite : WriteDbContext<DBContextWrite>, IAsyncDisposable`, class chi co `where DBContextWrite : WriteDbContext<DBContextWrite>`. | `IUnitOfWork.cs:6` vs `UnitOfWork.cs:9` | `IAsyncDisposable` trong interface la du thua (moi `DbContext` da trien khai `IAsyncDisposable`) nhung tao ra hai hop dong khac nhau cho cung mot khai niem, gay nham lan khi khai bao generic wrapper. |
| 13 | **`IUnitOfWork<DBContextWrite>` khong ke thua `IAsyncDisposable`** trong khi `UnitOfWork<DBContextWrite>` co. | `IUnitOfWork.cs:6` vs `UnitOfWork.cs:9`, `:124` | Consumer inject `IUnitOfWork<T>` **khong thay** `DisposeAsync`, khong dung duoc `await using` -> de ro ri `DbContext` va transaction neu DI container khong tu dispose. |
| 14 | **`Context()` khong thread-safe va khong kiem tra `_disposed`.** | `UnitOfWork.cs:24-34`; `:126-131` | (a) Hai luong goi song song co the tao hai `DbContext`, mot cai bi ghi de va **khong bao gio duoc dispose**. (b) Goi `Context()` sau `DisposeAsync()` tao context moi ma `UnitOfWork` khong con dispose (co `_disposed` da `true`) -> ro ri. |
| 15 | **`DisposeAsync` khong co `try/finally`**: neu `DisposeTransactionAsync()` nem exception, `_context` khong duoc dispose va `_disposed` da la `true` nen khong the dispose lai. | `UnitOfWork.cs:124-141` (thu tu `:133` truoc `:135`) | Ro ri `DbContext`/connection. Ngoai ra `DisposeAsync` khong rollback transaction tuong minh - phu thuoc hoan toan vao hanh vi dispose cua EF Core. |
| 16 | **`DispatchDomainEvents` mutate tham so dau vao `domainEvents`.** | `WriteDbContext.cs:428` (`domainEvents.AddRange(...)`) | Caller truyen list cua minh vao se thay list bi them cac event lay tu `ChangeTracker` - side effect ngoai y muon, dac biet neu list do duoc tai su dung. |
| 17 | **Domain event khong duoc xoa khoi entity sau khi publish** - `Aggregate.ClearDomainEvents()` khong duoc goi. | `WriteDbContext.cs:416-448`; `Abstractions/Aggregate.cs:30` | Neu cung instance entity duoc `Attach` lai vao mot context va luu lan nua, cac event cu con nam tren `DomainEvents` co the duoc publish lai. `Distinct()` (`:431`) chi loai trung trong pham vi mot loi goi. |
| 18 | **`Distinct()` khong co comparer** cho danh sach domain event. | `WriteDbContext.cs:431` | Ket qua loai trung phu thuoc vao kieu event do ung dung tieu thu dinh nghia (`class` -> reference equality, `record` -> value equality). Voi `record`, hai event khac nhau ve nghiep vu nhung giong noi dung se bi **mat mot cai**. Khong xac dinh duoc tu source code trong module nay. |
| 19 | **Exception trong mot domain event handler lam dut vong publish** - cac event con lai bi bo. | `WriteDbContext.cs:444-447` (khong co `try/catch` trong `foreach`) | Publish khong atomic: mot phan event da chay, phan con lai khong. Khong co retry, khong co outbox, khong co dead-letter. |
| 20 | **Toan bo log cua `UnitOfWork` dung muc `Warning`, ke ca cac su kien binh thuong** ("Create transaction.", "Commit transaction.", "Rollback transaction."). | `UnitOfWork.cs:49`, `:57`, `:80`, `:84`, `:96`, `:115`; `Extensions/Loggers/LoggerExtensions.cs:134-137` | Log noise: khong the loc canh bao that su ra khoi log van hanh binh thuong; ton chi phi logging/alerting neu he thong canh bao theo `LogLevel.Warning`. |
| 21 | **`OnBeforeSaveChanges` gan de `IsDeleted = false` va toan bo `Modified*` = `null` cho entity `Added`, vo dieu kien.** | `WriteDbContext.cs:154`, `:161-164` | Khong the insert ban ghi da o trang thai soft-delete; moi gia tri `Modified*` do caller co tinh dat truoc khi insert deu bi xoa. Nguoc lai, cac truong `Created*` dung `??=` (`:156-159`) nen **duoc giu**, khong nhat quan. |
| 22 | **Khong xu ly `EntityState.Deleted` va `EntityState.Unchanged`** trong `switch` cua `OnBeforeSaveChanges`. | `WriteDbContext.cs:150-182` | Khong co soft-delete tu dong: `Remove()` -> hard delete, khong stamp `IsDeleted`/`ModifiedDate`. Ung dung phai tu quan ly soft-delete. |
| 23 | **Nhanh `EntityState.Detached` trong `switch` co the la dead code.** `ChangeTracker.Entries()` chi tra ve cac entry dang duoc theo doi. | `WriteDbContext.cs:168` | Khong xac dinh duoc tu source code trong repo nay lieu nhanh nay co bao gio chay; neu khong, mo ta hanh vi cua ham dua vao `Detached` la sai lech. |
| 24 | **`filtered` la truy van LINQ lazy nhung duoc enumerate hai lan** (`IsNullOrEmpty` roi `foreach`). | `WriteDbContext.cs:131-134` va `:148` | Chi phep chieu `.Where(...)` bi duyet lai - `ChangeTracker.Entries()` van chi duoc **goi mot lan** tai `:132`. Lan duyet thu nhat la `Any()` (`CollectionHelpers.cs:36`) nen dung o phan tu khop dau tien. Chi phi trung lap nho; day la van de style/`ToList()` bi thieu chu khong phai van de hieu nang dang ke. Cung mau loi nay xuat hien o `DispatchDomainEvents`: `domainEventsInSaveChange` (`:420-424`) bi duyet boi `IsNullOrEmpty()` (`:426`) roi `AddRange` (`:428`). |
| 25 | **`CommitAsync` bo gia tri tra ve cua `SaveChangeAsync` bang `_ = ...`.** | `UnitOfWork.cs:74` | Caller khong biet so ban ghi bi anh huong; khong the phat hien truong hop "khong co gi duoc luu" (`0`) de xu ly nghiep vu tuong ung. |
| 26 | **`SaveChangeAsync` la `private`** - khong co API cong khai de luu ma khong commit transaction. | `UnitOfWork.cs:153-156`; `IUnitOfWork.cs:8-37` | Muon `SaveChanges` khong commit, phai lay context ra: `(await uow.Context(ct)).SaveChangesAsync(audit: ..., cancellationToken: ct)` - pha vo dong goi cua UnitOfWork. |
| 27 | **`_ = ` va so sanh khong nhat quan giua cac overload save**: `result > 0` (`:58`, `:88`) vs `result >= 1` (`:110`). | `WriteDbContext.cs:58`, `:88`, `:110` | Khong khac ve nghia voi `int` nhung lam kho doc/kho bao tri; goi y ba nhanh save duoc viet o cac thoi diem khac nhau, khong duoc dong bo. |
| 28 | **`using System.Formats.Asn1;` khong duoc su dung** trong `WriteDbContext.cs`. | `WriteDbContext.cs:1` | Chi la nhieu (noise), nhung la dau hieu cua auto-import khong mong muon. |
| 29 | **XML doc cua `IUnitOfWork.Context` goi day la "Property"** trong khi day la method. | `IUnitOfWork.cs:8-10` vs `:14` | Doc sai loai thanh vien; boilerplate chua duoc cap nhat. |
| 30 | **`serviceScopeFactory` cua `ReadDbContext` la dead parameter** - bat buoc cung cap nhung khong duoc dung. | `ReadDbContext.cs:20-22` | Ung dung tieu thu bat buoc phai dang ky `Lazy<IServiceScopeFactory>` trong DI de khoi tao duoc `ReadDbContext` du no khong can. XML doc `:18` da tu ghi nhan dieu nay. |
| 31 | **`IUnitOfWork`/`UnitOfWork` khong duoc tham chieu boi bat ky code nao khac trong repo, va khong co dong dang ky DI nao** cho `IDbContextFactory`, `Lazy<T>`, `IPublisher` hay `IUnitOfWork`. | `grep -rn "UnitOfWork" --include="*.cs"` chi khop 2 file trong `Data/SQL/UnitOfWork/`; khong co ket qua cho `AddDbContext`/`AddDbContextFactory` | Cac loi goi tren la trach nhiem cua ung dung tieu thu. Ham y: khong co test hay call-site nao trong repo nay xac minh hanh vi cua `UnitOfWork` -> cac loi #2, #3, #8 chua tung bi phat hien qua su dung thuc te trong repo. |
| 32 | **`CommonBaseConstant.DateTimeUtc()` mac dinh `addHour = 7`** (UTC+7), duoc dung lam `CreatedDate`/`ModifiedDate`. | `WriteDbContext.cs:156`, `:175`; `Constants/CommonBaseConstant.cs:47-50` | Cac truong ten "Date" luu gio Viet Nam chu **khong** phai UTC, du ten ham la `DateTimeUtc`. Ten ham gay hieu sai; khong the cau hinh mui gio tu `WriteDbContext`. |
| 33 | **Log loi cua `UnitOfWork` khong truyen doi tuong `Exception`.** `LoggerExtensions.Warning` co tham so `Exception e = null` (`LoggerExtensions.cs:254`) nhung ca hai loi goi trong `catch` chi noi `exception.Message` vao chuoi. | `UnitOfWork.cs:84-85`, `:96-97` | Mat stack trace, inner exception va cac thuoc tinh chan doan (`SqlException.Number`, `DbUpdateException.Entries`, ...) trong log -> rat kho dieu tra loi commit/rollback tren moi truong that. |
| 34 | **Khong null-check tham so primary constructor `logger` va `dbContext`.** | `UnitOfWork.cs:7-9` | `dbContext` la `null` -> `NullReferenceException` tai `:31` khi goi `Context()`; `logger` la `null` -> `NullReferenceException` tai loi goi `logger.Warning` dau tien (`:49`/`:57`/`:80`/`:115`). Loi chi lo ra o thoi diem su dung, khong phai thoi diem khoi tao. |
| 35 | **Cac truong ngoai `IBaseEntitySQL` khong duoc stamp.** `OnBeforeSaveChanges` chi gan cac thanh vien cua `IBaseEntitySQL`; cac truong `CreatedUserRegionId`/`CreatedUserLocationId`/`CreatedUserBranchId`, `ModifiedUserRegionId`/`ModifiedUserLocationId`/`ModifiedUserBranchId` cua `IEntityFullCreatedAndModifiedBase<T>` khong co dong code nao gan. | `WriteDbContext.cs:132`, `:148-183`; `Abstractions/Entities/BaseEntitySQL.cs:12-18` | Entity dung `IEntityFullCreatedAndModifiedBase<T>` mat toan bo audit theo don vi/khu vuc, du `CreatorInfo` da mang san `RegionId`/`BranchId`/`LocationId` (`Models/Audits/AuditModel.cs:52-64`). Ung dung tieu thu phai tu gan truoc khi goi save. |
| 36 | **`DispatchDomainEvents` duyet truy van lazy hai lan** (`IsNullOrEmpty()` roi `AddRange`). | `WriteDbContext.cs:420-424` va `:426`, `:428` | `ChangeTracker.Entries<Aggregate>()` duoc goi mot lan (`:421`) nhung phep chieu `Select`/`Where`/`SelectMany` bi duyet lai; thieu mot `ToList()`. Chi phi trung lap nho, cung mau loi voi #24. |
