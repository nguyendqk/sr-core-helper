# PrecateBuilderExtensions & ProjectToExtensions

> Nguon: `FTELSRCore.Shared/Extensions/PrecateBuilderExtensions.cs`, `FTELSRCore.Shared/Extensions/ProjectToExtensions.cs`
> Loai: hai `static class` doc lap trong namespace `FTELSRCore.Extensions`
> Cap nhat theo commit: `89c1ce9`

> [!NOTE]
> Ca hai file deu la **extension method library** thuan tuy reflection/expression-tree, khong tu inject dependency, khong tu ket noi DB. Chung duoc goi truc tiep boi `CoreMongoDB`, `CoreSQL`, `CoreSQLTenant` (xem cac file KB da co: `Data-MongoDB-CoreMongoDB.md`, `Data-SQL-CoreSQL.md`, `Data-SQL-CoreSQL-TwoEntity.md`) nhung ban than hai file nay khong phu thuoc nguoc lai cac file do.

## 1. Tong quan

`PrecateBuilderExtensions` cung cap cac ham dung de **xay va gop predicate** (`Expression<Func<T, bool>>`) mot cach dong: tao predicate luon dung/luon sai, gop hai predicate bang AND/OR, phu dinh predicate, va tao san predicate loc `IsDeleted`. Day la ky thuat PredicateBuilder pho bien (dua tren viec rebind ParameterExpression) de co the viet `filter.And(other)` ma khong bi loi "parameter khong thuoc expression nay".

`ProjectToExtensions` gop hai nhom chuc nang khac nhau trong cung mot static class: (1) **anh xa du lieu** giua entity/DTO bang reflection thuan (`ProjectTo`) hoac bang expression tree compile (`MapUsingExpression`, `ConvertTo`); (2) **xay dung `UpdateDefinition<T>` cho MongoDB** tu mot object (`MapUpdateDefinition`) va **dong dau truong audit** (created/modified) cho ca entity va `UpdateDefinition` (`SetDataCreatedDefault`, `SetDataUpdatedDefault`); (3) **chuyen doi kieu tham so cua predicate** giua hai kieu entity khac nhau (`ReplaceParameter`, `ReplaceParameters`, `WhereReplacerVisitor`) va **gop nhieu predicate cung kieu** (`CombineExpressions`). Ca hai file nam o tang **Shared/Extensions** — lop ha tang dung chung cho tat ca module Data (MongoDB/SQL) trong repo.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Gop nhieu `Expression<Func<T,bool>>` thanh mot bang AND/OR, phu dinh mot predicate | Khong tu suy luan predicate tu dieu kien nghiep vu; caller phai tu viet lambda |
| Tao predicate loc `IsDeleted == isDeleted` cho kieu `T` bat ky (qua ten property hardcode) | Khong kiem tra `T` co property `IsDeleted` luc compile; loi chi phat sinh luc runtime |
| Anh xa entity <-> DTO/entity khac theo ten property trung khop (`ProjectTo`, `ConvertTo`, `MapUsingExpression`) | Khong co cau hinh mapping tuy bien (khong co "ignore field X", "map field A sang field B") ngoai hai attribute `[NoMap]`/`[NoMapUpdateDefinition]` |
| Sinh `UpdateDefinition<T>` dang `$set` tu cac property khac `null` cua mot object (`MapUpdateDefinition`) | Khong ho tro cac operator MongoDB khac ngoai `Set` (khong `$unset`, `$inc`,...) |
| Dong dau tu dong `Created*/Modified*` cho entity hoac `UpdateDefinition` khi property dang `null` (hoac luon dong dau voi ban `UpdateDefinition`) | Khong validate gia tri `audit` dau vao (khong co exception ro rang khi thieu thong tin nguoi dung, chi fallback ve gia tri "Anonymous") |
| Doi kieu tham so lambda cua mot predicate/mang predicate tu `TFrom` sang `TTo` khi hai kieu co property cung ten (`ReplaceParameter(s)`) | Khong dam bao dich dung cho bieu thuc truy cap thanh vien long nhau nhieu cap (`x => x.Child.Code`) — chua xac dinh duoc tu source code, xem muc 3 |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `System.Linq.Expressions` (`Expression`, `Expression<T>`, `ExpressionVisitor`, `ParameterExpression`, `MemberExpression`, `MemberAssignment`) | Xay va bien doi expression tree cho predicate va cho phep gan gia tri (`MapUsingExpression`) |
| `System.Reflection` (`PropertyInfo`, `BindingFlags`) va `System` (`Activator` — luu y: `Activator` thuoc namespace `System`, khong phai `System.Reflection`, du thuong dung cung reflection) | Doc/ghi property bang reflection trong `ProjectTo`, `ConvertTo`, `MapUpdateDefinition`, `SetDataCreatedDefault`, `SetDataUpdatedDefault` |
| `MongoDB.Driver` (`FilterDefinition<T>`, `SortDefinition<T>`, `UpdateDefinition<T>`, `Builders<T>`, `UpdateDefinitionBuilder<T>`) | Kieu du lieu cho `QueryContext<TTable,TDto>` va cho cac ham sinh/dong dau `UpdateDefinition` |
| `FTELSRCore.Abstractions.Entities` (`EntityFullCreatedAndModifiedBase`, `BaseEntityMongoDB`) | Lay ten property chuan (`CreatedUser`, `ModifiedDate`, `IsDeleted`,...) qua `nameof(...)` khi dong dau audit — **khong** rang buoc kieu generic `TTable` phai ke thua lop nay, chi dung ten hang so |
| `FTELSRCore.Models.Audits.AuditModel` (record, co `CreatorInfo.Name/Code/Organization`) | Nguon du lieu nguoi thao tac dung de dong dau `Created*/Modified*` |
| `CommonBaseConstant` (`Anonymous`, `AnonymousCode`, `OrganizationForISC`, `DateTimeUtc()`, `ConfigLoggerExceptionByConsole(...)`) | Gia tri fallback khi thieu audit, ham lay thoi gian, ham ghi log loi ra Console |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `True<T>()` | PrecateBuilderExtensions — predicate co ban | Predicate luon tra `true` |
| `False<T>()` | PrecateBuilderExtensions — predicate co ban | Predicate luon tra `false` |
| `Create<T>(predicate)` | PrecateBuilderExtensions — predicate co ban | Tra lai chinh predicate dau vao (ho tro suy luan kieu) |
| `And<T>(first, second)` | PrecateBuilderExtensions — gop predicate | Gop hai predicate bang `AndAlso` |
| `Or<T>(first, second)` | PrecateBuilderExtensions — gop predicate | Gop hai predicate bang `OrElse` |
| `Not<T>(expression)` | PrecateBuilderExtensions — gop predicate | Phu dinh mot predicate |
| `AddIsDeleted<T>(isDeleted = false)` | PrecateBuilderExtensions — predicate dac thu | Tao predicate `x => x.IsDeleted == isDeleted` |
| `QueryContext<TTable,TDto>` (record) | ProjectToExtensions — kieu du lieu | Goi 3 tham so truy van Mongo: filter, sort, selector |
| `ProjectTo<TEntity,TDto>(this TEntity)` | ProjectToExtensions — anh xa | Anh xa mot object sang DTO bang reflection |
| `ProjectTo<TEntity,TDto>(this List<TEntity>)` | ProjectToExtensions — anh xa | Anh xa danh sach object sang danh sach DTO |
| `MapUsingExpression<TFrom,TTo>(source)` | ProjectToExtensions — anh xa | Anh xa bang expression tree compile (nhanh hon reflection lap lai) |
| `ConvertTo<TTableFrom,TTableTo>(source)` | ProjectToExtensions — anh xa | Anh xa mot object, chi copy khi ten **va** kieu property trung khop |
| `ConvertTo<TTableFrom,TTableTo>(IEnumerable<TTableFrom>)` | ProjectToExtensions — anh xa | Ban danh sach cua `ConvertTo` don |
| `MapUpdateDefinition<TTableTo>(request)` | ProjectToExtensions — Mongo update | Sinh `UpdateDefinition<TTableTo>` tu property khac `null` cua `request` |
| `MapUpdateDefinition<TTableTo>(IEnumerable<TTableTo>)` | ProjectToExtensions — Mongo update | Ban danh sach cua `MapUpdateDefinition` don |
| `SetDataCreatedDefault<TTable>(entity, audit)` | ProjectToExtensions — audit stamping | Dong dau `IsDeleted`/`Created*` con `null` tren entity |
| `SetDataCreatedDefault<TTable>(updateDefinition, audit)` | ProjectToExtensions — audit stamping | Them stage `Set` cho `IsDeleted`/`Created*` vao `UpdateDefinition` |
| `SetDataUpdatedDefault<TTable>(entity, audit)` | ProjectToExtensions — audit stamping | Dong dau `Modified*` con `null` tren entity |
| `SetDataUpdatedDefault<TTable>(updateDefinition, audit)` | ProjectToExtensions — audit stamping | Them stage `Set` cho `Modified*` vao `UpdateDefinition` |
| `ReplaceParameter<TFrom,TTo>(this Expression<Func<TFrom,bool>>)` | ProjectToExtensions — predicate cross-type | Doi kieu tham so cua mot predicate |
| `ReplaceParameters<TFrom,TTo>(this Expression<Func<TFrom,bool>>[])` | ProjectToExtensions — predicate cross-type | Ban mang cua `ReplaceParameter` |
| `CombineExpressions<TTo>(expressions)` | ProjectToExtensions — predicate cross-type | Gop mot mang predicate cung kieu `TTo` bang AND |
| `WhereReplacerVisitor<TFrom,TTo>` (public class) | ProjectToExtensions — ha tang predicate cross-type | `ExpressionVisitor` thuc hien viec doi tham so cho `ReplaceParameter`/`ReplaceParameters` |
| `NoMapAttribute` | ProjectToExtensions — attribute danh dau | Danh dau property khong duoc `ProjectTo` copy |
| `NoMapUpdateDefinitionAttribute` | ProjectToExtensions — attribute danh dau | Danh dau property khong duoc `MapUpdateDefinition` dua vao `$set` |

## 2. Chi tiet API

