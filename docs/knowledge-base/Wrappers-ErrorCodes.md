# Wrappers - ErrorCodes catalog & mapper

> Nguon: FTELSRCore.Shared/Wrappers/ErrorCodes/Catalogs/CatalogsErorrCode.cs, FTELSRCore.Shared/Wrappers/ErrorCodes/Catalogs/Systems/CatalogsErorrCodeForSystem.cs, FTELSRCore.Shared/Wrappers/ErrorCodes/ResponseWrapperByCodeMapper.cs
> Loai: static class (danh muc hang so) + class (logic mapping)
> Cap nhat theo commit: 89c1ce9

## 1. Tong quan

Module nay dinh nghia danh muc ma loi noi bo (`CatalogsErrorCodes`), bang tra cuu theo cap
`(HTTP status code, nguon loi)` (`CatalogsErrorCode.StatusMap`), va lop tra cuu cong khai
(`ResponseWrapperByCodeMapper`) dung de chuyen mot `HttpStatusCode` + `ErrorSourceType` thanh mot
`CatalogsErrorCodeModel` (Code/Message/Description/Retryable) dung nhat quan trong toan he thong.
Module nam o tang `Wrappers` cua `FTELSRCore.Shared`, duoc `Result`/`Result<T>` (`FTELSRCore.Shared/Wrappers/Result.cs`)
va middleware xu ly exception/JWT dung de dien truong `Error.Code` va `Error.Retryable` trong response
tra ve client.

Namespace thuc te trong code la `FTELSRCore.Wrappers.ErrorCodes...` (khong co doan `Shared` trong
namespace, du duong dan thu muc vat ly la `FTELSRCore.Shared/Wrappers/ErrorCodes/...`).
(CatalogsErorrCode.cs:3, CatalogsErorrCodeForSystem.cs:1, ResponseWrapperByCodeMapper.cs:4)

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Cung cap 11 ma loi dung san (`CatalogsErrorCodes.*`), moi ma gom Code/Message (tieng Viet)/Description (tieng Anh)/Retryable (CatalogsErorrCodeForSystem.cs:9-106) | Khong tu sinh ma loi cho status code/source khong co trong `StatusMap` — chi fallback ve mot model tu tao voi Code=`SYS_{TenEnum}` (ResponseWrapperByCodeMapper.cs:16-28) |
| Tra ve dung mot `CatalogsErrorCodeModel` khi cap `(statusCode, sourceType)` khop voi mot key trong `StatusMap` (CatalogsErorrCode.cs:7-46; ResponseWrapperByCodeMapper.cs:11-13) | Khong validate ArgumentNull hay throw loi tuong minh — `FromStatusCode` khong bao gio throw, luon co gia tri tra ve (khong xac dinh duoc case nao gay exception tu chinh ham nay) |
| Cho phep goi voi `sourceType` mac dinh la `ErrorSourceType.General` neu caller khong truyen (ResponseWrapperByCodeMapper.cs:9) | Khong su dung truong `Message`/`Description` trong bat ky diem goi hien co trong repo — cac noi goi (`Result.cs`, `ExceptionHandlerMiddleWare.cs`, `JWTBearerExtensions.cs`) chi doc `Code` va `Retryable` tu ket qua tra ve (Result.cs:39-56, 187-195; ExceptionHandlerMiddleWare.cs:85-92; JWTBearerExtensions.cs:74-77) |
| Ho tro 8 gia tri `ErrorSourceType` (General, Authentication, Database, Cache, MessageQueue, ExternalService, Network, Storage) (CatalogsErorrCode.cs:56-66) | 3/8 gia tri enum (`Cache`, `MessageQueue`, `Storage`) khong duoc dung trong bat ky entry nao cua `StatusMap` va khong xuat hien o noi nao khac trong repo (xac nhan bang grep toan repo, xem muc 3) |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `System.Net.HttpStatusCode` | Kieu tham so dau vao cua `FromStatusCode`, va duoc ep sang `int` de tra `StatusMap` (ResponseWrapperByCodeMapper.cs:2, 9, 12) |
| `FTELSRCore.Wrappers.ErrorCodes.Catalogs.Systems.CatalogsErrorCodes` | Nguon cung cap 11 hang so `CatalogsErrorCodeModel` duoc `CatalogsErrorCode.StatusMap` tham chieu toi (CatalogsErorrCode.cs:1, 14-45) |
| `FTELSRCore.Helpers.ConvertHelpers.ConvertEnum<TEnum>` | Duoc `FromStatusCodeDefault` goi de parse lai chinh `statusCode.ToString()` ve `HttpStatusCode?` (ResponseWrapperByCodeMapper.cs:19; ConvertHelpers.cs:149-152) |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `CatalogsErrorCode.StatusMap` | Constant/Dictionary | Bang tra cuu tinh, 11 entry, key la tuple `(int StatusCode, ErrorSourceType Source)` |
| `CatalogsErrorCodeModel` (record) | Model | Kieu du lieu mo ta mot ma loi: Code, Message, Description, Retryable |
| `ErrorSourceType` (enum) | Enum | Phan loai nguon phat sinh loi, dung lam thanh phan key cua `StatusMap` |
| `CatalogsErrorCodes.*` (11 hang so) | Constant | Danh sach ma loi cu the, xem bang chi tiet muc 2 |
| `ResponseWrapperByCodeMapper.FromStatusCode(HttpStatusCode, ErrorSourceType)` | Method (public static) | Tra ve `CatalogsErrorCodeModel` tuong ung, co fallback khi khong khop |
| `ResponseWrapperByCodeMapper.FromStatusCodeDefault(HttpStatusCode)` (private) | Method (private static) | Sinh `CatalogsErrorCodeModel` fallback khi `StatusMap` khong co entry khop |

