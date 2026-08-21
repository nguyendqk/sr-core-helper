# Domain primitives (Aggregate, IDomainEvent, BaseEntity)

> Nguon: `FTELSRCore.Shared/Abstractions/Aggregate.cs`, `FTELSRCore.Shared/Abstractions/IDomainEvent.cs`, `FTELSRCore.Shared/Abstractions/Entities/BaseEntityMongoDB.cs`, `FTELSRCore.Shared/Abstractions/Entities/BaseEntitySQL.cs`
> Loai: interface + abstract class + class (hon hop)
> Cap nhat theo commit: `89c1ce9`

## 1. Tong quan

Bon file nay dinh nghia cac "domain primitive" dung chung cho toan bo cac tang du lieu (Mongo va SQL) trong `FTELSRCore.Shared`: `IAggregate`/`Aggregate` cung cap co che gom domain event tren mot entity, `IDomainEvent` la hop dong toi thieu cho mot domain event (ke thua `MediatR.INotification` de co the publish qua `IPublisher`), `BaseEntityMongoDB` (va 4 lop ke thua) la base entity cho MongoDB, `BaseEntitySQL.cs` la tap interface marker mo ta field audit toi thieu cho entity SQL (khong co class cu the nao ten `BaseEntitySQL`). Cac kieu nay nam o tang thap nhat (Abstractions), duoc `Data/SQL/*` va `Data/MongoDB/*` tieu thu de xac dinh entity nao "co domain event" hoac "co field audit".

