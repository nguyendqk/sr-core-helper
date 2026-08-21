# Models - Paging/HealthCheck/Responses

> Nguon: FTELSRCore.Shared/Models/Pagings/CursorPayloadModel.cs, FTELSRCore.Shared/Models/Pagings/PagingModel.cs, FTELSRCore.Shared/Models/HealthChecks/HealthCheckModel.cs, FTELSRCore.Shared/Responses/GetAllEmployeeByFilterResponse.cs
> Loai: class (`CursorPayloadModel`) + abstract record/record (`PagingModel.cs`) + record (`HealthCheckModel.cs`) + record (`GetAllEmployeeByFilterResponse.cs`)
> Cap nhat theo commit: 89c1ce9

## 1. Tong quan

Module nay gom 4 file thuoc `FTELSRCore.Shared`, chia thanh 3 nhom khong lien quan truc tiep ve nghiep vu nhung deu la cac "kieu du lieu dung chung" (shared model/DTO) duoc export ra ngoai thu vien:

1. **Paging (`FTELSRCore.Shared/Models/Pagings/*.cs`)** - dinh nghia hai co che phan trang khac nhau:
   - Phan trang theo so trang (page-number paging): `Paging`, `PagingModel`, `PagingMongoDBModel`.
   - Phan trang theo con tro (cursor-based paging): `CursorPayloadModel` (payload thuc su nam trong cursor, co logic Encode/Decode), `PagingCursorModel` (request tu client) va `PageInfoCursorResponseModel` (metadata tra ve cho client).
2. **HealthCheck (`FTELSRCore.Shared/Models/HealthChecks/HealthCheckModel.cs`)** - DTO bao boc ket qua health-check, gan voi kieu `HealthStatus` cua `Microsoft.Extensions.Diagnostics.HealthChecks`.
3. **Responses (`FTELSRCore.Shared/Responses/GetAllEmployeeByFilterResponse.cs`)** - DTO phang (flat DTO) mo ta mot nhan vien, dung khi tra cuu danh sach nhan vien theo filter.

Ca 3 nhom deu la **kieu du lieu thuan (data holder)** - namespace la `FTELSRCore.Models.Pagings`, `FTELSRCore.Models.HealthChecks`, `FTELSRCore.Responses` (khac voi ten thu muc vat ly, do `RootNamespace` cua project la `FTELSRCore`, xem `FTELSRCore.Shared.csproj:8`). Ngoai `CursorPayloadModel.Encode()`/`Decode()`, khong co method nao khac chua logic - cac property con lai chi la getter/setter (auto-property), khong co validate/tinh toan ben trong.