## 2. Danh muc hang so - CatalogsErrorCodeModel (CatalogsErorrCodeForSystem.cs)

> Kieu: static class chi chua hang so (`public static readonly CatalogsErrorCodeModel`). Ap dung template rut gon.

| Ten hang so | Code | Message (tieng Viet) | Description (tieng Anh) | Retryable | Dong code |
|---|---|---|---|---|---|
| `BadRequest` | `BUSINESS_RULE_400` | Yêu cầu không hợp lệ | Invalid request payload or missing fields | `false` | CatalogsErorrCodeForSystem.cs:9-14 |
| `RequestTimeout` | `REQ_TIMEOUT` | Yêu cầu xử lý quá lâu | Request timeout exceeded | `true` | CatalogsErorrCodeForSystem.cs:16-22 |
| `RateLimit` | `RATE_429` | Gửi quá nhiều yêu cầu được gủi đến | Rate limit exceeded | `true` | CatalogsErorrCodeForSystem.cs:24-30 |
| `SystemError` | `SYS_500` | Lỗi hệ thống | Unhandled internal server exception | `false` | CatalogsErorrCodeForSystem.cs:32-37 |
| `Unauthorized` | `AUTH_401` | Chưa xác thực hoặc thiếu token | Missing or invalid authentication token | `false` | CatalogsErorrCodeForSystem.cs:43-48 |
| `Forbidden` | `AUTH_403` | Không có quyền truy cập | Permission denied | `false` | CatalogsErorrCodeForSystem.cs:50-55 |
| `UpgradeRequired` | `UPGRADE_REQUIRED` | Cần nâng cấp phiên bản client | Unsupported client version | `false` | CatalogsErorrCodeForSystem.cs:57-62 |
| `DatabaseError` | `DB_500` | Lỗi kết nối hoặc xử lý database | Database execution failure | `true` | CatalogsErorrCodeForSystem.cs:68-74 |
| `DatabaseUnavailable` | `DB_503` | Database quá tải hoặc không sẵn sàng | Database unavailable or overloaded | `true` | CatalogsErorrCodeForSystem.cs:76-82 |
| `NetworkError` | `NET_502` | Lỗi kết nối mạng nội bộ | Internal network communication failure | `true` | CatalogsErorrCodeForSystem.cs:88-94 |
| `ExternalTimeout` | `EXT_504` | Timeout khi gọi hệ thống bên ngoài | External dependency timeout | `true` | CatalogsErorrCodeForSystem.cs:100-106 |

Ghi chu ve `CatalogsErrorCodeModel` (record, khai bao trong CatalogsErorrCode.cs:49-54):

```csharp
public sealed record CatalogsErrorCodeModel(
    string Code,
    string Message,
    string Description = null,
    bool Retryable = false
);
```