**Diem can luu y ngay:** ten file `BaseEntitySQL.cs` de gay nham lan - file nay **khong dinh nghia bat ky class nao ten `BaseEntitySQL`**, ma chi co 4 `interface` (`IEntityCreatedAndModifiedNotHaveAreaBase<T>`, `IEntityFullCreatedAndModifiedBase<T>`, `IBaseEntitySQL`, `IEntityCreatedAndModifiedShortBase<T>`) (`BaseEntitySQL.cs:1-48`). Khac voi phia Mongo (`BaseEntityMongoDB` la class cu the, co field), phia SQL hoan toan la contract - moi entity SQL phai tu khai bao property va tu implement.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| `Aggregate` cho phep mot entity thu thap (`AddDomainEvent`, `AddRangeDomainEvent`) va lay-ra-roi-xoa (`ClearDomainEvents`) danh sach `IDomainEvent` dang cho xu ly (`Aggregate.cs:20-37`) | `Aggregate`/`IAggregate` **khong tu publish** event nao - viec publish do `WriteDbContext.DispatchDomainEvents` (module khac) thuc hien, va **chi voi entity ke thua lop `Aggregate`** cu the, khong phai bat ky the nao implement `IAggregate` (xem muc 3) |
| `IDomainEvent` cung cap gia tri mac dinh cho `EventId`, `OccurredOn`, `EventType` thong qua default interface implementation, cho phep khai bao mot domain event chi bang `class Foo : IDomainEvent { }` ma khong can code them (`IDomainEvent.cs:5-7`) | Cac gia tri mac dinh nay **khong on dinh qua nhieu lan doc** (`EventId`/`OccurredOn` la property tinh toan lai moi lan truy cap, khong co backing field) tru khi lop implement tu override lai bang property luu tru thuc su (xem muc 3) |
| `BaseEntityMongoDB` cap `Id` (tu sinh `ObjectId` moi khi tao doi tuong) va `IsDeleted` (mac dinh `false`) cho moi entity Mongo (`BaseEntityMongoDB.cs:9-18`) | `Id` la `private` - **lop ke thua khong doc/ghi duoc**, va class nay khong co method/logic nao (khong validate, khong tinh toan) |
| 4 lop `EntityXxxBase` ke thua `BaseEntityMongoDB` de cong them cac to hop field audit (`CreatedUser*`, `ModifiedUser*`, co/khong co Region/Location/BranchId) (`BaseEntityMongoDB.cs:25-167`) | Khong co lop nao trong 4 lop nay tu dong **gan gia tri** cho cac field audit - viec gan (`SetDataCreatedDefault`/`SetDataUpdatedDefault`) do `ProjectToExtensions` (module khac) dam nhiem |
| `IBaseEntitySQL` + 3 interface con dinh nghia hop dong toi thieu (Id, IsDeleted, field audit) ma mot entity SQL can co de duoc `WriteDbContext`/`CoreSQL` nhan dien va tu dong dong dau audit (`BaseEntitySQL.cs:3-47`) | Day chi la **interface** - khong co implementation mac dinh nao; tung entity SQL trong tung service phai tu viet toan bo property. Trong repo `sr-core-helper` nay **khong co class nao implement** cac interface nay (chi co dinh nghia, xem muc 3) |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `System.ComponentModel.DataAnnotations.Schema` (`NotMappedAttribute`) | `Aggregate.cs:1,14,17` - danh dau `_domainEvents`/`DomainEvents` de EF Core khong co gang map thanh cot khi entity ke thua `Aggregate` duoc dung trong `DbContext` |
| `MediatR` (goi tu package, khong nam trong 4 file nay) | `IDomainEvent : INotification` (`IDomainEvent.cs:3`) - de mot domain event co the duoc `IPublisher.Publish` (MediatR) nhan dien va gui toi handler; ban than 4 file khong dung `IPublisher` |
| `MongoDB.Bson`, `MongoDB.Bson.Serialization.Attributes` | `BaseEntityMongoDB.cs:1-2` - `BsonId`, `BsonRepresentation`, `BsonElement`, `BsonIgnoreExtraElements` dinh nghia cach entity duoc serialize/deserialize voi MongoDB driver |
| `System.Text.Json.Serialization` (`JsonConverter`) | `BaseEntityMongoDB.cs:3,33,57,89,121,144,159` - gan `DateTimeNullAbleConverter` cho tat ca property `DateTime?` (`CreatedDate`, `ModifiedDate`) khi serialize JSON |
| `FTELSRCore.Helpers.JSonParseHelpers.DateTimeNullAbleConverter` (`using static ...`, `BaseEntityMongoDB.cs:4`) | Converter cu the cho `DateTime?` - **thuoc mot file/module khac**, khong duoc tai lieu hoa o day; chi xac nhan la dependency truc tiep |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `IAggregate` | Interface | Hop dong aggregate root: `DomainEvents`, `ClearDomainEvents()` |
| `Aggregate` | Abstract class | Trien khai `IAggregate`; them `AddDomainEvent`, `AddRangeDomainEvent` |
| `IDomainEvent` | Interface (ke thua `MediatR.INotification`) | Hop dong domain event: `EventId`, `OccurredOn`, `EventType` (deu co gia tri mac dinh) |
| `BaseEntityMongoDB` | Class | `Id` (private) + `IsDeleted` cho entity Mongo |
| `EntityFullCreatedAndModifiedBase` | Abstract class | `BaseEntityMongoDB` + du field `Created*`/`Modified*` (gom Region/Location/BranchId) |
| `EntityFullCreatedBase` | Abstract class | `BaseEntityMongoDB` + du field `Created*` (gom Region/Location/BranchId), khong co `Modified*` |
| `EntityCreatedNotHaveAreaBase` | Abstract class | `BaseEntityMongoDB` + field `Created*` rut gon (khong Region/Location/BranchId) |
| `EntityCreatedAndModifiedNotHaveAreaBase` | Abstract class | `BaseEntityMongoDB` + field `Created*`/`Modified*` rut gon (khong Region/Location/BranchId) |
| `IBaseEntitySQL` | Interface | `IsDeleted` + field `Created*`/`Modified*` rut gon (khong Region/Location/BranchId) cho entity SQL |
| `IEntityCreatedAndModifiedNotHaveAreaBase<T>` | Interface (ke thua `IBaseEntitySQL`) | Them `Id` kieu generic `T` |
| `IEntityFullCreatedAndModifiedBase<T>` | Interface (ke thua `IBaseEntitySQL`) | Them `Id` + `CreatedUserRegionId/LocationId/BranchId` + `ModifiedUserRegionId/LocationId/BranchId` |
| `IEntityCreatedAndModifiedShortBase<T>` | Interface (**khong** ke thua `IBaseEntitySQL`) | `Id` + `IsDeleted` + `CreatedUser`/`CreatedDate` + `ModifiedUser`/`ModifiedDate` (khong co `*Code`/`*Organization`) |