**Ket qua ra soat trong toan repo:** ca 4 kieu deu **khong duoc bat ky class nao khac trong repo `sr-core-helper` ke thua, khoi tao hay tham chieu truc tiep**, ngoai chinh file dinh nghia cua chung va (voi `GetAllEmployeeByFilterResponse`) file `FTELSRCore.Shared/Audits/UserAudit.cs` (xem muc 1.2). Vi day la thu vien "Shared" (dong goi thanh `FTELSRCore.Shared` NuGet package), cac kieu nay ranh danh cho service khac ben ngoai repo nay tieu thu — pham vi su dung thuc te (ai goi `Encode()`, ai deserialize `HealthCheckModel`, ai tra ve `GetAllEmployeeByFilterResponse`) **khong xac dinh duoc tu source code trong repo nay**.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| `CursorPayloadModel.Encode()`/`Decode(string)`: ma hoa/giai ma mot payload cursor (enum loai cursor, StatusId, CreatedDate, PageSize, PageNumber) sang/tu chuoi Base64 chua JSON (`CursorPayloadModel.cs:24-47`). | Khong tu sinh `Cursor` cho client — `PagingCursorModel.Cursor` chi la `string` do client gui len, khong co lien ket code nao trong repo nay chung minh no duoc gan tu `CursorPayloadModel.Encode()` (xem muc 3 #1). |
| Cung cap 2 model phan trang theo so trang co san field mac dinh (`PageNumber = 1`, `PageSize = 10`) de cac request DTO khac ke thua (`PagingModel.cs:19-24`). | Khong validate `PageNumber`/`PageSize`/`FromDate`/`ToDate` — hoan toan la property tho, khong co logic rang buoc trong file nay. |
| Cung cap khung metadata chuan cho response phan trang kieu cursor (`HasPreviousPage`, `HasNextPage`, `StartCursor`, `EndCursor`) dung theo phong cach Relay/GraphQL cursor connection (`PagingModel.cs:33-62`). | Khong co logic tinh `HasPreviousPage`/`HasNextPage`/`StartCursor`/`EndCursor` — day chi la cho chua du lieu, logic tinh (neu co) nam o noi khac ngoai 4 file duoc doc. |
| `HealthCheckModel`/`IndividualHealthCheckResponse`: cung cap cau truc DTO de bieu dien ket qua tong hop cua health check (trang thai, tong thoi gian, danh sach ket qua tung check) (`HealthCheckModel.cs:5-23`). | Khong co ham mapping tu `Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport` sang `HealthCheckModel` trong 4 file nay; khong tim thay noi nao trong repo goi/khoi tao 2 record nay. |
| `GetAllEmployeeByFilterResponse`: cung cap DTO phang gom thong tin nhan vien, don vi, chuc danh, thong tin "Inside" (he thong ngoai) va `TotalRows`. | Khong co logic anh xa/query — day chi la "hinh dang" du lieu tra ve, khong co code nao trong 4 file xac dinh no duoc query the nao (Dapper/SQL/API nao tra ve). |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `System.Text.Encoding`/`System.Convert` (BCL) | `CursorPayloadModel.Encode()`/`Decode()` dung `Encoding.UTF8` + `Convert.ToBase64String`/`Convert.FromBase64String` de chuyen doi qua lai giua JSON va Base64 (`CursorPayloadModel.cs:24-47`). |
| `FTELSRCore.Helpers.JSonParseHelpers` (extension `ToJSon`, `JSonTryParse`) | `CursorPayloadModel.Encode()` goi `this.ToJSon()`; `CursorPayloadModel.Decode()` goi `json.JSonTryParse(out CursorPayloadModel result)` — xem chi tiet cau hinh serializer trong `JSonParseHelpers.cs:207-227` (anh huong truc tiep den dinh dang JSON, xem muc 2.2/2.3). |
| `Microsoft.Extensions.Diagnostics.HealthChecks` (`HealthStatus`) | `IndividualHealthCheckResponse.Status` dung enum `HealthStatus` cua thu vien health-check chuan cua .NET (`HealthCheckModel.cs:1,18`). |
| `FTELSRCore.Shared/Audits/UserAudit.cs` (`SetAudit(AuditModel, GetAllEmployeeByFilterResponse, string, CancellationToken)`) | Noi DUY NHAT trong repo doc cac property cua `GetAllEmployeeByFilterResponse` (`Code`, `Email`, `UserName`, `TitleCode`, `OrganizationCode`, `InsideBranchId`, `InsideLocationId`) de gan vao `AuditModel.CreatorInfo` (`UserAudit.cs:221-229`). Da doi chieu: cac ten property nay khop chinh xac voi dinh nghia trong `GetAllEmployeeByFilterResponse.cs` — khong phat hien sai lech so voi mo ta trong file KB `Audits-UserAudit.md`. |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `CursorPayloadModel.TypeCursorPayload` (enum: `NextPage = 1`, `PreviousPage = 2`) | Enum | Loai cursor — tien hay lui trang. |
| `CursorPayloadModel` (`TypeCursor`, `StatusId`, `CreatedDate`, `PageSize`, `PageNumber`) | Property | Du lieu tho se duoc dong goi vao cursor. |
| `CursorPayloadModel.Encode()` | Method | Ma hoa instance hien tai thanh chuoi cursor (Base64 cua JSON). |
| `CursorPayloadModel.Decode(string cursor)` | Method (static) | Giai ma chuoi cursor tro lai `CursorPayloadModel`, tra `null` neu loi. |
| `Paging` (abstract record: `PageNumber = 1`, `PageSize = 10`) | Record (base) | Khung phan trang theo so trang, dung chung cho SQL/generic. |
| `PagingModel` (abstract record : `Paging`; them `FromDate`, `ToDate`, `Search`) | Record | Khung request phan trang + loc theo khoang ngay (dang string) + tu khoa tim kiem, cho nguon du lieu dang SQL/generic. |
| `PagingMongoDBModel` (abstract record : `Paging`; them `StartDateTime`, `EndDateTime`, `Search`) | Record | Tuong tu `PagingModel` nhung dat ten field ngay theo phong cach MongoDB. |
| `PagingCursorModel` (record: `Cursor`, `PageSize = 20`) | Record | Request phan trang kieu cursor — client gui `Cursor` (chuoi da encode) + `PageSize` mong muon. |
| `PageInfoCursorResponseModel` (record: `HasPreviousPage`, `HasNextPage`, `StartCursor`, `EndCursor`) | Record | Metadata tra ve cho client sau khi phan trang kieu cursor. |
| `HealthCheckModel` (record: `Status`, `TotalDuration`, `Checks`) | Record | DTO tong hop ket qua health check toan he thong. |
| `IndividualHealthCheckResponse` (record: `Name`, `Status`, `Duration`, `Exception`) | Record | DTO ket qua cua tung health check don le. |
| `GetAllEmployeeByFilterResponse` (record, 36 property) | Record | DTO phang thong tin 1 nhan vien tra ve tu truy van loc nhan vien. |

## 2. Chi tiet API

### 2.1 CursorPayloadModel — cau truc du lieu

**Signature**
```csharp
public class CursorPayloadModel
{
    public enum TypeCursorPayload
    {
        NextPage = 1,
        PreviousPage = 2
    }

    public TypeCursorPayload TypeCursor { get; set; }
    public int StatusId { get; set; }
    public DateTime CreatedDate { get; set; }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
}
```
**Muc dich** - Day la "payload" thuc su duoc dong goi (encode) vao chuoi cursor ma client cam giu. Khac voi phan trang theo so trang (chi truyen `PageNumber`/`PageSize` tho), o day toan bo trang thai phan trang (huong di, `StatusId` dang loc, `CreatedDate` moc thoi gian, kich thuoc trang, so trang) duoc gop vao MOT chuoi opaque de client gui lai ma khong can tu quan ly cac tham so nay.

**Cac field**

| Tham so | Kieu | Y nghia (suy tu ten + cach dung trong Encode/Decode) |
|---|---|---|
| `TypeCursor` | `CursorPayloadModel.TypeCursorPayload` | Xac dinh cursor nay dung de lay trang KE TIEP (`NextPage = 1`) hay trang TRUOC (`PreviousPage = 2`) (`CursorPayloadModel.cs:7-12`). |
| `StatusId` | `int` | Gia tri filter theo trang thai (theo ten field). Khong co doan code nao trong repo gan/doc field nay ngoai dinh nghia property — **y nghia nghiep vu chinh xac va noi khoi tao thuc te khong xac dinh duoc tu source code trong repo nay**. |
| `CreatedDate` | `DateTime` | Moc thoi gian tao (theo ten field), co the dung nhu "anchor" de query "cac ban ghi sau/truoc thoi diem nay" trong truy van cursor. Cung khong co noi nao trong repo gan gia tri thuc te — **khong xac dinh duoc tu source code**. |
| `PageSize` | `int` | Kich thuoc trang mong muon khi tiep tuc phan trang tu cursor nay. |
| `PageNumber` | `int` | So trang hien tai/lien quan, duoc luu kem trong payload (ket hop mo hinh hybrid: cursor van mang theo so trang, khac voi cursor "thuan" chi dung khoa sort). |

**Gioi han** - Day la `class` (khong phai `record` nhu cac model khac trong module), khong override `Equals`/`GetHashCode`/`ToString`, khong co constructor tuy chinh — khoi tao qua object initializer thong thuong.

### 2.2 CursorPayloadModel.Encode()

**Signature**
```csharp
public string Encode()
```
**Muc dich** - Chuyen instance hien tai cua `CursorPayloadModel` thanh MOT chuoi duy nhat ("cursor") de tra ve cho client, dung lam `PageInfoCursorResponseModel.StartCursor`/`EndCursor` hoac de client gui lai qua `PagingCursorModel.Cursor` trong request sau.

**Dinh dang cursor (contract quan trong)** - Qua doc `CursorPayloadModel.cs:24-29` va `JSonParseHelpers.cs:19-31,207-213`:
1. Serialize instance sang JSON bang `System.Text.Json.JsonSerializer.Serialize` (qua extension `ToJSon()`), voi cau hinh `_defaultJsonOptions`: `PropertyNameCaseInsensitive = true`, `ReferenceHandler.IgnoreCycles`, bo qua property null khi ghi (`DefaultIgnoreCondition = WhenWritingNull`). **Khong co `PropertyNamingPolicy`** duoc thiet lap trong `_defaultJsonOptions`, nen ten field trong JSON giu dung PascalCase nhu ten C# (`TypeCursor`, `StatusId`, `CreatedDate`, `PageSize`, `PageNumber`) — VI DU: `{"TypeCursor":1,"StatusId":3,"CreatedDate":"2026-08-21T00:00:00","PageSize":20,"PageNumber":2}`.
2. Ma hoa chuoi JSON do sang bytes UTF-8 (`Encoding.UTF8.GetBytes`).
3. Ma hoa bytes do sang chuoi Base64 (`Convert.ToBase64String`).

Tom lai: **cursor = Base64( UTF8( JSON(CursorPayloadModel) ) )**. Day KHAC voi phan trang theo so trang (`Paging`/`PagingModel`/`PagingMongoDBModel`) — noi client tu truyen `PageNumber`/`PageSize` tho, khong ma hoa gi ca.

**Input hop le** - Khong nhan tham so; hoat dong tren state hien tai cua instance (`this`).

**Output** - `string` Base64. Khong co truong hop tra `null`/rong duoc mo ta rieng trong ham nay. `ToJSon()` co 2 nhanh xu ly loi khac nhau (`JSonParseHelpers.cs:33-61`): neu gap `NotSupportedException`, no fallback sang `Newtonsoft.Json.JsonConvert.SerializeObject(obj)` va van tra ve chuoi JSON hop le (khong rong); voi bat ky `Exception` khac, no log loi roi tra ve `string.Empty`. Chi trong nhanh thu hai, `Encode()` moi tiep tuc Base64-hoa chuoi rong do (khong co kiem tra rieng cho truong hop nay trong `CursorPayloadModel.cs`).

**Dieu kien xu ly** - Khong co nhanh re/dieu kien (guard clause) nao trong ham — luon thuc hien tuan tu ToJSon -> GetBytes -> ToBase64String.

**Side effect** - Khong co (khong ghi log/DB, khong goi ngoai, khong mutate state cua `this`).

**Error handling** - Ham nay khong co try/catch cua rieng no. `ToJSon()` tu bat exception noi bo va khong bao gio throw ra ngoai (`JSonParseHelpers.cs:33-61`): voi `NotSupportedException` no fallback sang serialize bang Newtonsoft.Json (van tra ve JSON hop le); voi cac `Exception` khac no log loi va tra ve `string.Empty`. Vi vay `Encode()` gan nhu khong bao gio throw ra ngoai trong dieu kien binh thuong, nhung ket qua co the la Base64 cua chuoi rong neu roi vao nhanh loi thu hai.

**Khi nao NEN dung** - Khi can tao gia tri cho `PageInfoCursorResponseModel.StartCursor`/`EndCursor` de tra ve cho client sau khi query mot trang du lieu.

**Khi nao KHONG dung** - Khong dung de luu tru lau dai hoac coi la bao mat: day chi la Base64 (khong ma hoa/khong ky), client hoac bat ky ai co the decode va doc duoc toan bo noi dung JSON ben trong (bao gom `StatusId`, `CreatedDate`).

**Gioi han** - Khong ky (sign) hay ma hoa (encrypt) cursor — client co the tu sua doi `StatusId`/`PageNumber`/... trong cursor roi Encode lai bang tay (vi day chi la Base64 cong khai), tiem an rui ro tampering neu server tin tuong tuyet doi vao noi dung cursor ma khong doi chieu lai voi context request khac.

### 2.3 CursorPayloadModel.Decode(string cursor)

**Signature**
```csharp
public static CursorPayloadModel Decode(string cursor)
```
**Muc dich** - Giai ma nguoc lai chuoi cursor (dinh dang tao boi `Encode()`, xem muc 2.2) tro ve instance `CursorPayloadModel`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `cursor` | `string` | Khong bat buoc ve mat kieu, nhung can chuoi Base64 hop le cua JSON dung dinh dang de tra ve khac `null` | `string.IsNullOrEmpty(cursor)` duoc kiem tra dau ham (`CursorPayloadModel.cs:33`); phan giai ma tiep theo nam trong `try/catch` bao toan bo (`CursorPayloadModel.cs:35-46`) | Khong co (tham so bat buoc truyen vao) |

**Output** - `CursorPayloadModel` hoac `null`:
- `cursor` la `null`/rong -> tra `null` ngay (`CursorPayloadModel.cs:33`).
- `cursor` khong phai Base64 hop le, hoac sau khi decode Base64+UTF8 khong phai JSON hop le, hoac JSON khong khop kieu `CursorPayloadModel` -> exception bi bat boi `catch` toan cuc -> tra `null` (`CursorPayloadModel.cs:43-46`).
- `cursor` hop le -> tra instance `CursorPayloadModel` da duoc gan gia tri tu JSON.
- **Truong hop dac biet**: neu `json.JSonTryParse(out result)` (`JSonParseHelpers.cs:149-195`) tra ve `false` (vi du JSON la `"null"`, `"{}"`, `"[]"`, rong, hoac deserialize loi) — `Decode()` **khong kiem tra gia tri `bool` tra ve** (dung `_ = json.JSonTryParse(...)`, `CursorPayloadModel.cs:39`) ma tra `result` truc tiep; do `CursorPayloadModel` la `class`, `result = default` trong truong hop that bai chinh la `null`, nen ket qua cuoi cung van la `null` — hanh vi dung, nhung viec discard gia tri `bool` la mot lua chon code co the gay nham lan khi doc.

**Dieu kien xu ly (theo thu tu thuc thi)**
1. Neu `cursor` la `null` hoac `""` -> tra `null` ngay, khong vao `try`.
2. Nguoc lai, vao `try`: decode Base64 -> bytes -> chuoi UTF-8 -> goi `JSonTryParse<CursorPayloadModel>` (deserialize bang `System.Text.Json` voi cau hinh `_jsonSerializerOptions`, gom them cac converter tuy chinh cho `bool/int/long/double/decimal/DateTime` va bien the nullable — `JSonParseHelpers.cs:214-227`).
3. Bat ky exception nao trong buoc 2 (bao gom `FormatException` tu `Convert.FromBase64String` khi chuoi khong dung Base64) deu roi vao `catch` va tra `null` (`CursorPayloadModel.cs:43-46`).

**Side effect** - Khong co (khong ghi log — khac voi `JSonTryParse` noi bo co the log neu duoc truyen `logger`, nhung `Decode()` goi `JSonTryParse` KHONG truyen `logger`, nen se dung nhanh log-to-console mac dinh trong `JSonParseHelpers.cs` khi co loi giai JSON, khong phai khong log gi ca — xem `JSonParseHelpers.cs:170-189`; rieng loi Base64/FormatException xay ra TRUOC khi goi `JSonTryParse` nen khong duoc log o dau ca, bi `catch` cua `Decode()` nuot hoan toan va im lang).

**Error handling** - `catch` khong khai bao kieu exception cu the (`catch { return null; }`, `CursorPayloadModel.cs:43-46}`) — bat TAT CA exception (bao gom ca loi khong mong doi ngoai `FormatException`) va luon tra `null`, khong throw lai, khong log. Day la thiet ke "fail-safe/silent" cho input tu client.

**Khi nao NEN dung** - Khi nhan `PagingCursorModel.Cursor` tu client va can giai ma lai thanh cac tham so phan trang de tiep tuc truy van.

**Khi nao KHONG dung** - Khong dung ket qua `null` de phan biet giua "cursor rong" va "cursor sai dinh dang" và "loi he thong khac" — ca 3 truong hop deu tra ve cung mot gia tri `null`, khong co ma loi/thong tin phan biet.

**Gioi han** - `catch` rong (bat `Exception` chung) che khuat moi loai loi thuc su (bug logic, loi converter, ...) thanh cung mot `null` — kho debug khi co loi he thong (khac voi loi du lieu client). Khong co gioi han so lan thu hay chong brute-force tren tham so `cursor`.

### 2.4 Paging / PagingModel / PagingMongoDBModel — phan trang theo so trang

**Signature**
```csharp
public abstract record Paging
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public abstract record PagingModel : Paging
{
    public string FromDate { get; set; }
    public string ToDate { get; set; }
    public string Search { get; set; } = string.Empty;
}

public abstract record PagingMongoDBModel : Paging
{
    public string StartDateTime { get; set; }
    public string EndDateTime { get; set; }
    public string Search { get; set; } = string.Empty;
}
```
**Muc dich** - Khung (base record) de cac DTO request phan trang cu the (o service/module khac, ngoai pham vi 4 file duoc doc) ke thua, tranh lap lai `PageNumber`/`PageSize`/khoang ngay/tu khoa tim kiem o moi noi.

**Input hop le / property**

| Property | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `Paging.PageNumber` | `int` | Khong (co default) | Khong co validate (khong check `> 0`) trong file nay | `1` (`PagingModel.cs:21`) |
| `Paging.PageSize` | `int` | Khong (co default) | Khong co validate (khong check gioi han max) trong file nay | `10` (`PagingModel.cs:23`) |
| `PagingModel.FromDate` | `string` | Khong | Khong validate dinh dang ngay — la `string` tho, khong phai `DateTime` | `null` |
| `PagingModel.ToDate` | `string` | Khong | Khong validate dinh dang ngay | `null` |
| `PagingModel.Search` | `string` | Khong | Khong validate | `string.Empty` |
| `PagingMongoDBModel.StartDateTime` | `string` | Khong | Khong validate dinh dang ngay | `null` |
| `PagingMongoDBModel.EndDateTime` | `string` | Khong | Khong validate dinh dang ngay | `null` |
| `PagingMongoDBModel.Search` | `string` | Khong | Khong validate | `string.Empty` |

**Output** - Khong ap dung (day la record du lieu, khong co method tra ve gia tri xu ly).

**Dieu kien xu ly / Side effect / Error handling** - Khong co (khong co logic, khong co method nao ngoai property tu sinh cua `record`).

**Khi nao NEN dung** - Khi mot request/query DTO khac trong he thong can chuan hoa cac field phan trang + loc theo khoang ngay (dang chuoi) + tu khoa tim kiem, va nguon du lieu la SQL (`PagingModel`) hoac MongoDB (`PagingMongoDBModel`, dat ten field theo phong cach Mongo).

**Khi nao KHONG dung** - Khi can phan trang kieu cursor (dung `PagingCursorModel`/`PageInfoCursorResponseModel` — muc 2.5/2.6) hoac khi can validate gia tri ngay thuc su (2 model nay khong parse/validate chuoi ngay).

**Gioi han**
- Ca 2 record deu la `abstract` va KHONG co class/record nao trong repo `sr-core-helper` ke thua chung — khong the xac nhan cach dung thuc te (field nao thuc su duoc bind tu request) tu source code trong repo nay.
- `FromDate`/`ToDate`/`StartDateTime`/`EndDateTime` deu la `string`, khong phai `DateTime`/`DateOnly` — viec parse (dinh dang nao, co the null hay khong, co throw khi sai dinh dang khong) hoan toan phu thuoc noi tieu thu (ngoai pham vi file nay), **khong xac dinh duoc tu source code**.
- `PagingModel` va `PagingMongoDBModel` gan nhu trung lap hoan toan ve cau truc (chi khac ten 2 field ngay) — khong co co che chia se logic ngoai ke thua chung `Paging`.

### 2.5 PagingCursorModel — request phan trang kieu cursor

**Signature**
```csharp
public record PagingCursorModel
{
    public string Cursor { get; set; }
    public int PageSize { get; set; } = 20;
}
```
**Muc dich** - DTO request ma client gui len khi phan trang kieu cursor: mang theo `Cursor` (chuoi da duoc encode — xem muc 2.2, tuy KHONG co bang chung code lien ket truc tiep giua field nay va `CursorPayloadModel.Encode()`, xem muc 3 #1) va `PageSize` mong muon cho lan tiep theo.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `Cursor` | `string` | Khong (khong co `[Required]` trong file nay) | Khong validate dinh dang/độ dai/Base64 trong record nay | `null` |
| `PageSize` | `int` | Khong | Khong validate gioi han | `20` |

**Output/Dieu kien xu ly/Side effect/Error handling** - Khong ap dung (khong co method, chi la data holder).

**Khi nao NEN dung** - Lam kieu tham so cho endpoint/query ho tro phan trang kieu cursor.

**Khi nao KHONG dung** - Khi API dung phan trang theo so trang (dung `Paging`/`PagingModel`/`PagingMongoDBModel` — muc 2.4).

**Gioi han** - Gia tri mac dinh `PageSize = 20` khac voi `Paging.PageSize` mac dinh la `10` — hai co che phan trang trong CUNG file khong dung chung 1 gia tri mac dinh; khong co giai thich nao trong code cho su khac biet nay.

### 2.6 PageInfoCursorResponseModel — metadata phan trang kieu cursor

**Signature**
```csharp
public record PageInfoCursorResponseModel
{
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
    public string StartCursor { get; set; }
    public string EndCursor { get; set; }
}
```
**Muc dich** - Theo dung XML doc trong code (`PagingModel.cs:35-61`): tra ve cho client biet co the lui/tien trang duoc khong, kem cursor cua node dau/cuoi trong danh sach hien tai — mo hinh giong "PageInfo" trong GraphQL Relay cursor connection.

**Input hop le** - Khong ap dung (day la DTO output, khong phai input can validate).

**Output (y nghia tung field, theo XML doc trong source)**

| Property | Kieu | Y nghia |
|---|---|---|
| `HasPreviousPage` | `bool` | Cho biet lieu con ket qua o trang truoc trang hien tai hay khong; giup xac dinh co the lui ve trang truoc duoc khong (`PagingModel.cs:36-38`). |
| `HasNextPage` | `bool` | Cho biet lieu con ket qua o trang sau trang hien tai hay khong; giup xac dinh co the chuyen sang trang tiep theo duoc khong (`PagingModel.cs:43-45`). |
| `StartCursor` | `string` | Cursor cua node DAU TIEN trong danh sach nodes hien tai; dung de truy van cac trang truoc do (`PagingModel.cs:50-52`). |
| `EndCursor` | `string` | Cursor cua node CUOI CUNG trong danh sach nodes hien tai; dung de truy van cac trang tiep theo (`PagingModel.cs:57-59`). |

**Dieu kien xu ly/Side effect/Error handling** - Khong ap dung (khong co logic trong file nay tinh cac gia tri tren; logic tinh, neu co, nam ngoai 4 file duoc doc).

**Khi nao NEN dung** - Lam kieu tra ve kem theo danh sach ket qua khi API dung phan trang kieu cursor.

**Khi nao KHONG dung** - Khong dung cho phan trang theo so trang (dung tong so trang/tong so ban ghi thay vi cursor).

**Gioi han** - Toan bo 4 property deu chi la "cho chua" — noi nao trong he thong thuc su GAN gia tri cho `StartCursor`/`EndCursor` (co dung `CursorPayloadModel.Encode()` hay khong) **khong xac dinh duoc tu source code trong repo nay**.

### 2.7 HealthCheckModel

**Signature**
```csharp
public record HealthCheckModel
{
    public string Status { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public IEnumerable<IndividualHealthCheckResponse> Checks { get; set; } = [];
}
```
**Muc dich** - DTO tong hop, du kien de bieu dien lai ket qua cua `Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport` (tong trang thai, tong thoi gian, danh sach chi tiet) theo dinh dang tuy bien cua he thong (thay vi tra thang `HealthReport` cua .NET).

**Input hop le** - Khong ap dung (data holder).

**Output (y nghia tung field, suy tu ten + kieu, khong co comment trong code)**

| Property | Kieu | Y nghia |
|---|---|---|
| `Status` | `string` | Trang thai tong the cua he thong (theo ten field va kieu `string` — KHAC voi `IndividualHealthCheckResponse.Status` la enum `HealthStatus`; **khong xac dinh duoc tu source code** gia tri hop le la gi, vi du co phai la `HealthStatus.ToString()` hay mot chuoi tuy bien khac). |
| `TotalDuration` | `TimeSpan` | Tong thoi gian thuc thi toan bo cac health check. |
| `Checks` | `IEnumerable<IndividualHealthCheckResponse>` | Danh sach ket qua chi tiet cua tung health check; mac dinh la mang rong `[]` (cu phap collection expression, `HealthCheckModel.cs:11`) — khong bao gio `null` khi chua duoc gan. |

**Dieu kien xu ly/Side effect/Error handling** - Khong ap dung (khong co logic trong file).

**Khi nao NEN dung** - Khi endpoint health-check cua service can tra ve JSON tuy chinh (thay vi dinh dang mac dinh cua ASP.NET Health Checks middleware).

**Khi nao KHONG dung** - Khong the dung truc tiep nhu tham so cho cac API cua `Microsoft.Extensions.Diagnostics.HealthChecks` (vi du `IHealthCheck.CheckHealthAsync` tra ve `HealthCheckResult`, khong phai kieu nay) — day chi la DTO output cuoi, can co lop mapping rieng (khong co trong 4 file duoc doc).

**Gioi han**
- `Checks` dung cu phap `= []` (collection expression) — yeu cau C# 12 (project dung `net9.0`, phu hop).
- Khong tim thay noi nao trong repo `sr-core-helper` thuc su khoi tao/tra ve `HealthCheckModel` (vi du trong mot health-check endpoint hay `Program.cs`) — **muc dich su dung thuc te khong xac dinh duoc tu source code trong repo nay**.

### 2.8 IndividualHealthCheckResponse

**Signature**
```csharp
public record IndividualHealthCheckResponse
{
    public string Name { get; set; }
    public HealthStatus Status { get; set; }
    public TimeSpan Duration { get; set; }
    public Exception Exception { get; set; }
}
```
**Muc dich** - DTO cho ket qua cua MOT health check don le, nam trong `HealthCheckModel.Checks`.

**Output (y nghia tung field)**

| Property | Kieu | Y nghia |
|---|---|---|
| `Name` | `string` | Ten cua health check (thuong khop voi ten dang ky trong `AddHealthChecks().AddCheck(name, ...)` cua ASP.NET, theo quy uoc chung cua .NET — khong co code trong repo nay xac nhan dieu do). |
| `Status` | `HealthStatus` (enum cua `Microsoft.Extensions.Diagnostics.HealthChecks`) | Trang thai cua check nay: `Healthy`/`Degraded`/`Unhealthy` (gia tri chuan cua enum goc, khong bi mo rong/thu hep trong file nay). |
| `Duration` | `TimeSpan` | Thoi gian thuc thi cua check nay. |
| `Exception` | `Exception` | Exception (neu co) phat sinh khi thuc thi check nay. |

**Gioi han (quan trong)** - `Exception` co kieu `System.Exception` — day la kieu KHONG duoc thiet ke de serialize JSON an toan (co the chua `StackTrace`, thong tin noi bo, hoac gay loi/circular khi serialize tuy theo serializer va cau hinh dung). Trong 4 file duoc doc **khong co bang chung nao** cho thay `HealthCheckModel`/`IndividualHealthCheckResponse` duoc serialize bang `ToJSon()` (voi `ReferenceHandler.IgnoreCycles` nhu `CursorPayloadModel` dung) hay bang serializer nao khac (vi du serializer JSON mac dinh cua ASP.NET Minimal API/Controller) — neu duoc tra ve truc tiep tu mot API endpoint bang serializer mac dinh, viec expose nguyen ven `Exception` (stack trace) ra response la rui ro ro lo thong tin, nhung **khong xac dinh duoc co xay ra trong thuc te hay khong tu source code trong repo nay**.

### 2.9 GetAllEmployeeByFilterResponse

**Signature**
```csharp
public record GetAllEmployeeByFilterResponse
{
    public int? Id { get; set; }
    public string Code { get; set; }
    public string UserName { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string OrganizationCode { get; set; }
    public string OrganizationName { get; set; }
    public string PositionCode { get; set; }
    public string PositionName { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; }
    public int? EmployeeRank { get; set; }
    public DateTime? EmployeeBirthday { get; set; }
    public string EmployeePhone { get; set; }
    public DateTime? CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public int? Gender { get; set; }
    public string GenderName { get; set; }
    public string TitleCode { get; set; }
    public string TitleName { get; set; }
    public string Address { get; set; }
    public string Identification { get; set; }
    public string InsideUserName { get; set; }
    public long? InsideUserId { get; set; }
    public string InsideEmail { get; set; }
    public int? InsideBranchId { get; set; }
    public string InsideBranchName { get; set; }
    public int? InsideLocationId { get; set; }
    public string InsideLocationName { get; set; }
    public int? InsideRegionId { get; set; }
    public string InsideRegionName { get; set; }
    public string ManagerEmployee { get; set; }
    public int TotalRows { get; set; }
    public bool? Sex { get; set; }
    public DateTime? BirthDay { get; set; }
    public string Phone { get; set; }
}
```
**Muc dich** - DTO phang (flat) mo ta MOT nhan vien, dung khi tra cuu danh sach nhan vien theo filter (theo ten record). Duoc tieu thu that su tai `SetAudit(AuditModel, GetAllEmployeeByFilterResponse, string, CancellationToken)` trong `FTELSRCore.Shared/Audits/UserAudit.cs:202-236` de gan thong tin nguoi tao (`CreatorInfo`) — xem file KB `Audits-UserAudit.md` muc 2.5.

**Input hop le** - Khong ap dung (day la DTO tra ve/output, khong phai input can validate trong file nay).

**Output — nhom field theo ngu nghia (suy tu ten, khong co comment trong code)**

| Nhom | Property | Kieu | Ghi chu |
|---|---|---|---|
| Dinh danh | `Id`, `Code`, `UserName`, `FullName`, `Email` | `int?`, `string`, `string`, `string`, `string` | Thong tin co ban cua nhan vien. |
| Don vi/Chuc danh | `OrganizationCode`, `OrganizationName`, `PositionCode`, `PositionName`, `TitleCode`, `TitleName` | `string` | Ma + ten hien thi di kem theo cap (pattern lap lai trong toan DTO). |
| Trang thai | `Status`, `StatusName` | `int`, `string` | Ma trang thai + ten hien thi. |
| Ca nhan | `EmployeeRank`, `EmployeeBirthday`, `EmployeePhone`, `Gender`, `GenderName`, `Address`, `Identification` | kieu tuong ung | Thong tin ca nhan mo rong. |
| Thoi gian | `CreateDate`, `UpdateDate` | `DateTime?` | Ngay tao/cap nhat ban ghi nhan vien. |
| He thong "Inside" (he thong ngoai/tich hop) | `InsideUserName`, `InsideUserId`, `InsideEmail`, `InsideBranchId`, `InsideBranchName`, `InsideLocationId`, `InsideLocationName`, `InsideRegionId`, `InsideRegionName` | kieu tuong ung | Nhom field tien to `Inside` — theo ten, day la thong tin doi chieu/lay tu mot he thong khac (vi du he thong chi nhanh/dia diem noi bo). Y nghia chinh xac cua "Inside" **khong xac dinh duoc tu source code trong repo nay** (khong co comment giai thich). |
| Quan ly | `ManagerEmployee` | `string` | Thong tin nguoi quan ly (dang la `string` don, khong phai object/ID rieng). |
| Phan trang | `TotalRows` | `int` | Tong so dong ket qua — su co mat field nay TRONG MOT DTO tung nhan vien goi y day la ket qua tra ve dang "flatten" tu mot truy van phang (vi du Dapper/raw SQL) kem tong so dong tren MOI dong, thay vi wrapper phan trang rieng; **khong co truy van nao trong 4 file duoc doc de xac nhan dieu nay**. |
| Truong trung lap (xem muc 3) | `Sex`, `BirthDay`, `Phone` | `bool?`, `DateTime?`, `string` | Trung ngu nghia voi `Gender`/`EmployeeBirthday`/`EmployeePhone` da co trong cung DTO — xem "Van de da biet" #7. |

**Dieu kien xu ly/Side effect/Error handling** - Khong ap dung (khong co method/logic trong file, chi la property).

**Khi nao NEN dung** - Khi can nhan/tra ket qua tra cuu 1 hoac nhieu nhan vien theo dieu kien loc, hoac khi can nguon du lieu de gan `AuditModel.CreatorInfo` (qua `SetAudit`).

**Khi nao KHONG dung** - Khong dung nhu entity luu DB (day la Response DTO, khong phai entity — khong co attribute mapping ORM nao trong file).

**Gioi han** - Xem chi tiet trong muc 3 (#6, #7): nullable khong dong bo (`Status: int` khong nullable nhung `Gender: int?`, `EmployeeRank: int?` — khong ro tieu chi vi sao mot so field bat buoc con so lai tuy chon), va cac cap field trung lap ve ngu nghia.

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | Khong co bang chung code (trong 4 file duoc doc) lien ket truc tiep `CursorPayloadModel.Encode()`/`Decode()` voi `PagingCursorModel.Cursor` hoac `PageInfoCursorResponseModel.StartCursor`/`EndCursor` — day chi la suy luan hop ly tu ten va kieu du lieu (deu la `string`), khong phai khang dinh da xac minh tu code. | `CursorPayloadModel.cs`, `PagingModel.cs:26-62` | Nguoi doc/AI agent co the hieu nham day la lien ket cung, trong khi thuc te co the co lop trung gian khac (ngoai pham vi 4 file) hoac hoan toan khong lien quan. |
| 2 | `CursorPayloadModel.Decode()` dung `catch` khong khai bao kieu (`catch { return null; }`), bat TAT CA exception (bao gom loi he thong khong lien quan den du lieu client) va khong log gi ca — khac voi `JSonTryParse` noi bo (co the log qua console/`ILogger` khi duoc truyen). | `CursorPayloadModel.cs:43-46` | Kho phan biet "cursor sai dinh dang do client" voi "loi bug/he thong thuc su" khi debug production — ca hai deu tra ve `null` va khong de lai vet log tu chinh `Decode()`. |
| 3 | `CursorPayloadModel.Decode()` discard gia tri `bool` tra ve cua `JSonTryParse` (`_ = json.JSonTryParse(...)`). | `CursorPayloadModel.cs:39` | Khong phai bug (vi `CursorPayloadModel` la `class` nen `default` = `null`, ket qua cuoi van dung), nhung la code-smell de gay hieu nham khi doc — co the tuong nham la loi khong kiem tra ket qua parse. |
| 4 | `PagingCursorModel.PageSize` mac dinh la `20` trong khi `Paging.PageSize` (dung cho `PagingModel`/`PagingMongoDBModel`, cung nam trong `PagingModel.cs`) mac dinh la `10`. | `PagingModel.cs:23,30` | Hai co che phan trang trong CUNG mot file khong dong bo gia tri mac dinh — neu mot service dung ca hai loai phan trang, hanh vi "trang mac dinh" se khac nhau ma khong co ly do nghiep vu duoc ghi lai trong code. |
| 5 | `Paging`, `PagingModel`, `PagingMongoDBModel` la `abstract` nhung khong co class/record nao trong repo `sr-core-helper` ke thua chung; tuong tu, `CursorPayloadModel`, `PagingCursorModel`, `PageInfoCursorResponseModel`, `HealthCheckModel`, `IndividualHealthCheckResponse` khong duoc khoi tao/tham chieu o dau khac trong repo ngoai file dinh nghia. | `PagingModel.cs` (toan file), `HealthCheckModel.cs` (toan file), `CursorPayloadModel.cs` (toan file) | Khong xac nhan duoc tu source code trong repo nay rang cac kieu nay dang duoc su dung thuc te (co the chi duoc tieu thu boi cac service khac ben ngoai repo, do day la thu vien Shared) — moi mo ta ve "cach dung" trong tai lieu nay dua tren suy luan ten/kieu du lieu, khong phai bang chung loi goi thuc te. |
| 6 | `IndividualHealthCheckResponse.Exception` co kieu `System.Exception` — kieu nay khong duoc thiet ke de serialize JSON an toan (co the lo `StackTrace` neu bi tra thang qua API bang serializer mac dinh). | `HealthCheckModel.cs:22` | Rui ro ro lo thong tin noi bo neu `HealthCheckModel` duoc tra ve nguyen ven qua HTTP response ma khong co buoc anh xa/loc lai; **khong xac dinh duoc co xay ra trong thuc te khong** vi khong tim thay endpoint nao dung kieu nay trong repo. |
| 7 | `GetAllEmployeeByFilterResponse` co cac cap field trung ngu nghia: `Gender` (`int?`) vs `Sex` (`bool?`); `EmployeeBirthday` (`DateTime?`) vs `BirthDay` (`DateTime?`); `EmployeePhone` (`string`) vs `Phone` (`string`). | `GetAllEmployeeByFilterResponse.cs:24,26,31,58,60,62` | Khong ro field nao la "nguon dung" khi ca 2 cung duoc gan gia tri khac nhau — nguy co doc sai du lieu (vi du code doc `Sex` nhung nguon du lieu chi gan `Gender`). Khong co comment nao trong file giai thich ly do ton tai song song; **nguyen nhan (hop nhat 2 nguon du lieu khac nhau, hay field cu/moi) khong xac dinh duoc tu source code trong repo nay**. |
| 8 | `GetAllEmployeeByFilterResponse.TotalRows` (kieu `int`, khong nullable) nam lan trong cac field cap-do-tung-nhan-vien. | `GetAllEmployeeByFilterResponse.cs:56` | Goi y DTO nay duoc dung cho ket qua truy van "flatten" (tong so dong nhan ban tren moi dong) thay vi wrapper phan trang rieng — can luu y khi doc/ghi log de tranh nham `TotalRows` la thuoc tinh rieng cua nhan vien. |