Tham chieu noi dung (grep nhanh trong repo): moi hang so `CatalogsErrorCodes.*` chi duoc doc mot lan
duy nhat, boi `CatalogsErrorCode.StatusMap` (CatalogsErorrCode.cs:14-45) - khong co diem goi truc tiep
`CatalogsErrorCodes.BadRequest`, ... o ben ngoai file nay (xac nhan bang `grep -rn "CatalogsErrorCodes"`
tren toan repo, ke ca `.claude/worktrees`).

## 3. Danh muc hang so - ErrorSourceType & StatusMap (CatalogsErorrCode.cs)

### 3.1 Enum `ErrorSourceType` (CatalogsErorrCode.cs:56-66)

| Gia tri enum | Gia tri thuc (int) | Y nghia (theo context su dung) | Noi dung tham chieu |
|---|---|---|---|
| `General` | 0 | Loi chung, khong xac dinh nguon cu the; dung lam default param cua `FromStatusCode` | Dung trong `StatusMap` cho 400/408/429/500 (CatalogsErorrCode.cs:14,23,29,32); dung lam default cho `FromStatusCode` (ResponseWrapperByCodeMapper.cs:9); goi tu `Result.cs` (2 vi tri) va `ExceptionHandlerMiddleWare.cs` (1 vi tri) |
| `Authentication` | 1 | Loi lien quan xac thuc/phan quyen | Dung trong `StatusMap` cho 401/403/426 (CatalogsErorrCode.cs:17,20,26); goi tu `JWTBearerExtensions.cs` (4 vi tri) |
| `Database` | 2 | Loi tu tang database | Dung trong `StatusMap` cho 500/503 (CatalogsErorrCode.cs:35,41) |
| `Cache` | 3 | Khong xac dinh duoc tu source code (khong co comment, khong xuat hien trong `StatusMap` hay bat ky noi goi nao trong repo) | Khong tim thay tham chieu nao (grep toan repo, `.claude/worktrees` da loai tru) |
| `MessageQueue` | 4 | Khong xac dinh duoc tu source code (tuong tu `Cache`) | Khong tim thay tham chieu nao |
| `ExternalService` | 5 | Loi khi goi dich vu/he thong ben ngoai | Dung trong `StatusMap` cho 504 (CatalogsErorrCode.cs:44) |
| `Network` | 6 | Loi ha tang mang | Dung trong `StatusMap` cho 502 (CatalogsErorrCode.cs:38) |
| `Storage` | 7 | Khong xac dinh duoc tu source code (tuong tu `Cache`) | Khong tim thay tham chieu nao |

### 3.2 Bang `CatalogsErrorCode.StatusMap` (CatalogsErorrCode.cs:7-46)

Kieu: `IReadOnlyDictionary<(int StatusCode, ErrorSourceType Source), CatalogsErrorCodeModel>`, 11 entry co dinh (khoi tao mot lan, khong co API them/sua/xoa entry luc runtime).

| Key: (StatusCode, ErrorSourceType) | Gia tri tra ve (`CatalogsErrorCodes.*`) | Dong code |
|---|---|---|
| (400, `General`) | `BadRequest` | CatalogsErorrCode.cs:14-15 |
| (401, `Authentication`) | `Unauthorized` | CatalogsErorrCode.cs:17-18 |
| (403, `Authentication`) | `Forbidden` | CatalogsErorrCode.cs:20-21 |
| (408, `General`) | `RequestTimeout` | CatalogsErorrCode.cs:23-24 |
| (426, `Authentication`) | `UpgradeRequired` | CatalogsErorrCode.cs:26-27 |
| (429, `General`) | `RateLimit` | CatalogsErorrCode.cs:29-30 |
| (500, `General`) | `SystemError` | CatalogsErorrCode.cs:32-33 |
| (500, `Database`) | `DatabaseError` | CatalogsErorrCode.cs:35-36 |
| (502, `Network`) | `NetworkError` | CatalogsErorrCode.cs:38-39 |
| (503, `Database`) | `DatabaseUnavailable` | CatalogsErorrCode.cs:41-42 |
| (504, `ExternalService`) | `ExternalTimeout` | CatalogsErorrCode.cs:44-45 |