## 2. Chi tiet API

### 2.1 IAggregate / Aggregate.DomainEvents

**Signature**
```csharp
public interface IAggregate
{
    List<IDomainEvent> DomainEvents { get; }
    IDomainEvent[] ClearDomainEvents();
}

[NotMapped]
public List<IDomainEvent> DomainEvents => _domainEvents;
```
**Muc dich** - Expose danh sach domain event dang cho xu ly cua mot aggregate (`Aggregate.cs:7,18`).

**Input hop le** - Khong co tham so (property).

**Output** - `List<IDomainEvent>` - tham chieu **truc tiep** den field noi bo `_domainEvents` (khong phai ban sao). Danh sach rong `[]` ngay sau khi khoi tao (`Aggregate.cs:15`).

**Dieu kien xu ly** - Khong co nhanh re; luon tra ve field hien tai.

**Side effect** - Khong co khi chi doc. Nhung vi day la tham chieu truc tiep (khong phai `IReadOnlyList` hay ban sao), **code ben ngoai co the mutate truc tiep** danh sach nay (vi du `entityWithDomainEventsTo.DomainEvents.AddRange(domainEvents)` tai `CoreSQLTenant.cs:653`) ma khong can di qua `AddDomainEvent`.

**Error handling** - Khong co.

**Khi nao NEN dung** - Doc de kiem tra co domain event dang cho hay khong (`x.DomainEvents.Count > 0`, `WriteDbContext.cs:423`).

**Khi nao KHONG dung** - Khong nen dua ra ngoai bien assembly ma khong kiem soat, vi caller co the `Add`/`Clear` truc tiep tren danh sach tra ve, pha vo tinh dong goi du dinh (chi expose qua `AddDomainEvent`/`ClearDomainEvents`).

**Gioi han** - Khong co gioi han so luong event; khong thread-safe (`List<T>` thong thuong, khong co lock/immutable).

### 2.2 Aggregate.AddDomainEvent(IDomainEvent domainEvent)