### 2.1 True&lt;T&gt;()

**Signature**
```csharp
public static Expression<Func<T, bool>> True<T>()
```
**Muc dich** - Tao mot predicate hang so luon tra `true`, dung nhu gia tri khoi dau khi gop nhieu dieu kien bang `And` (`PrecateBuilderExtensions.cs:16-17`).

**Input hop le** - Khong co tham so; `T` la kieu generic bat ky.

**Output** - `Expression<Func<T, bool>>` tuong duong `param => true`. Khong bao gio `null`.

**Dieu kien xu ly** - Khong co nhanh re; luon tra ve bieu thuc hang so.

**Side effect** - Khong co.

**Error handling** - Khong co try/catch; ham khong the nem exception.

**Khi nao NEN dung** - Khoi tao mot predicate "rong" truoc khi `And` nhieu dieu kien dong (pattern PredicateBuilder kinh dien).

**Khi nao KHONG dung** - Khong co ly do ky thuat de tranh dung; can luu y ham nay **khong duoc goi truc tiep** boi bat ky file source khac trong repo (xem muc 3, phat hien #1).

**Gioi han** - Khong xac dinh duoc tu source code muc dich thiet ke cu the ngoai vai tro "gia tri khoi dau" cua PredicateBuilder.

### 2.2 False&lt;T&gt;()

**Signature**
```csharp
public static Expression<Func<T, bool>> False<T>()
```
**Muc dich** - Tao mot predicate hang so luon tra `false`, doi xung voi `True<T>()`, thuong dung nhu gia tri khoi dau khi gop dieu kien bang `Or` (`PrecateBuilderExtensions.cs:23-24`).

**Input hop le** - Khong co tham so.

**Output** - `Expression<Func<T, bool>>` tuong duong `param => false`. Khong bao gio `null`.

**Dieu kien xu ly** - Khong co nhanh re.

**Side effect** - Khong co.

**Error handling** - Khong co try/catch.

**Khi nao NEN dung** - Khoi tao predicate truoc khi `Or` nhieu dieu kien dong.

**Khi nao KHONG dung** - Khong tim thay noi goi ham nay trong repo (xem muc 3, phat hien #1).

**Gioi han** - Khong xac dinh duoc tu source code muc dich thiet ke cu the.

### 2.3 Create&lt;T&gt;(predicate)

**Signature**
```csharp
public static Expression<Func<T, bool>> Create<T>(Expression<Func<T, bool>> predicate)
```
**Muc dich** - Tra lai chinh `predicate` dau vao khong thay doi; muc dich thuc te la ho tro trinh bien dich **suy luan kieu generic `T`** tu mot bieu thuc lambda ma khong can khai bao kieu ro rang (`PrecateBuilderExtensions.cs:30-31`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `predicate` | `Expression<Func<T,bool>>` | Co | Khong validate; nhan ca `null` | Khong co |

**Output** - Tra lai chinh doi tuong `predicate` (bao gom `null` neu dau vao la `null`) — **khong tao ban sao**.

**Dieu kien xu ly** - Khong co nhanh re; mot lenh `return` duy nhat.

**Side effect** - Khong co.

**Error handling** - Khong co try/catch; khong the nem exception voi than ham hien tai.

**Khi nao NEN dung** - Khi muon viet `var p = PrecateBuilderExtensions.Create<T>(x => x.Field == value);` de co bien `p` voi kieu `Expression<Func<T,bool>>` ro rang, phuc vu goi tiep `.And(...)`/`.Or(...)`.

**Khi nao KHONG dung** - Khong can dung neu da khai bao kieu bien lambda truoc do; khong tim thay noi goi ham nay trong repo (xem muc 3, phat hien #1).

**Gioi han** - Khong lam gi khac ngoai tra lai tham so; khong co gia tri xu ly nghiep vu.

### 2.4 And&lt;T&gt;(first, second)

**Signature**
```csharp
public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
```
**Muc dich** - Gop `first` va `second` thanh mot predicate duy nhat bang phep AND logic (`Expression.AndAlso`), dam bao ca hai predicate dung chung mot `ParameterExpression` (`PrecateBuilderExtensions.cs:37-40`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `first` (this) | `Expression<Func<T,bool>>` | Co | **Khong guard `null`** | Khong co |
| `second` | `Expression<Func<T,bool>>` | Co | **Khong guard `null`** | Khong co |

**Output** - `Expression<Func<T,bool>>` moi, tham so lambda lay tu `first.Parameters`, body la `first.Body AndAlso second.Body` (sau khi rebind tham so cua `second` ve tham so cua `first`).

**Dieu kien xu ly** - Goi noi bo `first.Compose(second, Expression.AndAlso)` (ham `Compose` private, `PrecateBuilderExtensions.cs:80-92`): (1) zip tham so cua `second` sang tham so cua `first` thanh `Dictionary`; (2) dung `ParameterRebinder` (private `ExpressionVisitor`, dong 94-118) de thay tham so trong body cua `second`; (3) tao `Expression.Lambda` moi voi `merge(first.Body, secondBody)` va tham so cua `first`.

**Side effect** - Khong co (khong mutate `first`/`second` dau vao — expression tree la immutable, ham tra ve **doi tuong moi**).

**Error handling** - Khong co try/catch trong `And` hay `Compose`. Neu `first` la `null`: goi `first.Compose(...)` truy cap `first.Parameters` ben trong `Compose` (dong 83) se nem **`NullReferenceException`**. Neu `second` la `null`: bieu thuc `second.Parameters[i]` (dong 84) cung nem `NullReferenceException`. Khong co exception nao duoc bat lai hay nem loai rieng.

**Khi nao NEN dung** - Gop dan nhieu dieu kien loc dong (vi du dieu kien nghiep vu AND voi dieu kien `IsDeleted`) khi ca hai ben la `Expression<Func<T,bool>>` khong `null`.

**Khi nao KHONG dung** - Khi khong chac `first`/`second` co the la `null` — phai tu kiem tra truoc, ham khong tu bao ve.

**Gioi han** - Khong co null-guard cho ca hai tham so; day la nguyen nhan goc cua nhieu canh bao "NullReferenceException neu filter la null" trong cac file KB CoreMongoDB/CoreSQL da co (vi cac ham do goi `filter.And(addIsDeleted)` truc tiep).

### 2.5 Or&lt;T&gt;(first, second)

**Signature**
```csharp
public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
```
**Muc dich** - Gop `first` va `second` bang phep OR logic (`Expression.OrElse`), cung dung chung co che `Compose` voi `And<T>` (`PrecateBuilderExtensions.cs:46-49`).

**Input hop le** - Giong `And<T>` (muc 2.4): ca hai tham so bat buoc, khong guard `null`.

**Output** - `Expression<Func<T,bool>>` moi, body la `first.Body OrElse second.Body` (sau rebind tham so).

**Dieu kien xu ly** - Giong `And<T>`, chi khac merge function truyen vao `Compose` la `Expression.OrElse` thay vi `Expression.AndAlso`.

**Side effect** - Khong co.

**Error handling** - Giong `And<T>`: `NullReferenceException` neu `first` hoac `second` la `null`, khong co try/catch.

**Khi nao NEN dung** - Gop nhieu dieu kien loc theo kieu "thoa mot trong cac dieu kien".

**Khi nao KHONG dung** - Khi khong chac chan ca hai tham so khac `null`. Ngoai ra, khong tim thay noi goi `Or<T>` trong repo hien tai (xem muc 3, phat hien #1) — can kiem tra ky truoc khi dua vao logic nghiep vu moi.

**Gioi han** - Khong co null-guard, giong `And<T>`.

### 2.6 Not&lt;T&gt;(expression)

**Signature**
```csharp
public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expression)
```
**Muc dich** - Phu dinh logic cua mot predicate: tao `Expression.Not(expression.Body)` roi boc lai thanh lambda voi cung tham so (`PrecateBuilderExtensions.cs:56-59`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `expression` (this) | `Expression<Func<T,bool>>` | Co | **Khong guard `null`** | Khong co |

**Output** - `Expression<Func<T,bool>>` moi tuong duong `param => !(<body cua expression>)`, dung lai `expression.Parameters` goc (khong tao tham so moi, khac voi `Compose`).

**Dieu kien xu ly** - Hai buoc: `Expression.Not(expression.Body)` roi `Expression.Lambda<Func<T,bool>>(negated, expression.Parameters)`.

**Side effect** - Khong co; `expression` dau vao khong bi doi (immutable).

**Error handling** - Khong co try/catch. Neu `expression` la `null`, truy cap `expression.Body` nem **`NullReferenceException`** ngay dong dau ham.

**Khi nao NEN dung** - Can dieu kien phu dinh mot predicate da co san ma khong muon viet lai lambda.

**Khi nao KHONG dung** - Khi `expression` co the la `null`. Ngoai ra khong tim thay noi goi `Not<T>` trong repo hien tai ngoai chinh file dinh nghia (xem muc 3, phat hien #1).

**Gioi han** - Khong guard `null`; khong xu ly truong hop `expression.Body` khong phai kieu `bool` (truong hop nay khong the xay ra voi kieu generic dang khai bao la `Expression<Func<T,bool>>` nen khong phai rui ro thuc te).

### 2.7 AddIsDeleted&lt;T&gt;(isDeleted = false)

**Signature**
```csharp
public static Expression<Func<T, bool>> AddIsDeleted<T>(bool isDeleted = false)
```
**Muc dich** - Tao san mot predicate loc theo truong `IsDeleted` cho kieu `T` bat ky, dung ket hop voi `And<T>` de ghep vao filter chinh (`PrecateBuilderExtensions.cs:68-74`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `isDeleted` | `bool` | Khong | Khong validate, nhan moi gia tri `bool` | `false` |

**Output** - `Expression<Func<T,bool>>` tuong duong `x => x.IsDeleted == isDeleted`. Khong bao gio `null`.

**Dieu kien xu ly** - (1) `Expression.Parameter(typeof(T), "x")`; (2) `Expression.Property(param, "IsDeleted")` — **truy cap property bang ten chuoi hardcode `"IsDeleted"`**, khong dung `nameof(...)`; (3) `Expression.Equal(property, Expression.Constant(isDeleted))`; (4) boc thanh `Expression.Lambda<Func<T,bool>>`.

**Side effect** - Khong co.

**Error handling** - Khong co try/catch. Neu kieu `T` **khong co property cong khai ten `"IsDeleted"`**, `Expression.Property(param, "IsDeleted")` nem **`ArgumentException`** ngay khi goi ham (truoc khi cham DB) — day la loi runtime, khong phai loi bien dich, vi ten property la chuoi.

**Khi nao NEN dung** - Voi cac kieu `T` co property `bool IsDeleted` (vi du cac entity ke thua `BaseEntityMongoDB`), can loc theo trang thai xoa mem.

**Khi nao KHONG dung** - Voi kieu `T` khong co property `IsDeleted` — se nem exception ngay.

**Gioi han** - Ten property `"IsDeleted"` hardcode dang chuoi (dong 71) — doi ten property nay o entity se lam ham hong ma compiler khong the phat hien truoc; day la mot gioi han da duoc cac file KB CoreMongoDB/CoreSQL da co ghi nhan giong nhau (xem muc 3, phan doi chieu).

### 2.8 QueryContext&lt;TTable, TDto&gt; (record)

**Signature**
```csharp
public record QueryContext<TTable, TDto>(
    FilterDefinition<TTable> Predicate,
    SortDefinition<TTable> Sorting = null,
    Expression<Func<TTable, TDto>> Selector = null) where TDto : class
                                                    where TTable : class;
```
**Muc dich** - Goi 3 thanh phan can thiet cho mot truy van MongoDB co the paging/sort/project (`ProjectToExtensions.cs:10-14`), duoc cac ham `FindAllPagingAsync<TDto>` v.v. trong `CoreMongoDB` nhan lam tham so (xem `Data-MongoDB-CoreMongoDB.md` muc lien quan).

**Input hop le** - `Predicate` bat buoc (khong co gia tri mac dinh trong signature record — nhung day la positional record parameter, C# khong ep validate `null` tai noi khoi tao). `Sorting` va `Selector` co gia tri mac dinh `null`.

**Output** - Day la kieu du lieu (record), khong co "output" theo nghia ham; hai record voi cung gia tri 3 thuoc tinh duoc coi la bang nhau (hanh vi record chuan cua C#).

**Dieu kien xu ly** - Khong co logic; day la auto-generated constructor/properties cua `record`.

**Side effect** - Khong co.

**Error handling** - Khong co; record khong tu validate.

**Khi nao NEN dung** - Khi goi cac ham cua `CoreMongoDB` can gop filter + sort + selector thanh mot tham so duy nhat.

**Khi nao KHONG dung** - Khong co han che dac biet, nhung caller can tu dam bao `Predicate` khac `null` neu ham nhan `QueryContext` khong tu guard (xem file KB `Data-MongoDB-CoreMongoDB.md`, phat hien #20 — khong thuoc pham vi file nay).

**Gioi han** - `Sorting` va `Selector` co the la `null`; ban than record khong xac dinh hanh vi khi `null` — hanh vi thuc te tuy vao ham tieu thu `QueryContext` (ngoai pham vi file nay).

### 2.9 ProjectTo&lt;TEntity, TDto&gt;(this TEntity entity)

**Signature**
```csharp
public static TDto ProjectTo<TEntity, TDto>(this TEntity entity)
```
**Muc dich** - Chuyen doi mot doi tuong tu kieu `TEntity` sang kieu `TDto` bang cach doc gia tri cac property cung ten qua reflection va gan vao doi tuong `TDto` moi tao (`ProjectToExtensions.cs:27-63`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entity` (this) | `TEntity` | Co (nhan `null`) | Khong validate kieu; `TDto` **phai co constructor cong khai khong tham so** de `Activator.CreateInstance` thanh cong | Khong co |

**Output** - `TDto`. Neu `entity` khac `null`: instance `TDto` moi voi cac property da duoc gan gia tri tu `entity` (property khong khop dieu kien se giu gia tri mac dinh cua `TDto`). Neu `entity` la `null`: **tra ve mot instance `TDto` rong (khong phai `null`)** vi `Activator.CreateInstance` duoc goi truoc buoc kiem tra `entity is null` (dong 29-31).

**Dieu kien xu ly** - (1) `object dto = Activator.CreateInstance(typeof(TDto))` (dong 29 — chay **truoc ca khi biet `entity` co null hay khong**); (2) neu `entity is null` tra `dto` ngay; (3) lay `entityType.GetProperties()`; (4) voi moi property cua `TEntity`, tim property cung ten tren `TDto` bang `BindingFlags.Public | Instance | FlattenHierarchy` (dong 43-45); (5) bo qua neu `dtoProp is null`, hoac `dtoProp.CanWrite is false`, hoac `dtoProp` **hoac** `entityProp` co gan `[NoMap]` (dong 47-52); (6) neu qua duoc tat ca dieu kien, goi `dtoProp.SetValue(dto, entityProp.GetValue(entity))`.

**Side effect** - Khong ghi DB. Neu `SetValue` nem loi (vi du **kieu property khong khop** giua `TEntity` va `TDto` du trung ten), loi duoc bat va ghi ra **Console** qua `CommonBaseConstant.ConfigLoggerExceptionByConsole(nameof(ProjectToExtensions), nameof(ProjectTo), exception, guidId)` (dong 58), **khong nem ra ngoai, khong dung qua `ILogger`**.

**Error handling** - `try/catch` boc quanh **tung property** (dong 41-59); loi cua mot property khong lam hong cac property khac, nhung cung khong duoc bao ve len caller — property loi giu gia tri mac dinh cua kieu (`null`/`0`/...). Rieng `Activator.CreateInstance(typeof(TDto))` (dong 29) nam **ngoai** khoi `try/catch` nay: neu `TDto` khong co constructor cong khai khong tham so, ham **nem `MissingMethodException` thang ra caller**.

**Khi nao NEN dung** - Anh xa nhanh entity sang DTO khi hai kieu co nhieu property cung ten va khong can hieu nang toi da (moi lan goi deu chay lai reflection, khong cache).

**Khi nao KHONG dung** - Khi can bao dam moi property duoc anh xa dung (khong swallow loi), hoac khi `TEntity`/`TDto` co property cung ten nhung **khac kieu** — truong hop nay se nem exception noi bo bi nuot va property se **khong duoc gan**, khong co canh bao nao cho caller.

**Gioi han** - (1) `Activator.CreateInstance` chay truoc kiem tra null — hanh vi tra "DTO rong" khi input `null` co the gay nham lan voi "khong tim thay du lieu"; (2) reflection khong cache metadata property — chi phi CPU tuyen tinh theo so property x so lan goi; (3) khong kiem tra kieu truoc `SetValue` — sai kieu se bi nuot am tham; (4) log loi chi ra Console, khong vao he thong log tap trung.

### 2.10 ProjectTo&lt;TEntity, TDto&gt;(this List&lt;TEntity&gt; entities)

**Signature**
```csharp
public static List<TDto> ProjectTo<TEntity, TDto>(this List<TEntity> entities)
```
**Muc dich** - Ban danh sach cua `ProjectTo` don: anh xa tung phan tu cua `entities` sang `TDto` va tra ve danh sach ket qua (`ProjectToExtensions.cs:76-127`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entities` (this) | `List<TEntity>` | Co | **Khong guard `null`** — `foreach (TEntity entity in entities)` (dong 88) se nem `NullReferenceException` neu `entities` la `null` | Khong co |

**Output** - `List<TDto>`. Danh sach rong `[]` neu `entities` rong hoac neu **moi** phan tu deu gap loi trong qua trinh tao/anh xa. Kich thuoc danh sach ket qua co the **nho hon** `entities.Count` neu co phan tu bi loai (xem Error handling).

**Dieu kien xu ly** - Lay `entityType.GetProperties()` **mot lan** truoc vong lap (dong 86, khac ban don — day la mot khac biet ve hieu nang). Voi moi `entity` trong `entities`: tao `dto` moi qua `Activator.CreateInstance<TDto>()` (dong 92), roi lap qua `entityProps` va ap dieu kien bo qua giong ban don (`!CanWrite`, `[NoMap]` tren ca hai phia) truoc khi `SetValue`.

**Side effect** - Khong ghi DB. Loi tung property hoac loi tao `dto` cho tung phan tu deu duoc ghi ra **Console** qua `ConfigLoggerExceptionByConsole`.

**Error handling** - Hai lop `try/catch` long nhau: lop **trong** (dong 96-115) bat loi cua **tung property**, phan tu `dto` van duoc `Add` vao `dtos` voi property loi giu gia tri mac dinh; lop **ngoai** (dong 90-123) bat loi cua **ca buoc tao `dto`** (`Activator.CreateInstance<TDto>()` tai dong 92 nam **trong** khoi `try` nay, khac voi ban don noi `Activator.CreateInstance` nam **ngoai** `try`) — neu buoc nay loi (vi du `TDto` khong co constructor khong tham so), **toan bo phan tu do bi bo qua, khong duoc them vao `dtos`**, va khong co exception nao thoat ra caller.

**Khi nao NEN dung** - Anh xa danh sach entity sang danh sach DTO khi chap nhan chi phi reflection va chap nhan rui ro mat phan tu am tham khi loi.

**Khi nao KHONG dung** - Khi can phan biet ro "danh sach rong vi khong co du lieu" voi "danh sach rong/thieu vi loi anh xa" — ca hai truong hop deu tra cung dang ket qua ma khong co dau hieu loi ro rang cho caller.

**Gioi han** - (1) Bat doi xung ro rang so voi ban don: loi tao instance dich duoc **nuot** o ban list nhung **nem ra** o ban don (xem muc 3); (2) reflection chay cho tung phan tu x tung property, khong cache; (3) log chi ra Console.

### 2.11 MapUsingExpression&lt;TFrom, TTo&gt;(source)

**Signature**
```csharp
public static TTo MapUsingExpression<TFrom, TTo>(TFrom source)
    where TTo : class
    where TFrom : class
```
**Muc dich** - Anh xa `source` sang mot doi tuong `TTo` moi bang cach **xay va compile mot expression tree** `MemberInit` gan tung property cung ten/cung kieu, thay vi dung `PropertyInfo.SetValue` truc tiep nhu `ProjectTo` (`ProjectToExtensions.cs:138-188`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `source` | `TFrom` (class) | Co | Khong guard `null` — `lambda(source)` (dong 187) se chay voi `source = null`, ket qua tuy vao body da compile (doc property tren tham so `null` se nem `NullReferenceException` luc thuc thi lambda neu co it nhat mot binding) | Khong co |

**Output** - `TTo`: instance moi tao boi `Expression.MemberInit(Expression.New(typeof(TTo)), bindings)` da compile va thuc thi. Luon la instance **moi**, khong bao gio `null` (tru khi lambda nem exception).

**Dieu kien xu ly** - (1) Tao `ParameterExpression parameter` kieu `TFrom` (dong 144); (2) voi moi property cong khai cua `TTo`, tim property cung ten tren `TFrom`; **bo qua** neu khong tim thay, hoac `sourceProp.CanWrite is false`, hoac **kieu khac nhau** (`sourceProp.PropertyType != targetProp.PropertyType`, dong 157-163) — day la diem **khac ProjectTo**: co kiem tra kieu truoc khi bind; (3) neu qua dieu kien, tao `MemberAssignment` bang `Expression.Bind(targetProp, sourcePropAccess)`; (4) gop tat ca binding hop le (loai `null`) thanh `MemberInitExpression`; (5) `Expression.Lambda<Func<TFrom,TTo>>(body, parameter).Compile()` roi goi ngay voi `source`.

**Side effect** - Khong ghi DB. Loi trong qua trinh xay binding cho tung property duoc ghi ra **Console** (xem Error handling); khong co side effect khac.

**Error handling** - `try/catch` (dong 150-177) chi boc quanh **buoc xay `MemberAssignment` cho tung property dich** — loi (vi du truy cap property khong hop le) duoc bat, ghi Console, va property do **khong co binding** (bi loai khoi danh sach, khong nem ra). Buoc **compile** (`.Compile()`) va **goi lambda** (`lambda(source)`) nam **ngoai** try/catch nay — neu compile hoac thuc thi loi (vi du `TTo` khong co constructor khong tham so khien `Expression.New` loi luc xay expression, hoac `source` gay `NullReferenceException` khi doc property), exception **nem thang ra caller**.

**Khi nao NEN dung** - Can anh xa entity sang entity/DTO **nhieu lan lap lai voi cung cap TFrom/TTo** va muon uu tien tinh an toan ve kieu (bo qua khi kieu khac nhau thay vi nem exception nhu `ProjectTo`).

**Khi nao KHONG dung** - Trong vong lap lon: ham **compile lai expression tree moi lan goi** (khong cache `Func` da compile), nen chi phi CPU cao hon ProjectTo o quy mo lon neu goi lien tuc cho tung phan tu.

**Gioi han** - (1) Khong cache `Compile()` — diem nghen hieu nang ro rang khi goi trong vong lap; (2) chi bind khi property nguon `CanWrite` (dong 158) du chi **doc** gia tri nguon (khong ghi vao `source`) — dieu kien nay loai bo ca cac property chi-doc (`{ get; }`) cua `TFrom` ma khong co ly do ro rang tu source code; (3) khong co attribute `[NoMap]` nao duoc ap dung cho ham nay (khac `ProjectTo`).

### 2.12 ReplaceParameter&lt;TFrom, TTo&gt;(this Expression&lt;Func&lt;TFrom, bool&gt;&gt; target)

**Signature**
```csharp
public static Expression<Func<TTo, bool>> ReplaceParameter<TFrom, TTo>(this Expression<Func<TFrom, bool>> target)
```
**Muc dich** - Doi kieu tham so cua mot predicate tu `TFrom` sang `TTo`, cho phep tai su dung dieu kien loc viet cho `TFrom` tren kieu `TTo` co cung ten property (`ProjectToExtensions.cs:198-208`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `target` (this) | `Expression<Func<TFrom,bool>>` | Co (nhan `null`) | Neu `null`, tra predicate luon dung, khong nem loi | Khong co |

**Output** - `Expression<Func<TTo,bool>>`. Neu `target` la `null`, hoac ket qua `Visit` la `null`, tra ve `x => true` (predicate luon dung). Nguoc lai tra ve predicate da doi tham so sang `TTo`.

**Dieu kien xu ly** - (1) `if (target is null) return x => true;`; (2) `new WhereReplacerVisitor<TFrom,TTo>().Visit(target)` roi ep kieu ve `Expression<Func<TTo,bool>>`; (3) neu ket qua `null`, tra `x => true`; (4) nguoc lai tra ket qua.

**Side effect** - Khong co.

**Error handling** - Khong co try/catch. Neu `Visit` tra ve mot bieu thuc khong the ep kieu `(Expression<Func<TTo, bool>>)`, se nem `InvalidCastException`. Neu bieu thuc trong `target` truy cap mot property khong ton tai tren `TTo` (qua `WhereReplacerVisitor.VisitMember`, xem muc 2.19), se nem `ArgumentException` tu `Expression.PropertyOrField`.

**Khi nao NEN dung** - Khi da co predicate viet san cho `TFrom` va can ap dung dieu kien tuong tu (theo ten property) cho truy van tren `TTo`. **Da kiem tra lai bang grep toan repo**: overload **don** (`ReplaceParameter`, khong `s`) nay **khong co noi goi nao** trong bat ky file `.cs` nao cua repo (chi ton tai dinh nghia); noi thuong duoc goi thuc te trong `CoreSQLTenant.cs` (vi du dong 324, 366, 404,...) la overload **mang** `ReplaceParameters<TFrom,TTo>()` (muc 2.17, khac ham), **khong phai** ham don nay — xem lai muc 3, phat hien #1 (da bo sung).

**Khi nao KHONG dung** - Voi predicate co truy cap thanh vien long nhau nhieu cap (`x => x.Child.Code == "A"`) — hanh vi dich khong duoc kiem chung day du tu source code nay (xem muc 3). Ngoai ra, ham don nay (khong co hau to `s`) hien khong co bang chung su dung thuc te nao trong repo hien tai.

**Gioi han** - Phu thuoc hoan toan vao `WhereReplacerVisitor` (muc 2.19); khong co co che bao loi ro rang ngoai exception tu `Expression.PropertyOrField`. Rieng ham nay (overload don, khac `ReplaceParameters` mang o muc 2.17) khong tim thay noi goi nao trong repo — xem muc 3, phat hien #1.

### 2.13 ConvertTo&lt;TTableFrom, TTableTo&gt;(source)

**Signature**
```csharp
public static TTableTo ConvertTo<TTableFrom, TTableTo>(TTableFrom source)
```
**Muc dich** - Sao chep gia tri cac property **cung ten va cung kieu** tu `source` sang mot instance `TTableTo` moi tao, bang reflection thuan (khong dung expression tree) (`ProjectToExtensions.cs:218-239`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `source` | `TTableFrom` | Co | Khong guard `null`; `typeof(TTableFrom).GetProperties()` van chay duoc, nhung `sourceProperty.GetValue(source)` voi `source = null` nem `TargetException` | Khong co |

**Output** - `TTableTo` moi tao boi `Activator.CreateInstance<TTableTo>()`, cac property cung ten/cung kieu duoc gan gia tri tu `source`; property khong khop giu gia tri mac dinh.

**Dieu kien xu ly** - Voi moi property cua `TTableFrom`: tim property cung ten tren `TTableTo`; **bo qua** neu khong tim thay, hoac `!CanWrite`; neu tim thay **va** `targetProperty.PropertyType == sourceProperty.PropertyType`, goi `SetValue`.

**Side effect** - Khong co (khong ghi DB, khong log).

**Error handling** - **Khong co try/catch nao trong ham nay** (khac han `ProjectTo`/`MapUsingExpression`). Neu `Activator.CreateInstance<TTableTo>()` loi (khong co constructor khong tham so) hoac `SetValue`/`GetValue` loi, exception **nem thang ra caller**, khong duoc log.

**Khi nao NEN dung** - Can sao chep don gian giua hai kieu co cung ten/cung kieu property va muon loi hien ra ngay (khong bi nuot am tham).

**Khi nao KHONG dung** - Khi can attribute `[NoMap]` duoc ton trong — ham nay **khong kiem tra** `NoMapAttribute`/`NoMapUpdateDefinitionAttribute` (khac voi `ProjectTo`), moi property cung ten/cung kieu deu duoc copy. **Da kiem tra lai bang grep toan repo**: ca hai overload `ConvertTo` (don va `IEnumerable`, muc 2.13-2.14) **khong co noi goi nao** trong bat ky file `.cs` khac cua repo nay (chi ton tai dinh nghia va lenh goi noi bo giua hai overload) — xem muc 3, phat hien #1 (da bo sung).

**Gioi han** - Khong log loi, khong co co che bo qua field theo attribute; chi phi reflection tuyen tinh theo so property, khong cache. Khong tim thay noi goi ham nay (ca hai overload) trong repo hien tai.

### 2.14 ConvertTo&lt;TTableFrom, TTableTo&gt;(IEnumerable&lt;TTableFrom&gt; sources)

**Signature**
```csharp
public static IEnumerable<TTableTo> ConvertTo<TTableFrom, TTableTo>(IEnumerable<TTableFrom> sources)
```
**Muc dich** - Ban danh sach cua `ConvertTo` don, ap dung cho tung phan tu cua `sources` (`ProjectToExtensions.cs:249-266`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `sources` | `IEnumerable<TTableFrom>` | Co (nhan `null`) | `if (sources is null || !sources.Any()) return [];` — **duyet `IEnumerable` hai lan** (`Any()` roi `foreach`) | Khong co |

**Output** - `IEnumerable<TTableTo>` (thuc chat la `List<TTableTo>`). Danh sach rong `[]` neu `sources` la `null`/rong; nguoc lai danh sach ket qua tuong ung tung phan tu qua `ConvertTo` don.

**Dieu kien xu ly** - Guard `sources is null || !sources.Any()`; sau do `foreach` tung `source`, goi `ConvertTo<TTableFrom,TTableTo>(source)`, chi `Add` vao `result` khi `data is not null` (dong 259-262).

**Side effect** - Khong co.

**Error handling** - Khong co try/catch trong ham nay; loi tu `ConvertTo` don (xem muc 2.13) **nem thang ra**, huy vong lap giua duong (cac phan tu da xu ly truoc do bi mat, khong duoc tra ve).

**Khi nao NEN dung** - Chuyen doi danh sach entity giua hai kieu cung cau truc property, chap nhan dung lai toan bo khi co mot phan tu loi.

**Khi nao KHONG dung** - Voi `sources` la `IEnumerable` "lazy" chi duyet duoc mot lan (vi du ket qua truc tiep tu mot truy van chua materialize) — ham duyet **hai lan** (`Any()` va `foreach`) nen co the cho ket qua sai hoac loi tuy nguon du lieu.

**Gioi han** - `ConvertTo` don khong bao gio tra `null` (luon `Activator.CreateInstance` thanh cong hoac nem exception) nen dieu kien `data is not null` (dong 260) **thuc te khong bao gio `false`** — nhanh nay la du thua/khong co tac dung thuc te tu source code hien tai.

### 2.15 MapUpdateDefinition&lt;TTableTo&gt;(request)

**Signature**
```csharp
public static UpdateDefinition<TTableTo> MapUpdateDefinition<TTableTo>(TTableTo request)
```
**Muc dich** - Sinh mot `UpdateDefinition<TTableTo>` dang chuoi cac stage `$set`, moi stage tuong ung mot property cua `request` **khac `null`** va khong bi danh dau `[NoMapUpdateDefinition]` (`ProjectToExtensions.cs:276-311`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `request` | `TTableTo` | Co (nhan `null`) | Khong guard `null` o dau ham; neu `request` la `null`, moi `property.GetValue(request)` (dong 291) tra `null` (vi `GetValue(null)` tren instance property nem exception that ra — xem Error handling) | Khong co |

**Output** - `UpdateDefinition<TTableTo>`. Tra ve **`null`** neu khong co property nao duoc dua vao `$set` (`entitys.Count <= 0`, dong 307). Nguoc lai tra ve ket qua `updateBuilder.Combine(entitys)` — mot `UpdateDefinition` gop nhieu stage `Set`.

**Dieu kien xu ly** - Voi moi `property` trong `typeof(TTableTo).GetProperties()`: (1) bo qua neu `!property.CanRead` (dong 288); (2) doc `value = typeof(TTableTo).GetProperty(property.Name)?.GetValue(request)` (dong 291 — **lay lai property bang ten**, thuc chat tuong duong `property.GetValue(request)`); (3) bo qua neu `value is null` (dong 293-296); (4) bo qua neu property co `[NoMapUpdateDefinitionAttribute]` (dong 298-301); (5) neu qua het, `entitys.Add(updateBuilder.Set(property.Name, value))`. Cuoi cung, neu `entitys` rong tra `null`, nguoc lai `Combine(entitys)`.

**Side effect** - Khong ghi DB (chi **xay** `UpdateDefinition`, khong thuc thi lenh update). Khong mutate `request`.

**Error handling** - **Khong co try/catch**. Neu `request` la `null`: `property.GetValue(request)` tren mot instance property voi target `null` nem **`TargetException`** ("Non-static method requires a target") — nem thang ra caller, khong log.

**Khi nao NEN dung** - Tao `UpdateDefinition` "$set moi truong khac null" cho mot object cap nhat (thuong la entity da duoc gan gia tri moi tu request nghiep vu), dung lam tham so cho `UpdateOneAsync`/`UpdateManyAsync` cua MongoDB Driver.

**Khi nao KHONG dung** - Khi `TTableTo` co property **value type khong nullable** (vi du `bool`, `int`, `DateTime` khong `?`) ma muon "khong cap nhat" gia tri do khi chua duoc set — ham nay **khong the phan biet** "gia tri mac dinh cua kieu" voi "chua duoc gan", nen cac property nay **luon** duoc dua vao `$set` (tru khi co `[NoMapUpdateDefinition]`).

**Gioi han** - (1) Chi bo qua khi `value is null` — property value-type khong nullable (nhu `bool IsDeleted`) **luon nam trong** `$set`, ke ca khi caller khong co y dinh thay doi truong do; day la rui ro ghi de/"hoi sinh" du lieu da xoa mem da duoc ghi nhan trong `Data-MongoDB-CoreMongoDB.md` (phat hien #4) va duoc xac nhan dung voi source code tai day; (2) tra `null` khi khong co gi de `$set` — caller phai tu kiem tra `null` truoc khi dung; (3) khong log gi khi tra `null`.

### 2.16 MapUpdateDefinition&lt;TTableTo&gt;(IEnumerable&lt;TTableTo&gt; request)

**Signature**
```csharp
public static IEnumerable<UpdateDefinition<TTableTo>> MapUpdateDefinition<TTableTo>(IEnumerable<TTableTo> request)
```
**Muc dich** - Ban danh sach cua `MapUpdateDefinition` don, ap dung cho tung phan tu cua `request` (`ProjectToExtensions.cs:320-337`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `request` | `IEnumerable<TTableTo>` | Co (nhan `null`) | `if (request is null \|\| !request.Any()) return [];` — duyet hai lan (`Any()` roi `foreach`) | Khong co |

**Output** - `IEnumerable<UpdateDefinition<TTableTo>>` (thuc chat `List<...>`). Danh sach rong `[]` neu `request` null/rong hoac neu **moi** phan tu deu cho `MapUpdateDefinition` don tra `null`.

**Dieu kien xu ly** - Guard null/empty; `foreach` tung `item`, goi `MapUpdateDefinition(item)`, chi `Add` vao `result` khi `data is not null` (dong 330-333) — **phan tu co `UpdateDefinition` la `null` bi bo qua im lang, khong log**.

**Side effect** - Khong co.

**Error handling** - Khong co try/catch trong ham nay; loi tu `MapUpdateDefinition` don (vi du `item` la `null`, xem muc 2.15) **nem thang ra**, huy vong lap.

**Khi nao NEN dung** - Sinh danh sach `UpdateDefinition` cho thao tac bulk-write nhieu doi tuong cung kieu.

**Khi nao KHONG dung** - Khi cac phan tu cua `request` co the la `null`, hoac khi can biet chinh xac phan tu nao bi bo qua vi khong co truong nao khac `null` — ham nay khong cung cap thong tin do. **Da kiem tra lai bang grep toan repo**: ca 3 noi goi `MapUpdateDefinition` trong `CoreMongoDB.cs` (dong 622, 863, 940) deu truyen mot doi tuong don (`entity`), tuc goi overload don (muc 2.15) — overload `IEnumerable` nay **khong co noi goi nao** trong repo hien tai, xem muc 3, phat hien #1 (da bo sung).

**Gioi han** - Bo qua im lang cac `UpdateDefinition` rong; khong log; ke thua toan bo gioi han cua `MapUpdateDefinition` don (muc 2.15). Khong tim thay noi goi overload nay trong repo.

### 2.17 ReplaceParameters&lt;TFrom, TTo&gt;(this Expression&lt;Func&lt;TFrom, bool&gt;&gt;[] targets)

**Signature**
```csharp
public static Expression<Func<TTo, bool>>[] ReplaceParameters<TFrom, TTo>(this Expression<Func<TFrom, bool>>[] targets)
```
**Muc dich** - Ap dung `ReplaceParameter` (muc 2.12) cho tung phan tu cua mang `targets`, tra ve mang predicate da doi kieu tham so sang `TTo` (`ProjectToExtensions.cs:347-364`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `targets` (this) | `Expression<Func<TFrom,bool>>[]` | Co (nhan `null`) | `if (targets is null \|\| targets.Length <= 0) return [x => true];` | Khong co |

**Output** - `Expression<Func<TTo,bool>>[]`. Mang chi co mot predicate `x => true` neu `targets` null/rong. Nguoc lai, mang cac predicate da doi kieu, **bo qua** phan tu ma `WhereReplacerVisitor.Visit` tra `null` (dong 358: `if (item is null) continue;`).

**Dieu kien xu ly** - Guard null/rong; `foreach` tung `target` trong `targets`, tao `new WhereReplacerVisitor<TFrom,TTo>().Visit(target)` roi ep kieu; bo qua neu `null`; nguoc lai `Add` vao `result`; tra `[.. result]`.

**Side effect** - Khong co.

**Error handling** - Khong co try/catch quanh `Visit`; loi tu `WhereReplacerVisitor` (vi du `Expression.PropertyOrField` khong tim thay thanh vien tren `TTo`) **nem thang ra caller**, huy vong lap (khong giong `ReplaceParameter` don — ham don co fallback `x => true` khi ket qua `null`, con ham nay chi `continue` khi ket qua `null`, khong co fallback tuong tu cho loi nem ra).

**Khi nao NEN dung** - Doi kieu tham so cho **nhieu** predicate cung luc (vi du danh sach dieu kien loc dong).

**Khi nao KHONG dung** - Khi bat ky phan tu trong `targets` co truy cap thanh vien khong ton tai tren `TTo` — se nem exception giua vong lap thay vi bo qua phan tu do.

**Gioi han** - Moi `WhereReplacerVisitor` moi duoc tao **cho tung phan tu** (dong 355) thay vi dung chung mot instance — khong sai ve logic (vi `_parameter` la field instance rieng cho tung ParameterExpression cua tung predicate), nhung the hien khong toi uu bo nho/allocation.

### 2.18 CombineExpressions&lt;TTo&gt;(expressions)

**Signature**
```csharp
public static Expression<Func<TTo, bool>> CombineExpressions<TTo>(Expression<Func<TTo, bool>>[] expressions)
```
**Muc dich** - Gop mot mang predicate **cung kieu `TTo`** thanh mot predicate duy nhat bang AND, dung `Expression.Invoke` (goi lai cac lambda con) thay vi rebind tham so nhu `Compose` (`ProjectToExtensions.cs:373-390`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `expressions` | `Expression<Func<TTo,bool>>[]` | Co (nhan `null`) | `if (expressions == null \|\| expressions.Length <= 0) return x => true;` | Khong co |

**Output** - `Expression<Func<TTo,bool>>`. Neu `expressions` null/rong: `x => true`. Neu co it nhat 1 phan tu: predicate voi body la `expressions[0].Body AndAlso Invoke(expressions[1], p) AndAlso Invoke(expressions[2], p) ...` voi `p` la tham so moi tao rieng cho ham nay (dong 378).

**Dieu kien xu ly** - `combined = expressions[0].Body` (dong 381 — **giu nguyen than tham so cua `expressions[0]`**, khong doi ve `parameter` moi); voi moi `expression` con lai (`Skip(1)`), tao `Expression.Invoke(expression, parameter)` (goi lambda do voi tham so moi) roi `AndAlso` vao `combined`; cuoi cung boc `combined` (voi tham so goc cua `expressions[0]`, **khong phai** `parameter` moi tao) vao `Expression.Lambda<Func<TTo,bool>>(combined, parameter)`.

**Side effect** - Khong co.

**Error handling** - Khong co try/catch. Neu `expressions[0]` la `null`, `expressions[0].Body` (dong 381) nem `NullReferenceException`. Ham khong kiem tra tham so lambda cua `expressions[0]` co trung voi `parameter` moi khong.

**Khi nao NEN dung** - Gop mot mang predicate da co san (cung kieu `TTo`) thanh mot dieu kien AND duy nhat, khi cac predicate nay se **khong bi tai su dung tham so** (moi predicate ngoai `expressions[0]` duoc goi qua `Invoke`, khong rebind).

**Khi nao KHONG dung** - Can luu y `Expression.Lambda<Func<TTo,bool>>(combined, parameter)` (dong 389) khai bao tham so hinh thuc la `parameter` (bien moi tao dong 378), nhung `combined` khoi tao tu `expressions[0].Body` (dong 381) **dung tham so goc cua `expressions[0]`**, khong phai `parameter`. Day la **hai `ParameterExpression` khac nhau trong cung mot bieu thuc lambda** — MongoDB Driver / LINQ provider co the dich sai hoac nem exception khi build lai truy van tu bieu thuc nay; **khong xac dinh duoc tu source code repo nay hanh vi thuc te khi bieu thuc duoc thuc thi**, vi khong tim thay noi goi `CombineExpressions` trong repo de doi chieu (xem muc 3).

**Gioi han** - (1) Chi ho tro AND (khong co tham so chon OR du comment dong 386 nhac den); (2) rui ro tron tham so nhu da neu tren; (3) khong tim thay noi goi ham nay trong bat ky file `.cs` khac trong repo — chua co bang chung hanh vi thuc te.

### 2.19 WhereReplacerVisitor&lt;TFrom, TTo&gt; (public class, ExpressionVisitor)

**Signature**
```csharp
public class WhereReplacerVisitor<TFrom, TTo> : ExpressionVisitor
```
**Muc dich** - `ExpressionVisitor` noi bo duoc `ReplaceParameter`/`ReplaceParameters` dung de doi tham so lambda tu kieu `TFrom` sang `TTo`, dua tren gia dinh hai kieu co property/field cung ten (`ProjectToExtensions.cs:392-426`).

**Thanh phan** - Field `_parameter` (`ParameterExpression` kieu `TTo`, tao moi cho tung instance visitor). Hai method override: `VisitLambda<T>` va `VisitMember`.

**Dieu kien xu ly** - `VisitLambda<T>` (dong 404-408): thay body bang `Visit(node.Body)` va boc lai voi tham so la `_parameter` (bo qua danh sach tham so goc cua `node`). `VisitMember` (dong 416-425): neu `node.Expression is ParameterExpression` (nghia la truy cap thanh vien **truc tiep** tren tham so lambda, vi du `x.Field`), thay bang `Expression.PropertyOrField(_parameter, node.Member.Name)`; **nguoc lai** (truy cap long nhau nhu `x.Child.Field`, hoac truy cap qua bien khac), goi `base.VisitMember(node)` — tuc de `ExpressionVisitor` mac dinh tu duyet tiep xuong `node.Expression`.

**Side effect** - Khong co (Expression tree immutable, `Visit` tra ve cay moi).

**Error handling** - Khong co try/catch. `Expression.PropertyOrField(_parameter, node.Member.Name)` nem `ArgumentException` neu `TTo` khong co property/field cong khai cung ten.

**Khi nao NEN dung** - Khong duoc thiet ke de dung truc tiep tu ben ngoai; day la thanh phan ha tang cho `ReplaceParameter`/`ReplaceParameters`. Du la `public`, khong tim thay noi nao trong repo tao instance `WhereReplacerVisitor` truc tiep ngoai chinh hai ham noi tren.

**Khi nao KHONG dung** - Voi bieu thuc dung cac toan tu LINQ phuc tap (method call, indexer, closure bien ngoai) — visitor nay chi xu ly rieng truong hop `MemberExpression` truc tiep tren tham so; cac dang khac di qua `base.Visit...` mac dinh cua `ExpressionVisitor`, ket qua **chua duoc kiem chung tu source code nay**.

**Gioi han** - Voi truy cap thanh vien long nhau (`x.Child.Code`), hanh vi ket hop giua `VisitMember` va `base.VisitMember` **co kha nang** dich dung (vi `base.VisitMember` de quy xuong `node.Expression` truoc khi tai tao lai `MemberExpression` ngoai), nhung file nay **khong co unit test di kem** de xac nhan; day la vung xam da duoc mot file KB khac (`Data-SQL-CoreSQL-TwoEntity.md`) ghi nhan la "chua kiem chung", va tai lieu nay dong y voi muc do khong chac chan do — xem muc 3.

### 2.20 SetDataUpdatedDefault&lt;TTable&gt;(entity, audit)

**Signature**
```csharp
public static TTable SetDataUpdatedDefault<TTable>(TTable entity, AuditModel audit = default)
```
**Muc dich** - Dong dau 4 truong "Modified*" (`ModifiedUser`, `ModifiedUserCode`, `ModifiedUserOrganization`, `ModifiedDate`) tren `entity` **chi khi truong do dang `null`**, lay gia tri tu `audit` (co fallback "Anonymous") (`ProjectToExtensions.cs:428-458`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entity` | `TTable` | Co (nhan `null`) | Guard `entity is null` | Khong co |
| `audit` | `AuditModel` | Khong | Guard `audit is null` (ca hai deu lam ham tra ve som) | `default` (= `null`) |

**Output** - `TTable`: **chinh doi tuong `entity` dau vao** (da bi mutate), hoac tra lai nguyen `entity`/`null` neu guard kich hoat.

**Dieu kien xu ly** - `if (entity is null || audit is null) return entity;` (dong 430) — **khac voi `SetDataCreatedDefault` cung ten, ham nay doi hoi `audit` khac `null` moi lam gi**. Sau guard: tinh `userName`/`userCode`/`organization` tu `audit?.CreatorInfo?...` voi fallback hang so `CommonBaseConstant.Anonymous`/`AnonymousCode`/`OrganizationForISC` khi rong/whitespace; lay `typeof(TTable).GetProperties()`; ham noi bo `SetPropertyIfNull(name, value)` chi gan gia tri khi tim thay property cung ten **va** `prop.GetValue(entity) is null`; goi 4 lan cho 4 truong Modified* (dong 452-455), dung ten lay tu `nameof(EntityFullCreatedAndModifiedBase.ModifiedUser)` v.v (khong yeu cau `TTable` thuc su ke thua lop nay).

**Side effect** - **Mutate `entity` dau vao** qua `prop.SetValue(entity, value)` (khong tao ban sao).

**Error handling** - Khong co try/catch. Neu `TTable` khong co cac property tren, `SetPropertyIfNull` don gian `continue` (khong nem loi, vi da `FirstOrDefault` + kiem tra `!= null`).

**Khi nao NEN dung** - Truoc khi ghi mot entity da sua doi xuong DB, de dam bao cac truong audit "Modified*" duoc dien khi entity chua tu dien (property con `null`).

**Khi nao KHONG dung** - Khi khong co `audit` (truyen `null`/bo qua tham so) va van muon cac truong Modified* duoc dong dau bang gia tri fallback — ham nay se **khong lam gi ca** trong truong hop do (khac han hanh vi cua `SetDataCreatedDefault`, xem muc 3).

**Gioi han** - (1) Chi gan khi property **hien dang `null`** — neu entity da co gia tri Modified* tu truoc (vi du doc tu DB roi sua lai), ham se **khong** cap nhat lai gia tri moi cho cac truong do; (2) doi hoi `audit != null` moi chay, bat doi xung voi ban `SetDataCreatedDefault(TTable,...)`.

### 2.21 SetDataUpdatedDefault&lt;TTable&gt;(updateDefinition, audit)

**Signature**
```csharp
public static UpdateDefinition<TTable> SetDataUpdatedDefault<TTable>(UpdateDefinition<TTable> updateDefinition, AuditModel audit = default)
```
**Muc dich** - Them 4 stage `$set` cho `ModifiedUser`/`ModifiedUserCode`/`ModifiedUserOrganization`/`ModifiedDate` vao mot `UpdateDefinition<TTable>` co san (`ProjectToExtensions.cs:460-476`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `updateDefinition` | `UpdateDefinition<TTable>` | Co (nhan `null`) | Guard `updateDefinition is null` | Khong co |
| `audit` | `AuditModel` | Khong | Guard `audit is null` (ca hai lam ham tra ve som) | `default` |

**Output** - `UpdateDefinition<TTable>`: ban `updateDefinition` moi (MongoDB Driver `UpdateDefinition` la immutable, `.Set(...)` tra ve **doi tuong moi**, khong mutate ban goc), da noi them 4 stage `Set`. Neu guard kich hoat, tra lai nguyen `updateDefinition` (co the la `null`).

**Dieu kien xu ly** - `if (updateDefinition is null || audit is null) return updateDefinition;`; tinh `userName/userCode/organization` giong muc 2.20; goi lien tiep `updateDefinition = updateDefinition.Set(nameof(...), value)` 4 lan **luon luon** (khong kiem tra gia tri hien tai, vi day la `UpdateDefinition` — khai niem "gia tri hien tai" khong ap dung).

**Side effect** - Khong mutate tham so dau vao (do tinh immutable cua `UpdateDefinition`), nhung **khong ghi DB** — chi xay dinh nghia, giong cac ham `SetData*Default` khac.

**Error handling** - Khong co try/catch; `.Set(...)` cua MongoDB Driver ve nguyen tac khong nem loi luc xay dinh nghia (loi neu co chi phat sinh luc thuc thi tren DB, ngoai pham vi ham nay).

**Khi nao NEN dung** - Khi cap nhat truc tiep bang `UpdateDefinition` (khong doc entity ra roi ghi lai) va can dam bao stage Modified* luon duoc **ghi de** (khong dieu kien `null`, vi ban chat `$set` tren MongoDB la ghi de).

**Khi nao KHONG dung** - Khi `audit` la `null` va van muon co stage Modified* voi gia tri fallback — se khong co stage nao duoc them (giong 2.20).

**Gioi han** - Khac voi ban `TTable` (2.20 — chi set khi dang `null`), ban `UpdateDefinition` nay **luon ghi de** khi da qua guard `audit != null`; day la khac biet co ban giua "mutate object trong bo nho co dieu kien" va "khai bao lenh `$set` vo dieu kien".

### 2.22 SetDataCreatedDefault&lt;TTable&gt;(entity, audit)

**Signature**
```csharp
public static TTable SetDataCreatedDefault<TTable>(TTable entity, AuditModel audit = default)
```
**Muc dich** - Dong dau `IsDeleted` (ve `false`) va 4 truong "Created*" (`CreatedUser`, `CreatedUserCode`, `CreatedUserOrganization`, `CreatedDate`) tren `entity` khi property dang `null`, tuong tu `SetDataUpdatedDefault(TTable,...)` nhung **chi doi hoi `entity` khac `null`** (`ProjectToExtensions.cs:478-510`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `entity` | `TTable` | Co (nhan `null`) | Guard **chi** `entity is null` (dong 480) | Khong co |
| `audit` | `AuditModel` | Khong | **Khong guard** — dung `audit?.CreatorInfo?...` an toan voi `null`, fallback ve hang so Anonymous/AnonymousCode/OrganizationForISC | `default` |

**Output** - `TTable`: chinh `entity` dau vao (da mutate), hoac `entity` nguyen ban (co the `null`) neu guard kich hoat.

**Dieu kien xu ly** - `if (entity is null) return entity;` (dong 480 — **khong** kiem tra `audit`); tinh `userName/userCode/organization` voi fallback nhu tren; lay `properties`; ham `SetPropertyIfNull` giong muc 2.20; goi `SetPropertyIfNull(nameof(BaseEntityMongoDB.IsDeleted), false)` (dong 502) **truoc tien**, roi 4 lan cho Created* (dong 504-507).

**Side effect** - Mutate `entity` dau vao.

**Error handling** - Khong co try/catch; khong nem loi cho `TTable` thieu property (bi bo qua im lang qua `FirstOrDefault`).

**Khi nao NEN dung** - Truoc khi insert mot entity moi xuong DB, de tu dong dien cac truong tao/soft-delete con thieu, **ke ca khi khong co `audit`** (van dong dau gia tri fallback).

**Khi nao KHONG dung** - Khong co han che dac biet trong pham vi kiem tra cua file nay.

**Gioi han** - `SetPropertyIfNull(nameof(BaseEntityMongoDB.IsDeleted), false)` (dong 502) **khong co tac dung thuc te**: `BaseEntityMongoDB.IsDeleted` la `bool` (khong nullable) voi gia tri khoi tao mac dinh la `false` (`BaseEntityMongoDB.cs:17`), nen `prop.GetValue(entity) is null` **khong bao gio dung** — dieu kien nay la du thua tu source code hien tai (xem muc 3).

### 2.23 SetDataCreatedDefault&lt;TTable&gt;(updateDefinition, audit)

**Signature**
```csharp
public static UpdateDefinition<TTable> SetDataCreatedDefault<TTable>(UpdateDefinition<TTable> updateDefinition, AuditModel audit = default)
```
**Muc dich** - Them stage `$set` cho `IsDeleted = false` va 4 truong Created* vao mot `UpdateDefinition<TTable>`, tuong tu ban `entity` nhung luon them **vo dieu kien** (`ProjectToExtensions.cs:512-529`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `updateDefinition` | `UpdateDefinition<TTable>` | Co (nhan `null`) | Guard **chi** `updateDefinition is null` | Khong co |
| `audit` | `AuditModel` | Khong | Khong guard; dung `?.` an toan | `default` |

**Output** - `UpdateDefinition<TTable>` moi (da noi 5 stage `Set`), hoac `updateDefinition` nguyen ban (co the `null`) neu guard kich hoat.

**Dieu kien xu ly** - `if (updateDefinition is null) return updateDefinition;`; tinh 3 bien fallback; goi lien tiep `.Set(nameof(BaseEntityMongoDB.IsDeleted), false)` roi 4 `.Set(...)` cho Created* — **luon thuc hien du `audit` co `null` hay khong** (khac han ban `SetDataUpdatedDefault(UpdateDefinition,...)` yeu cau `audit != null`).

**Side effect** - Khong mutate tham so dau vao (immutable), khong ghi DB.

**Error handling** - Khong co try/catch.

**Khi nao NEN dung** - Xay `UpdateDefinition` cho thao tac "insert-or-update" (upsert) can dam bao document moi tao co du truong Created*/IsDeleted, bat ke co `audit` hay khong.

**Khi nao KHONG dung** - Khong co han che dac biet. **Da kiem tra lai bang grep toan repo**: overload `UpdateDefinition` cua `SetDataCreatedDefault` nay **khong co noi goi nao** trong bat ky file `.cs` khac cua repo (chi `SetDataCreatedDefault(entity, audit)` va `SetDataUpdatedDefault(updateDefinition, audit)` duoc `CoreMongoDB.cs` goi thuc te; rieng overload nay chua co bang chung su dung) — xem muc 3, phat hien #1 (da bo sung).

**Gioi han** - Bat doi xung voi cap `SetDataUpdatedDefault`: ban Created (ca `entity` va `UpdateDefinition`) khong doi hoi `audit != null` de chay, trong khi ban Updated (ca hai overload) deu doi hoi `audit != null`. Day la mau hinh lap lai xuyen suot 4 ham `SetData*Default`, khong chi rieng cap `entity` da duoc file KB khac ghi nhan (xem muc 3). Rieng overload nay khong tim thay noi goi trong repo hien tai.

### 2.24 NoMapAttribute

**Signature**
```csharp
[AttributeUsage(AttributeTargets.All)]
public class NoMapAttribute : Attribute
```
**Muc dich** - Attribute danh dau (marker, khong co tham so/property) de bao `ProjectTo<TEntity,TDto>` **bo qua** property duoc gan attribute nay — ap dung ca khi gan tren property phia entity nguon hoac phia DTO dich (`ProjectToExtensions.cs:531-534`).

**Pham vi ap dung** - `AttributeUsage(AttributeTargets.All)` cho phep gan tren **bat ky** thanh vien/kieu (class, method, field,...), khong rieng property — nhung logic kiem tra thuc te trong `ProjectTo` chi doc attribute nay tren `PropertyInfo` (qua `GetCustomAttribute<NoMapAttribute>()`), nen gan tren cac thanh phan khac se khong co tac dung gi voi `ProjectTo`.

**Duoc doc boi** - Chi `ProjectTo<TEntity,TDto>` (ca hai overload, muc 2.9-2.10). **Khong** duoc `MapUsingExpression`, `ConvertTo`, hay `MapUpdateDefinition` kiem tra.

**Khi nao NEN dung** - Gan tren property cua entity hoac DTO ma khong muon `ProjectTo` tu dong copy gia tri (vi du property nhay cam, property tinh toan rieng cho tung phia).

**Khi nao KHONG dung** - Khong co tac dung voi cac ham anh xa khac (`MapUsingExpression`, `ConvertTo`) — neu can chan anh xa cho cac ham do, phai tu kiem tra logic khac (khong co san trong file nay).

**Gioi han** - Ten attribute khong the phan biet "khong map chieu doc" vs "khong map chieu ghi" (dung chung mot attribute cho ca property nguon va dich); pham vi `AttributeUsage(AttributeTargets.All)` rong hon nhu cau thuc te (chi dung cho property).

### 2.25 NoMapUpdateDefinitionAttribute

**Signature**
```csharp
[AttributeUsage(AttributeTargets.All)]
public class NoMapUpdateDefinitionAttribute : Attribute
```
**Muc dich** - Attribute danh dau de bao `MapUpdateDefinition<TTableTo>` **bo qua** property duoc gan, khong dua vao `$set` du gia tri property khac `null` (`ProjectToExtensions.cs:536-539`).

**Pham vi ap dung** - Khai bao cho phep gan tren moi loai thanh vien (`AttributeUsage(AttributeTargets.All)`), nhung chi co tac dung thuc te khi gan tren **property cua `TTableTo`**, vi day la doi tuong duoc `MapUpdateDefinition` kiem tra qua `property.GetCustomAttribute<NoMapUpdateDefinitionAttribute>()`.

**Duoc doc boi** - Chi `MapUpdateDefinition<TTableTo>` (ca hai overload, muc 2.15-2.16). Day la attribute **rieng**, khac `NoMapAttribute` — mot property co the mang mot trong hai, ca hai, hoac khong attribute nao, tuy nhu cau ("khong duoc doc bang ProjectTo" khac "khong duoc dua vao lenh cap nhat Mongo").

**Khi nao NEN dung** - Gan tren property **value-type khong nullable** (vi du `bool`, `int`) ma khong muon bi tu dong dua vao `$set` khi object dau vao co gia tri "mac dinh that" (0/false) khong phai chu dich cap nhat — day la cach duy nhat trong file nay de tranh rui ro da neu o muc 2.15.

**Khi nao KHONG dung** - Khong anh huong den `ProjectTo` hoac cac ham doc/anh xa khac — chi rieng `MapUpdateDefinition`.

**Gioi han** - Phai tu ap dung thu cong tren tung property can bao ve; khong co canh bao/validate nao neu quen gan cho mot property value-type nhay cam.

## 3. Van de da biet

> [!IMPORTANT]
> Doi chieu voi 3 file KB da co (`Data-MongoDB-CoreMongoDB.md`, `Data-SQL-CoreSQL.md`, `Data-SQL-CoreSQL-TwoEntity.md`) cho 6 ham trong tam (`And`, `AddIsDeleted`, `SetDataCreatedDefault`, `SetDataUpdatedDefault`, `MapUpdateDefinition`, `ProjectTo`): **khong phat hien cau mo ta nao mau thuan voi source code**. Cac khang dinh sau da duoc kiem chung khop voi source code tai day: (1) `Data-MongoDB-CoreMongoDB.md` mo ta `And<T>` "gop hai predicate bang Expression.AndAlso sau khi rebind parameter" va "NullReferenceException neu filter null" — khop voi `PrecateBuilderExtensions.cs:37-40, 80-92`; (2) mo ta `AddIsDeleted` dung `x => x.IsDeleted == isDeleted` va ten property hardcode — khop voi `PrecateBuilderExtensions.cs:68-74`; (3) mo ta `MapUpdateDefinition` chi bo qua property `null`, property value-type khong nullable (ke ca `IsDeleted`) luon vao `$set` — khop voi `ProjectToExtensions.cs:288-305`; (4) mo ta `SetDataUpdatedDefault` bo qua hoan toan khi `audit == null` con `SetDataCreatedDefault` chi can `entity != null` — khop voi `ProjectToExtensions.cs:430, 480`; (5) mo ta hai overload `ProjectTo` xu ly loi khac nhau (don: `Activator.CreateInstance` ngoai try/catch, nem loi; danh sach: trong try/catch, nuot loi va loai phan tu) — khop voi `ProjectToExtensions.cs:29, 56-59, 90-123`. Khong sua cac file KB cu trong buoc nay.

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `True<T>`, `False<T>`, `Create<T>`, `Or<T>`, `Not<T>`, `CombineExpressions<TTo>` khong co noi goi nao trong cac file `.cs` cua repo nay (grep toan repo, chi tim thay chinh file dinh nghia va cac file build output `.dll`). **Kiem tra doc lap bo sung** (khong co trong ban goc cua tai lieu nay, tu xac minh lai bang grep) cho thay cung tinh trang nay ap dung cho **5 ham/overload khac**: `ReplaceParameter<TFrom,TTo>` (overload don, khac `ReplaceParameters` mang o muc 2.17 — noi goi thuc te trong `CoreSQLTenant.cs` la ban mang, khong phai ban don nay; muc 2.12 cua tai lieu nay truoc day mo ta nham "thuong dung trong CoreSQLTenant" cho ham don, da duoc sua), `ConvertTo<TTableFrom,TTableTo>` (ca hai overload, muc 2.13-2.14), `MapUpdateDefinition<TTableTo>(IEnumerable<TTableTo>)` (muc 2.16 — ca 3 noi goi thuc te trong `CoreMongoDB.cs` deu dung overload don), va `SetDataCreatedDefault<TTable>(UpdateDefinition<TTable>, AuditModel)` (muc 2.23) | `PrecateBuilderExtensions.cs:16-59`; `ProjectToExtensions.cs:198-208, 218-266, 320-337, 373-390, 512-529` | Day la thu vien `FTELSRCore.Shared` co the duoc du an khac tieu thu, nen **khong the ket luan la dead code** chi tu source cua repo nay — chi ghi nhan "khong tim thay noi goi trong repo hien tai", khong suy dien them. Rieng truong hop `ReplaceParameter` don la nghiem trong hon vi ban goc cua tai lieu **da vo tinh gan mac** su dung thuc te cua ham (mang) `ReplaceParameters` sang cho ham don — day la loi mo ta capability thuc te, da duoc sua truc tiep trong muc 2.12 |
| 2 | `And<T>` va `Or<T>` (qua `Compose`) khong guard `null` cho ca `first` va `second`: goi tren `first == null` nem `NullReferenceException` tai buoc doc `first.Parameters` | `PrecateBuilderExtensions.cs:37-49, 80-92` | Moi noi goi `filter.And(...)`/`filter.Or(...)` phai tu dam bao `filter` khac `null` truoc; day cung la nguyen nhan cua canh bao tuong tu da co trong `Data-MongoDB-CoreMongoDB.md` |
| 3 | `Not<T>` khong guard `null` cho `expression`; truy cap `expression.Body` nem `NullReferenceException` ngay | `PrecateBuilderExtensions.cs:56-59` | Goi `Not<T>` tren predicate `null` se nem loi ngay tai dong dau, khong co thong bao ro nghia |
| 4 | `AddIsDeleted<T>` dung ten property `"IsDeleted"` hardcode dang chuoi, khong `nameof(...)` | `PrecateBuilderExtensions.cs:71` | Doi ten property `IsDeleted` cua entity se lam ham nay nem `ArgumentException` luc runtime ma compiler khong canh bao truoc |
| 5 | `ProjectTo` (ban don, muc 2.9) khong kiem tra kieu truoc `SetValue`; ban `MapUsingExpression`/`ConvertTo` co kiem tra kieu (`sourceProp.PropertyType == targetProp.PropertyType`) truoc khi gan | `ProjectToExtensions.cs:54` (khong kiem tra) doi chieu `:159-160` va `:231` (co kiem tra) | Hai co che anh xa trong cung file co tieu chuan an toan kieu khac nhau; dung sai ham cho tinh huong co the gay swallow loi am tham (`ProjectTo`) hoac bo qua property am tham (`MapUsingExpression`/`ConvertTo`) |
| 6 | `ProjectTo` (ban `List<TEntity>`) nuot loi ca khi `Activator.CreateInstance<TDto>()` that bai (nam trong try/catch, dong 90-123); ban don (mot doi tuong) de loi nay thoat ra caller (`Activator.CreateInstance` nam ngoai try/catch, dong 29, 56-59) | `ProjectToExtensions.cs:27-63` doi chieu `76-127` | Cung mot loi cau hinh `TDto` thieu constructor khong tham so tao ra hai hanh vi khac nhau tuy goi ham don hay ham danh sach — kho phat hien khi debug |
| 7 | `ConvertTo` (ca hai overload) khong kiem tra `[NoMapAttribute]`/`[NoMapUpdateDefinitionAttribute]`, khac voi `ProjectTo` | `ProjectToExtensions.cs:218-266` | Property da danh dau `[NoMap]` de chan `ProjectTo` van bi `ConvertTo` copy binh thuong neu dung nham ham; rui ro ro loi ngoai y muon voi truong nhay cam |
| 8 | `ConvertTo(IEnumerable<TTableFrom>)` co dieu kien `if (data is not null)` truoc khi `Add`, nhung `ConvertTo` don khong bao gio tra `null` (luon `Activator.CreateInstance` thanh cong hoac nem exception thang ra) | `ProjectToExtensions.cs:255-262` doi chieu `218-239` | Nhanh kiem tra `null` la dead code trong dieu kien hien tai cua source; khong gay loi nhung gay hieu nham khi doc code |
| 9 | `SetDataCreatedDefault<TTable>(entity, audit)` co dong `SetPropertyIfNull(nameof(BaseEntityMongoDB.IsDeleted), false)`, nhung `IsDeleted` la `bool` khong nullable voi gia tri khoi tao `false`, nen dieu kien `prop.GetValue(entity) is null` khong bao gio dung | `ProjectToExtensions.cs:502`; `BaseEntityMongoDB.cs:17` | Dong code khong co tac dung thuc te trong pham vi kieu hien tai (van dung, chi la thua); day la van de da duoc `Data-MongoDB-CoreMongoDB.md` ghi nhan (phat hien #17), tai lieu nay xac nhan lai dung voi source |
| 10 | Bon ham `SetData*Default` co mau hinh bat doi xung lap lai o **ca hai** overload (entity va `UpdateDefinition`): ban "Created" chi doi hoi `entity`/`updateDefinition` khac `null` (khong can `audit`); ban "Updated" doi hoi ca hai khac `null` (bao gom `audit`) | `ProjectToExtensions.cs:430` (Updated/entity), `462` (Updated/UpdateDefinition) doi chieu `480` (Created/entity), `514` (Created/UpdateDefinition) | Goi cap nhat ma khong truyen `audit` se **khong** dong dau `Modified*` (khong canh bao); goi tao moi ma khong truyen `audit` **van** dong dau gia tri fallback "Anonymous"/"0"/"FTEL". `Data-MongoDB-CoreMongoDB.md` da ghi nhan dieu nay cho cap overload `entity`; tai lieu nay bo sung them cap overload `UpdateDefinition` co cung mau hinh, chua duoc noi ro trong KB cu |
| 11 | `CombineExpressions<TTo>` boc `combined` (duoc xay tu `expressions[0].Body`, dung tham so goc cua `expressions[0]`) vao `Expression.Lambda` voi tham so hinh thuc la mot `ParameterExpression` **moi tao** (`parameter`, dong 378) — hai tham so nay khong phai la mot doi tuong | `ProjectToExtensions.cs:378-389` | Bieu thuc lambda ket qua co the chua tham so "treo" (khong khop giua khai bao lambda va bieu thuc con dau tien) khi `expressions.Length == 1`; hanh vi thuc te luc dich/thuc thi **khong xac dinh duoc tu source code repo nay** vi khong tim thay noi goi ham de doi chieu (xem phat hien #1) |
| 12 | `WhereReplacerVisitor.VisitMember` chi xu ly rieng truong hop truy cap thanh vien **truc tiep** tren tham so lambda (`node.Expression is ParameterExpression`); truy cap long nhau (`x.Child.Code`) di qua nhanh `base.VisitMember` mac dinh | `ProjectToExtensions.cs:416-425` | `Data-SQL-CoreSQL-TwoEntity.md` (dong 137) da ghi "chua kiem chung duoc hanh vi chinh xac tu source code, can test truoc khi dung" cho truong hop nay; tai lieu nay xac nhan lai cung muc do khong chac chan, khong tu ket luan them ve ket qua dich dung/sai vi thieu bang chung runtime |