Luu y: cung mot status code co the anh xa toi 2 model khac nhau tuy `ErrorSourceType` truyen vao - vi
du 500+`General` -> `SystemError`, nhung 500+`Database` -> `DatabaseError` (CatalogsErorrCode.cs:32-36).
Neu goi `FromStatusCode(HttpStatusCode.InternalServerError)` ma khong truyen `sourceType`, ket qua se
la `SystemError` (vi tham so mac dinh la `General`), khong bao gio tu dong ra `DatabaseError` tru khi
caller chu dong truyen `ErrorSourceType.Database`.

## 4. Chi tiet API - ResponseWrapperByCodeMapper (ResponseWrapperByCodeMapper.cs)

> Kieu: class (khong sealed, khong static, nhung khong co field instance va khong co constructor
> tuong minh - chi co 2 static method). Khong co XML doc `/// <summary>` cho ca 2 method.

### 4.1 FromStatusCode

**Signature**
```csharp
public static CatalogsErrorCodeModel FromStatusCode(
     HttpStatusCode statusCode, ErrorSourceType sourceType = ErrorSourceType.General)
```

**Muc dich** - Tra ve mot `CatalogsErrorCodeModel` (Code/Message/Description/Retryable) ung voi cap
`(statusCode, sourceType)`, uu tien tra cuu trong `CatalogsErrorCode.StatusMap`; neu khong co entry
khop thi tu sinh mot model fallback (khong con chua ma tieng Viet/mo ta co san).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `statusCode` | `System.Net.HttpStatusCode` | Co | Khong co validate tuong minh; duoc ep sang `int` de tra bang (ResponseWrapperByCodeMapper.cs:12) - moi gia tri `HttpStatusCode` hop le (bao gom ca gia tri ep tu `int` khong nam trong enum, vi day la enum `int`-backed khong co `[Flags]` han che) deu duoc chap nhan | Khong co |
| `sourceType` | `ErrorSourceType` | Khong (co default) | Khong validate; dung truc tiep lam thanh phan key tra bang | `ErrorSourceType.General` |

**Output** - `CatalogsErrorCodeModel` (khong bao gio null):
- Neu `(int)statusCode, sourceType)` khop mot key trong `StatusMap`: tra ve dung instance hang so
  tuong ung trong `CatalogsErrorCodes` (vi du `BadRequest`, `Unauthorized`, ...).
- Neu khong khop: tra ve ket qua cua `FromStatusCodeDefault(statusCode)` - mot model moi duoc tao
  runtime, khong nam trong danh sach 11 hang so co san.

**Dieu kien xu ly** (theo thu tu thuc thi, ResponseWrapperByCodeMapper.cs:11-13):
1. Goi `CatalogsErrorCode.StatusMap.TryGetValue(((int)statusCode, sourceType), out errorCode)`.
2. Neu `TryGetValue` tra `true` -> tra ve `errorCode` (gia tri tim duoc trong bang).
3. Neu `TryGetValue` tra `false` -> tra ve `FromStatusCodeDefault(statusCode: statusCode)`.

**Side effect** - Khong co (ham thuan, khong ghi log, khong goi ngoai, khong mutate tham so dau vao;
`StatusMap` la `readonly` nen khong bi thay doi qua loi goi nay).

**Error handling** - Khong co try/catch, khong throw loi tuong minh trong ham nay. Vi `TryGetValue`
khong throw va nhanh else luon tra ve gia tri hop le tu `FromStatusCodeDefault`, ham nay khong xac
dinh duoc truong hop nao tu than no gay exception (rui ro exception, neu co, nam trong
`ConvertHelpers.ConvertEnum` duoc goi giup - xem muc 4.2).

**Khi nao NEN dung** - Khi can chuan hoa mot `HttpStatusCode` (co hoac khong kem `ErrorSourceType`)
thanh mot ma loi noi bo dong nhat (Code) de tra ve client, dac biet trong middleware xu ly exception
hoac su kien xac thuc JWT (da duoc dung trong `ExceptionHandlerMiddleWare.cs:85-87` va
`JWTBearerExtensions.cs:74-77, 102-105, 146-149, 175-178`).

