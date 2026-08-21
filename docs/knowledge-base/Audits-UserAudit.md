# Audit domain (IUserAudit, UserAudit, AuditModel)

> Nguon: `FTELSRCore.Shared/Audits/IUserAudit.cs`, `FTELSRCore.Shared/Audits/UserAudit.cs`, `FTELSRCore.Shared/Models/Audits/AuditModel.cs`, `FTELSRCore.Shared/Models/Audits/SnapshotAuditModel.cs`, `FTELSRCore.Shared/Models/Audits/GetAllConcurrentAreaEmployeeModel.cs`
> Loai: `interface` (IUserAudit) + `record` (UserAudit, AuditModel, CreatorInfo, SnapshotAuditModel, GetAllConcurrentAreaEmployeeModel)
> Cap nhat theo commit: `89c1ce9`

## 1. Tong quan

`IUserAudit`/`UserAudit` la lop doc thong tin nguoi dung hien tai (claims cua `ClaimsPrincipal` trong `HttpContext`) va dung no de dung ra mot `AuditModel` — doi tuong audit duoc truyen xuyen suot xuong tang du lieu (`WriteDbContext.SaveChangesAsync(AuditModel, ...)` phia SQL, `CoreMongoDB`/`ProjectToExtensions.SetDataCreatedDefault`/`SetDataUpdatedDefault` phia MongoDB) de dien cac cot `Created*`/`Modified*` cua entity. `AuditModel` va `CreatorInfo` (dinh nghia trong cung file `AuditModel.cs`) la kieu du lieu thuan (khong logic); `SnapshotAuditModel` va `GetAllConcurrentAreaEmployeeModel` la hai DTO phu tro. Module nam o tang `FTELSRCore.Shared` (thu vien dung chung), duoc cac repo API tieu thu qua NuGet/DLL — **khong co dang ky Dependency Injection cho `IUserAudit`/`UserAudit` trong repo nay** (grep `AddScoped`/`AddTransient` + `IUserAudit` trong `FTELSRCore.Shared` = 0 ket qua), nen vong doi (scoped/transient) va noi goi `new UserAudit(...)` thuc te **khong xac dinh duoc tu source code trong repo nay**.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Doc cac claim chuan (`ClaimTypes.NameIdentifier/Name/Email`) va claim tuy bien (`BranchId`, `LocationId`, `TitleCode`, `Organization`, `RegionId`, `SR.ConcurrentArea`, `SR.SRRoles`, `SR.FTelRoles`, `Permissions`) tu `ClaimsPrincipal` cua `HttpContext` hien tai (`UserAudit.cs:16-99`) | Khong tu lam moi/refresh claim khi token thay doi trong cung request — toan bo gia tri duoc doc **mot lan** trong constructor (`UserAudit.cs:12-100`) |
| Suy ra `RoleSR` uu tien cao nhat cua nguoi dung tu danh sach `RolesSR` qua `RoleDataConstant.GetRoleData` (`UserAudit.cs:126-132`) | Khong validate/whitelist gia tri claim (vd `TitleCode`, `Organization` tuy y, khong kiem tra ton tai trong he thong) |
| Dung ra `AuditModel` (gom `CreatorInfo` + `Method`/`Address`/`Ip`/`Device` cua request hien tai) qua `GetAuditCurrentAsync` (`UserAudit.cs:141-152`) | Khong dien `CreatorInfo.RegionId` khi dung tu `GetAuditCurrentUser` — RegionId doc duoc tu claim nhung **khong** duoc gan vao `AuditModel.CreatorInfo` (xem muc 3 #1) |
| Cho phep ghi de ten nguoi tao bang `defaultName`, hoac tu dat "SYSTEM-SR" khi khong xac dinh duoc nguoi dung (`UserAudit.cs:282-295`) | Khong tu ghi audit log (bang/ban ghi lich su) — `AuditModel` chi la DTO truyen tham so, **khong** tu ghi vao dau; viec co ghi audit log hay khong hoan toan phu thuoc tang tieu thu (xem doi chieu voi `WriteDbContext` o muc 3) |
| Cung cap 2 overload `SetAudit` (tu `GetAllEmployeeByFilterResponse` hoac tu `CreatorInfo` co san) de lop con/derived logic gan `CreatorInfo` theo nguon khac claim (`UserAudit.cs:202-273`) | `SnapshotAuditModel` duoc dinh nghia san (`KeyValues`, `OldValues`, `NewValues`, `ChangedColumns`, `TemporaryProperties`) nhung **khong co dong code nao trong repo nay khoi tao instance cua no** — hoan toan la dead type (xem muc 3 #6) |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `Lazy<IHttpContextAccessor>` (Microsoft.AspNetCore.Http) | Nguon lay `HttpContext` hien tai; `.Value` duoc truy cap ngay trong constructor (`UserAudit.cs:17`) va lai mot lan nua trong `GetAuditCurrentUser` (`UserAudit.cs:164`) |
| `ConvertHelpers.ConvertClaimsPrincipalToData` (`Helpers/ConvertHelpers.cs:250-266`) | Doc 1 claim theo `claimType`, tra `setDataDefault` khi khong tim thay hoac loi (bat moi `Exception`) |
| `ClaimTypesConstant` (`Constants/ClaimTypesConstant.cs`) | Cung cap ten claim tuy bien: `SRRoles`, `FTelRoles`, `Permissions`, `ConcurrentArea` (dung truc tiep); `BranchId`, `TitleCode`, `LocationId`, `Organization` **co dinh nghia san nhung UserAudit khong dung truc tiep**, ma dung `nameof(...)` (xem muc 3 #2) |
| `CommonBaseConstant` (`Constants/CommonBaseConstant.cs:29-33`) | Gia tri fallback: `AnonymousCode = "0"`, `Anonymous = "Anonymous"`, `OrganizationForISC = "FTEL"` |
| `RoleDataConstant.GetRoleData(List<string>)` (`Constants/RoleData/RoleDataConstant.cs:48-59`) | Anh xa danh sach `RolesSR` (chuoi ma role) sang enum `RoleSR` theo do uu tien; tra `RoleSR.ONLY_CREATE` khi danh sach rong hoac khong khop |
| `RoleSR` (enum, `Enum/RoleSR.cs`) | Kieu tra ve cua `RoleData`/`CreatorInfo.Role` |
| `GetAllEmployeeByFilterResponse` (`Responses/GetAllEmployeeByFilterResponse.cs`) | Nguon du lieu thay the cho overload `SetAudit(AuditModel, GetAllEmployeeByFilterResponse, ...)` khi da tra duoc thong tin nhan vien tu he thong khac (thay vi tu claim) |
| `GetAllConcurrentAreaEmployeeModel` | DTO phan tu cua `ConcurrentAreas`, deserialize tu JSON trong claim `SR.ConcurrentArea` |
| `item.Value.JSonTryParse<List<GetAllConcurrentAreaEmployeeModel>>(...)` (`Helpers/JSonParseHelpers.cs:149-167`) | Parse JSON (dung `System.Text.Json`, bat exception, tra `false` khi loi/chuoi rong/`"null"`/`"{}"`/`"[]"`) |
| `Microsoft.EntityFrameworkCore.ChangeTracking.PropertyEntry` | Kieu phan tu `SnapshotAuditModel.TemporaryProperties` |
| `WriteDbContext<TContext>` (tang tieu thu, `Data/SQL/DbContexts/Write/WriteDbContext.cs`) | Noi `AuditModel` duoc **doc** de gan `CreatedUser*`/`ModifiedUser*` cua entity SQL (chi doc `CreatorInfo.Name/Code/Organization` — `WriteDbContext.cs:139-146`) |
| `ProjectToExtensions.SetDataCreatedDefault`/`SetDataUpdatedDefault` (tang tieu thu, MongoDB) | Noi `AuditModel` duoc **doc** de gan cot audit cua document Mongo |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `IUserAudit` | Interface | Hop dong doc thong tin nguoi dung hien tai + tao `AuditModel` |
| `UserAudit` | Class (record) | Cai dat duy nhat cua `IUserAudit` trong repo nay |
| `UserAudit(Lazy<IHttpContextAccessor>)` | Constructor | Doc toan bo claim tu `HttpContext.User` mot lan |
| `RegionId`, `BranchId`, `LocationId` | Property (`int?`) | ID vung/chi nhanh/dia diem, parse tu claim so |
| `EmployeeEmail`, `EmployeeCode`, `EmployeeUserName` | Property (`string`) | Email/ma/ten dang nhap, tu claim chuan `ClaimTypes.*` |
| `TitleCode`, `Organization` | Property (`string`) | Vai tro/don vi, tu claim tuy bien. **Khong doi xung**: `TitleCode` = `null` khi claim vang mat hoac rong; `Organization` = `"FTEL"` (`CommonBaseConstant.OrganizationForISC`) khi claim **vang mat**, va chi la `null` khi claim ton tai nhung gia tri rong/whitespace (xem muc 2.1 va muc 3 #9) |
| `RolesSR`, `RolesFTel`, `Permissions` | Property (`List<string>`) | Danh sach gia tri cua tat ca claim cung ten |
| `ConcurrentAreas` | Property (`List<GetAllConcurrentAreaEmployeeModel>`) | Danh sach dia ban kiem nhiem, parse JSON tu claim |
| `RoleData` | Property (`RoleSR`, tinh toan) | Role uu tien cao nhat, tinh lai moi lan doc |
| `GetAuditCurrentAsync(string, CancellationToken)` | Method (public, async) | Tra `AuditModel` day du tu nguoi dung hien tai |
| `GetAuditCurrentUser(CancellationToken)` | Method (protected) | Dung `AuditModel` tho tu `HttpContext` + property da parse |
| `SetAudit(AuditModel, GetAllEmployeeByFilterResponse, string, CancellationToken)` | Method (protected static) | Gan `CreatorInfo` tu ket qua tra cuu nhan vien (hoac fallback ten mac dinh) |
| `SetAudit(AuditModel, CreatorInfo, string, CancellationToken)` | Method (protected static) | Gan `CreatorInfo` co san truc tiep (hoac fallback ten mac dinh) |
| `SetAuditDefaultWithOwner(AuditModel, string)` | Method (private static) | Logic fallback ten: dung `defaultName` hoac doi "Anonymous" -> "SYSTEM-SR" |
| `AuditModel` | Record (DTO) | Ip/Device/Method/Address cua request + `CreatorInfo` |
| `CreatorInfo` | Record (DTO, dinh nghia trong `AuditModel.cs`) | Thong tin nguoi tao: Code/Name/Email/Organization/Role/RegionId/BranchId/LocationId/TitleCode/RolesSR/RolesFTel/ConcurrentAreas |
| `SnapshotAuditModel` | Record (DTO) | Snapshot thay doi entity (bang, khoa, gia tri cu/moi, cot thay doi) — hien khong duoc khoi tao o dau |
| `GetAllConcurrentAreaEmployeeModel` | Record (DTO) | 1 dia ban kiem nhiem: `BranchId`, `LocationId` |

## 2. Chi tiet API

### 2.1 UserAudit(Lazy<IHttpContextAccessor> httpContextAccessor)

**Signature**
```csharp
public UserAudit(Lazy<IHttpContextAccessor> httpContextAccessor)
```
**Muc dich** — Khoi tao mot `UserAudit` bang cach doc toan bo claim can thiet tu `ClaimsPrincipal` cua request hien tai (`httpContextAccessor.Value.HttpContext.User`) va gan vao cac property init-only (`get`-only) cua record. Day la **noi duy nhat** cac property duoc gan gia tri — sau khi construct xong, object bat bien (immutable) doi voi cac claim da doc (`UserAudit.cs:12-100`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `httpContextAccessor` | `Lazy<IHttpContextAccessor>` | Khong bat buoc ve mat compile (khong co `[NotNull]`), nhung neu `null` thi `claimsPrincipal` se la `null` va moi property tra ve gia tri fallback | Truy cap qua chuoi `?.Value?.HttpContext?.User` — an toan voi `null` o bat ky mat xich nao (`UserAudit.cs:16-17`) | Khong co (khong phai optional parameter) |

**Output** — Khong co gia tri tra ve (constructor). Ket qua la cac property duoc gan nhu sau (tat ca deu bam vao `claimsPrincipal` = `httpContextAccessor?.Value?.HttpContext?.User`):

| Property | Nguon claim | Gia tri khi khong tim thay / rong |
|---|---|---|
| `EmployeeCode` | `ClaimTypes.NameIdentifier` | `CommonBaseConstant.AnonymousCode` ("0") |
| `EmployeeUserName` | `ClaimTypes.Name` | `CommonBaseConstant.Anonymous` ("Anonymous") |
| `EmployeeEmail` | `ClaimTypes.Email` | `string.Empty` |
| `TitleCode` | `nameof(TitleCode)` = "TitleCode" | `null` — vi `setDataDefault: string.Empty` (`UserAudit.cs:39`), nen khi claim khong ton tai `ConvertClaimsPrincipalToData` tra `""`, khien dieu kien `!IsNullOrWhiteSpace` fail -> `null` |
| `Organization` | `nameof(Organization)` = "Organization" | **KHONG phai luon la `null`** — `setDataDefault` truyen vao la `CommonBaseConstant.OrganizationForISC` ("FTEL"), khac voi `TitleCode` (`UserAudit.cs:47`). Khi claim "Organization" **khong ton tai**, `ConvertClaimsPrincipalToData` tra thang `"FTEL"` (khong rong/whitespace) -> `Organization = "FTEL"`, **khong phai `null`**. `Organization` chi la `null` khi claim **ton tai nhung gia tri (sau `Trim()`) la rong/whitespace** — xem phan tich chi tiet o "Dieu kien xu ly" va muc 3 #9 |
| `RegionId` | `nameof(RegionId)` = "RegionId" | `null` (neu rong hoac `int.TryParse` fail) |
| `BranchId` | `nameof(BranchId)` = "BranchId" | `null` (neu rong hoac `int.TryParse` fail) |
| `LocationId` | `nameof(LocationId)` = "LocationId" | `null` (neu rong hoac `int.TryParse` fail) |
| `ConcurrentAreas` | Moi claim co `Type` = `ClaimTypesConstant.ConcurrentArea` ("SR.ConcurrentArea"), moi `Value` la 1 chuoi JSON cua `List<GetAllConcurrentAreaEmployeeModel>`, duoc `SelectMany` gop lai | `[]` (rieng phan tu `null` sau khi parse se bi loai bang `.Where(x => x is not null)`) |
| `RolesSR` | Tat ca claim co `Type` = `ClaimTypesConstant.SRRoles` ("SR.SRRoles"), lay `Value` | `[]` |
| `RolesFTel` | Tat ca claim co `Type` = `ClaimTypesConstant.FTelRoles` ("SR.FTelRoles"), lay `Value` | `[]` |
| `Permissions` | Tat ca claim co `Type` = `ClaimTypesConstant.Permissions` ("Permissions"), lay `Value` | `[]` |

**Dieu kien xu ly** — Thu tu thuc thi trong constructor: (1) luu `httpContextAccessor` vao field `_contextAccessor`; (2) lay `claimsPrincipal`; (3) gan tuan tu `EmployeeCode` -> `EmployeeUserName` -> `EmployeeEmail` -> `TitleCode` -> `Organization` -> `RegionId` -> `BranchId` -> `LocationId` -> `ConcurrentAreas` -> `RolesSR` -> `RolesFTel` -> `Permissions` (dung thu tu khai bao code, `UserAudit.cs:19-99`). Moi gia tri so (`RegionId`/`BranchId`/`LocationId`) can **ca hai** dieu kien: chuoi khong rong/whitespace **va** `int.TryParse` thanh cong, neu khong se la `null`.

**Luu y quan trong (bat dong bo giua `TitleCode` va `Organization`)** — Ca hai dung chung 1 pattern `ConvertClaimsPrincipalToData(...) is string x && !IsNullOrWhiteSpace(x) ? x : null`, nhung tham so `setDataDefault` truyen vao **khac nhau**: `TitleCode` dung `string.Empty` (`UserAudit.cs:39`) con `Organization` dung `CommonBaseConstant.OrganizationForISC` = `"FTEL"` (`UserAudit.cs:47`). Vi `ConvertClaimsPrincipalToData` tra `setDataDefault` **ngay khi khong tim thay claim**, ket qua la: neu claim "TitleCode" vang mat -> `TitleCode = null`; nhung neu claim "Organization" vang mat -> `Organization = "FTEL"` (khong phai `null`). Day la mot su khong nhat quan thuc su trong code nguon (xem muc 3 #9), khong phai loi tai lieu.

**Side effect** — Khong ghi DB, khong goi API ngoai, khong mutate `HttpContext.User`. Ep buoc `.Value` cua `Lazy<IHttpContextAccessor>` duoc resolve ngay tai thoi diem construct object (khong con "lazy" doi voi ban than accessor sau buoc nay). **Ngoai le co dieu kien**: neu gia tri claim `SR.ConcurrentArea` la mot chuoi JSON khong hop le (khong rong/`"null"`/`"{}"`/`"[]"` nhung parse `JsonSerializer.Deserialize` that bai), `item.Value.JSonTryParse(...)` (`UserAudit.cs:78`, khong truyen `logger`) se bat `Exception` va goi `CommonBaseConstant.ConfigLoggerExceptionByConsole(...)` (`Helpers/JSonParseHelpers.cs:184`), ham nay thuc hien **`Console.WriteLine(...)`** (`Constants/CommonBaseConstant.cs:63`) — tuc la constructor **co the ghi ra console/log** trong truong hop nay, khong phai "khong ghi log" tuyet doi nhu mo ta truoc day (xem muc 3 #10).

**Error handling** — Khong co try/catch trong constructor; toan bo viec bat loi nam trong `ConvertHelpers.ConvertClaimsPrincipalToData` (bat `Exception`, tra `setDataDefault`, **khong log**) va `JSonTryParse` (bat `Exception`, tra `false`, **nhung co log ra console** qua `ConfigLoggerExceptionByConsole` khi khong truyen `logger` — xem "Side effect" tren). Neu ban than `httpContextAccessor.Value` nem exception (vi du DI container loi khi resolve `IHttpContextAccessor`), exception se lan truyen ra ngoai constructor — **khong xac dinh duoc tu source code** repo nay lop goi (DI container) xu ly the nao.

**Khi nao NEN dung** — Duoc DI container tao 1 lan cho moi scope/request (gia dinh; khong co dang ky DI trong repo nay de xac nhan lifetime chinh xac); dung khi can thong tin nguoi dung dang thuc hien request HTTP hien tai.

**Khi nao KHONG dung** — Ngoai ngu canh HTTP request (background job, message consumer khong co `HttpContext`) — moi property se tra ve gia tri fallback "Anonymous"/"0"/`null`/`[]`/**`"FTEL"`** (rieng `Organization` fallback ve `"FTEL"`, khong phai `null` — xem "Luu y" o tren va muc 3 #9), khong bao gio nem exception nhung du lieu se sai lech voi nguoi dung thuc.

**Gioi han** — (1) Tat ca gia tri chi doc **mot lan** khi object duoc tao; neu token/`HttpContext.User` doi trong cung 1 scope song UserAudit khong duoc tao lai, du lieu se cu. (2) `RegionId` la property public tren `UserAudit` nhung **khong** co trong `IUserAudit` — code chi giu tham chieu qua interface se khong truy cap duoc `RegionId` (phai ep kieu ve `UserAudit`). (3) La `record`, C# tu sinh `Equals`/`GetHashCode` dua tren **toan bo instance field**, bao gom ca field private `_contextAccessor` (kieu `Lazy<IHttpContextAccessor>` khong override `Equals` -> so sanh theo tham chieu); vi vay hai `UserAudit` co cung du lieu claim nhung duoc tao tu hai `Lazy<IHttpContextAccessor>` khac nhau se **khong** duoc coi la bang nhau qua `==`/`Equals` — gia tri value-equality cua `record` gan nhu vo nghia doi voi kieu nay.

### 2.2 RoleData

**Signature**
```csharp
public RoleSR RoleData
{
    get
    {
        return RoleDataConstant.GetRoleData(RolesSR);
    }
}
```
**Muc dich** — Tra ve `RoleSR` co do uu tien cao nhat ma nguoi dung dang co, suy tu danh sach ma role SR (`RolesSR`) hien co cua nguoi dung.

**Input hop le** — Khong nhan tham so; dung noi bo `RolesSR` (property da duoc gan trong constructor).

**Output** — `RoleSR` (khong nullable). Neu `RolesSR` rong hoac khong khop bat ky nhom ma role nao trong `RoleDataConstant.ComplexityForRolesSR`, tra `RoleSR.ONLY_CREATE` (`RoleDataConstant.cs:50-58`).

**Dieu kien xu ly** — Moi lan doc property se goi lai `RoleDataConstant.GetRoleData(RolesSR)` — **khong cache**. Ham nay loc cac nhom co it nhat 1 ma role trung voi `RolesSR`, sap xep theo `Order` (chinh la gia tri numeric cua `RoleSR`) va lay nhom dau tien.

**Side effect** — Khong co (chi tinh toan tu du lieu san co trong memory).

**Error handling** — Khong co try/catch; `RoleDataConstant.GetRoleData` khong nem exception voi input hop le (`roles` co the `null`, ham tu xu ly).

**Khi nao NEN dung** — Moi khi can biet quyen han cao nhat cua nguoi dung hien tai de quyet dinh logic phan quyen.

**Khi nao KHONG dung** — Khi can biet **tat ca** role cua nguoi dung (chi tra ve role uu tien nhat) — dung truc tiep `RolesSR` cho truong hop nay.

**Gioi han** — Tinh lai moi lan goi (khong dang ke ve hieu nang do danh sach nho, nhung can luu y neu goi trong loop).

### 2.3 GetAuditCurrentAsync(string defaultName = "", CancellationToken cancellationToken = default)

**Signature**
```csharp
public async Task<AuditModel> GetAuditCurrentAsync(
    string defaultName = "", CancellationToken cancellationToken = default)
```
**Muc dich** — API cong khai duy nhat cua `IUserAudit` de lay `AuditModel` hoan chinh (gom thong tin request HTTP + thong tin nguoi tao) cho nguoi dung hien tai, dung de truyen vao cac ham ghi du lieu (`CreateAsync`/`UpdateAsync` cua `CoreSQL`/`CoreMongoDB`, `WriteDbContext.SaveChangesAsync`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `defaultName` | `string` | Khong | Khong validate ngoai `IsNullOrWhiteSpace` ben trong `SetAuditDefaultWithOwner` | `""` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` goi 2 lan: dau `GetAuditCurrentAsync` (`:144`) va dau `GetAuditCurrentUser` (`:162`) — **khong** duoc truyen tiep vao bat ky `await` thuc su nao vi ham khong co I/O bat dong bo | `default` |

**Output** — `Task<AuditModel>` — luon tra ve mot `AuditModel` **khong null** (do `SetAudit` co `audit ??= new();`). Khong co truong hop tra `null` hoac nem loi "khong tim thay" — day la ham dung du lieu tu bo nho (claim da parse), khong co truong hop "not found" theo nghia CSDL.

**Dieu kien xu ly**
1. Kiem tra `cancellationToken` bi huy -> nem `OperationCanceledException` neu co.
2. Goi `GetAuditCurrentUser(cancellationToken)` de dung `AuditModel` tho (Method/Address/Ip/Device tu `HttpContext.Request` hien tai + `CreatorInfo` tu cac property da parse trong constructor).
3. Goi `SetAudit(audit, employee: null, defaultName, cancellationToken)` — vi `employee` luon `null` o day, nhanh `case false` cua `SetAudit` luon duoc chon -> goi `SetAuditDefaultWithOwner(audit, defaultName)`.
4. Boc ket qua trong `Task.FromResult(...)` (khong co bat dong bo thuc su ben trong ham nay).

**Side effect** — Khong ghi DB/log. Khong mutate tham so dau vao (`defaultName`, `cancellationToken` la kieu gia tri/immutable). Doc `HttpContext.Request.Method/GetEncodedUrl()`, `HttpContext.Connection.RemoteIpAddress`, `HttpContext.Request.Headers.UserAgent` tai thoi diem goi (khac voi cac claim, nhung gia tri nay duoc doc **moi lan goi** `GetAuditCurrentAsync`, khong phai 1 lan trong constructor).

**Error handling** — Khong co try/catch rieng trong ham nay; ngoai tru huy tac vu qua `cancellationToken`, khong co duong loi nao khac duoc xu ly rieng (moi loi tiem an nam trong `ConvertHelpers`/`JSonTryParse` da duoc bat o buoc doc claim, khong o day).

**Khi nao NEN dung** — Bat ky luc can 1 `AuditModel` day du (Ip/Device/Method/Address + CreatorInfo) tu ngu canh HTTP hien tai, vi du truoc khi goi `CreateAsync(entity, audit, ct)` cua CoreSQL/CoreMongoDB.

**Khi nao KHONG dung** — Khi can `AuditModel` voi `CreatorInfo` lay tu nguon khac claim (vi du tra cuu nhan vien tu he thong khac) — nen dung truc tiep `SetAudit(audit, employee, ...)`/`SetAudit(audit, creatorInfo, ...)` (protected, chi goi duoc tu lop con) hoac tu dung ra `AuditModel` roi gan `CreatorInfo` thu cong.

**Gioi han** — (1) `defaultName` khi khac rong se **luon** ghi de `CreatorInfo.Name`, **bat ke** nguoi dung hien tai co xac thuc hop le hay khong (xem chi tiet muc 2.6) — day la hanh vi de gay nham lan neu goi nham `defaultName` khac rong cho request co nguoi dung thuc. (2) `CreatorInfo.RegionId` khong duoc dien (xem muc 3 #1). (3) Tham so `cancellationToken` chi dung de kiem tra huy ngay dau ham, khong lien ket voi bat ky I/O thuc su.

### 2.4 GetAuditCurrentUser(CancellationToken cancellationToken = default)

**Signature**
```csharp
protected AuditModel GetAuditCurrentUser(CancellationToken cancellationToken = default)
```
**Muc dich** — Dung ra `AuditModel` "tho": lay `HttpContext` hien tai qua `_contextAccessor.Value?.HttpContext` va gan `CreatorInfo` tu cac property da parse san (`BranchId`, `LocationId`, `RoleData`, `RolesSR`, `RolesFTel`, `ConcurrentAreas`, `TitleCode`, `EmployeeEmail`, `EmployeeCode`, `EmployeeUserName`, `Organization`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `cancellationToken` | `CancellationToken` | Khong | Chi `ThrowIfCancellationRequested()` | `default` |

**Output** — `AuditModel` khong null, voi:
- `Method` = `httpContext?.Request?.Method ?? ""`
- `Address` = `httpContext?.Request?.GetEncodedUrl() ?? ""`
- `Ip` = `httpContext?.Connection?.RemoteIpAddress?.ToString() ?? ""`
- `Device` = `httpContext?.Request?.Headers.UserAgent.ToString() ?? ""`
- `CreatorInfo.BranchId`/`LocationId` = tu property cung ten
- `CreatorInfo.Role` = `RoleData` (tinh lai tai thoi diem goi)
- `CreatorInfo.RolesSR`/`RolesFTel`/`ConcurrentAreas` = tham chieu **cung danh sach** voi property tren `UserAudit` (khong sao chep moi — xem "Gioi han")
- `CreatorInfo.TitleCode` = `TitleCode ?? ""`
- `CreatorInfo.Email` = `EmployeeEmail ?? ""`
- `CreatorInfo.Code` = `EmployeeCode ?? CommonBaseConstant.AnonymousCode`
- `CreatorInfo.Name` = `EmployeeUserName ?? CommonBaseConstant.Anonymous`
- `CreatorInfo.Organization` = `Organization ?? CommonBaseConstant.OrganizationForISC`
- `CreatorInfo.RegionId` = **khong duoc gan** -> giu gia tri mac dinh cua `CreatorInfo` la `null` (xem muc 3 #1)

**Dieu kien xu ly** — Khong co nhanh re (khong `if`/`switch`) ngoai cac operator `??`/`?.` khi tao object. Toan bo la mot bieu thuc khoi tao object duy nhat (`UserAudit.cs:166-188`).

**Side effect** — Khong ghi DB/log. Doc `HttpContext` moi lan goi (khong cache).

**Error handling** — Khong co try/catch; moi truy cap deu qua `?.` nen an toan voi `HttpContext` = `null` (vi du khi khong o trong request HTTP).

**Khi nao NEN dung** — Chi goi tu noi bo `UserAudit` hoac lop con ke thua (protected); dung khi can `AuditModel` "tho" truoc khi ap dung `SetAudit`.

**Khi nao KHONG dung** — Khong the goi tu ngoai class (khong `public`/`internal`).

**Gioi han** — `CreatorInfo.RolesSR`, `RolesFTel`, `ConcurrentAreas` la **tham chieu chia se** voi property cua `UserAudit` (khong `.ToList()` sao chep) — sua doi (mutate) 1 trong 2 list nay tu ben ngoai se anh huong ca hai; tuy nhien trong code hien tai khong co noi nao mutate cac list nay sau khi tao (`RolesSR`/`RolesFTel`/`ConcurrentAreas` chi co getter, khong co setter public).

### 2.5 SetAudit(AuditModel audit, GetAllEmployeeByFilterResponse employee, string defaultName = "", CancellationToken cancellationToken = default)

**Signature**
```csharp
protected static AuditModel SetAudit(AuditModel audit,
                                     GetAllEmployeeByFilterResponse employee,
                                     string defaultName = "",
                                     CancellationToken cancellationToken = default)
```
**Muc dich** — Gan `CreatorInfo` cua `audit` tu ket qua tra cuu nhan vien (`GetAllEmployeeByFilterResponse`) khi co, hoac fallback ve logic dat ten mac dinh khi khong co.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `audit` | `AuditModel` | Khong | `audit ??= new();` — neu `null` se tao moi voi `CreatorInfo` la instance mac dinh (`Role = RoleSR.ONLY_CREATE`, cac list rong) | Khong co (khong phai optional) |
| `employee` | `GetAllEmployeeByFilterResponse` | Khong | Kiem tra `is not null` de re nhanh; khong validate tung field cua `employee` | Khong co (khong phai optional) |
| `defaultName` | `string` | Khong | Dung trong nhanh `employee is null` | `""` |
| `cancellationToken` | `CancellationToken` | Khong | Chi `ThrowIfCancellationRequested()` | `default` |

**Output** — `AuditModel` (cung tham chieu `audit` dau vao sau khi mutate, khong tao instance moi tru khi `audit` ban dau la `null`).

**Dieu kien xu ly** — `switch (employee is not null)`:
- `false` (employee null): goi `SetAuditDefaultWithOwner(audit, defaultName)`.
- `true`: gan **de** (khong `??=`, luon ghi de bat ke gia tri cu) 6 truong cua `audit.CreatorInfo`: `Code = employee.Code`, `Email = employee.Email`, `Name = employee.UserName`, `TitleCode = employee.TitleCode`, `Organization = employee.OrganizationCode`, `BranchId = employee.InsideBranchId`, `LocationId = employee.InsideLocationId`. **Khong** gan `RegionId`, `Role`, `RolesSR`, `RolesFTel`, `ConcurrentAreas` tu `employee` (nhung `GetAllEmployeeByFilterResponse` cung khong co field tuong ung cho `RolesSR`/`RolesFTel`/`ConcurrentAreas`; co `InsideRegionId` nhung khong duoc doc o day).

**Side effect** — **Mutate `audit.CreatorInfo`** truc tiep (tham chieu, khong sao chep). Neu `audit.CreatorInfo` la `null` khi `employee is not null`, dong `audit.CreatorInfo.Code = ...` se nem `NullReferenceException` — tuy nhien `audit ??= new()` chi dam bao `audit` khong null, **khong** dam bao `audit.CreatorInfo` khong null (neu caller tu tao `AuditModel` va gan `CreatorInfo = null` truoc khi goi `SetAudit`, day la rui ro thuc su — xem "Gioi han").

**Error handling** — Khong co try/catch; khong bat `NullReferenceException` neu roi vao truong hop tren.

**Khi nao NEN dung** — Khi da co san ket qua tra cuu nhan vien (`GetAllEmployeeByFilterResponse`) tu nguon khac (vi du HR service) va muon dung no lam nguon `CreatorInfo` thay cho claim.

**Khi nao KHONG dung** — Khi chi co claim cua `HttpContext` hien tai — nen dung `GetAuditCurrentAsync` (tu dong goi qua `GetAuditCurrentUser`).

**Gioi han** — (1) Method `protected static` — chi goi duoc tu ben trong `UserAudit` hoac lop con ke thua, khong the goi truc tiep tu ngoai qua `IUserAudit`. (2) Rui ro `NullReferenceException` neu `audit.CreatorInfo` la `null` va `employee` khac `null` (xem tren). (3) Ghi de hoan toan, khong merge — cac gia tri cu cua `CreatorInfo` (neu co, vi du tu `GetAuditCurrentUser`) bi mat cho 6 field ke tren.

### 2.6 SetAudit(AuditModel audit, CreatorInfo creatorInfo, string defaultName = "", CancellationToken cancellationToken = default)

**Signature**
```csharp
protected static AuditModel SetAudit(AuditModel audit,
                                     CreatorInfo creatorInfo,
                                     string defaultName = "",
                                     CancellationToken cancellationToken = default)
```
**Muc dich** — Tuong tu 2.5 nhung nhan truc tiep mot `CreatorInfo` da dung san (thay vi `GetAllEmployeeByFilterResponse`) va gan **thay the toan bo** `audit.CreatorInfo`.

**Input hop le** — Giong 2.5 ve `audit`/`defaultName`/`cancellationToken`; tham so thu 2 la `creatorInfo` (`CreatorInfo`, khong validate field con.

**Output** — `AuditModel` (cung tham chieu `audit`).

**Dieu kien xu ly** — `switch (creatorInfo is not null)`:
- `false`: goi `SetAuditDefaultWithOwner(audit, defaultName)`.
- `true`: `audit.CreatorInfo = creatorInfo;` — **thay the toan bo tham chieu**, khong phai gan tung field.

**Side effect** — Mutate `audit` (gan lai property `CreatorInfo`, khong mutate `creatorInfo` dau vao).

**Error handling** — Khong co try/catch. Khong co rui ro `NullReferenceException` nhu 2.5 vi day la phep gan tham chieu, khong truy cap field con cua `audit.CreatorInfo` cu.

**Khi nao NEN dung** — Khi lop con da tu dung mot `CreatorInfo` hoan chinh tu nguon bat ky va chi muon gan vao `AuditModel` co san (vi du de giu nguyen `Ip`/`Device`/`Method`/`Address` cua `audit` nhung thay `CreatorInfo`).

**Khi nao KHONG dung** — Khi chi muon cap nhat mot vai field cua `CreatorInfo` (ham nay thay the toan bo object, khong merge).

**Gioi han** — `protected static` — cung han che pham vi nhu 2.5. Khong co overload nao trong repo nay thuc su goi ham nay (chi `SetAudit(AuditModel, GetAllEmployeeByFilterResponse, ...)` duoc `GetAuditCurrentAsync` su dung) — muc dich su dung thuc te (lop con nao goi ham nay) **khong xac dinh duoc tu source code trong repo nay**.

### 2.7 SetAuditDefaultWithOwner(AuditModel audit, string defaultName)

**Signature**
```csharp
private static AuditModel SetAuditDefaultWithOwner(AuditModel audit, string defaultName)
```
**Muc dich** — Logic fallback dat `CreatorInfo.Name` khi khong co nguon du lieu nhan vien nao khac (khong co `employee`/`creatorInfo`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `audit` | `AuditModel` | Co (khong optional) | Gia dinh `audit.CreatorInfo` va `audit.CreatorInfo.Name` khong `null` (xem "Gioi han") | Khong co |
| `defaultName` | `string` | Co (khong optional) | Kiem tra `IsNullOrWhiteSpace` | Khong co |

**Output** — `AuditModel` (cung tham chieu `audit`, da mutate `CreatorInfo.Name`).

**Dieu kien xu ly** —
1. Neu `defaultName` khong rong/whitespace -> `audit.CreatorInfo.Name = defaultName` (ghi de **vo dieu kien**, bat ke `Name` hien tai la gi).
2. Neu khong -> kiem tra `audit.CreatorInfo.Name.Equals(CommonBaseConstant.Anonymous, StringComparison.CurrentCultureIgnoreCase)`; neu dung -> gan `audit.CreatorInfo.Name = "SYSTEM-SR"` (chuoi hang-code, khong phai hang so dat ten trong `CommonBaseConstant`).
3. Neu khong khop ca hai dieu kien tren -> giu nguyen `Name` hien tai.

**Side effect** — Mutate `audit.CreatorInfo.Name`.

**Error handling** — Khong co try/catch. Neu `audit.CreatorInfo` la `null`, dong `audit.CreatorInfo.Name.Equals(...)` (nhanh `else if`) se nem `NullReferenceException`.

**Khi nao NEN dung** — Chi goi noi bo tu `SetAudit` (ca 2 overload) khi nhanh "khong co nguon du lieu" duoc chon.

**Khi nao KHONG dung** — Khong goi duoc tu ngoai (`private`).

**Gioi han** — (1) `"SYSTEM-SR"` la chuoi hard-code ngay trong than ham, khong dua vao hang so chung (`CommonBaseConstant`) — neu can doi ten he thong nay phai sua truc tiep tai `UserAudit.cs:291`. (2) So sanh case-insensitive theo `CurrentCulture` (khong phai `Ordinal`) — co the cho ket qua khac nhau tuy locale server, du voi chuoi ASCII don gian ("Anonymous") rui ro nay thap. (3) Neu goi qua `GetAuditCurrentAsync` voi nguoi dung **da xac thuc thuc su** (Name khac "Anonymous") nhung `defaultName` van duoc truyen khac rong, ten thuc cua nguoi dung se bi **ghi de** boi `defaultName` — day la hanh vi theo dung code hien tai (khong phai loi), nhung de gay nham lan cho nguoi goi khong doc ky.

### 2.8 AuditModel / CreatorInfo (record du lieu)

**Vai tro** — `AuditModel` la DTO duoc `IUserAudit.GetAuditCurrentAsync` tra ve va duoc tang du lieu (`WriteDbContext`, `CoreSQL`, `CoreMongoDB`, `ProjectToExtensions`) doc lai de dien cot audit. Khong co method/logic ben trong — chi property `get; set;`.

**Bang thuoc tinh `AuditModel`** (`AuditModel.cs:3-14`)

| Thuoc tinh | Kieu | Gia tri mac dinh | Y nghia (theo comment/context) |
|---|---|---|---|
| `Ip` | `string` | `null` (khong khoi tao) | Dia chi IP cua request, do `GetAuditCurrentUser` dien |
| `Device` | `string` | `null` | User-Agent cua request |
| `Method` | `string` | `null` | HTTP method (GET/POST/...) |
| `Address` | `string` | `null` | URL day du cua request (`GetEncodedUrl()`) |
| `CreatorInfo` | `CreatorInfo` | `null` (khong khoi tao san — khac voi cac list trong `CreatorInfo` deu co `= []`) | Thong tin nguoi thuc hien hanh dong |

**Bang thuoc tinh `CreatorInfo`** (`AuditModel.cs:16-89`)

| Thuoc tinh | Kieu | Gia tri mac dinh | Y nghia (theo XML doc trong code) |
|---|---|---|---|
| `Code` | `string` | `null` | "Mã định danh của người tạo." (`:22`) |
| `Name` | `string` | `null` | "Tên người tạo." (`:28`) |
| `Email` | `string` | `null` | "Email người tạo." (`:34`) |
| `Organization` | `string` | `null` | "Tên đơn vị/cơ quan của người tạo." (`:40`) |
| `Role` | `RoleSR?` | `RoleSR.ONLY_CREATE` | "Vai trò chính của người tạo trong hệ thống SR." (`:46`) |
| `RegionId` | `int?` | `null` | Comment ghi "ID chi nhánh nơi người tạo làm việc." (`:49`) — **trung voi comment cua `BranchId`** (`:55`), ro rang la loi copy-paste trong XML doc (xem muc 3 #4) |
| `BranchId` | `int?` | `null` | "ID chi nhánh nơi người tạo làm việc." (`:55`) |
| `LocationId` | `int?` | `null` | "ID địa điểm cụ thể của người tạo." (`:61`) |
| `TitleCode` | `string` | `null` | "Thông tin vai trò của nhân viên." (`:67`) |
| `RolesSR` | `List<string>` | `[]` | "Danh sách vai trò trong hệ thống SR mà người tạo có." (`:73`) |
| `RolesFTel` | `List<string>` | `[]` | "Danh sách vai trò trong hệ thống FTel mà người tạo có." (`:79`) |
| `ConcurrentAreas` | `List<GetAllConcurrentAreaEmployeeModel>` | `[]` | "Danh sách khu vực làm việc đồng thời của người tạo." (`:85`) |

**Muc su dung thuc te (doi chieu tang tieu thu)** — Chi 3 field `CreatorInfo.Name`, `CreatorInfo.Code`, `CreatorInfo.Organization` duoc `WriteDbContext.OnBeforeSaveChanges` doc de dien `CreatedUser*`/`ModifiedUser*` cua entity SQL (`WriteDbContext.cs:139-146`, xem doi chieu chi tiet o muc 3). `Ip`/`Device`/`Method`/`Address` cua `AuditModel` **khong** duoc doc o bat ky duong code dang hoat dong nao trong `WriteDbContext` (chi xuat hien trong khoi comment "NOT SUPPORT").

**Gioi han** — `AuditModel.CreatorInfo` khong co gia tri mac dinh (khac voi cac list con ben trong `CreatorInfo`) — neu code tu tao `new AuditModel()` ma khong gan `CreatorInfo`, moi truy cap `audit.CreatorInfo.X` se nem `NullReferenceException` (xem rui ro nay lai xuat hien o muc 2.5/2.7).

### 2.9 SnapshotAuditModel (record du lieu)

**Signature**
```csharp
public record SnapshotAuditModel
{
    public CreatorInfo Creator { get; init; }
    public string TableName { get; init; }
    public string Ip { get; init; }
    public string Address { get; init; }
    public string Device { get; init; }
    public string Method { get; init; }
    public Dictionary<string, object> KeyValues { get; } = [];
    public Dictionary<string, object> OldValues { get; } = [];
    public Dictionary<string, object> NewValues { get; } = [];
    public List<string> ChangedColumns { get; } = [];
    public List<PropertyEntry> TemporaryProperties { get; } = [];

    public bool HasTemporaryProperties => TemporaryProperties.Count is not 0;
}
```
**Muc dich (theo thiet ke suy ra tu ten field)** — Luu "anh chup" 1 thay doi entity: ten bang, khoa chinh (`KeyValues`), gia tri truoc/sau (`OldValues`/`NewValues`), danh sach cot da doi (`ChangedColumns`), cung thong tin nguoi thuc hien va request (`Creator`/`Ip`/`Address`/`Device`/`Method`).

**Tinh trang thuc te trong repo nay** — Day la kieu tra ve cua `WriteDbContext.DetectChangesAudit(AuditModel)` (`WriteDbContext.cs:192`), nhung than ham nay: (1) tra `[]` ngay khi `audit is null` (`:194-197`); (2) toan bo logic con lai — bao gom moi noi co the tao instance `SnapshotAuditModel`/tuong duong — nam trong khoi comment `#region NOT SUPPORT` (`:201-353`); (3) cau lenh cuoi ham luon `return [];` (`:355`, dong code that thi hanh — phan `auditEntries.Where(...)` bi comment). Ket qua: **khong co bat ky dong code dang hoat dong nao trong repo nay khoi tao `SnapshotAuditModel`** — day la kieu du lieu "chet" (dead type) tai thoi diem audit commit `89c1ce9`.

**Input hop le / Output / Dieu kien xu ly / Side effect / Error handling** — Khong ap dung: day la record thuan property, khong co method/logic hanh vi rieng ngoai `HasTemporaryProperties` (computed property don gian, khong nem loi, khong side effect).

**Khi nao NEN dung** — Khong xac dinh duoc tu source code — hien khong co nguon nao trong repo tao instance de dung.

**Khi nao KHONG dung** — Khong nen dua vao kieu nay de doc "lich su thay doi" trong runtime hien tai — no khong duoc dien du lieu boi bat ky luong nao dang hoat dong.

**Gioi han** — Phu thuoc `Microsoft.EntityFrameworkCore.ChangeTracking.PropertyEntry` cho `TemporaryProperties` — dieu nay lam `SnapshotAuditModel` (nam trong `FTELSRCore.Shared`, dung chung ca SQL va Mongo) rang buoc vao EF Core dau ca khi khong lien quan MongoDB.

### 2.10 GetAllConcurrentAreaEmployeeModel (record du lieu)

**Signature**
```csharp
public record GetAllConcurrentAreaEmployeeModel
{
    public int? BranchId { get; set; }
    public int? LocationId { get; set; }
}
```
**Muc dich** — DTO don gian bieu dien 1 "dia ban kiem nhiem" (chi nhanh + dia diem) cua nhan vien, dung lam (1) phan tu cua `IUserAudit.ConcurrentAreas`/`CreatorInfo.ConcurrentAreas`, va (2) kieu deserialize JSON cho gia tri claim `SR.ConcurrentArea` (`UserAudit.cs:78`).

**Input hop le / Output** — Khong co method; chi 2 property `int?` co `get`/`set`, khong co XML doc, khong co validate rieng (khong rang buoc `BranchId`/`LocationId` phai dương hay khac null dong thoi).

**Dieu kien xu ly / Side effect / Error handling** — Khong ap dung (property thuan).

**Khi nao NEN dung** — Khi can bieu dien/nhan JSON mot dia ban kiem nhiem tu claim hoac tu API tra cuu nhan vien.

**Khi nao KHONG dung** — Khong dung de luu thong tin dia ban day du (khong co ten chi nhanh/dia diem, chi co ID).

**Gioi han** — Khong co rang buoc logic nao trong kieu nay; toan bo validate (JSON hop le, `BranchId`/`LocationId` co y nghia) nam ben ngoai (`JSonTryParse`, phia goi).

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `UserAudit.RegionId` duoc parse tu claim `"RegionId"` trong constructor, nhung `GetAuditCurrentUser` **khong** gan gia tri nay vao `CreatorInfo.RegionId` khi dung `AuditModel` (chi gan `BranchId`, `LocationId`, khong co `RegionId`) | `UserAudit.cs:102` (khai bao property) so voi `UserAudit.cs:166-182` (khoi tao `CreatorInfo` — khong co dong `RegionId = RegionId`) | Moi `AuditModel` tao ra tu `GetAuditCurrentAsync` luon co `CreatorInfo.RegionId = null`, du claim `RegionId` cua nguoi dung co gia tri hop le. Ket hop voi phat hien da co trong `Data-SQL-UnitOfWork-DbContexts.md` (dong 901: "`OnBeforeSaveChanges` khong gan `CreatedUserRegionId`/... du `CreatorInfo` co san `RegionId`") thi `RegionId` la truong **hai lan bi bo qua**: khong duoc UserAudit dien vao, va ngay ca khi duoc dien thu cong thi tang SQL cung khong doc |
| 2 | Claim type cua `BranchId`, `LocationId`, `TitleCode`, `Organization` duoc `UserAudit` doc bang `nameof(PropertyName)` thay vi tham chieu truc tiep hang so tuong ung trong `ClaimTypesConstant` (`ClaimTypesConstant.BranchId`, `.TitleCode`, `.LocationId`, `.Organization`) — trong khi `RolesSR`/`FTelRoles`/`Permissions`/`ConcurrentArea` lai dung hang so truc tiep | `UserAudit.cs:37-70` (dung `nameof`) so voi `UserAudit.cs:73,88,93,98` (dung `ClaimTypesConstant.X`); doi chieu voi `Constants/ClaimTypesConstant.cs:9,11,13,21` (hang so co san, gia tri trung voi `nameof` tuong ung) | Hien tai gia tri chuoi trung nhau nen khong co loi chuc nang, nhung neu doi ten property (`BranchId` -> khac) ma khong sua claim type thuc te do JWT phat hanh, `nameof` se doi theo ten property con hang so `ClaimTypesConstant` thi khong — 2 co che khong lien ket voi nhau bang compiler, de gay lech tham lang le. Rieng `RegionId` khong co hang so tuong ung nao trong `ClaimTypesConstant` |
| 3 | Method 2.6 `SetAudit(AuditModel, CreatorInfo, string, CancellationToken)` khong duoc goi boi bat ky method nao khac trong `UserAudit.cs` (`GetAuditCurrentAsync` chi goi overload nhan `GetAllEmployeeByFilterResponse`) | `UserAudit.cs:141-152` (noi duy nhat goi `SetAudit`, dung overload `GetAllEmployeeByFilterResponse`) so voi khai bao overload `CreatorInfo` tai `UserAudit.cs:247-273` | Overload nay chi co the duoc dung boi lop con ke thua `UserAudit` (do la `protected static`) — khong co lop con nao trong repo nay, nen kha nang su dung thuc te **khong xac dinh duoc tu source code trong repo nay** |
| 4 | XML doc cua `CreatorInfo.RegionId` ("ID chi nhánh nơi người tạo làm việc.") trung chinh xac voi XML doc cua `CreatorInfo.BranchId`, ro la loi copy-paste (RegionId phai la "vùng", khong phai "chi nhánh") | `Models/Audits/AuditModel.cs:49` (RegionId) va `:55` (BranchId) | Sai lech tai lieu nguon (khong sai logic runtime) — nguoi doc XML doc/IntelliSense co the hieu nham `RegionId` va `BranchId` la cung mot khai niem |
| 5 | `SetAudit(AuditModel, GetAllEmployeeByFilterResponse, ...)` truy cap `audit.CreatorInfo.Code = ...` truc tiep (khong `?.`) trong nhanh `employee is not null`; nen ham se nem `NullReferenceException` neu caller tu tao `AuditModel` voi `CreatorInfo = null` roi goi `SetAudit` voi `employee` khac null. Tuong tu, `SetAuditDefaultWithOwner` truy cap `audit.CreatorInfo.Name.Equals(...)` khong qua `?.` | `UserAudit.cs:221-229` (SetAudit) va `UserAudit.cs:288-289` (SetAuditDefaultWithOwner) | Rui ro crash chi xay ra khi caller tu dung `AuditModel` (khong qua `GetAuditCurrentUser`, vi ham do luon dien `CreatorInfo`) va co y/vo y de `CreatorInfo = null`. Trong luong chinh (`GetAuditCurrentAsync`) khong xay ra vi `GetAuditCurrentUser` luon tra `AuditModel` co `CreatorInfo` day du |
| 6 | `SnapshotAuditModel` duoc dinh nghia day du (`KeyValues`, `OldValues`, `NewValues`, `ChangedColumns`, `TemporaryProperties`, `HasTemporaryProperties`) nhung khong co dong code dang hoat dong nao trong repo nay khoi tao instance cua no — toan bo logic lien quan nam trong khoi comment `NOT SUPPORT` cua `WriteDbContext.DetectChangesAudit` | `Models/Audits/SnapshotAuditModel.cs:5-20`; `WriteDbContext.cs:192-355` (dac biet `:355` `return [];`) | Xac nhan lai (khong mau thuan) voi phat hien da co trong `Data-SQL-CoreSQL.md` (#28) va `Data-SQL-UnitOfWork-DbContexts.md` (dong 948: "SnapshotAuditModel ... khong bao gio duoc khoi tao"). Doi voi module Audit dang tai lieu hoa: day la mot DTO hoan toan chet, khong co lien ket runtime nao voi `IUserAudit`/`UserAudit` |
| 7 | Doi chieu nguoc voi 8 file KB cu (theo yeu cau rieng cua module): grep `IUserAudit`/`UserAudit`/`GetAuditCurrentAsync` tren ca 8 file (`Utilizes-CallApiWithHttp.md`, `Utilizes-CallApi.md`, `Data-MongoDB-CoreMongoDB.md`, `Data-SQL-CoreSQL.md`, `Data-SQL-CoreSQL-TwoEntity.md`, `Data-SQL-UnitOfWork-DbContexts.md`, `Data-SQL-Dapper.md`, `Data-SQL-Resilience.md`) tra ve **0 ket qua** — khong file nao mo ta `IUserAudit`/`UserAudit` | Ket qua grep tren toan bo `docs/knowledge-base/*.md` lien quan | Khong co gi de doi chieu ve nguon goc `AuditModel`. Rieng phan mo ta **cach `AuditModel`/`CreatorInfo` duoc CoreSQL/CoreMongoDB/WriteDbContext *tieu thu*** (field nao duoc doc, field nao bi bo qua, gia tri fallback) trong `Data-SQL-CoreSQL.md`, `Data-SQL-CoreSQL-TwoEntity.md`, `Data-MongoDB-CoreMongoDB.md`, `Data-SQL-UnitOfWork-DbContexts.md` deu **khop chinh xac** voi cau truc `AuditModel`/`CreatorInfo` doc truc tiep tu `AuditModel.cs` trong tai lieu nay (ten field, kieu, gia tri fallback `Anonymous`/`AnonymousCode`/`OrganizationForISC`) — **khong phat hien mo ta sai/thieu nao can ghi nhan o day** |
| 8 | `UserAudit` la `record` nhung field private `_contextAccessor` (kieu `Lazy<IHttpContextAccessor>`) tham gia vao value-equality tu sinh cua record (vi la field instance, khong chi property) | `UserAudit.cs:10-14` | Hai `UserAudit` co cung du lieu claim nhung khac instance `Lazy<IHttpContextAccessor>` se khong `Equals`/`==` nhau — value-equality cua record gan nhu khong co tac dung thuc te cho kieu nay (xem chi tiet muc 2.1) |
| 9 | `TitleCode` va `Organization` dung cung 1 pattern gan gia tri trong constructor nhung `setDataDefault` truyen cho `ConvertClaimsPrincipalToData` khac nhau: `TitleCode` dung `string.Empty`, `Organization` dung `CommonBaseConstant.OrganizationForISC` ("FTEL"). Ket qua: khi claim "TitleCode" vang mat -> `TitleCode = null`; nhung khi claim "Organization" vang mat -> `Organization = "FTEL"` (khong phai `null` nhu `TitleCode`) | `UserAudit.cs:37-42` (TitleCode) so voi `UserAudit.cs:44-49` (Organization) | Bat ky code tieu thu nao dua vao gia dinh "`Organization` la `null` khi nguoi dung khong co claim Organization" (tuong tu cach `TitleCode` hoat dong) se sai — `Organization` se ngam dinh la `"FTEL"` trong truong hop do. Day la hanh vi thuc te cua code hien tai (co the la chu dich vi `OrganizationForISC` la ten don vi mac dinh cua he thong ISC), nhung khong doi xung voi `TitleCode` va de gay hieu nham neu doc luot code |
| 10 | Constructor `UserAudit` co the ghi log ra console (`Console.WriteLine`) khi claim `SR.ConcurrentArea` chua chuoi JSON khong hop le (khac rong/`"null"`/`"{}"`/`"[]"` nhung deserialize loi) — `item.Value.JSonTryParse(...)` khong truyen `logger` nen roi vao nhanh `default` cua `JSonTryParse`, goi `CommonBaseConstant.ConfigLoggerExceptionByConsole(...)` -> `Console.WriteLine(...)` | `UserAudit.cs:78` (goi `JSonTryParse` khong logger); `Helpers/JSonParseHelpers.cs:149-195` (catch + goi `ConfigLoggerExceptionByConsole` khi `logger` null); `Constants/CommonBaseConstant.cs:60-64` (`ConfigLoggerExceptionByConsole` thuc hien `Console.WriteLine`) | Constructor **khong hoan toan "khong ghi log/goi ngoai"** nhu mo ta truoc day — voi du lieu claim bi hong (token bi can thiep hoac loi phat hanh token), moi request tao `UserAudit` moi (neu DI la scoped/transient) se in 1 dong `[ERR] [LOGCONSOLE]...` ra console. Day la side effect co dieu kien, chi xay ra voi input khong hop le, nhung truoc day tai lieu khang dinh tuyet doi "khong ghi log" la khong chinh xac |
| 11 | XML doc cua `IUserAudit.LocationId` ghi "Thông tin vùng." (nghia: thong tin VUNG/khu vuc) — trung ngu nghia voi khai niem "vùng" ma `RegionId` dai dien (xem `CreatorInfo.RegionId`/`ClaimTypesConstant`), **khong phai** "địa điểm" (location) nhu ten property va nhu comment cua `CreatorInfo.LocationId` ("ID địa điểm cụ thể của người tạo.") dang mo ta | `Audits/IUserAudit.cs:11-15` (`LocationId` doc "Thông tin vùng.") so voi `Models/Audits/AuditModel.cs:60-64` (`CreatorInfo.LocationId` doc "ID địa điểm cụ thể...") | Loi tai lieu nguon (XML doc/IntelliSense sai), tuong tu loai loi copy-paste da ghi nhan o muc 3 #4 cho `CreatorInfo.RegionId`/`BranchId`, nhung day la mot vi tri khac (`IUserAudit.cs`) — khong anh huong hanh vi runtime, chi gay hieu nham cho nguoi doc IntelliSense/XML doc cua interface |