**Signature**
```csharp
public void AddDomainEvent(IDomainEvent domainEvent)
```
**Muc dich** - Them mot domain event vao danh sach cho xu ly cua aggregate (`Aggregate.cs:20-23`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `domainEvent` | `IDomainEvent` | Co | **Khong kiem tra `null`** - goi `_domainEvents.Add(domainEvent)` truc tiep | Khong co |

**Output** - `void`.

**Dieu kien xu ly** - Khong co nhanh re; luon `Add` vao `_domainEvents`.

**Side effect** - Mutate state noi bo cua doi tuong `Aggregate` (them phan tu vao `_domainEvents`).

**Error handling** - Khong co `try`/`catch`. Neu `domainEvent` la `null`, `List<T>.Add(null)` **khong nem exception** (danh sach chap nhan `null` nhu mot phan tu hop le) - hau qua la `ClearDomainEvents()` co the tra ve mang chua phan tu `null`.

**Khi nao NEN dung** - Trong logic nghiep vu cua entity ke thua `Aggregate`, ngay sau khi thay doi state can thong bao, de "danh dau" mot domain event se duoc publish khi luu thanh cong.

**Khi nao KHONG dung** - Khong dung de publish ngay lap tuc - event chi thuc su duoc publish khi co code khac (`WriteDbContext.DispatchDomainEvents`) doc va goi `IPublisher`.

**Gioi han** - Khong validate `null`; khong gioi han so luong; khong kiem tra trung lap (co the `Add` cung mot instance nhieu lan).

### 2.3 Aggregate.AddRangeDomainEvent(List\<IDomainEvent\> domainEvents)

**Signature**
```csharp
public void AddRangeDomainEvent(List<IDomainEvent> domainEvents)
```
**Muc dich** - Them nhieu domain event cung luc (`Aggregate.cs:25-28`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `domainEvents` | `List<IDomainEvent>` | Co | **Khong kiem tra `null`** | Khong co |

**Output** - `void`.

**Dieu kien xu ly** - Khong co nhanh re; goi `_domainEvents.AddRange(domainEvents)` truc tiep.

**Side effect** - Mutate `_domainEvents`.

**Error handling** - Khong co guard. Neu `domainEvents` la `null`, `List<T>.AddRange(null)` **nem `ArgumentNullException`** (hanh vi cua BCL, khong duoc bat lai trong ham nay).

**Khi nao NEN dung** - Khi da co san mot `List<IDomainEvent>` tu nguon khac va can gop vao aggregate hien tai - vi du `CoreSQLTenant` chuyen tiep `DomainEvents` tu entity nguon (`TEntityFrom`) sang entity dich (`TEntityTo`) khi ca hai deu la `IAggregate` (`CoreSQLTenant.cs:651-654`, dung truc tiep tren property `DomainEvents` chu khong qua ham nay - xem muc 3).

**Khi nao KHONG dung** - Khong truyen `null` cho `domainEvents`.

**Gioi han** - Khong lam sach trung lap; khong kiem tra `null` truoc khi goi BCL.

### 2.4 Aggregate.ClearDomainEvents()

**Signature**
```csharp
public IDomainEvent[] ClearDomainEvents()
```
**Muc dich** - Lay toan bo domain event hien co (duoi dang mang, tach ban) va xoa sach danh sach noi bo (`Aggregate.cs:30-37`).

**Input hop le** - Khong co tham so.

**Output** - `IDomainEvent[]` - ban sao (`[.. _domainEvents]`) cua danh sach **truoc khi** bi xoa. Neu chua co event nao, tra ve mang rong (khong bao gio `null`).

**Dieu kien xu ly**
1. Sao chep `_domainEvents` sang mang moi `dequeuedEvents` (`Aggregate.cs:32`).
2. Xoa toan bo `_domainEvents` (`Aggregate.cs:34`).
3. Tra ve mang da sao chep (`Aggregate.cs:36`).

**Side effect** - Mutate `_domainEvents` (xoa het phan tu). Day la thao tac "dequeue" mot lan - goi lai lan thu hai se tra ve mang rong.

**Error handling** - Khong co.

**Khi nao NEN dung** - Khi can lay danh sach event de publish **va** dam bao khong publish lai (vi du sau khi luu DB thanh cong).

**Khi nao KHONG dung** - Neu chi can **doc** ma khong muon xoa, dung property `DomainEvents` thay vi ham nay.

**Gioi han** - Khong thread-safe: hai luong goi `ClearDomainEvents()` dong thoi tren cung mot instance co the dan den doc/xoa khong nhat quan (khong co `lock`).

### 2.5 IDomainEvent (EventId, OccurredOn, EventType)

**Signature**
```csharp
public interface IDomainEvent : INotification
{
    Guid EventId => Guid.NewGuid();
    public DateTime OccurredOn => DateTime.Now;
    public string EventType => GetType().AssemblyQualifiedName;
}
```
**Muc dich** - Cung cap gia tri mac dinh (default interface implementation, C# 8+) cho 3 thuoc tinh nhan dien mot domain event, de lop implement co the bo qua khong can khai bao lai (`IDomainEvent.cs:3-8`).

**Input hop le** - Khong co tham so; day la 3 property chi-doc.

**Output**
- `EventId` (`Guid`) - **moi lan truy cap tra ve mot `Guid.NewGuid()` moi**, vi day la expression-bodied property khong co backing field. Khong co gia tri "cua rieng" mot instance domain event tru khi lop cu the tu override bang mot auto-property luu tru thuc su.
- `OccurredOn` (`DateTime`) - `DateTime.Now` (gio **local cua may chay**, khong phai UTC) tai **thoi diem truy cap**, khong phai thoi diem tao doi tuong, tru khi override.
- `EventType` (`string`) - `GetType().AssemblyQualifiedName` cua **doi tuong thuc te** (polymorphic - tra ve ten kieu cu the implement `IDomainEvent`, khong phai `"IDomainEvent"`).

**Dieu kien xu ly** - Khong co nhanh re; moi property tinh truc tiep bieu thuc ben phai `=>` mot lan mai lan goi getter.

**Side effect** - Khong co (khong mutate state, khong ghi log/DB).

**Error handling** - Khong co try/catch. `GetType()` khong bao gio null tren mot instance da khoi tao nen `EventType` khong nem exception trong dieu kien thong thuong.

**Khi nao NEN dung** - Khi mot domain event chi can 3 thuoc tinh nay o muc "cho co", khong quan tam gia tri co on dinh giua cac lan doc hay khong (vi du chi doc `EventType` mot lan de log).

**Khi nao KHONG dung** - **Khong nen dung `EventId`/`OccurredOn` mac dinh nay o bat ky noi nao can gia tri on dinh** (vi du: log lai `EventId` roi sau do so sanh, luu `OccurredOn` vao DB roi doc lai de tinh do tre) - vi hai lan doc lien tiep tren cung mot instance se ra hai gia tri khac nhau (xem muc 3, day la mot rui ro thiet ke duoc suy ra truc tiep tu than ham, khong phai gia dinh).

**Gioi han** - Khong co lop nao trong repo `sr-core-helper` hien tai implement `IDomainEvent` truc tiep de kiem chung co override lai `EventId`/`OccurredOn` hay khong (grep khong ra ket qua) - **khong xac dinh duoc tu source code** trong repo nay lieu cac domain event thuc te (o cac repo API khac tieu thu `FTELSRCore.Shared`) co override 3 property nay hay dung nguyen mac dinh.

### 2.6 BaseEntityMongoDB

**Signature**
```csharp
[BsonIgnoreExtraElements]
public class BaseEntityMongoDB
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    private string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.Boolean)]
    [BsonElement(nameof(IsDeleted))]
    public bool IsDeleted { get; set; } = false;
}
```

| Field | Kieu | Access modifier | Attribute | Gia tri mac dinh | Ghi chu |
|---|---|---|---|---|---|
| `Id` | `string` | `private` | `[BsonId]`, `[BsonRepresentation(BsonType.ObjectId)]` | `ObjectId.GenerateNewId().ToString()` (sinh moi khi tao doi tuong) | `BaseEntityMongoDB.cs:13`. Vi la `private`, **lop ke thua khong the doc hoac ghi truc tiep** property nay; chi Mongo driver (qua reflection/Bson serializer) moi thao tac duoc |
| `IsDeleted` | `bool` | `public` | `[BsonRepresentation(BsonType.Boolean)]`, `[BsonElement(nameof(IsDeleted))]` | `false` | `BaseEntityMongoDB.cs:17`. Khong nullable - khong the bieu dien trang thai "chua xac dinh" |

`[BsonIgnoreExtraElements]` tren class (`BaseEntityMongoDB.cs:8`) cho phep MongoDB driver bo qua field co trong document nhung khong co property tuong ung trong class - khong bao loi khi doc du lieu cu/thua field.

### 2.7 EntityFullCreatedAndModifiedBase

Ke thua `BaseEntityMongoDB` (`BaseEntityMongoDB.cs:25`). Field bo sung:

| Field | Kieu | Ghi chu |
|---|---|---|
| `CreatedUser` | `string` | `BaseEntityMongoDB.cs:28` |
| `CreatedDate` | `DateTime?` | `[JsonConverter(typeof(DateTimeNullAbleConverter))]`; hai dong `[BsonRepresentation]`/`[BsonSerializer]` **bi comment** (`BaseEntityMongoDB.cs:31-32`) |
| `CreatedUserCode` | `string` | `BaseEntityMongoDB.cs:37` |
| `CreatedUserOrganization` | `string` | `BaseEntityMongoDB.cs:40` |
| `CreatedUserRegionId` | `int?` | `BaseEntityMongoDB.cs:43` |
| `CreatedUserLocationId` | `int?` | `BaseEntityMongoDB.cs:46` |
| `CreatedUserBranchId` | `int?` | `BaseEntityMongoDB.cs:49` |
| `ModifiedUser` | `string` | `BaseEntityMongoDB.cs:52` |
| `ModifiedDate` | `DateTime?` | `[JsonConverter(typeof(DateTimeNullAbleConverter))]` (`BaseEntityMongoDB.cs:54-58`) |
| `ModifiedUserCode` | `string` | `BaseEntityMongoDB.cs:61` |
| `ModifiedUserOrganization` | `string` | `BaseEntityMongoDB.cs:64` |
| `ModifiedUserRegionId` | `int?` | `BaseEntityMongoDB.cs:67` |
| `ModifiedUserLocationId` | `int?` | `BaseEntityMongoDB.cs:70` |
| `ModifiedUserBranchId` | `int?` | `BaseEntityMongoDB.cs:73` |

Khong co method/logic nao trong class nay - thuan tui du lieu (`abstract class`, khong the khoi tao truc tiep).

### 2.8 EntityFullCreatedBase

Ke thua `BaseEntityMongoDB` (`BaseEntityMongoDB.cs:81`). Chi co nhom field `Created*` giong 2.7 (`CreatedUser`, `CreatedDate`, `CreatedUserCode`, `CreatedUserOrganization`, `CreatedUserRegionId`, `CreatedUserLocationId`, `CreatedUserBranchId` - `BaseEntityMongoDB.cs:83-105`), **khong co bat ky field `Modified*` nao**.

### 2.9 EntityCreatedNotHaveAreaBase

Ke thua `BaseEntityMongoDB` (`BaseEntityMongoDB.cs:113`). Chi co `CreatedUser`, `CreatedDate`, `CreatedUserCode`, `CreatedUserOrganization` (`BaseEntityMongoDB.cs:115-128`) - **khong co** `CreatedUserRegionId/LocationId/BranchId` va **khong co** `Modified*`.

### 2.10 EntityCreatedAndModifiedNotHaveAreaBase

Ke thua `BaseEntityMongoDB` (`BaseEntityMongoDB.cs:136`). Co `CreatedUser`, `CreatedDate`, `CreatedUserCode`, `CreatedUserOrganization`, `ModifiedUser`, `ModifiedDate`, `ModifiedUserCode`, `ModifiedUserOrganization` (`BaseEntityMongoDB.cs:138-166`) - **khong co** bat ky `*RegionId`/`*LocationId`/`*BranchId` nao (day la diem khac biet duy nhat so voi 2.7).

### 2.11 IBaseEntitySQL va cac interface lien quan

**Signature**
```csharp
public interface IBaseEntitySQL
{
    public bool IsDeleted { get; set; }
    public string CreatedUser { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string CreatedUserCode { get; set; }
    public string CreatedUserOrganization { get; set; }
    public string ModifiedUser { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string ModifiedUserCode { get; set; }
    public string ModifiedUserOrganization { get; set; }
}
```

| Interface | Ke thua | Field rieng | Vi tri |
|---|---|---|---|
| `IBaseEntitySQL` | (khong) | `IsDeleted`, `CreatedUser`, `CreatedDate`, `CreatedUserCode`, `CreatedUserOrganization`, `ModifiedUser`, `ModifiedDate`, `ModifiedUserCode`, `ModifiedUserOrganization` | `BaseEntitySQL.cs:21-34` |
| `IEntityCreatedAndModifiedNotHaveAreaBase<T>` | `IBaseEntitySQL` | `T Id` | `BaseEntitySQL.cs:3-6` |
| `IEntityFullCreatedAndModifiedBase<T>` | `IBaseEntitySQL` | `T Id`, `CreatedUserRegionId/LocationId/BranchId` (`int?`), `ModifiedUserRegionId/LocationId/BranchId` (`int?`) | `BaseEntitySQL.cs:8-19` |
| `IEntityCreatedAndModifiedShortBase<T>` | **khong ke thua `IBaseEntitySQL`** (dinh nghia doc lap) | `T Id`, `bool IsDeleted`, `string CreatedUser`, `DateTime? CreatedDate`, `string ModifiedUser`, `DateTime? ModifiedDate` (khong co `*Code`/`*Organization`) | `BaseEntitySQL.cs:36-47` |

**Muc dich** - Cho phep `WriteDbContext.OnBeforeSaveChanges` nhan dien entity can dong dau audit bang mot cau kiem tra duy nhat `entry.Entity is IBaseEntitySQL` (khong lien quan den generic `T`), roi ep kieu `(IBaseEntitySQL)entry.Entity` de gan `IsDeleted`/`CreatedDate`/`CreatedUser`/`CreatedUserCode`/`CreatedUserOrganization` (khi them moi) hoac `ModifiedDate`/`ModifiedUser`/`ModifiedUserCode`/`ModifiedUserOrganization` (khi sua, va chi khi `audit != null`) - da xac nhan khop 100% ten field voi source code tai `WriteDbContext.cs:129-180`.

**Output/Dieu kien xu ly/Side effect/Error handling** - Khong ap dung (day la interface, khong co than ham).

**Khi nao NEN dung** - Entity SQL can duoc `WriteDbContext`/`CoreSQL` tu dong dong dau audit thi phai implement `IBaseEntitySQL` (truc tiep hoac qua mot trong hai interface generic `IEntityCreatedAndModifiedNotHaveAreaBase<T>`/`IEntityFullCreatedAndModifiedBase<T>`).

**Khi nao KHONG dung** - Neu entity implement **chi** `IEntityCreatedAndModifiedShortBase<T>` (khong implement `IBaseEntitySQL`), entity se **khong duoc** `WriteDbContext.OnBeforeSaveChanges` nhan dien (dieu kien la `is IBaseEntitySQL`) - tu do KHONG duoc tu dong dong dau audit du ten field trung mot phan. Day la suy luan truc tiep tu dieu kien loc tai `WriteDbContext.cs:129` doi chieu voi khai bao `BaseEntitySQL.cs:36` (khong ke thua `IBaseEntitySQL`).

**Gioi han** - Trong repo `sr-core-helper` hien tai, grep toan bo `*.cs` (loai `obj/`, `bin/`, worktree) khong tim thay class/entity cu the nao implement 4 interface nay - **khong xac dinh duoc tu source code trong repo nay** entity thuc te nao dang dung chung, ten cot SQL tuong ung ra sao; can tra o repo API tieu thu package `FTELSRCore.Shared` (xem `CopyToOtherLibs` trong `FTELSRCore.Shared.csproj`).

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `EventId => Guid.NewGuid()` va `OccurredOn => DateTime.Now` la default interface implementation **khong co backing field** - moi lan truy cap tinh lai gia tri moi | `IDomainEvent.cs:5-6` | Doc `domainEvent.EventId` (hoac `OccurredOn`) hai lan lien tiep tren **cung mot instance** cho hai gia tri khac nhau, tru khi lop cu the tu khai bao lai property nay bang auto-property co luu tru. Neu code nghiep vu (hoac log) doc `EventId` nhieu lan roi ky vong cung mot gia tri, se sai |
| 2 | `OccurredOn` dung `DateTime.Now` (gio local cua may chay), khac voi quy uoc `CommonBaseConstant.DateTimeUtc()` (UTC+7, da ghi nhan trong `Data-MongoDB-CoreMongoDB.md`/`Data-SQL-*`) dung cho cac field audit `CreatedDate`/`ModifiedDate` | `IDomainEvent.cs:6` | Neu he thong chay tren server co `TimeZone` khac VN, `OccurredOn` cua domain event se **lech gio** so voi `CreatedDate`/`ModifiedDate` cua entity phat sinh event do, gay kho khi doi chieu thoi gian giua log/event va du lieu DB |
| 3 | `AddDomainEvent(IDomainEvent domainEvent)` va `AddRangeDomainEvent(List<IDomainEvent> domainEvents)` khong kiem tra `null` | `Aggregate.cs:20-28` | `AddDomainEvent(null)` khong nem loi nhung dua `null` vao danh sach, co the gay `NullReferenceException` khi code khac (vi du `WriteDbContext.DispatchDomainEvents` -> `publisher.Publish(domainEvent, ...)`) duyet qua va goi member tren `null`. `AddRangeDomainEvent(null)` nem `ArgumentNullException` tu BCL, khong duoc bat lai va khong co message rieng |
| 4 | `ten file` `BaseEntitySQL.cs` khong chua class nao ten `BaseEntitySQL` - chi co 4 interface | `BaseEntitySQL.cs:1-48` | De nham lan khi tim kiem theo ten "BaseEntitySQL" trong code hoac tai lieu, mong doi mot class co san (nhu `BaseEntityMongoDB`) nhung thuc te phai tu implement toan bo tu interface |
| 5 | `IEntityCreatedAndModifiedShortBase<T>` khong ke thua `IBaseEntitySQL` du ten va tap field gan giong (`IsDeleted`, `CreatedUser`, `CreatedDate`, `ModifiedUser`, `ModifiedDate`) | `BaseEntitySQL.cs:36-47` so voi `BaseEntitySQL.cs:21-34` | Entity chi implement interface nay se **khong** duoc `WriteDbContext.OnBeforeSaveChanges` nhan dien (dieu kien loc la `is IBaseEntitySQL`, `WriteDbContext.cs:129`), nen khong duoc tu dong dong dau audit, du interface trong ve "tuong tu" `IBaseEntitySQL` |
| 6 | `WriteDbContext.DispatchDomainEvents` chi thu domain event tu `ChangeTracker.Entries<Aggregate>()` - **lop `Aggregate` cu the**, khong phai interface `IAggregate` | `WriteDbContext.cs:421` (theo `Data-SQL-CoreSQL-TwoEntity.md`, muc "Khong lam duoc", dong 35 cua file do) | Entity chi implement `IAggregate` (khong ke thua tu `Aggregate`) van thoa dieu kien `is IAggregate` trong `CoreSQLTenant.cs:638` (nen `DomainEvents` van duoc doc/gop khi chuyen doi TEntityFrom -> TEntityTo) nhung **se khong bao gio duoc publish** boi `WriteDbContext`, vi dieu kien loc o do la generic `Entries<Aggregate>()` chu khong phai `Entries<IAggregate>()`. Doi chieu voi source code `Aggregate.cs`/`IDomainEvent.cs` cho thay day la thiet ke that (khong phai suy dien) - can luu y khi viet entity moi: phai ke thua `Aggregate` (khong chi implement `IAggregate`) de domain event duoc publish |
| 7 | Hai dong `[BsonRepresentation(BsonType.DateTime)]` va `[BsonSerializer(typeof(VietnamDateTimeSerializer))]` bi **comment** phia tren tat ca property `CreatedDate`/`ModifiedDate` trong `BaseEntityMongoDB.cs` | `BaseEntityMongoDB.cs:31-32`, `55-56`, `87-88`, `119-120`, `142-143`, `157-158` | Code chet lap lai 6 lan; cho thay tung co du dinh dung serializer rieng cho gio Viet Nam nhung da bi tat - hien tai cac field nay serialize/deserialize BSON theo hanh vi mac dinh cua driver (khong co serializer tuy chinh), **khong xac dinh duoc tu source code** trong repo nay ly do vo hieu hoa |
| 8 | Doi chieu voi `Data-MongoDB-CoreMongoDB.md` va `Data-SQL-CoreSQL*.md`/`Data-SQL-UnitOfWork-DbContexts.md` (hien co): danh sach field cua `BaseEntityMongoDB`/dan xuat (`IsDeleted`, `CreatedUser`, `CreatedDate`, `CreatedUserCode`, `CreatedUserOrganization`, `ModifiedUser`, `ModifiedDate`, `ModifiedUserCode`, `ModifiedUserOrganization`) va cua `IBaseEntitySQL` ma cac file KB do tham chieu **khop hoan toan** voi source code trong 4 file cua module nay - **khong phat hien mo ta sai/thieu nao** can ghi nhan o day |