**Khi nao KHONG dung** - Khi can lay noi dung `Message`/`Description` da duoc dia phuong hoa (tieng
Viet) cho nguoi dung cuoi trong truong hop fallback: `FromStatusCodeDefault` chi tra ve ten enum
`HttpStatusCode` (tieng Anh, vi du "NotFound") lam ca `Message` va `Description`, khong co ban dich
tieng Viet nhu cac hang so trong `CatalogsErrorCodes` (xem muc 4.2).

**Gioi han**
- Danh sach status code duoc "cong nhan" chinh thuc chi gom 11 cap `(status, source)` co dinh trong
  `StatusMap` (CatalogsErorrCode.cs:14-45); moi to hop khac (vi du 404+`General`, hoac 401+`General`
  thay vi 401+`Authentication`) deu roi vao nhanh fallback.
- Tham so mac dinh `sourceType = ErrorSourceType.General` co nghia: neu caller quen truyen `sourceType`
  cho mot status ma trong `StatusMap` chi duoc dinh nghia voi source khac `General` (vi du 401/403/426
  chi co ban ghi `Authentication`, hoac 502/503/504 chi co ban ghi `Network`/`Database`/`ExternalService`
  tuong ung, khong co ban ghi `General`), ket qua se la fallback thay vi dung ma loi cu the da dinh
  nghia san. Rieng 500 KHONG thuoc nhom nay vi `StatusMap` co san ca hai entry `(500, General)` ->
  `SystemError` va `(500, Database)` -> `DatabaseError` (CatalogsErorrCode.cs:32-36) - goi
  `FromStatusCode(HttpStatusCode.InternalServerError)` khong truyen `sourceType` van khop entry
  `(500, General)` va tra ve `SystemError` (mot ma loi dinh nghia san), khong roi vao fallback.

### 4.2 FromStatusCodeDefault (private)

**Signature**
```csharp
private static CatalogsErrorCodeModel FromStatusCodeDefault(HttpStatusCode statusCode)
```

**Muc dich** - Sinh mot `CatalogsErrorCodeModel` fallback khi `FromStatusCode` khong tim duoc entry
khop trong `StatusMap`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `statusCode` | `System.Net.HttpStatusCode` | Co | Khong validate | Khong co |

**Output** - Luon tra ve mot `CatalogsErrorCodeModel` moi (khong bao gio null), voi:
- `Code` = `"SYS_" + statusCodeConvertEnum.ToString()` (vi du `SYS_NotFound`).
- `Description` = `statusCodeConvertEnum.ToString()`.
- `Message` = `statusCodeConvertEnum.ToString()`.
- `Retryable` = `false` (hardcode, khong phu thuoc statusCode).

**Dieu kien xu ly** (ResponseWrapperByCodeMapper.cs:18-27):
1. Goi `ConvertHelpers.ConvertEnum<HttpStatusCode>(statusCode.ToString())` - chuyen `statusCode` thanh
   chuoi (vi du "NotFound") roi PARSE LAI chuoi do ve `HttpStatusCode?` bang `Enum.TryParse`
   (ConvertHelpers.cs:149-152).
2. Tra ve `CatalogsErrorCodeModel` moi voi ca 3 truong Code/Description/Message deu lay tu
   `statusCodeConvertEnum.ToString()`.

**Side effect** - Khong co.

**Error handling** - Khong co try/catch trong ham nay. `ConvertHelpers.ConvertEnum` noi bo dung
`Enum.TryParse` (khong throw), tra `null` neu parse thất bai; neu `statusCodeConvertEnum` la `null`
(kieu `HttpStatusCode?`), `.ToString()` tren mot `Nullable<T>` co gia tri null tra ve chuoi rong
(khong throw NullReferenceException) - vi vay ham nay ve ly thuyet khong nem exception, nhung neu roi
vao nhanh null se tao ra `Code = "SYS_"` va `Message`/`Description` la chuoi rong.

**Khi nao NEN dung** - Chi duoc goi noi bo boi `FromStatusCode` khi khong co entry khop trong
`StatusMap`; khong duoc thiet ke de goi truc tiep tu ngoai class (private).

**Khi nao KHONG dung** - Khong ap dung (khong the goi tu ngoai vi la `private`).

**Gioi han**
- **Van mau thuan quan trong (Van de #1 o muc 5):** logic cua ham nay thuc chat la
  `statusCode.ToString() -> parse lai chinh no -> ToString()`, tuc la mot phep round-trip du thua:
  vi `statusCode` da la mot `HttpStatusCode` hop le, `ConvertEnum<HttpStatusCode>(statusCode.ToString())`
  hau het cac truong hop se cho ra dung lai `statusCode` ban dau (khong xac dinh duoc tu source code
  ly do thiet ke tai sao can buoc round-trip nay thay vi dung truc tiep `statusCode.ToString()`).
- Khong co ban dich tieng Viet cho `Message` trong nhanh fallback (chi co ten enum tieng Anh), khac voi
  toan bo 11 hang so trong `CatalogsErrorCodes` deu co `Message` tieng Viet.
- `Code` fallback co tien to co dinh `SYS_` bat ke `ErrorSourceType` truyen vao la gi (tham so
  `sourceType` cua `FromStatusCode` khong duoc truyen xuong `FromStatusCodeDefault` - chi dung de
  tra `StatusMap`, hoan toan khong anh huong den noi dung fallback).

## 5. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `FromStatusCodeDefault` thuc hien round-trip du thua: `statusCode.ToString()` roi parse nguoc lai bang `ConvertHelpers.ConvertEnum<HttpStatusCode>`, ket qua tren thuc te tuong duong voi dung truc tiep `statusCode.ToString()`. Khong xac dinh duoc tu source code ly do thiet ke. | ResponseWrapperByCodeMapper.cs:18-27 | Code du thua, kho doc; khong gay sai lech ket qua trong dieu kien binh thuong nhung tang chi phi bao tri/hieu suat khong can thiet. |
| 2 | 3/8 gia tri enum `ErrorSourceType` (`Cache`, `MessageQueue`, `Storage`) khong duoc `StatusMap` su dung va khong xuat hien trong bat ky file `.cs` nao khac trong repo (xac nhan bang grep toan repo, gom ca thu muc `.claude/worktrees`). | CatalogsErorrCode.cs:61,62,65 | Enum co ve duoc chuan bi cho nhu cau mo rong trong tuong lai nhung hien tai la dead code khong duoc dung; khong anh huong hanh vi runtime. |
| 3 | Truong `Message` va `Description` cua `CatalogsErrorCodeModel` duoc dinh nghia day du (bao gom ban dich tieng Viet) trong 11 hang so, nhung khong co diem goi nao trong repo hien tai doc 2 truong nay - `Result.cs`, `ExceptionHandlerMiddleWare.cs`, `JWTBearerExtensions.cs` chi doc `Code` va `Retryable`. | Result.cs:39-56,187-195; ExceptionHandlerMiddleWare.cs:85-92; JWTBearerExtensions.cs:74-77,102-105,146-149,175-178 | Cong suc dich tieng Viet trong catalog hien khong duoc tan dung boi code hien co trong pham vi da doc; khong ro co consumer khac ngoai pham vi 3 file nguon duoc giao hay khong (khong xac dinh duoc tu source code duoc doc). |
| 4 | Nhanh fallback (`FromStatusCodeDefault`) khong co ban dich tieng Viet cho `Message`, khac voi toan bo cac ma loi duoc dinh nghia san trong `CatalogsErrorCodes` (deu co `Message` tieng Viet). Neu client hien thi truc tiep `Message` cho nguoi dung cuoi (khong xac dinh duoc tu 3 file nguon duoc giao vi khong thay noi dung do voi `Message`), cac status code ngoai 11 cap da dinh nghia se hien thi ten enum tieng Anh thay vi tieng Viet. | ResponseWrapperByCodeMapper.cs:21-27 | Khong dong nhat ngon ngu giua ma loi "chinh thuc" va ma loi fallback. |
| 5 | Ten file va namespace chua loi chinh ta "Erorr" (dung "Error") - `CatalogsErorrCode.cs`, `CatalogsErorrCodeForSystem.cs`. Day la ten file/thu muc vat ly, khong anh huong bien dich vi ten class/namespace ben trong (`CatalogsErrorCode`, namespace `...ErrorCodes...`) danh van dung. | CatalogsErorrCode.cs (ten file); CatalogsErorrCodeForSystem.cs (ten file) | Chi anh huong kha nang tim kiem/doc hieu, khong anh huong hanh vi runtime. |
